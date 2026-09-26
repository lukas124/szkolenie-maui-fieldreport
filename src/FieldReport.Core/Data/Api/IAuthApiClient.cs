using FieldReport.Core.Models;

namespace FieldReport.Core.Data.Api;

public interface IAuthApiClient
{
    Task<Result<TokenResponseDto>> LoginAsync(string email, string pin, CancellationToken ct = default);
    Task<Result<TokenResponseDto>> RefreshAsync(string refreshToken, CancellationToken ct = default);
}
