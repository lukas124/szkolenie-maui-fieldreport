using FieldReport.Core.Services;

namespace FieldReport.App.Extensions;

public static class TaskExtensions
{
    // świadomy fire-and-forget: wyjątek trafia do logów, nie w próżnię
    public static async void FireAndForget(this Task task, IErrorHandler errors)
    {
        try { await task; }
        catch (Exception ex) { errors.Handle(ex); }
    }
}
