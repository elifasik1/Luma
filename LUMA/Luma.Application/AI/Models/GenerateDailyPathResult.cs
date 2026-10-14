namespace Luma.Application.AI.Models;

public sealed record GenerateDailyPathResult(
    IReadOnlyList<DailyActionItem> Actions);

public sealed record DailyActionItem(
    string GoalTitle,
    string Title,
    string Description,
    string TimeOfDay,
    int EstimatedMinutes,
    string Intention,
    int Order);
