using CommunityToolkit.Mvvm.Messaging;
using FieldReport.App.Navigation;
using FieldReport.App.Push;
using FieldReport.App.Pages;
using FieldReport.App.Services;
using FieldReport.App.Services.Localization;
using FieldReport.Core.Data;
using FieldReport.Core.Data.Api;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.Services.Push;
using FieldReport.Core.Services.Sync;
using Microsoft.Extensions.Http.Resilience;
using FieldReport.Core.ViewModels;
using FieldReport.Core.ViewModels.NewReport;
using FieldReport.Core.ViewModels.Playground;

namespace FieldReport.App;

// Rejestracje w metodach rozszerzających: MauiProgram zostaje krótki i czytelny.
// Czasy życia: Singleton = usługi, repozytorium, nawigacja (stan współdzielony);
// Transient = ViewModele i strony (nowa instancja przy każdym wejściu). Scoped w MAUI nie ma naturalnego scope'u.
public static class ServiceCollectionExtensions
{
    // IReportService, repozytorium, fabryki, formatery, IUserFeedback… (FieldReport.Core, bez MAUI)
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IReportRepository, SqliteReportRepository>(); // praca offline: dane w SQLite; inna implementacja repozytorium = zmiana tylko tej linii
        services.AddTransient<IReportService, ReportService>(); // transient: korzysta z typed HttpClient (IReportsApiClient)
        // Logowanie technika przez API. Transient, bo korzysta z typed HttpClient (HttpClient z fabryki nie trzymamy w singletonie).
        services.AddTransient<IAuthService, ApiAuthService>();
        services.AddTransient<IAccountService, AccountService>();
        services.AddSingleton<SessionExpiredHandler>(); // nieudane odświeżenie tokenu → logowanie

        // Offline i synchronizacja: wysyłka niewysłanych. SyncService transient (typed HttpClient), zlecenia w tle przez singleton SyncScheduler,
        // który dostaje fabrykę (Func) i tworzy nowy SyncService przy każdym zleceniu
        services.AddTransient<ISyncService, SyncService>();
        services.AddSingleton<Func<ISyncService>>(sp => () => sp.GetRequiredService<ISyncService>());
        services.AddSingleton<ISyncScheduler, SyncScheduler>();
        services.AddSingleton<ICredentialsValidator, CredentialsValidator>();
        services.AddSingleton<IDeepLinkRouter, DeepLinkRouter>();

        services.AddSingleton<IReportFormatter, ReportFormatter>();
        services.AddSingleton<IReportItemFactory, ReportItemFactory>();
        services.AddSingleton<IReportDetailsFactory, ReportDetailsFactory>();
        services.AddSingleton<IErrorMessageProvider, ErrorMessageProvider>();
        services.AddSingleton<IUserFeedback, UserFeedback>();

