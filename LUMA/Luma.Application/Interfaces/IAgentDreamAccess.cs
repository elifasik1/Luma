namespace Luma.Application.Interfaces;

public interface IAgentDreamAccess
{
    Task<bool> IsOwnedByAsync(
        Guid dreamId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
