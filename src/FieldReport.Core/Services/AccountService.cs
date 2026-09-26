using FieldReport.Core.Data;
using FieldReport.Core.Models;
using FieldReport.Core.Services.Push;

namespace FieldReport.Core.Services;

public sealed class AccountService(
    IAuthService auth, IDeviceRegistrationService devices, IReportRepository reports, IPhotoStorage photos) : IAccountService
{
    public Task<Session?> GetSessionAsync(CancellationToken ct = default) => auth.GetSessionAsync(ct);

    public async Task<int> CountPendingAsync(CancellationToken ct = default) =>
        (await reports.GetAllAsync(ct)).Count; // w telefonie są tylko niewysłane

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        await devices.UnregisterAsync(ct); // najpierw urządzenie (potrzebny jeszcze token logowania)
        await auth.SignOutAsync(ct);
        await reports.DeleteAllAsync(ct);  // niewysłane dane technika nie zostają dla następnego
        await photos.DeleteAllAsync(ct);
    }
}
