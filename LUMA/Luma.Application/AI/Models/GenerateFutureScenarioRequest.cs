namespace Luma.Application.AI.Models;

public sealed record GenerateFutureScenarioRequest(
    Guid UserId,
    string DreamTitle,
    string DreamDescription,
    string Identity,
    string Environment,
    IReadOnlyList<string> Lifestyle,
    IReadOnlyList<string> Aspirations,
    IReadOnlyList<string> Values,
    string Mood,
    string Pace,
    int TargetYear,
    string Atmosphere,
    IReadOnlyList<string> Imagery);
