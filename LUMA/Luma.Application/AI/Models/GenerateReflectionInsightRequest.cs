namespace Luma.Application.AI.Models;

public sealed record GenerateReflectionInsightRequest(
    Guid UserId,
    string DreamTitle,
    string Identity,
    string ReflectionMood,
    string ReflectionNotes,
    IReadOnlyList<string> CompletedActions,
    IReadOnlyList<string> SkippedActions);
