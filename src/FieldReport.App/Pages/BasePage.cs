using System.Windows.Input;
using FieldReport.App.Extensions;
using FieldReport.Core.Navigation;
using FieldReport.Core.Services;

namespace FieldReport.App.Pages;

/// <summary>
/// Strona z nagłówkiem, która przekazuje swój cykl życia do ViewModelu. Wygląd nagłówka i wskaźnik ładowania opisuje szablon
/// „PageTemplate” w Templates/PageTemplate.xaml (taki sam na Androidzie i iOS), a strona ustawia tylko treść nagłówka: tytuł (Title),
/// strzałkę „wstecz” (CanNavigateBack) albo „X” (ShowCloseButton) z lewej i ikonę akcji z prawej (ActionIcon, ActionCommand).
/// Strona bez nagłówka (np. logowanie): IsHeaderVisible = false.
/// </summary>
public abstract class BasePage(IErrorHandler errors) : ContentPage
{
    public static readonly BindableProperty IsHeaderVisibleProperty =
        BindableProperty.Create(nameof(IsHeaderVisible), typeof(bool), typeof(BasePage), true);

    public static readonly BindableProperty CanNavigateBackProperty =
        BindableProperty.Create(nameof(CanNavigateBack), typeof(bool), typeof(BasePage), false);

    public static readonly BindableProperty ShowCloseButtonProperty =
        BindableProperty.Create(nameof(ShowCloseButton), typeof(bool), typeof(BasePage), false);

    // Zmiana ikony zmienia też HasAction (szablon pokazuje przycisk akcji tylko, gdy jest ikona)
    public static readonly BindableProperty ActionIconProperty =
        BindableProperty.Create(nameof(ActionIcon), typeof(ImageSource), typeof(BasePage),
            propertyChanged: (page, _, _) => ((BasePage)page).OnPropertyChanged(nameof(HasAction)));

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(BasePage));

    /// <summary>false = strona bez nagłówka.</summary>
    public bool IsHeaderVisible
    {
        get => (bool)GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }

    /// <summary>true = w nagłówku jest strzałka „wstecz”.</summary>
    public bool CanNavigateBack
    {
        get => (bool)GetValue(CanNavigateBackProperty);
        set => SetValue(CanNavigateBackProperty, value);
    }

    /// <summary>true = w nagłówku jest „X” (zamknięcie strony, np. porzucenie kreatora).</summary>
    public bool ShowCloseButton
    {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>Ikona akcji z prawej strony nagłówka (brak ikony = brak przycisku).</summary>
    public ImageSource? ActionIcon
    {
        get => (ImageSource?)GetValue(ActionIconProperty);
        set => SetValue(ActionIconProperty, value);
    }

    /// <summary>true = strona ma ikonę akcji, więc nagłówek pokazuje przycisk z prawej.</summary>
    public bool HasAction => ActionIcon is not null;

    /// <summary>Komenda wykonywana po dotknięciu ikony akcji.</summary>
    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    /// <summary>Obsługa błędów — także dla stron tworzonych w code-behind (new PlaygroundPage(Errors)).</summary>
    protected IErrorHandler Errors => errors;

    // Szablon (styl z App.xaml) jest nakładany już w konstruktorze ContentPage, zanim wykona się kod BasePage,
    // dlatego komendy tworzymy przy pierwszym odczycie, a nie w konstruktorze
    private Command? _backButtonCommand;
    private Command? _closeButtonCommand;

    /// <summary>Komenda strzałki „wstecz” (wiąże ją szablon nagłówka).</summary>
    public ICommand BackButtonCommand => _backButtonCommand ??= new Command(GoBack);

    /// <summary>Komenda „X” (wiąże ją szablon nagłówka).</summary>
    public ICommand CloseButtonCommand => _closeButtonCommand ??= new Command(Close);

    // Strzałka „wstecz” = systemowy przycisk „wstecz”: decyduje ViewModel strony; strona bez ViewModelu po prostu wraca
    private void GoBack()
    {
        if (BindingContext is BaseViewModel vm) vm.OnBackRequestedAsync().FireAndForget(errors);
        else Navigation.PopAsync().FireAndForget(errors);
    }

    // „X”: decyzję (np. potwierdzenie porzucenia kreatora) podejmuje ViewModel strony
    private void Close()
    {
        if (BindingContext is BaseViewModel vm) vm.OnCloseRequestedAsync().FireAndForget(errors);
    }

    // true = stronę przykrył dialog (DialogPage); jego otwarcie i zamknięcie nie jest dla ViewModelu wyjściem ani powrotem na stronę
    private bool _coveredByDialog;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_coveredByDialog) { _coveredByDialog = false; return; }
        if (BindingContext is BaseViewModel vm) vm.OnAppearingAsync().FireAndForget(errors);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Navigation.ModalStack.LastOrDefault() is DialogPage) { _coveredByDialog = true; return; }
        if (BindingContext is BaseViewModel vm) vm.OnDisappearingAsync().FireAndForget(errors);
    }

    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is not BaseViewModel vm) return base.OnBackButtonPressed();

        // Korzeń stosu: oddajemy przycisk systemowi, żeby „wstecz” z listy zamykało aplikację (start i routing).
        if (Navigation.NavigationStack.Count <= 1 && Navigation.ModalStack.Count == 0)
            return base.OnBackButtonPressed();

        vm.OnBackRequestedAsync().FireAndForget(errors);
        return true;
    }
}
