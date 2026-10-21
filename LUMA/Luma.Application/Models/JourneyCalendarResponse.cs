namespace Luma.Application.Models;

public sealed record JourneyCalendarMonthResponse(
    int Year,
    int Month,
    IReadOnlyList<JourneyCalendarDaySummaryResponse> Days);

public sealed record JourneyCalendarDaySummaryResponse(
    DateOnly PlanDate,
    bool HasSnapshot,
    int ActionCount,
    int CompletedActionCount,
    IReadOnlyList<JourneyReflectionSummaryResponse> Reflections);

public sealed record JourneyCalendarDayResponse(
    Guid? SnapshotId,
    DateOnly PlanDate,
    DateTime? GeneratedAt,
    IReadOnlyList<JourneySnapshotActionResponse> Actions,
    IReadOnlyList<JourneyReflectionSummaryResponse> Reflections);

public sealed record JourneySnapshotActionResponse(
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
    DateTime? CompletedAt);

public sealed record JourneyReflectionSummaryResponse(
    Guid Id,
    string Mood,
    string Notes,
    string Insight,
    DateOnly PlanDate,
    DateTime CreatedAt);
