using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Domain.Entities;

namespace Luma.Application.Services;

public class DreamAnalysisService
{
    private sealed record DailyRhythmModifier(ThemeAtmosphere Atmosphere, ThemeMotion Motion);
    private sealed record CoreAnchorDefinition(string Label, IReadOnlyList<ThemeImagery> Imagery);

    private static readonly IReadOnlyDictionary<string, ThemeConfigAnalysis> VisualWorldThemes =
        new Dictionary<string, ThemeConfigAnalysis>(StringComparer.Ordinal)
        {
            ["calm_free"] = CreateTheme(ThemePalette.WarmDream, ThemeAtmosphere.Calm, ThemeMotion.Gentle, ThemeIllustration.Dreamer, ThemeImagery.Home, ThemeImagery.Ocean),
            ["big_city"] = CreateTheme(ThemePalette.CityNight, ThemeAtmosphere.Focused, ThemeMotion.Soft, ThemeIllustration.UrbanDreamer, ThemeImagery.City, ThemeImagery.Workspace, ThemeImagery.Technology),
            ["nature"] = CreateTheme(ThemePalette.ForestMist, ThemeAtmosphere.Calm, ThemeMotion.Gentle, ThemeIllustration.NatureDreamer, ThemeImagery.Nature, ThemeImagery.Home),
            ["creative"] = CreateTheme(ThemePalette.LavenderMilk, ThemeAtmosphere.Creative, ThemeMotion.Floating, ThemeIllustration.CreativeDreamer, ThemeImagery.Books, ThemeImagery.Home),
            ["social"] = CreateTheme(ThemePalette.SocialSunrise, ThemeAtmosphere.Cozy, ThemeMotion.Floating, ThemeIllustration.Dreamer, ThemeImagery.Bakery, ThemeImagery.Home, ThemeImagery.Travel),
            ["ambitious"] = CreateTheme(ThemePalette.AmberSummit, ThemeAtmosphere.Energetic, ThemeMotion.Soft, ThemeIllustration.ProfessionalDreamer, ThemeImagery.Workspace, ThemeImagery.Technology),
            ["learning"] = CreateTheme(ThemePalette.LavenderMilk, ThemeAtmosphere.Focused, ThemeMotion.Soft, ThemeIllustration.Dreamer, ThemeImagery.Books, ThemeImagery.Campus, ThemeImagery.Technology),
            ["exploring"] = CreateTheme(ThemePalette.OceanLight, ThemeAtmosphere.Airy, ThemeMotion.Floating, ThemeIllustration.Dreamer, ThemeImagery.Travel, ThemeImagery.Ocean),
            ["independent"] = CreateTheme(ThemePalette.SunsetCream, ThemeAtmosphere.Cozy, ThemeMotion.Gentle, ThemeIllustration.Dreamer, ThemeImagery.Home, ThemeImagery.Books),
            ["building_something"] = CreateTheme(ThemePalette.MakerWorkshop, ThemeAtmosphere.Focused, ThemeMotion.Soft, ThemeIllustration.ProfessionalDreamer, ThemeImagery.Workspace, ThemeImagery.Technology, ThemeImagery.Books),
            ["ocean"] = CreateTheme(ThemePalette.OceanLight, ThemeAtmosphere.Calm, ThemeMotion.Floating, ThemeIllustration.Dreamer, ThemeImagery.Ocean, ThemeImagery.Travel),
            ["not_sure"] = CreateTheme(ThemePalette.WarmDream, ThemeAtmosphere.Calm, ThemeMotion.Floating, ThemeIllustration.Dreamer),
        };

