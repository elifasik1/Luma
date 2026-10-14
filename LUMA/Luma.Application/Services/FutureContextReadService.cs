using Luma.Application.Agent;
using Luma.Application.Interfaces;

namespace Luma.Application.Services;

public sealed class FutureContextReadService
{
    private readonly IFutureContextQuery _query;

    public FutureContextReadService(IFutureContextQuery query)
    {
        _query = query;
    }

    public Task<FutureContextResult?> GetAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return _query.GetAsync(context.DreamId, cancellationToken);
    }
}
