using Luma.Application.Agent;

namespace Luma.Application.Interfaces;

public interface IFutureContextQuery
{
    Task<FutureContextResult?> GetAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default);
}
