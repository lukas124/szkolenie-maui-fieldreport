using FieldReport.App.Navigation;
using FieldReport.App.Pages;
using FieldReport.App.Services;
using FieldReport.App.Services.Localization;
using FieldReport.Core.Data;
using FieldReport.Core.Navigation;
using FieldReport.Core.Presentation;
using FieldReport.Core.Services;
using FieldReport.Core.ViewModels;

namespace FieldReport.App;

// Rejestracje w metodach rozszerzających: MauiProgram zostaje krótki i czytelny.
// Czasy życia: Singleton = usługi, repozytorium, nawigacja (stan współdzielony);
// Transient = ViewModele i strony (nowa instancja przy każdym wejściu). Scoped w MAUI nie ma naturalnego scope'u.
public static class ServiceCollectionExtensions
{
    // IReportService, repozytorium, fabryki, formatery, IUserFeedback… (FieldReport.Core, bez MAUI)
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // TODO: zarejestruj jako Singleton: IReportRepository → InMemoryReportRepository, IReportService → ReportService,
        // IReportFormatter, IReportItemFactory, IErrorMessageProvider, IUserFeedback
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

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<ReportsViewModel>(); // kontener sam przekaże zależności do konstruktora
        return services;
    }

    // Strony i nawigacja
    public static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<ReportsPage>();
        return services;
    }
}
