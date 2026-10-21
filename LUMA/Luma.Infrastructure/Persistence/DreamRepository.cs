using Luma.Application.Interfaces;
using Luma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Luma.Infrastructure.Persistence;

public class DreamRepository : IDreamRepository
{
    private readonly LumaDbContext _dbContext;

    public DreamRepository(LumaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dream> AddAsync(Dream dream, CancellationToken cancellationToken = default)
    {
        _dbContext.Dreams.Add(dream);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return dream;
    }

    public async Task<Dream?> GetByIdAsync(Guid dreamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Dreams
            .Include(d => d.User)
            .Include(d => d.FutureProfile)
                .ThenInclude(fp => fp!.ThemeConfig)
            .Include(d => d.FutureScenario)
            .Include(d => d.Goals.OrderBy(g => g.Order))
            .Include(d => d.DailyActions.OrderBy(a => a.Order))
            .Include(d => d.Reflections.OrderByDescending(r => r.CreatedAt))
            .FirstOrDefaultAsync(d => d.Id == dreamId, cancellationToken);
    }

    public Task<bool> IsOwnedByAsync(
        Guid dreamId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Dreams.AnyAsync(
            dream => dream.Id == dreamId && dream.UserId == userId,
            cancellationToken);
    }

    public async Task<FutureScenario> AddScenarioAsync(FutureScenario scenario, CancellationToken cancellationToken = default)
    {
        _dbContext.FutureScenarios.Add(scenario);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return scenario;
    }

    public async Task<IReadOnlyList<Goal>> AddGoalsAsync(IEnumerable<Goal> goals, CancellationToken cancellationToken = default)
    {
        var list = goals.ToList();
        _dbContext.Goals.AddRange(list);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return list;
    }

    public async Task<IReadOnlyList<DailyAction>> AddDailyActionsAsync(IEnumerable<DailyAction> actions, CancellationToken cancellationToken = default)
    {
        var list = actions.ToList();
        _dbContext.DailyActions.AddRange(list);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return list;
    }

    public async Task DeleteDailyActionsAsync(Guid dreamId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DailyActions
            .Where(a => a.DreamId == dreamId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.DailyActions.RemoveRange(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<DailyPlanSnapshot?> GetDailyPlanSnapshotAsync(
        Guid dreamId,
        DateOnly planDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DailyPlanSnapshots
            .AsNoTracking()
            .Include(snapshot => snapshot.Actions.OrderBy(action => action.Order))
            .FirstOrDefaultAsync(
                snapshot => snapshot.DreamId == dreamId && snapshot.PlanDate == planDate,
                cancellationToken);
    }

    public Task<DailyPlanSnapshot?> GetLatestDailyPlanSnapshotAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DailyPlanSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.DreamId == dreamId)
            .OrderByDescending(snapshot => snapshot.PlanDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DailyPlanSnapshot> AddDailyPlanSnapshotAsync(
        DailyPlanSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        _dbContext.DailyPlanSnapshots.Add(snapshot);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return snapshot;
        }
        catch (DbUpdateException exception) when (IsDailyPlanDateConflict(exception))
        {
            _dbContext.ChangeTracker.Clear();
            var existingSnapshot = await GetDailyPlanSnapshotAsync(
                snapshot.DreamId,
                snapshot.PlanDate,
                cancellationToken);
            if (existingSnapshot is null)
            {
                throw;
            }

            return existingSnapshot;
        }
    }

    public async Task<IReadOnlyList<DailyAction>> ReplaceDailyActionsAndSnapshotAsync(
        DailyPlanSnapshot snapshot,
        IReadOnlyCollection<DailyAction> actions,
        CancellationToken cancellationToken = default)
    {
        // The caller has already read the dream graph. Start this replacement with
        // one tracked copy of each row so a refresh cannot re-save stale children.
        _dbContext.ChangeTracker.Clear();

        var existingActions = await _dbContext.DailyActions
            .Where(action => action.DreamId == snapshot.DreamId)
            .ToListAsync(cancellationToken);

        var existingSnapshot = await _dbContext.DailyPlanSnapshots
            .Include(plan => plan.Actions)
            .FirstOrDefaultAsync(
                plan => plan.DreamId == snapshot.DreamId && plan.PlanDate == snapshot.PlanDate,
                cancellationToken);

        if (existingSnapshot is null)
        {
            _dbContext.DailyActions.RemoveRange(existingActions);
            _dbContext.DailyPlanSnapshots.Add(snapshot);
            _dbContext.DailyActions.AddRange(actions);
        }
        else
        {
            // A snapshot is the durable record for its date. A same-day refresh may
            // replace the live path, but must never rewrite the recorded plan.
            _dbContext.DailyActions.RemoveRange(existingActions);
            _dbContext.DailyActions.AddRange(actions);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return actions.OrderBy(action => action.Order).ToArray();
        }
        catch (DbUpdateException exception) when (IsDailyPlanDateConflict(exception))
        {
            _dbContext.ChangeTracker.Clear();
            return await _dbContext.DailyActions
                .AsNoTracking()
                .Where(action => action.DreamId == snapshot.DreamId)
                .OrderBy(action => action.Order)
                .ToListAsync(cancellationToken);
        }
    }

    public async Task<DailyAction?> UpdateDailyActionStatusAsync(
        Guid dreamId,
        Guid actionId,
        string status,
        DateOnly? scheduledDate,
        DateOnly planDate,
        CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.DailyActions
            .FirstOrDefaultAsync(a => a.DreamId == dreamId && a.Id == actionId, cancellationToken);

        if (action is null)
        {
            return null;
        }

        action.Status = status;
        if (scheduledDate.HasValue)
        {
            action.ScheduledDate = scheduledDate.Value;
        }
        if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            action.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            action.CompletedAt = null;
        }

        var snapshotAction = await _dbContext.DailyPlanActionSnapshots
            .Include(snapshotAction => snapshotAction.DailyPlanSnapshot)
            .FirstOrDefaultAsync(
                snapshotAction => snapshotAction.SourceDailyActionId == actionId &&
                                  snapshotAction.DailyPlanSnapshot!.DreamId == dreamId &&
                                  snapshotAction.DailyPlanSnapshot.PlanDate == planDate,
                cancellationToken);
        if (snapshotAction is not null)
        {
            snapshotAction.Status = action.Status;
            snapshotAction.CompletedAt = action.CompletedAt;
        }
        else
        {
            var fallbackSnapshotAction = await _dbContext.DailyPlanActionSnapshots
                .Include(sa => sa.DailyPlanSnapshot)
                .FirstOrDefaultAsync(
                    sa => sa.DailyPlanSnapshot!.DreamId == dreamId &&
                          sa.DailyPlanSnapshot.PlanDate == planDate &&
                          (sa.Order == action.Order || sa.Title == action.Title),
                    cancellationToken);
            if (fallbackSnapshotAction is not null)
            {
                fallbackSnapshotAction.SourceDailyActionId = action.Id;
                fallbackSnapshotAction.Status = action.Status;
                fallbackSnapshotAction.CompletedAt = action.CompletedAt;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return action;
    }

    public async Task SaveFutureProfileAsync(
        FutureProfile profile,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<FutureProfile?> UpdateTargetYearAsync(
        Guid dreamId,
        int targetYear,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.FutureProfiles
            .FirstOrDefaultAsync(item => item.DreamId == dreamId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        profile.TargetYear = targetYear;
        profile.TargetDate = new DateOnly(targetYear, 1, 1);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<FutureProfile?> UpdateTargetDateAsync(
        Guid dreamId,
        DateOnly targetDate,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.FutureProfiles
            .FirstOrDefaultAsync(item => item.DreamId == dreamId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        profile.TargetDate = targetDate;
        profile.TargetYear = targetDate.Year;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<IReadOnlyList<DailyAction>?> ApplyDailyPathChangeAsync(
        Guid dreamId,
        Guid keepActionId,
        IReadOnlyCollection<Guid> restActionIds,
        DateOnly planDate,
        CancellationToken cancellationToken = default)
    {
        var requestedIds = restActionIds.Append(keepActionId).ToArray();
        if (requestedIds.Distinct().Count() != requestedIds.Length)
        {
            return null;
        }

        var actions = await _dbContext.DailyActions
            .Where(action => action.DreamId == dreamId && requestedIds.Contains(action.Id))
            .ToListAsync(cancellationToken);

        if (actions.Count != requestedIds.Length ||
            actions.Any(action => !string.IsNullOrWhiteSpace(action.Status) && !string.Equals(action.Status, "Pending", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        foreach (var action in actions)
        {
            if (action.Id == keepActionId)
            {
                action.Status = "Pending";
                action.CompletedAt = null;
            }
            else
            {
                action.Status = "Skipped";
                action.CompletedAt = null;
            }
        }

        var actionById = actions.ToDictionary(action => action.Id);
        var snapshotActions = await _dbContext.DailyPlanActionSnapshots
            .Include(snapshotAction => snapshotAction.DailyPlanSnapshot)
            .Where(snapshotAction => snapshotAction.SourceDailyActionId.HasValue &&
                                     requestedIds.Contains(snapshotAction.SourceDailyActionId.Value) &&
                                     snapshotAction.DailyPlanSnapshot!.DreamId == dreamId &&
                                     snapshotAction.DailyPlanSnapshot.PlanDate == planDate)
            .ToListAsync(cancellationToken);
        foreach (var snapshotAction in snapshotActions)
        {
            var sourceAction = actionById[snapshotAction.SourceDailyActionId!.Value];
            snapshotAction.Status = sourceAction.Status;
            snapshotAction.CompletedAt = sourceAction.CompletedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext.DailyActions
            .Where(action => action.DreamId == dreamId)
            .OrderBy(action => action.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<DreamReflection> AddReflectionAsync(DreamReflection reflection, CancellationToken cancellationToken = default)
    {
        _dbContext.Reflections.Add(reflection);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return reflection;
    }

    public async Task<IReadOnlyList<DreamReflection>> GetReflectionsAsync(Guid dreamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reflections
            .Where(r => r.DreamId == dreamId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<DreamReflection?> GetReflectionAsync(Guid dreamId, Guid reflectionId, CancellationToken cancellationToken = default) =>
        _dbContext.Reflections.FirstOrDefaultAsync(
            reflection => reflection.DreamId == dreamId && reflection.Id == reflectionId,
            cancellationToken);

    public async Task<DreamReflection?> UpdateReflectionAsync(
        Guid dreamId,
        Guid reflectionId,
        string mood,
        string notes,
        CancellationToken cancellationToken = default)
    {
        var reflection = await GetReflectionAsync(dreamId, reflectionId, cancellationToken);
        if (reflection is null) return null;

        reflection.Mood = mood;
        reflection.Notes = notes;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return reflection;
    }

    private static bool IsDailyPlanDateConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName == "IX_DailyPlanSnapshots_DreamId_PlanDate")
            {
                return true;
            }
        }

        return false;
    }
}
