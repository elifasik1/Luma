namespace Luma.Application.AI.Models;

public sealed record GenerateGoalsResult(
    IReadOnlyList<GoalAnalysisItem> Goals);

public sealed record GoalAnalysisItem(
    string Title,
    string Description,
    string Category,
    string WhyItMatters,
    string DesiredOutcome,
    string CurrentState,
    string Gap,
    string Status,
    int Order);
