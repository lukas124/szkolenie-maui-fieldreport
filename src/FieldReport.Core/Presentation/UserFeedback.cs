using FieldReport.Core.Models;
using FieldReport.Core.Services;

namespace FieldReport.Core.Presentation;

// to, czego używa ViewModel: „pokaż błąd”, „zapytaj o potwierdzenie”
public sealed class UserFeedback(IDialogService dialogs, IErrorMessageProvider messages, ILocalizer localizer) : IUserFeedback
{
    public Task ShowErrorAsync(Error error) =>
        dialogs.AlertAsync(localizer.GetString("Common_ErrorTitle"), // tytuł z zasobów
            messages.GetMessage(error),                            // treść: błąd → tekst
            localizer.GetString("Common_Ok"));                     // przycisk „OK”
}
