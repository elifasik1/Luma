namespace Luma.Application.Agent;

public interface IAgentPlanner
{
    Task<AgentPlan> PlanAsync(
        AgentPlannerRequest request,
        IReadOnlyList<AgentToolMetadata> availableTools,
        CancellationToken cancellationToken = default);
}
