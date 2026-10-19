using System.Security.Claims;
using System.Text;
using Luma.Application.AI;
using Luma.Application.Agent;
using Luma.Application.Agent.Tools;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Application.Services;
using Luma.Infrastructure.AI;
using Luma.Infrastructure.Persistence;
using Luma.Api.Auth;
using Luma.Api.Mcp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = Encoding.UTF8.GetBytes(jwtOptions.Secret);
if (signingKey.Length < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Secret with at least 32 bytes using User Secrets or environment configuration.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
        };
    });
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<LumaDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<LumaDbContext>(options =>
        options.UseInMemoryDatabase("LumaDb"));
}

builder.Services.AddScoped<IDreamRepository, DreamRepository>();
builder.Services.AddScoped<IAgentDreamAccess, AgentDreamAccess>();
builder.Services.AddScoped<IAgentToolRegistry, AgentToolRegistry>();
builder.Services.AddScoped<IAdaptiveDailyPlanCapability, AdaptiveDailyPlanCapability>();
builder.Services.AddScoped<IFutureContextQuery, FutureContextQuery>();
builder.Services.AddScoped<IGoalReadQuery, GoalReadQuery>();
builder.Services.AddScoped<IDailyPlanReadQuery, DailyPlanReadQuery>();
builder.Services.AddScoped<IReflectionReadQuery, ReflectionReadQuery>();
builder.Services.AddScoped<IJourneyCalendarQuery, JourneyCalendarQuery>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPasswordHasher<Luma.Domain.Entities.User>, PasswordHasher<Luma.Domain.Entities.User>>();

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<GeminiAIService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(options.BaseUrl) ? "https://generativelanguage.googleapis.com" : options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
});
builder.Services.AddHttpClient<GeminiAgentPlanner>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(options.BaseUrl)
        ? "https://generativelanguage.googleapis.com"
        : options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
});
builder.Services.AddHttpClient<GeminiAgentResponseGenerator>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(options.BaseUrl)
        ? "https://generativelanguage.googleapis.com"
        : options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
});
builder.Services.AddHttpClient<GeminiAgentActionPlanner>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
    client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(options.BaseUrl)
        ? "https://generativelanguage.googleapis.com"
        : options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
});

builder.Services.AddScoped<DevelopmentAIService>();
builder.Services.AddScoped<FallbackAIService>();
builder.Services.AddScoped<DeterministicAgentPlanner>();
builder.Services.AddScoped<FallbackAgentPlanner>();
builder.Services.AddScoped<DeterministicAgentResponseGenerator>();
builder.Services.AddScoped<FallbackAgentResponseGenerator>();
builder.Services.AddScoped<DeterministicAgentActionPlanner>();
builder.Services.AddScoped<FallbackAgentActionPlanner>();

var aiProvider = builder.Configuration["AI:Provider"] ?? "Gemini";
if (string.Equals(aiProvider, "Development", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IAIService, DevelopmentAIService>();
    builder.Services.AddScoped<IAgentPlanner, DeterministicAgentPlanner>();
    builder.Services.AddScoped<IAgentResponseGenerator, DeterministicAgentResponseGenerator>();
    builder.Services.AddScoped<IAgentActionPlanner, DeterministicAgentActionPlanner>();
}
else
{
    builder.Services.AddScoped<IAIService, FallbackAIService>();
    builder.Services.AddScoped<IAgentPlanner, FallbackAgentPlanner>();
    builder.Services.AddScoped<IAgentResponseGenerator, FallbackAgentResponseGenerator>();
    builder.Services.AddScoped<IAgentActionPlanner, FallbackAgentActionPlanner>();
}

builder.Services.AddScoped<DreamAnalysisService>();
builder.Services.AddScoped<DreamQueryService>();
builder.Services.AddScoped<FutureScenarioService>();
builder.Services.AddScoped<GoalService>();
builder.Services.AddScoped<DailyPathService>();
builder.Services.AddScoped<ReflectionService>();
builder.Services.AddScoped<JourneyCalendarService>();
builder.Services.AddScoped<TalkToLumaService>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<FutureContextReadService>();
builder.Services.AddScoped<IAgentTool, FutureContextTool>();
builder.Services.AddScoped<ActiveGoalsReadService>();
builder.Services.AddScoped<IAgentTool, ActiveGoalsTool>();
builder.Services.AddScoped<DailyPlanReadService>();
builder.Services.AddScoped<IAgentTool, DailyPlanTool>();
builder.Services.AddScoped<IAgentTool, ProgressTool>();
builder.Services.AddScoped<ReflectionReadService>();
builder.Services.AddScoped<IAgentTool, RecentReflectionsTool>();
builder.Services.AddScoped<UpdateDailyPlanTool>();
builder.Services.AddScoped<IUpdateDailyPlanTool>(sp => sp.GetRequiredService<UpdateDailyPlanTool>());
builder.Services.AddScoped<IAgentTool>(sp => sp.GetRequiredService<UpdateDailyPlanTool>());

builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddLumaMcp();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "LUMA" }));

