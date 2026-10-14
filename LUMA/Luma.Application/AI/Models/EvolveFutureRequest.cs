namespace Luma.Application.AI.Models;

public sealed record EvolveFutureRequest(
    Guid UserId,
    string DreamTitle,
    string DreamDescription,
    string ReflectionNotes,
    string CurrentIdentity,
    string CurrentEnvironment,
    string CurrentSummary,
    string CurrentMood,
    string CurrentPace,
    IReadOnlyList<string> CurrentLifestyle,
    IReadOnlyList<string> CurrentAspirations,
    IReadOnlyList<string> CurrentValues);
