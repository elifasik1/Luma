namespace Luma.Application.Agent;

public sealed record DailyPlanResult(
    DailyPlanContext? Plan);

public sealed record DailyPlanContext(
    IReadOnlyList<DailyActionContext> Actions,
    DateTime GeneratedAt,
    int TotalActions,
    int CompletedActions,
    int SkippedActions,
    int PendingActions,
    string MomentumMessage);

public sealed record DailyActionContext(
    Guid Id,
    Guid? GoalId,
    string GoalTitle,
    string Title,
    string Description,
    string TimeOfDay,
    int EstimatedMinutes,
    string Intention,
    int Order,
    string Status,
    DateTime? CompletedAt,
    DateTime GeneratedAt);

public sealed record DailyPlanReadResult(
    IReadOnlyList<DailyActionContext> Actions);