var authRoutes = app.MapGroup("/auth");
authRoutes.MapPost("/register", async (
    RegisterRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RegisterAsync(request, cancellationToken);
    return result.Succeeded
        ? Results.Json(result.Authentication, statusCode: StatusCodes.Status201Created)
        : Results.Json(new { error = result.Error }, statusCode: result.StatusCode);
});
authRoutes.MapPost("/login", async (
    LoginRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.LoginAsync(request, cancellationToken);
    return result.Succeeded
        ? Results.Ok(result.Authentication)
        : Results.Json(new { error = result.Error }, statusCode: result.StatusCode);
});
authRoutes.MapGet("/me", async (
    ClaimsPrincipal principal,
    AuthService service,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await service.GetCurrentUserAsync(userId, cancellationToken);
    return user is null ? Results.Unauthorized() : Results.Ok(user);
}).RequireAuthorization();

authRoutes.MapGet("/theme", async (
    ClaimsPrincipal principal,
    AuthService service,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    var theme = await service.GetUserThemeAsync(userId, cancellationToken);
    return Results.Ok(theme);
}).RequireAuthorization();

authRoutes.MapPut("/theme", async (
    UpdateThemeRequest request,
    ClaimsPrincipal principal,
    AuthService service,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    var (theme, error, statusCode) = await service.UpdateUserThemeAsync(userId, request, cancellationToken);
    return theme is not null
        ? Results.Ok(theme)
        : Results.Json(new { error }, statusCode: statusCode);
}).RequireAuthorization();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Demo:Enabled", false))
{
    authRoutes.MapGet("/dev-token", async (LumaDbContext db, AuthService service, DemoSeeder seeder) =>
    {
        var user = await seeder.EnsureDemoUserAndDataAsync();
        var authResult = service.CreateAuthenticationForUser(user);
        if (authResult.Authentication is null)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        var auth = authResult.Authentication;
        var dream = await db.Dreams
            .Where(d => d.UserId == user.Id)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();

        return Results.Ok(new
        {
            auth.AccessToken,
            auth.ExpiresAt,
            auth.User,
            dreamId = dream?.Id
        });
    });
}

var dreamRoutes = app.MapGroup("/dreams").RequireAuthorization();
dreamRoutes.AddEndpointFilter(async (context, next) =>
{
    if (context.HttpContext.Request.RouteValues.TryGetValue("dreamId", out var rawDreamId))
    {
        if (!Guid.TryParse(rawDreamId?.ToString(), out var dreamId) ||
            !TryGetUserId(context.HttpContext.User, out var userId))
        {
            return Results.NotFound(new { error = "Dream not found." });
        }

        var repository = context.HttpContext.RequestServices.GetRequiredService<IDreamRepository>();
        if (!await repository.IsOwnedByAsync(dreamId, userId, context.HttpContext.RequestAborted))
        {
            return Results.NotFound(new { error = "Dream not found." });
        }
    }

    return await next(context);
});

dreamRoutes.MapGet("/latest", async (
    ClaimsPrincipal principal,
    LumaDbContext db,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    var latestDream = await db.Dreams
        .AsNoTracking()
        .Where(d => d.UserId == userId)
        .OrderByDescending(d => d.CreatedAt)
        .Select(d => new { d.Id, d.Title, d.CreatedAt })
        .FirstOrDefaultAsync(cancellationToken);

    return latestDream is null
        ? Results.NotFound(new { error = "No dreams found for user." })
        : Results.Ok(latestDream);
});

