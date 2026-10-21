namespace Luma.Application.Models;

public sealed record DailyPathResponse(
    IReadOnlyList<DailyActionResponse> Actions,
    DateTime GeneratedAt,
    int TotalActions,
    int CompletedActions,
    int SkippedActions,
    string MomentumMessage);

public sealed record DailyActionResponse(
    Guid Id,
    Guid DreamId,
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
    DateOnly ScheduledDate);
