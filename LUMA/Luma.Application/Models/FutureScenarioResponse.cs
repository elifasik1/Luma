namespace Luma.Application.Models;

public sealed record FutureScenarioResponse(
    Guid Id,
    Guid DreamId,
    string Title,
    string Intro,
    string Morning,
    string Daytime,
    string Evening,
    string EmotionalTone,
    string ClosingReflection,
    DateTime CreatedAt);
