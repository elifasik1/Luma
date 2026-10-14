namespace Luma.Application.AI.Models;

public sealed record GenerateFutureScenarioResult(
    string Title,
    string Intro,
    string Morning,
    string Daytime,
    string Evening,
    string EmotionalTone,
    string ClosingReflection);
