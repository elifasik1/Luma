using Luma.Domain.Entities;

namespace Luma.Application.AI.Models;

public sealed record AnalyzeDreamResult(
    FutureProfileAnalysis FutureProfile,
    ThemeConfigAnalysis ThemeConfig,
    string? Summary = null);

public sealed record FutureProfileAnalysis(
    string Identity,
    string Environment,
    IReadOnlyList<string> Lifestyle,
    IReadOnlyList<string> Aspirations,
    IReadOnlyList<string> Values,
    string Mood,
    string Pace);

public sealed record ThemeConfigAnalysis(
    ThemePalette Palette,
    ThemeAtmosphere Atmosphere,
    ThemeIllustration Illustration,
    ThemeMotion Motion,
    IReadOnlyList<ThemeImagery> Imagery);

public enum ThemePalette
{
    WarmDream,
    LavenderMilk,
    SunsetCream,
    CityNight,
    ForestMist,
    OceanLight,
    SocialSunrise,
    AmberSummit,
    MakerWorkshop
}

public enum ThemeAtmosphere
{
    Airy,
    Cozy,
    Energetic,
    Calm,
    Creative,
    Focused
}

public enum ThemeIllustration
{
    Dreamer,
    UrbanDreamer,
    CreativeDreamer,
    NatureDreamer,
    ProfessionalDreamer
}

public enum ThemeMotion
{
    Soft,
    Gentle,
    Floating,
    Still
}
