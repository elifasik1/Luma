namespace Luma.Application.AI.Models;

public sealed record GenerateDailyPathRequest(
    Guid UserId,
    string DreamTitle,
    string Identity,
    string Environment,
    string Mood,
    string Pace,
    IReadOnlyList<GoalSummary> Goals);

public sealed record GoalSummary(
    Guid GoalId,
    string Title,
    string Category,
    string Gap);
