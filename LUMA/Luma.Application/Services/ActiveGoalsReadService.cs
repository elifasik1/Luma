using Luma.Application.Agent;
using Luma.Application.Interfaces;

namespace Luma.Application.Services;

public sealed class ActiveGoalsReadService
{
    private readonly IGoalReadQuery _query;

    public ActiveGoalsReadService(IGoalReadQuery query)
    {
        _query = query;
    }

    public async Task<ActiveGoalsResult?> GetAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _query.GetAsync(context.DreamId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        var activeGoals = result.Goals
            .Where(goal => !string.Equals(goal.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .OrderBy(goal => goal.Order)
            .ToArray();

        return new ActiveGoalsResult(activeGoals);
    }
}
