using Luma.Application.Agent;

namespace Luma.Application.Interfaces;

public interface IGoalReadQuery
{
    Task<GoalReadResult?> GetAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default);
}