    private static readonly IReadOnlyDictionary<string, DailyRhythmModifier> DailyRhythmModifiers =
        new Dictionary<string, DailyRhythmModifier>(StringComparer.Ordinal)
        {
            ["creating"] = new(ThemeAtmosphere.Creative, ThemeMotion.Floating),
            ["achieving"] = new(ThemeAtmosphere.Energetic, ThemeMotion.Soft),
            ["exploring"] = new(ThemeAtmosphere.Airy, ThemeMotion.Floating),
            ["connecting"] = new(ThemeAtmosphere.Cozy, ThemeMotion.Gentle),
            ["learning"] = new(ThemeAtmosphere.Focused, ThemeMotion.Soft),
            ["slowing_down"] = new(ThemeAtmosphere.Calm, ThemeMotion.Gentle),
            ["energizing"] = new(ThemeAtmosphere.Energetic, ThemeMotion.Floating),
            ["reflecting"] = new(ThemeAtmosphere.Calm, ThemeMotion.Still),
            ["building"] = new(ThemeAtmosphere.Focused, ThemeMotion.Soft),
            ["balancing"] = new(ThemeAtmosphere.Airy, ThemeMotion.Gentle),
        };

    private static readonly IReadOnlyDictionary<ThemeMotion, int> MotionCalmness =
        new Dictionary<ThemeMotion, int>
        {
            [ThemeMotion.Still] = 0,
            [ThemeMotion.Gentle] = 1,
            [ThemeMotion.Soft] = 2,
            [ThemeMotion.Floating] = 3,
        };

    private static readonly IReadOnlyDictionary<string, CoreAnchorDefinition> CoreAnchorDefinitions =
        new Dictionary<string, CoreAnchorDefinition>(StringComparer.Ordinal)
        {
            ["career"] = new("Career", new[] { ThemeImagery.Workspace, ThemeImagery.Technology }),
            ["education"] = new("Education", new[] { ThemeImagery.Books, ThemeImagery.Campus, ThemeImagery.Technology }),
            ["travel"] = new("Travel", new[] { ThemeImagery.Travel, ThemeImagery.Ocean, ThemeImagery.City }),
            ["financial_independence"] = new("Financial independence", new[] { ThemeImagery.Home, ThemeImagery.Workspace }),
            ["health"] = new("Health", new[] { ThemeImagery.Nature, ThemeImagery.Home, ThemeImagery.Ocean }),
            ["family"] = new("Family", new[] { ThemeImagery.Home, ThemeImagery.Bakery, ThemeImagery.Nature }),
            ["building_something"] = new("Building something", new[] { ThemeImagery.Workspace, ThemeImagery.Technology, ThemeImagery.Books }),
            ["creativity"] = new("Creativity", new[] { ThemeImagery.Books, ThemeImagery.Home }),
            ["calmer_life"] = new("A calmer life", new[] { ThemeImagery.Home, ThemeImagery.Nature, ThemeImagery.Ocean }),
        };

    private readonly IDreamRepository _dreamRepository;
    private readonly IAIService _aiService;

    public DreamAnalysisService(IDreamRepository dreamRepository, IAIService aiService)
    {
        _dreamRepository = dreamRepository;
        _aiService = aiService;
    }

    public async Task<DreamAnalysisResult> CreateDreamAsync(
        CreateDreamRequest request,
        Guid authenticatedUserId,
        string authenticatedUserName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Dream title is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Dream description is required.", nameof(request));
        }

        ThemeConfigAnalysis? selectedVisualWorldTheme = null;
        if (request.VisualWorldKey is not null)
        {
            var visualWorldKey = request.VisualWorldKey.Trim();
            if (!VisualWorldThemes.TryGetValue(visualWorldKey, out selectedVisualWorldTheme))
            {
                throw new ArgumentException("VisualWorldKey is not a supported LUMA visual world.", nameof(request));
            }
        }

        var selectedRhythms = ValidateDailyRhythmKeys(request);
        var selectedCoreAnchors = ValidateCoreAnchorKeys(request);

