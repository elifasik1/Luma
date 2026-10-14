using Luma.Application.Agent;

namespace Luma.Application.Interfaces;

public interface IDailyPlanReadQuery
{
    Task<DailyPlanReadResult?> GetAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default);
}
