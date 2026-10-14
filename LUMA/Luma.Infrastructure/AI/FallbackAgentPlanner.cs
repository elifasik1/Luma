using Luma.Application.Agent;
using Microsoft.Extensions.Logging;

namespace Luma.Infrastructure.AI;

public sealed class FallbackAgentPlanner : IAgentPlanner
{
    private readonly GeminiAgentPlanner _primaryPlanner;
    private readonly DeterministicAgentPlanner _fallbackPlanner;
    private readonly ILogger<FallbackAgentPlanner> _logger;

    public FallbackAgentPlanner(
        GeminiAgentPlanner primaryPlanner,
        DeterministicAgentPlanner fallbackPlanner,
        ILogger<FallbackAgentPlanner> logger)
    {
        _primaryPlanner = primaryPlanner;
        _fallbackPlanner = fallbackPlanner;
        _logger = logger;
    }

    public async Task<AgentPlan> PlanAsync(
        AgentPlannerRequest request,
        IReadOnlyList<AgentToolMetadata> availableTools,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryPlanner.PlanAsync(request, availableTools, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini Agent planner unavailable ({ErrorType}). Falling back to deterministic planner.", ex.GetType().Name);
            return await _fallbackPlanner.PlanAsync(request, availableTools, cancellationToken);
        }
    }
}
