using Luma.Application.Agent;
using Luma.Application.Models;

namespace Luma.Application.Interfaces;

public interface IAdaptiveDailyPlanCapability
{
    Task<DailyPathChangeProposal?> ProposeAsync(
        AgentExecutionContext context,
        string userSituation,
        CancellationToken cancellationToken = default);

    Task<DailyPathResponse?> ApplyAsync(
        AgentExecutionContext context,
        DailyPathChangeProposal proposal,
        CancellationToken cancellationToken = default);
}
