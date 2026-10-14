using Luma.Application.Interfaces;

namespace Luma.Infrastructure.Persistence;

public sealed class AgentDreamAccess : IAgentDreamAccess
{
    private readonly IDreamRepository _dreamRepository;

    public AgentDreamAccess(IDreamRepository dreamRepository)
    {
        _dreamRepository = dreamRepository;
    }

    public Task<bool> IsOwnedByAsync(
        Guid dreamId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dreamRepository.IsOwnedByAsync(dreamId, userId, cancellationToken);
    }
}
