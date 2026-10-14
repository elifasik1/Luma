namespace Luma.Application.AI.Models;

public sealed record GenerateGoalsRequest(
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
    string? ScenarioTitle,
    string? ScenarioIntro,
    string? ScenarioDaytime,
    string? ScenarioEmotionalTone);
