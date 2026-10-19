namespace Luma.Api.Auth;

public sealed record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? ThemePreference = null,
    bool ThemePreferenceIsExplicit = false);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string Name,
    string Email,
    string? ThemePreference = null);

public sealed record UserThemeResponse(
    string Palette,
    string Atmosphere,
    string Illustration,
    string Motion,
    bool IsExplicit = false);

public sealed record UpdateThemeRequest(
    string Palette,
    string? Atmosphere = null,
    string? Illustration = null,
    string? Motion = null);

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthenticatedUserResponse User);

public sealed record AuthOperationResult(
    AuthTokenResponse? Authentication,
    string? Error,
    int StatusCode)
{
    public bool Succeeded => Authentication is not null;
}
