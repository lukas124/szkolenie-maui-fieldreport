using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using FieldReport.App.Services;
using FieldReport.App.Services.Localization;
using FieldReport.Core.Data;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;
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
        services.AddSingleton<IReportRepository, InMemoryReportRepository>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IReportFormatter, ReportFormatter>();
        services.AddSingleton<IReportItemFactory, ReportItemFactory>();
        services.AddSingleton<IReportDetailsFactory, ReportDetailsFactory>();
        services.AddSingleton<IErrorMessageProvider, ErrorMessageProvider>();
        services.AddSingleton<IUserFeedback, UserFeedback>();
        return services;
    }

    // IDialogService, IClock, IErrorHandler… (implementacje MAUI; w testach mocki)
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IDialogService, MauiDialogService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IErrorHandler, LoggingErrorHandler>();
        services.AddSingleton<ILocalizer, Localizer>();
        return services;
    }

    // ViewModele stron i komponentów: transient, nowa instancja przy każdym wejściu na stronę
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<ReportsViewModel>(); // kontener sam przekaże zależności do konstruktora
        services.AddTransient<ReportDetailsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PlaygroundViewModel>();
        services.AddTransient<CounterViewModel>();
        return services;
    }

    // Strony i nawigacja. Która strona należy do którego ViewModelu — mapa PageRoutes w MauiProgram.
    public static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<RootPage>(); // nowy korzeń dla każdego okna
        services.AddTransient<ReportsPage>();
        services.AddTransient<ReportDetailsPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<PlaygroundPage>();
        return services;
    }
}
