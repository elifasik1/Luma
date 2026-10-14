using Luma.Application.Agent;
using Luma.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Luma.Infrastructure.Persistence;

public sealed class ReflectionReadQuery : IReflectionReadQuery
{
    private readonly LumaDbContext _dbContext;

    public ReflectionReadQuery(LumaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReflectionReadResult?> GetRecentAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        var dreamExists = await _dbContext.Dreams
            .AsNoTracking()
            .AnyAsync(dream => dream.Id == dreamId, cancellationToken);

        if (!dreamExists)
        {
            return null;
        }

        var reflections = await _dbContext.Reflections
            .AsNoTracking()
            .Where(reflection => reflection.DreamId == dreamId)
            .OrderByDescending(reflection => reflection.CreatedAt)
            .Take(RecentReflectionPolicy.Limit)
            .Select(reflection => new ReflectionContext(
                reflection.Id,
                reflection.Mood,
                reflection.Notes,
                reflection.Insight,
                reflection.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new ReflectionReadResult(reflections);
    }
}
