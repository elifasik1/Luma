using Luma.Application.Agent;
using Luma.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Luma.Infrastructure.Persistence;

public sealed class DailyPlanReadQuery : IDailyPlanReadQuery
{
    private readonly LumaDbContext _dbContext;

    public DailyPlanReadQuery(LumaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyPlanReadResult?> GetAsync(
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

        var actions = await _dbContext.DailyActions
            .AsNoTracking()
            .Where(action => action.DreamId == dreamId)
            .OrderBy(action => action.Order)
            .Select(action => new DailyActionContext(
                action.Id,
                action.GoalId,
                action.GoalTitle,
                action.Title,
                action.Description,
                action.TimeOfDay,
                action.EstimatedMinutes,
                action.Intention,
                action.Order,
                action.Status,
                action.CompletedAt,
                action.GeneratedAt))
            .ToArrayAsync(cancellationToken);

        return new DailyPlanReadResult(actions);
    }
}
