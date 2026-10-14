using Luma.Application.Agent;

namespace Luma.Application.Interfaces;

public interface IReflectionReadQuery
{
    Task<ReflectionReadResult?> GetRecentAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default);
}
