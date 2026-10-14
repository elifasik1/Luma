namespace Luma.Api.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthenticatedUserResponse(Guid Id, string Name, string Email);

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
