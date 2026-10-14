using Luma.Domain.Entities;

namespace Luma.Application.Interfaces;

public interface IDreamRepository
{
    Task<Dream> AddAsync(Dream dream, CancellationToken cancellationToken = default);
    Task<Dream?> GetByIdAsync(Guid dreamId, CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByAsync(Guid dreamId, Guid userId, CancellationToken cancellationToken = default);
    Task<FutureScenario> AddScenarioAsync(FutureScenario scenario, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Goal>> AddGoalsAsync(IEnumerable<Goal> goals, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyAction>> AddDailyActionsAsync(IEnumerable<DailyAction> actions, CancellationToken cancellationToken = default);
    Task DeleteDailyActionsAsync(Guid dreamId, CancellationToken cancellationToken = default);
    Task<DailyPlanSnapshot?> GetDailyPlanSnapshotAsync(Guid dreamId, DateOnly planDate, CancellationToken cancellationToken = default);
    Task<DailyPlanSnapshot?> GetLatestDailyPlanSnapshotAsync(Guid dreamId, CancellationToken cancellationToken = default);
    Task<DailyPlanSnapshot> AddDailyPlanSnapshotAsync(DailyPlanSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyAction>> ReplaceDailyActionsAndSnapshotAsync(
        DailyPlanSnapshot snapshot,
        IReadOnlyCollection<DailyAction> actions,
        CancellationToken cancellationToken = default);
    Task<DailyAction?> UpdateDailyActionStatusAsync(Guid dreamId, Guid actionId, string status, DateOnly planDate, CancellationToken cancellationToken = default);
    Task SaveFutureProfileAsync(FutureProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyAction>?> ApplyDailyPathChangeAsync(
        Guid dreamId,
        Guid keepActionId,
        IReadOnlyCollection<Guid> restActionIds,
        DateOnly planDate,
        CancellationToken cancellationToken = default);
    Task<DreamReflection> AddReflectionAsync(DreamReflection reflection, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DreamReflection>> GetReflectionsAsync(Guid dreamId, CancellationToken cancellationToken = default);
}
