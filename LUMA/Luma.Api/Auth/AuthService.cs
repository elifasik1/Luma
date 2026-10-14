using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Luma.Domain.Entities;
using Luma.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Luma.Api.Auth;

public sealed class AuthService
{
    private const int MinimumPasswordLength = 12;
    private const int MaximumPasswordLength = 128;

    private readonly LumaDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        LumaDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthOperationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        var email = NormalizeEmail(request.Email);
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return new(null, "Name is required and must be at most 100 characters.", 400);
        }

        if (email is null)
        {
            return new(null, "A valid email address is required.", 400);
        }

        if (password is null ||
            password.Length < MinimumPasswordLength ||
            password.Length > MaximumPasswordLength)
        {
            return new(null, $"Password must be between {MinimumPasswordLength} and {MaximumPasswordLength} characters.", 400);
        }

        if (await _dbContext.Users.AnyAsync(
                user => user.Email != null && user.Email.ToLower() == email,
                cancellationToken))
        {
            return new(null, "An account with this email already exists.", 409);
        }

        var user = new User { Name = name, Email = email };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEmail(exception))
        {
            return new(null, "An account with this email already exists.", 409);
        }

        return CreateAuthentication(user);
    }

    public async Task<AuthOperationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (email is null || string.IsNullOrEmpty(request.Password))
        {
            return new(null, "Email or password is incorrect.", 401);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                candidate => candidate.Email != null && candidate.Email.ToLower() == email,
                cancellationToken);

        if (user?.PasswordHash is null)
        {
            return new(null, "Email or password is incorrect.", 401);
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return new(null, "Email or password is incorrect.", 401);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return CreateAuthentication(user);
    }

    public Task<AuthenticatedUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Where(user => user.Id == userId && user.Email != null)
            .Select(user => new AuthenticatedUserResponse(user.Id, user.Name, user.Email!))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public AuthOperationResult CreateAuthenticationForUser(User user) => CreateAuthentication(user);

    private AuthOperationResult CreateAuthentication(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_jwtOptions.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.Name),
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new(
            new AuthTokenResponse(
                new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt,
                new AuthenticatedUserResponse(user.Id, user.Name, user.Email!)),
            null,
            200);
    }

    private static string? NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Length > 254 ||
            !new EmailAddressAttribute().IsValid(normalized))
        {
            return null;
        }

        return normalized;
    }

    private static bool IsDuplicateEmail(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Users_Email_Lower"
        };
    }
}
