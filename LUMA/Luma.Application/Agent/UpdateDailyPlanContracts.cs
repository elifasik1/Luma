using Luma.Application.Models;

namespace Luma.Application.Agent;

public sealed record UpdateDailyPlanRequest(
    string UserSituation,
    bool Approve = false,
    DailyPathChangeProposal? Proposal = null);

public sealed record UpdateDailyPlanResult(
    bool Applied,
    bool RequiresApproval,
    DailyPathChangeProposal? Proposal,
    DailyPathResponse? UpdatedPlan,
    string Message);

public interface IUpdateDailyPlanTool : IAgentActionTool
{
    Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        UpdateDailyPlanRequest request,
        CancellationToken cancellationToken = default);
}
