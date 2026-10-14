namespace Luma.Application.Agent;

public sealed record ActiveGoalsResult(
    IReadOnlyList<ActiveGoalContext> Goals);

public sealed record ActiveGoalContext(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string WhyItMatters,
    string DesiredOutcome,
    string CurrentState,
    string Gap,
    string Status,
    int Order);

public sealed record GoalReadResult(
    IReadOnlyList<ActiveGoalContext> Goals);
