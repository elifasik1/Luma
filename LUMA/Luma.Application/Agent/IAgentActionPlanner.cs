namespace Luma.Application.Agent;

public interface IAgentActionPlanner
{
    Task<AgentActionDecision> DecideAsync(
        AgentPlannerRequest request,
        AgentPlan plan,
        IReadOnlyList<AgentToolExecutionResult> toolResults,
        CancellationToken cancellationToken = default);
}
