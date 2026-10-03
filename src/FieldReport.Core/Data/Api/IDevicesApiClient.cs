using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

public interface IDevicesApiClient
{
    Task<Result> RegisterAsync(string token, string platform, CancellationToken ct = default);
    Task<Result> UnregisterAsync(string token, CancellationToken ct = default);
}