        // powiadomienia push
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<IPushMessageRouter, PushMessageRouter>();
        services.AddSingleton<ForegroundPushHandler>();
        services.AddTransient<IDeviceRegistrationService, DeviceRegistrationService>();
        services.AddSingleton<Func<IDeviceRegistrationService>>(sp => () => sp.GetRequiredService<IDeviceRegistrationService>());
        services.AddTransient<INotificationOnboarding, NotificationOnboarding>();
        services.AddTransient<INotificationSettingsService, NotificationSettingsService>();
        return services;
    }

    // API szkoleniowe: typed clients z IHttpClientFactory. BaseUrl kończy się „/”, ścieżki w klientach są względne.
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        services.Configure<ApiSettings>(o => o.BaseUrl = ApiSettings.TrainingBaseUrl);
        services.AddTransient<AuthHeaderHandler>(); // dokleja Bearer z SecureStorage

        services.AddHttpClient<IAuthApiClient, AuthApiClient>(ConfigureApi);
        services.AddHttpClient<IDevicesApiClient, DevicesApiClient>(ConfigureApi)
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .AddStandardResilienceHandler(ConfigureResilience);
        services.AddHttpClient<IReportsApiClient, ReportsApiClient>(ConfigureApi)
            .AddHttpMessageHandler<AuthHeaderHandler>()          // 1. token
            .AddStandardResilienceHandler(ConfigureResilience);  // 2. Polly
        return services;
    }

    // Automatyczny retry tylko dla bezpiecznych metod (GET/HEAD/OPTIONS…): zapis po zgubionej odpowiedzi jest
    // niejednoznaczny — dlatego POST z nagłówkiem Idempotency-Key, a ponowienie zapisu = kolejna synchronizacja.
    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        // retry tylko GET/HEAD/…
        options.Retry.DisableForUnsafeHttpMethods();
        // wysyłka zdjęcia po słabym LTE
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
        // ≥ 2 × AttemptTimeout (walidacja opcji)
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(40);
    }

    private static void ConfigureApi(IServiceProvider sp, HttpClient client)
    {
        client.BaseAddress = new Uri(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>().Value.BaseUrl);
        // całkowity limit pilnuje też handler odporności
        client.Timeout = TimeSpan.FromSeconds(60);
    }

    // IDialogService, IClock, IErrorHandler… (implementacje MAUI; w testach mocki)
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IDialogService, MauiDialogService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IErrorHandler, LoggingErrorHandler>();
        services.AddSingleton<ILocalizer, Localizer>();
        services.AddSingleton<ISessionStore, SecureSessionStore>();
        services.AddSingleton<IAppEnvironment, MauiAppEnvironment>();
        services.AddSingleton<IAppPaths, MauiAppPaths>(); // FileSystem.AppDataDirectory
        services.AddSingleton<IConnectivityService, MauiConnectivityService>();
        services.AddSingleton<IPermissionService, MauiPermissionService>();
        services.AddSingleton<IPhotoStorage, PhotoStorage>();
        services.AddSingleton<IMediaService, MauiMediaService>();
        services.AddSingleton<IAppPreferences, MauiAppPreferences>();
        services.AddSingleton<IClipboardService, MauiClipboardService>();

        // powiadomienia push: Firebase — plugin tylko tu i w FirebasePushAdapter
        services.AddSingleton(_ => Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current);
        services.AddSingleton<FirebasePushAdapter>();
        services.AddSingleton<IPushTokenProvider>(sp => sp.GetRequiredService<FirebasePushAdapter>());
        services.AddSingleton<IPushPermissionService, MauiPushPermissionService>();
        services.AddSingleton<IInAppNotifier, SnackbarInAppNotifier>();
        services.AddSingleton<PushStartup>();
        return services;
    }

    // ViewModele stron i komponentów: transient, nowa instancja przy każdym wejściu na stronę
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<StartupViewModel>(); // kontener sam przekaże zależności do konstruktora
        services.AddTransient<StartupErrorViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReportDetailsViewModel>();
        services.AddTransient<NewReportFlowViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<NotificationPrimingViewModel>();
        services.AddTransient<PlaygroundViewModel>();
        services.AddTransient<CounterViewModel>();

        // kroki kreatora nowego zgłoszenia: ViewModele komponentów, nowe dla każdego kreatora
        services.AddTransient<DetailsStepViewModel>();
        services.AddTransient<PhotoStepViewModel>();
        services.AddTransient<SummaryStepViewModel>();
        return services;
    }

    // Strony i nawigacja. Która strona należy do którego ViewModelu — mapa PageRoutes w MauiProgram.
    public static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<RootPage>(); // nowy korzeń dla każdego okna
        services.AddTransient<StartupPage>();
        services.AddTransient<StartupErrorPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<ReportsPage>();
        services.AddTransient<ReportDetailsPage>();
        services.AddTransient<NewReportFlowPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<NotificationPrimingPage>();
        services.AddTransient<PlaygroundPage>();
        return services;
    }
}
