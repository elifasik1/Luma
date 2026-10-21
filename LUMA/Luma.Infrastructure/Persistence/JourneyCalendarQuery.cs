using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Luma.Infrastructure.Persistence;

public sealed class JourneyCalendarQuery : IJourneyCalendarQuery
{
    private readonly LumaDbContext _dbContext;

    public JourneyCalendarQuery(LumaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JourneyCalendarMonthResponse?> GetMonthAsync(
        Guid dreamId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        if (!await DreamExistsAsync(dreamId, cancellationToken))
        {
            return null;
        }

        var firstDate = new DateOnly(year, month, 1);
        var lastDate = firstDate.AddMonths(1).AddDays(-1);
        var snapshots = await _dbContext.DailyPlanSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.DreamId == dreamId &&
                               snapshot.PlanDate >= firstDate &&
                               snapshot.PlanDate <= lastDate)
            .Include(snapshot => snapshot.Actions)
            .Include(snapshot => snapshot.Reflections)
            .ToListAsync(cancellationToken);
        var reflections = await _dbContext.Reflections
            .AsNoTracking()
            .Where(reflection => reflection.DreamId == dreamId &&
                                 reflection.PlanDate >= firstDate &&
                                 reflection.PlanDate <= lastDate)
            .ToListAsync(cancellationToken);

        var snapshotsByDate = snapshots.ToDictionary(snapshot => snapshot.PlanDate);
        var reflectionsByDate = reflections.GroupBy(reflection => reflection.PlanDate)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var days = Enumerable.Range(0, lastDate.Day)
            .Select(offset => firstDate.AddDays(offset))
            .Select(date => snapshotsByDate.TryGetValue(date, out var snapshot)
                ? MapSummary(snapshot, dreamId, reflectionsByDate.GetValueOrDefault(date) ?? Array.Empty<DreamReflection>())
                : new JourneyCalendarDaySummaryResponse(
                    date,
                    false,
                    0,
                    0,
                    MapReflections(reflectionsByDate.GetValueOrDefault(date) ?? Array.Empty<DreamReflection>())) )
            .ToArray();

        return new JourneyCalendarMonthResponse(year, month, days);
    }

    public async Task<JourneyCalendarDayResponse?> GetDayAsync(
        Guid dreamId,
        DateOnly planDate,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _dbContext.DailyPlanSnapshots
            .AsNoTracking()
            .Where(item => item.DreamId == dreamId && item.PlanDate == planDate)
            .Include(item => item.Actions)
            .Include(item => item.Reflections)
            .SingleOrDefaultAsync(cancellationToken);

        var reflections = await _dbContext.Reflections
            .AsNoTracking()
            .Where(reflection => reflection.DreamId == dreamId && reflection.PlanDate == planDate)
            .ToListAsync(cancellationToken);

        return snapshot is null
            ? (reflections.Count == 0 ? null : new JourneyCalendarDayResponse(null, planDate, null, Array.Empty<JourneySnapshotActionResponse>(), MapReflections(reflections)))
            : MapDay(snapshot, dreamId);
    }

    private Task<bool> DreamExistsAsync(Guid dreamId, CancellationToken cancellationToken) =>
        _dbContext.Dreams.AsNoTracking().AnyAsync(dream => dream.Id == dreamId, cancellationToken);

    private static JourneyCalendarDaySummaryResponse MapSummary(
        Luma.Domain.Entities.DailyPlanSnapshot snapshot,
        Guid dreamId,
        IEnumerable<DreamReflection> reflections) =>
        new(
            snapshot.PlanDate,
            true,
            snapshot.Actions.Count,
            snapshot.Actions.Count(action => string.Equals(action.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            MapReflections(snapshot.Reflections
                .Concat(reflections)
                .Where(reflection => reflection.DreamId == dreamId && reflection.PlanDate == snapshot.PlanDate)
                .GroupBy(reflection => reflection.Id)
                .Select(group => group.First())));

    private static JourneyCalendarDayResponse MapDay(
        Luma.Domain.Entities.DailyPlanSnapshot snapshot,
        Guid dreamId) =>
        new(
            snapshot.Id,
            snapshot.PlanDate,
            snapshot.GeneratedAt,
            snapshot.Actions
                .OrderBy(action => action.Order)
                .Select(action => new JourneySnapshotActionResponse(
                    action.Id,
                    action.GoalId,
                    action.GoalTitle,
                    action.Title,
                    action.Description,
                    action.TimeOfDay,
                    action.EstimatedMinutes,
                    action.Intention,
                    action.Order,
                    string.IsNullOrWhiteSpace(action.Status) ? "Pending" : action.Status,
                    action.CompletedAt))
                .ToArray(),
            MapReflections(snapshot.Reflections.Where(reflection => reflection.DreamId == dreamId && reflection.PlanDate == snapshot.PlanDate)));

    private static IReadOnlyList<JourneyReflectionSummaryResponse> MapReflections(
        IEnumerable<DreamReflection> reflections) =>
        reflections
            .OrderBy(reflection => reflection.CreatedAt)
            .Select(reflection => new JourneyReflectionSummaryResponse(
                reflection.Id,
                reflection.Mood,
                reflection.Notes,
                reflection.Insight,
                reflection.PlanDate,
                reflection.CreatedAt))
            .ToArray();
}
