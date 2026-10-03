using CommunityToolkit.Mvvm.ComponentModel;
using FieldReport.Core.Models;

namespace FieldReport.Core.ViewModels.NewReport;

/// <summary>
/// Kontrakt kroku kreatora (LSP: każdy krok wymienny). Krok to ViewModel komponentu (ObservableObject),
/// nie strony — nie nawiguje; przepływ pyta go tylko o <see cref="IsValid"/>.
/// </summary>
public abstract partial class WizardStepViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isValid;

    // zdarzenie: „moja poprawność się zmieniła”
    public event EventHandler? ValidityChanged;
    partial void OnIsValidChanged(bool value) => ValidityChanged?.Invoke(this, EventArgs.Empty);

    public abstract string Title { get; }
    public abstract void Attach(NewReportDraft draft);
    public virtual void OnShown() { }
}
