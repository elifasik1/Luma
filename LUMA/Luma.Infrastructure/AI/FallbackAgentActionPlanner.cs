using Luma.Application.Agent;
using Microsoft.Extensions.Logging;

namespace Luma.Infrastructure.AI;

public sealed class FallbackAgentActionPlanner : IAgentActionPlanner
{
    private readonly GeminiAgentActionPlanner _primaryPlanner;
    private readonly DeterministicAgentActionPlanner _fallbackPlanner;
    private readonly ILogger<FallbackAgentActionPlanner> _logger;

    public FallbackAgentActionPlanner(
        GeminiAgentActionPlanner primaryPlanner,
        DeterministicAgentActionPlanner fallbackPlanner,
        ILogger<FallbackAgentActionPlanner> logger)
    {
        _primaryPlanner = primaryPlanner;
        _fallbackPlanner = fallbackPlanner;
        _logger = logger;
    }

    public async Task<AgentActionDecision> DecideAsync(
        AgentPlannerRequest request,
        AgentPlan plan,
        IReadOnlyList<AgentToolExecutionResult> toolResults,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryPlanner.DecideAsync(request, plan, toolResults, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini Agent action planner unavailable ({ErrorType}). Falling back to deterministic action planner.", ex.GetType().Name);
            return await _fallbackPlanner.DecideAsync(request, plan, toolResults, cancellationToken);
        }
    }
}
