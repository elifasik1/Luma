namespace Luma.Application.Agent;

public sealed record FutureContextResult(
    Guid DreamId,
    string Title,
    string Description,
    DateTime CreatedAt,
    FutureProfileContext? FutureProfile,
    FutureScenarioContext? FutureScenario);

public sealed record FutureProfileContext(
    string Identity,
    string Environment,
    IReadOnlyList<string> Lifestyle,
    IReadOnlyList<string> Aspirations,
    IReadOnlyList<string> Values,
    string Mood,
    string Pace,
    string? Summary,
    ThemeConfigContext? ThemeConfig,
    int TargetYear = 0);

public sealed record ThemeConfigContext(
    string Palette,
    string Atmosphere,
    IReadOnlyList<string> Imagery,
    string Illustration,
    string Motion,
    IReadOnlyList<string> CoreAnchorKeys);

public sealed record FutureScenarioContext(
    string Title,
    string Intro,
    string Morning,
    string Daytime,
    string Evening,
    string EmotionalTone,
    string ClosingReflection);