        var dream = new Dream
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            UserId = authenticatedUserId,
        };

        var discoveryAnswers = new Dictionary<string, string>
        {
            ["title"] = dream.Title,
            ["userName"] = authenticatedUserName,
        };
        if (selectedRhythms.Count > 0)
        {
            discoveryAnswers["dailyRhythmKeys"] = string.Join(", ", selectedRhythms);
        }
        if (selectedCoreAnchors.Count > 0)
        {
            discoveryAnswers["coreAnchorKeys"] = string.Join(", ", selectedCoreAnchors);
        }

        var aiResult = await _aiService.AnalyzeDreamAsync(
            new AnalyzeDreamRequest(
                authenticatedUserId,
                dream.Description,
                discoveryAnswers),
            cancellationToken);

        dream.FutureProfile = MapFutureProfile(
            aiResult.FutureProfile,
            aiResult.Summary,
            selectedCoreAnchors);
        var composedTheme = selectedVisualWorldTheme ?? aiResult.ThemeConfig;
        if (selectedVisualWorldTheme is not null && selectedRhythms.Count > 0)
        {
            composedTheme = ComposeTheme(selectedVisualWorldTheme, selectedRhythms);
        }

        dream.FutureProfile.ThemeConfig = MapThemeConfig(
            composedTheme,
            selectedCoreAnchors);

        var savedDream = await _dreamRepository.AddAsync(dream, cancellationToken);
        var savedProfile = savedDream.FutureProfile!;
        var savedTheme = savedProfile.ThemeConfig!;

        return new DreamAnalysisResult(
            savedDream.Id,
            savedProfile.Id,
            savedTheme.Id,
            savedDream.Title,
            savedProfile.Summary ?? "A clear future is taking shape.",
            savedProfile.Environment,
            savedProfile.Mood,
            savedTheme.Palette,
            savedTheme.Atmosphere,
            savedTheme.Illustration,
            savedTheme.Motion);
    }

    private static IReadOnlyList<string> ValidateCoreAnchorKeys(CreateDreamRequest request)
    {
        var requestedAnchors = request.CoreAnchorKeys;
        if (requestedAnchors is null)
        {
            return Array.Empty<string>();
        }

        if (request.VisualWorldKey is null)
        {
            throw new ArgumentException("CoreAnchorKeys require a VisualWorldKey.", nameof(request));
        }

        if (requestedAnchors.Count is < 1 or > 9)
        {
            throw new ArgumentException("Select between 1 and 9 supported CoreAnchorKeys.", nameof(request));
        }

        var normalizedAnchors = new List<string>(requestedAnchors.Count);
        var seenAnchors = new HashSet<string>(StringComparer.Ordinal);
        foreach (var requestedAnchor in requestedAnchors)
        {
            if (string.IsNullOrWhiteSpace(requestedAnchor))
            {
                throw new ArgumentException("CoreAnchorKeys cannot contain an empty key.", nameof(request));
            }

            var anchorKey = requestedAnchor.Trim();
            if (!CoreAnchorDefinitions.ContainsKey(anchorKey))
            {
                throw new ArgumentException($"'{anchorKey}' is not a supported CoreAnchorKey.", nameof(request));
            }

            if (!seenAnchors.Add(anchorKey))
            {
                throw new ArgumentException($"CoreAnchorKey '{anchorKey}' was selected more than once.", nameof(request));
            }

            normalizedAnchors.Add(anchorKey);
        }

        return normalizedAnchors;
    }

    private static IReadOnlyList<string> ValidateDailyRhythmKeys(CreateDreamRequest request)
    {
        var requestedRhythms = request.DailyRhythmKeys;
        if (requestedRhythms is null)
        {
            return Array.Empty<string>();
        }

        if (request.VisualWorldKey is null)
        {
            throw new ArgumentException("DailyRhythmKeys require a VisualWorldKey.", nameof(request));
        }

        if (requestedRhythms.Count is < 1 or > 10)
        {
            throw new ArgumentException("Select between 1 and 10 supported DailyRhythmKeys.", nameof(request));
        }

        var normalizedRhythms = new List<string>(requestedRhythms.Count);
        var seenRhythms = new HashSet<string>(StringComparer.Ordinal);
        foreach (var requestedRhythm in requestedRhythms)
        {
            if (string.IsNullOrWhiteSpace(requestedRhythm))
            {
                throw new ArgumentException("DailyRhythmKeys cannot contain an empty key.", nameof(request));
            }

            var rhythmKey = requestedRhythm.Trim();
            if (!DailyRhythmModifiers.ContainsKey(rhythmKey))
            {
                throw new ArgumentException($"'{rhythmKey}' is not a supported DailyRhythmKey.", nameof(request));
            }

            if (!seenRhythms.Add(rhythmKey))
            {
                throw new ArgumentException($"DailyRhythmKey '{rhythmKey}' was selected more than once.", nameof(request));
            }

            normalizedRhythms.Add(rhythmKey);
        }

        return normalizedRhythms;
    }

    private static ThemeConfigAnalysis ComposeTheme(
        ThemeConfigAnalysis baseWorld,
        IReadOnlyList<string> selectedRhythms)
    {
        var modifiers = selectedRhythms
            .Select(key => DailyRhythmModifiers[key])
            .ToArray();
        var dominantAtmosphere = modifiers[0].Atmosphere;
        var calmestMotion = modifiers
            .Select(modifier => modifier.Motion)
            .OrderBy(motion => MotionCalmness[motion])
            .First();

        return baseWorld with
        {
            Atmosphere = dominantAtmosphere,
            Motion = calmestMotion,
        };
    }

    private static FutureProfile MapFutureProfile(
        FutureProfileAnalysis aiProfile,
        string? summary,
        IReadOnlyList<string> coreAnchorKeys)
    {
        return new FutureProfile
        {
            Identity = aiProfile.Identity,
            Environment = aiProfile.Environment,
            Lifestyle = aiProfile.Lifestyle.ToList(),
            Aspirations = aiProfile.Aspirations.ToList(),
            Values = aiProfile.Values
                .Concat(coreAnchorKeys.Select(key => CoreAnchorDefinitions[key].Label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Mood = aiProfile.Mood,
            Pace = aiProfile.Pace,
            Summary = summary ?? $"This future is shaped around a {aiProfile.Environment.Replace('_', ' ')} lifestyle, meaningful {string.Join(", ", aiProfile.Aspirations)}, and a {aiProfile.Mood} pace.",
        };
    }

    private static ThemeConfig MapThemeConfig(
        ThemeConfigAnalysis aiTheme,
        IReadOnlyList<string> coreAnchorKeys)
    {
        var anchorImagery = coreAnchorKeys
            .SelectMany(key => CoreAnchorDefinitions[key].Imagery);

        return new ThemeConfig
        {
            Palette = ToPaletteKey(aiTheme.Palette),
            Atmosphere = aiTheme.Atmosphere.ToString(),
            Illustration = aiTheme.Illustration.ToString(),
            Motion = aiTheme.Motion.ToString(),
            Imagery = aiTheme.Imagery.Concat(anchorImagery).Distinct().ToList(),
            CoreAnchorKeys = coreAnchorKeys.ToList(),
        };
    }

    private static string ToPaletteKey(ThemePalette palette)
    {
        return palette switch
        {
            ThemePalette.WarmDream => "warm_dream",
            ThemePalette.LavenderMilk => "lavender_milk",
            ThemePalette.SunsetCream => "sunset_cream",
            ThemePalette.CityNight => "city_night",
            ThemePalette.ForestMist => "forest_mist",
            ThemePalette.OceanLight => "ocean_light",
            ThemePalette.SocialSunrise => "social_sunrise",
            ThemePalette.AmberSummit => "amber_summit",
            ThemePalette.MakerWorkshop => "maker_workshop",
            _ => throw new ArgumentOutOfRangeException(nameof(palette), palette, "Unsupported theme palette.")
        };
    }

    private static ThemeConfigAnalysis CreateTheme(
        ThemePalette palette,
        ThemeAtmosphere atmosphere,
        ThemeMotion motion,
        ThemeIllustration illustration,
        params ThemeImagery[] imagery)
    {
        return new ThemeConfigAnalysis(palette, atmosphere, illustration, motion, imagery);
    }
}
