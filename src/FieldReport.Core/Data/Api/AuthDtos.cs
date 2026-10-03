namespace FieldReport.Core.Data.Api;

// Kontrakt API szkoleniowego: POST api/auth/login, POST api/auth/refresh, POST api/devices
public sealed record LoginRequestDto(string Email, string Pin);
public sealed record RefreshRequestDto(string RefreshToken);
public sealed record TokenResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, string TechnicianName);
public sealed record RegisterDeviceRequestDto(string Token, string Platform);
