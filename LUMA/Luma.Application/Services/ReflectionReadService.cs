using Luma.Application.Agent;
using Luma.Application.Interfaces;

namespace Luma.Application.Services;

public sealed class ReflectionReadService
{
    private readonly IReflectionReadQuery _query;

    public ReflectionReadService(IReflectionReadQuery query)
    {
        _query = query;
    }

    public async Task<RecentReflectionsResult?> GetRecentAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _query.GetRecentAsync(context.DreamId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        var reflections = result.Reflections
            .OrderByDescending(reflection => reflection.CreatedAt)
            .Take(RecentReflectionPolicy.Limit)
            .ToArray();

        return new RecentReflectionsResult(reflections, reflections.Length);
    }
}
