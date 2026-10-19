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

        var user = new User
        {
            Name = name,
            Email = email,
            ThemePreference = NormalizeThemePreference(
                request.ThemePreference,
                request.ThemePreferenceIsExplicit)
        };
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
            .Include(u => u.Dreams)
                .ThenInclude(d => d.FutureProfile)
                    .ThenInclude(fp => fp!.ThemeConfig)
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

        if (string.IsNullOrWhiteSpace(user.ThemePreference))
        {
            var fallbackTheme = user.Dreams
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => d.FutureProfile?.ThemeConfig?.Palette)
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

            if (!string.IsNullOrWhiteSpace(fallbackTheme))
            {
                user.ThemePreference = fallbackTheme;
            }
        }

        return CreateAuthentication(user);
    }

    public async Task<AuthenticatedUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.Dreams)
                .ThenInclude(d => d.FutureProfile)
                    .ThenInclude(fp => fp!.ThemeConfig)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Email != null, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var themePref = user.ThemePreference;
        if (string.IsNullOrWhiteSpace(themePref))
        {
            themePref = user.Dreams
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => d.FutureProfile?.ThemeConfig?.Palette)
                .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
        }

        return new AuthenticatedUserResponse(
            user.Id,
            user.Name,
            user.Email!,
            GetThemePreferencePalette(themePref));
    }

    public async Task<UserThemeResponse> GetUserThemeAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.Dreams)
                .ThenInclude(d => d.FutureProfile)
                    .ThenInclude(fp => fp!.ThemeConfig)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return GetDefaultTheme();
        }

        if (!string.IsNullOrWhiteSpace(user.ThemePreference))
        {
            var parsed = ParseThemePreference(user.ThemePreference);
            if (parsed is not null)
            {
                return parsed;
            }
        }

        var latestThemeConfig = user.Dreams
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => d.FutureProfile?.ThemeConfig)
            .FirstOrDefault(tc => tc != null);

        if (latestThemeConfig is not null)
        {
            return new UserThemeResponse(
                NormalizePalette(latestThemeConfig.Palette),
                NormalizeAtmosphere(latestThemeConfig.Atmosphere),
                NormalizeIllustration(latestThemeConfig.Illustration),
                NormalizeMotion(latestThemeConfig.Motion),
                IsExplicit: false);
        }

        return GetDefaultTheme();
    }

    public async Task<(UserThemeResponse? Theme, string? Error, int StatusCode)> UpdateUserThemeAsync(
        Guid userId,
        UpdateThemeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Palette))
        {
            return (null, "Palette is required.", 400);
        }

        var trimmedPalette = request.Palette.Trim().ToLowerInvariant().Replace('-', '_');
        if (!ValidPalettes.Contains(trimmedPalette))
        {
            return (null, $"Unsupported palette '{request.Palette}'. Must be a valid LUMA palette.", 400);
        }

        var normalizedPalette = trimmedPalette;

        var user = await _dbContext.Users
            .Include(u => u.Dreams)
                .ThenInclude(d => d.FutureProfile)
                    .ThenInclude(fp => fp!.ThemeConfig)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return (null, "User not found.", 404);
        }

        var normalizedAtmosphere = NormalizeAtmosphere(request.Atmosphere);
        var normalizedIllustration = NormalizeIllustration(request.Illustration);
        var normalizedMotion = NormalizeMotion(request.Motion);

        var updatedTheme = new UserThemeResponse(
            normalizedPalette,
            normalizedAtmosphere,
            normalizedIllustration,
            normalizedMotion,
            IsExplicit: true);

        var json = SerializeThemePreference(updatedTheme, isExplicit: true);
        user.ThemePreference = json;

        // Also synchronize the latest dream's ThemeConfig if present
        var latestThemeConfig = user.Dreams
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => d.FutureProfile?.ThemeConfig)
            .FirstOrDefault(tc => tc != null);

        if (latestThemeConfig is not null)
        {
            latestThemeConfig.Palette = normalizedPalette;
            latestThemeConfig.Atmosphere = normalizedAtmosphere;
            latestThemeConfig.Illustration = normalizedIllustration;
            latestThemeConfig.Motion = normalizedMotion;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (updatedTheme, null, 200);
    }

    private static readonly HashSet<string> ValidPalettes = new(StringComparer.OrdinalIgnoreCase)
    {
        "warm_dream",
        "forest_mist",
        "city_night",
        "lavender_milk",
        "social_sunrise",
        "amber_summit",
        "ocean_light",
        "sunset_cream",
        "maker_workshop"
    };

    private static string NormalizePalette(string? palette)
    {
        if (string.IsNullOrWhiteSpace(palette)) return "warm_dream";
        var trimmed = palette.Trim().ToLowerInvariant().Replace('-', '_');
        return ValidPalettes.Contains(trimmed) ? trimmed : "warm_dream";
    }

    private static string NormalizeAtmosphere(string? atmosphere)
    {
        if (string.IsNullOrWhiteSpace(atmosphere)) return "calm";
        var trimmed = atmosphere.Trim().ToLowerInvariant();
        return trimmed switch
        {
            "airy" or "cozy" or "energetic" or "calm" or "creative" or "focused" or "grounded" => trimmed,
            _ => "calm"
        };
    }

    private static string NormalizeIllustration(string? illustration)
    {
        if (string.IsNullOrWhiteSpace(illustration)) return "dreamer";
        var trimmed = illustration.Trim().ToLowerInvariant().Replace('-', '_');
        return trimmed switch
        {
            "dreamer" or "urban_dreamer" or "creative_dreamer" or "nature_dreamer" or "professional_dreamer" => trimmed,
            _ => "dreamer"
        };
    }

    private static string NormalizeMotion(string? motion)
    {
        if (string.IsNullOrWhiteSpace(motion)) return "soft";
        var trimmed = motion.Trim().ToLowerInvariant();
        return trimmed switch
        {
            "gentle" or "soft" or "floating" or "still" => trimmed,
            _ => "soft"
        };
    }

    private static UserThemeResponse GetDefaultTheme() =>
        new("warm_dream", "calm", "dreamer", "soft", false);

    public async Task<bool> ApplyDreamThemeIfNotExplicitAsync(
        Guid userId,
        UserThemeResponse dreamTheme,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken);

        if (user is null || IsExplicitThemePreference(user.ThemePreference))
        {
            return false;
        }

        user.ThemePreference = SerializeThemePreference(dreamTheme, isExplicit: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static bool IsExplicitThemePreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference)) return false;
        var trimmed = preference.Trim();
        if (!trimmed.StartsWith('{')) return true;

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<PersistedThemePreference>(
                trimmed,
                JsonOptions);
            return parsed?.IsExplicit ?? true;
        }
        catch
        {
            return true;
        }
    }

    private static string? NormalizeThemePreference(string? preference, bool isExplicit)
    {
        if (string.IsNullOrWhiteSpace(preference)) return null;
        var trimmed = preference.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<PersistedThemePreference>(
                    trimmed,
                    JsonOptions);
                if (parsed is not null && ValidPalettes.Contains(NormalizePalette(parsed.Palette)))
                {
                    return SerializeThemePreference(new UserThemeResponse(
                        NormalizePalette(parsed.Palette),
                        NormalizeAtmosphere(parsed.Atmosphere),
                        NormalizeIllustration(parsed.Illustration),
                        NormalizeMotion(parsed.Motion)),
                        isExplicit);
                }
            }
            catch
            {
                // ignore JSON parse error, treat as plain string
            }
        }

        var normalizedPalette = NormalizePalette(trimmed);
        return SerializeThemePreference(
            new UserThemeResponse(normalizedPalette, "calm", "dreamer", "soft"),
            isExplicit);
    }

    private static UserThemeResponse? ParseThemePreference(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<PersistedThemePreference>(
                    trimmed,
                    JsonOptions);
                if (parsed is not null)
                {
                    return new UserThemeResponse(
                        NormalizePalette(parsed.Palette),
                        NormalizeAtmosphere(parsed.Atmosphere),
                        NormalizeIllustration(parsed.Illustration),
                        NormalizeMotion(parsed.Motion),
                        parsed.IsExplicit);
                }
            }
            catch
            {
                // ignore
            }
        }

        var palette = NormalizePalette(trimmed);
        return new UserThemeResponse(palette, "calm", "dreamer", "soft", true);
    }

    private static string? GetThemePreferencePalette(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference)) return null;

        return ParseThemePreference(preference)?.Palette;
    }

    private static string SerializeThemePreference(UserThemeResponse theme, bool isExplicit) =>
        System.Text.Json.JsonSerializer.Serialize(new PersistedThemePreference(
            theme.Palette,
            theme.Atmosphere,
            theme.Illustration,
            theme.Motion,
            isExplicit));

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record PersistedThemePreference(
        string Palette,
        string Atmosphere,
        string Illustration,
        string Motion,
        bool IsExplicit = true);

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
                new AuthenticatedUserResponse(
                    user.Id,
                    user.Name,
                    user.Email!,
                    GetThemePreferencePalette(user.ThemePreference))),
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
