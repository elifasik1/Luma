using Luma.Application.Agent;
using Luma.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Luma.Infrastructure.Persistence;

public sealed class GoalReadQuery : IGoalReadQuery
{
    private readonly LumaDbContext _dbContext;

    public GoalReadQuery(LumaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GoalReadResult?> GetAsync(
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

        var goals = await _dbContext.Goals
            .AsNoTracking()
            .Where(goal => goal.DreamId == dreamId)
            .OrderBy(goal => goal.Order)
            .Select(goal => new ActiveGoalContext(
                goal.Id,
                goal.Title,
                goal.Description,
                goal.Category,
                goal.WhyItMatters,
                goal.DesiredOutcome,
                goal.CurrentState,
                goal.Gap,
                goal.Status,
                goal.Order))
            .ToArrayAsync(cancellationToken);

        return new GoalReadResult(goals);
    }
}
