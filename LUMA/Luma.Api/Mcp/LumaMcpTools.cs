using System.ComponentModel;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Luma.Application.Agent;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Application.Services;
using Luma.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Luma.Api.Mcp;

/// <summary>
/// Exposes LUMA Agent capabilities as standardized MCP tools over Streamable HTTP.
/// Uses existing IAgentTool, AgentService, and IUpdateDailyPlanTool implementations to maintain
/// consistent business logic, ownership checks, and approval workflows.
/// </summary>
[McpServerToolType]
public sealed class LumaMcpTools
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LumaDbContext _dbContext;
    private readonly AgentService _agentService;
    private readonly IAgentToolRegistry _toolRegistry;
    private readonly IUpdateDailyPlanTool _updateDailyPlanTool;
    private readonly ILogger<LumaMcpTools> _logger;

    public LumaMcpTools(
        IHttpContextAccessor httpContextAccessor,
        LumaDbContext dbContext,
        AgentService agentService,
        IAgentToolRegistry toolRegistry,
        IUpdateDailyPlanTool updateDailyPlanTool,
        ILogger<LumaMcpTools> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
        _agentService = agentService;
        _toolRegistry = toolRegistry;
        _updateDailyPlanTool = updateDailyPlanTool;
        _logger = logger;
    }

    [McpServerTool(Name = "get_future_context"), Description("Reads the owned Dream's future profile and optional future scenario without creating or changing data.")]
    public async Task<object?> GetFutureContextAsync(
        [Description("The unique identifier of the user's dream/world. Optional if user has a dream.")] string? dreamId = null,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveExecutionContextAsync(dreamId, cancellationToken);
        var tool = _toolRegistry.Get("get_future_context")
            ?? throw new McpException("Tool 'get_future_context' is not registered.");

        var result = await tool.ExecuteAsync(context, cancellationToken);
        return result.Data;
    }

    [McpServerTool(Name = "get_active_goals"), Description("Reads the owned Dream's incomplete goals and current gaps without creating or changing data.")]
    public async Task<object?> GetActiveGoalsAsync(
        [Description("The unique identifier of the user's dream/world. Optional if user has a dream.")] string? dreamId = null,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveExecutionContextAsync(dreamId, cancellationToken);
        var tool = _toolRegistry.Get("get_active_goals")
            ?? throw new McpException("Tool 'get_active_goals' is not registered.");

        var result = await tool.ExecuteAsync(context, cancellationToken);
        return result.Data;
    }

    [McpServerTool(Name = "get_daily_plan"), Description("Reads the owned Dream's existing daily plan without creating or changing actions.")]
    public async Task<object?> GetDailyPlanAsync(
        [Description("The unique identifier of the user's dream/world. Optional if user has a dream.")] string? dreamId = null,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveExecutionContextAsync(dreamId, cancellationToken);
        var tool = _toolRegistry.Get("get_daily_plan")
            ?? throw new McpException("Tool 'get_daily_plan' is not registered.");

        var result = await tool.ExecuteAsync(context, cancellationToken);
        return result.Data;
    }

    [McpServerTool(Name = "get_progress"), Description("Reads progress calculated from the owned Dream's existing daily actions without changing data.")]
    public async Task<object?> GetProgressAsync(
        [Description("The unique identifier of the user's dream/world. Optional if user has a dream.")] string? dreamId = null,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveExecutionContextAsync(dreamId, cancellationToken);
        var tool = _toolRegistry.Get("get_progress")
            ?? throw new McpException("Tool 'get_progress' is not registered.");

        var result = await tool.ExecuteAsync(context, cancellationToken);
        return result.Data;
    }

    [McpServerTool(Name = "get_recent_reflections"), Description("Reads the most recent persisted reflections for the owned Dream without creating or changing data.")]
    public async Task<object?> GetRecentReflectionsAsync(
        [Description("The unique identifier of the user's dream/world. Optional if user has a dream.")] string? dreamId = null,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveExecutionContextAsync(dreamId, cancellationToken);
        var tool = _toolRegistry.Get("get_recent_reflections")
            ?? throw new McpException("Tool 'get_recent_reflections' is not registered.");

        var result = await tool.ExecuteAsync(context, cancellationToken);
        return result.Data;
    }

    [McpServerTool(Name = "update_daily_plan"), Description("Proposes an adaptive daily-plan change and applies it only after explicit approval.")]
    public async Task<object?> UpdateDailyPlanAsync(
        [Description("Description of the user's current situation, constraints, or time available (e.g., 'feeling tired, only have 15 minutes'). Required.")] string userSituation,
        [Description("Whether to apply the proposed change. Must be false for the initial proposal; set to true ONLY when user explicitly approved.")] bool approve = false,
        [Description("The GUID of the action to keep. Required when approve is true.")] string? keepActionId = null,
        [Description("Comma-separated GUIDs of actions to rest. Required when approve is true.")] string? restActionIds = null,
        [Description("The unique identifier of the user's dream/world. Optional if user has a dream.")] string? dreamId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userSituation))
        {
            throw new McpException("A user situation description is required.");
        }

        var context = await ResolveExecutionContextAsync(dreamId, cancellationToken);

        if (!approve)
        {
            // Phase 1: Propose only. No database modifications allowed.
            var proposalResult = await _updateDailyPlanTool.ExecuteAsync(
                context,
                new UpdateDailyPlanRequest(userSituation, Approve: false),
                cancellationToken);

            var updateResult = proposalResult.Data as UpdateDailyPlanResult;
            if (updateResult?.Proposal is null)
            {
                return new
                {
                    applied = false,
                    requiresApproval = false,
                    proposal = (object?)null,
                    message = updateResult?.Message ?? "No eligible pending actions are available for an adaptive proposal."
                };
            }

            return new
            {
                applied = false,
                requiresApproval = true,
                proposal = new
                {
                    type = updateResult.Proposal.Type,
                    keepActionId = updateResult.Proposal.KeepActionId,
                    keepActionTitle = updateResult.Proposal.KeepActionTitle,
                    keepActionEstimatedMinutes = updateResult.Proposal.KeepActionEstimatedMinutes,
                    restActionIds = updateResult.Proposal.RestActionIds
                },
                message = updateResult.Message,
                instruction = "Explicit user approval is required before applying this change. Confirm with the user, then call update_daily_plan with approve=true, keepActionId, and restActionIds."
            };
        }

        // Phase 2: Apply approved proposal.
        if (string.IsNullOrWhiteSpace(keepActionId) || string.IsNullOrWhiteSpace(restActionIds))
        {
            throw new McpException("An existing adaptive daily-plan proposal (keepActionId and restActionIds) is required when approve is true.");
        }

        if (!Guid.TryParse(keepActionId, out var keepGuid))
        {
            throw new McpException($"Invalid keepActionId GUID: '{keepActionId}'.");
        }

        var restGuids = restActionIds
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        if (restGuids.Count == 0)
        {
            throw new McpException("At least one valid restActionId GUID is required when approve is true.");
        }

        var proposal = new DailyPathChangeProposal(
            "suggest_plan_change",
            keepGuid,
            string.Empty,
            0,
            restGuids);

        try
        {
            var applyResult = await _updateDailyPlanTool.ExecuteAsync(
                context,
                new UpdateDailyPlanRequest(userSituation, Approve: true, proposal),
                cancellationToken);

            var updateResult = applyResult.Data as UpdateDailyPlanResult;
            return new
            {
                applied = updateResult?.Applied ?? true,
                requiresApproval = false,
                message = updateResult?.Message ?? "The approved daily-plan change was applied.",
                updatedPlan = updateResult?.UpdatedPlan
            };
        }
        catch (ArgumentException ex)
        {
            throw new McpException($"Failed to apply plan change: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            throw new McpException($"Failed to apply plan change: {ex.Message}");
        }
    }

    private async Task<AgentExecutionContext> ResolveExecutionContextAsync(
        string? dreamIdString,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null || !TryGetUserId(httpContext.User, out var userId))
        {
            throw new McpException("Authentication required: valid Bearer token is missing or unauthorized.");
        }

        Guid dreamId;
        if (!string.IsNullOrWhiteSpace(dreamIdString))
        {
            if (!Guid.TryParse(dreamIdString, out dreamId))
            {
                throw new McpException($"Invalid dream ID format: '{dreamIdString}'. A valid GUID is required.");
            }
        }
        else
        {
            var latestDreamId = await _dbContext.Dreams
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => d.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestDreamId == Guid.Empty)
            {
                throw new McpException("No dream/world found for the authenticated user. Please create a dream first.");
            }

            dreamId = latestDreamId;
        }

        try
        {
            return await _agentService.AuthorizeAsync(userId, dreamId, cancellationToken);
        }
        catch (AgentDreamNotFoundException)
        {
            throw new McpException($"Dream '{dreamId}' not found or does not belong to the authenticated user.");
        }
    }

    private static bool TryGetUserId(ClaimsPrincipal? principal, out Guid userId)
    {
        userId = Guid.Empty;
        if (principal is null) return false;

        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(subject, out userId);
    }
}