dreamRoutes.MapPost("", async (
    CreateDreamRequest request,
    ClaimsPrincipal principal,
    DreamAnalysisService service,
    AuthService authService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var userName = principal.FindFirstValue(ClaimTypes.Name) ?? "Dreamer";
        var result = await service.CreateDreamAsync(request, userId, userName, cancellationToken);

        await authService.ApplyDreamThemeIfNotExplicitAsync(
            userId,
            new UserThemeResponse(
                result.Palette,
                result.Atmosphere,
                result.Illustration,
                result.Motion),
            cancellationToken);

        return Results.Created($"/dreams/{result.DreamId}", result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (GeminiTransientException ex)
    {
        logger.LogWarning(ex, "Gemini provider unavailable while analyzing dream");
        return GeminiProviderUnavailable(ex);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to analyze dream: {Message}", ex.Message);
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while analyzing dream");
        return Results.Json(new { error = "An unexpected error occurred while processing your dream. Please try again later." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

dreamRoutes.MapGet("/{dreamId:guid}", async (Guid dreamId, DreamQueryService service, CancellationToken cancellationToken) =>
{
    var response = await service.GetDreamDetailsAsync(dreamId, cancellationToken);
    if (response is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(response);
});

dreamRoutes.MapGet("/{dreamId:guid}/future-scenario", async (
    Guid dreamId,
    FutureScenarioService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.GetOrCreateFutureScenarioAsync(dreamId, cancellationToken);
        if (response is null)
        {
            return Results.NotFound(new { error = "Dream not found." });
        }

        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to generate future scenario for Dream {DreamId}: {Message}", dreamId, ex.Message);
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (GeminiTransientException ex)
    {
        logger.LogWarning(ex, "Gemini provider unavailable while generating future scenario for Dream {DreamId}", dreamId);
        return GeminiProviderUnavailable(ex);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while generating future scenario for Dream {DreamId}", dreamId);
        return Results.Json(new { error = "An unexpected error occurred while generating your future scenario. Please try again later." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

dreamRoutes.MapGet("/{dreamId:guid}/goals", async (
    Guid dreamId,
    GoalService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.GetOrCreateGoalsAsync(dreamId, cancellationToken);
        if (response is null)
        {
            return Results.NotFound(new { error = "Dream not found." });
        }

        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to generate goals for Dream {DreamId}: {Message}", dreamId, ex.Message);
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (GeminiTransientException ex)
    {
        logger.LogWarning(ex, "Gemini provider unavailable while generating goals for Dream {DreamId}", dreamId);
        return GeminiProviderUnavailable(ex);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while generating goals for Dream {DreamId}", dreamId);
        return Results.Json(new { error = "An unexpected error occurred while generating your goals. Please try again later." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

dreamRoutes.MapGet("/{dreamId:guid}/daily-path", async (
    Guid dreamId,
    DailyPathService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken,
    bool refresh = false) =>
{
    try
    {
        var response = await service.GetOrCreateDailyPathAsync(dreamId, refresh, cancellationToken);
        if (response is null)
        {
            return Results.NotFound(new { error = "Dream not found." });
        }

        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to generate daily path for Dream {DreamId}: {Message}", dreamId, ex.Message);
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (GeminiTransientException ex)
    {
        logger.LogWarning(ex, "Gemini provider unavailable while generating daily path for Dream {DreamId}", dreamId);
        return GeminiProviderUnavailable(ex);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while generating daily path for Dream {DreamId}", dreamId);
        return Results.Json(new { error = "An unexpected error occurred while generating your daily path. Please try again later." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

dreamRoutes.MapPost("/{dreamId:guid}/daily-path/adaptive-change", async (
    Guid dreamId,
    ApplyDailyPathChangeRequest request,
    DailyPathService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.ApplyAdaptiveChangeAsync(dreamId, request, cancellationToken);
        return response is null
            ? Results.NotFound(new { error = "Dream not found." })
            : Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogInformation(ex, "Daily Path changed before adaptive adjustment for Dream {DreamId}", dreamId);
        return Results.Conflict(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while applying adaptive Daily Path change for Dream {DreamId}", dreamId);
        return Results.Json(new { error = "An unexpected error occurred while updating today's path." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

dreamRoutes.MapPatch("/{dreamId:guid}/daily-actions/{actionId:guid}", async (
    Guid dreamId,
    Guid actionId,
    UpdateDailyActionStatusRequest request,
    DailyPathService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.UpdateActionStatusAsync(dreamId, actionId, request.Status, cancellationToken);
        if (response is null)
        {
            return Results.NotFound(new { error = "Daily action not found." });
        }

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while updating daily action {ActionId} for Dream {DreamId}", actionId, dreamId);
        return Results.Json(new { error = "An unexpected error occurred while updating the daily action." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

dreamRoutes.MapGet("/{dreamId:guid}/journey-calendar", async (
    Guid dreamId,
    int year,
    int month,
    JourneyCalendarService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.GetMonthAsync(dreamId, year, month, cancellationToken);
        return response is null
            ? Results.NotFound(new { error = "Dream not found." })
            : Results.Ok(response);
    }
    catch (ArgumentOutOfRangeException)
    {
        return Results.BadRequest(new { error = "Year and month must form a valid calendar month." });
    }
});

dreamRoutes.MapGet("/{dreamId:guid}/journey-calendar/{planDate:datetime}", async (
    Guid dreamId,
    DateOnly planDate,
    JourneyCalendarService service,
    CancellationToken cancellationToken) =>
{
    var response = await service.GetDayAsync(dreamId, planDate, cancellationToken);
    return response is null
        ? Results.NotFound(new { error = "Daily plan snapshot not found." })
        : Results.Ok(response);
});

dreamRoutes.MapPost("/{dreamId:guid}/reflections", async (
    Guid dreamId,
    CreateReflectionRequest request,
    ReflectionService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.CreateReflectionAsync(dreamId, request, cancellationToken);
        if (response is null)
        {
            return Results.NotFound(new { error = "Dream not found." });
        }

        return Results.Created($"/dreams/{dreamId}/reflections/{response.Id}", response);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to create reflection for Dream {DreamId}: {Message}", dreamId, ex.Message);
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (GeminiTransientException ex)
    {
        logger.LogWarning(ex, "Gemini provider unavailable while creating reflection for Dream {DreamId}", dreamId);
        return GeminiProviderUnavailable(ex);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while creating reflection for Dream {DreamId}", dreamId);
        return Results.Json(new { error = "An unexpected error occurred while processing your reflection. Please try again later." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

dreamRoutes.MapGet("/{dreamId:guid}/reflections", async (
    Guid dreamId,
    ReflectionService service,
    CancellationToken cancellationToken) =>
{
    var responses = await service.GetReflectionsAsync(dreamId, cancellationToken);
    return Results.Ok(responses);
});

dreamRoutes.MapPost("/{dreamId:guid}/conversation", async (
    Guid dreamId,
    SendLumaMessageRequest request,
    TalkToLumaService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.SendMessageAsync(dreamId, request, cancellationToken);
        return response is null
            ? Results.NotFound(new { error = "Dream not found." })
            : Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to create LUMA conversation response for Dream {DreamId}", dreamId);
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (GeminiTransientException ex)
    {
        logger.LogWarning(ex, "Gemini provider unavailable during LUMA conversation for Dream {DreamId}", dreamId);
        return GeminiProviderUnavailable(ex);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during LUMA conversation for Dream {DreamId}", dreamId);
        return Results.Json(new { error = "An unexpected error occurred. Please try again later." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

var agentRoutes = app.MapGroup("/agent").RequireAuthorization();
agentRoutes.MapPost("/{dreamId:guid}/message", async (
    Guid dreamId,
    AgentMessageRequest request,
    ClaimsPrincipal principal,
    AgentService service,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message is required." });
    }

    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    try
    {
        Console.WriteLine($"[AGENT_TRACE] EndpointDreamId={dreamId}");
        var response = await service.ExecuteAsync(
            userId,
            new AgentRequest(dreamId, request.Message.Trim()),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.Error))
        {
            logger.LogWarning("Agent request was rejected: {Error}", response.Error);
            return Results.Json(new { error = "LUMA could not process that request." }, statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(MapAgentMessageResponse(response));
    }
    catch (AgentDreamNotFoundException)
    {
        return Results.NotFound(new { error = "Dream not found." });
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected Agent error for Dream {DreamId}", dreamId);
        return Results.Json(new { error = "LUMA could not respond right now." }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapLumaMcp("/mcp").RequireAuthorization();

app.Run();

static AgentMessageResponse MapAgentMessageResponse(AgentResponse response)
{
    var actionProposal = response.ActionProposal;
    if (actionProposal?.ProposalResult.Data is not UpdateDailyPlanResult updateResult)
    {
        return new AgentMessageResponse(
            response.Reply,
            response.ToolResults?
                .Where(result => result.Success)
                .Select(result => result.ToolName)
                .ToArray() ?? Array.Empty<string>());
    }

    return new AgentMessageResponse(
        response.Reply,
        response.ToolResults?
            .Where(result => result.Success)
            .Select(result => result.ToolName)
            .ToArray() ?? Array.Empty<string>(),
        new AgentActionProposalResponse(
            actionProposal.ActionName,
            actionProposal.Reason,
            RequiresApproval: true,
            updateResult.Proposal,
            updateResult.Message));
}

static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
{
    var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(subject, out userId);
}

static IResult GeminiProviderUnavailable(GeminiTransientException exception)
{
    return Results.Json(
        new
        {
            error = exception.Message,
            errorType = "ai_provider_unavailable"
        },
        statusCode: StatusCodes.Status503ServiceUnavailable);
}
