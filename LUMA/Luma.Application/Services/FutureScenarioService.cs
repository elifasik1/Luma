using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Domain.Entities;

namespace Luma.Application.Services;

public sealed class FutureScenarioService
{
    private readonly IDreamRepository _dreamRepository;
    private readonly IAIService _aiService;

    public FutureScenarioService(IDreamRepository dreamRepository, IAIService aiService)
    {
        _dreamRepository = dreamRepository;
        _aiService = aiService;
    }

    public async Task<FutureScenarioResponse?> GetOrCreateFutureScenarioAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        var dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        if (dream.FutureScenario is not null)
        {
            return MapToResponse(dream.FutureScenario, dream.FutureProfile?.TargetYear ?? TargetYearRules.CurrentYear);
        }

        var profile = dream.FutureProfile;
        if (profile is null)
        {
            throw new InvalidOperationException("Future profile must be analyzed before generating a scenario.");
        }

        var themeConfig = profile.ThemeConfig;
        var request = new GenerateFutureScenarioRequest(
            UserId: dream.UserId,
            DreamTitle: dream.Title,
            DreamDescription: dream.Description,
            Identity: profile.Identity,
            Environment: profile.Environment,
            Lifestyle: profile.Lifestyle,
            Aspirations: profile.Aspirations,
            Values: profile.Values,
            Mood: profile.Mood,
            Pace: profile.Pace,
            TargetYear: profile.TargetYear,
            Atmosphere: themeConfig?.Atmosphere ?? "Calm",
            Imagery: themeConfig?.Imagery.Select(i => i.ToString()).ToList() ?? new List<string>());

        var aiResult = await _aiService.GenerateFutureScenarioAsync(request, cancellationToken);

        var scenario = new FutureScenario
        {
            DreamId = dream.Id,
            Title = aiResult.Title,
            Intro = aiResult.Intro,
            Morning = aiResult.Morning,
            Daytime = aiResult.Daytime,
            Evening = aiResult.Evening,
            EmotionalTone = aiResult.EmotionalTone,
            ClosingReflection = aiResult.ClosingReflection,
        };

        var saved = await _dreamRepository.AddScenarioAsync(scenario, cancellationToken);

        return MapToResponse(saved, profile.TargetYear);
    }

    private static FutureScenarioResponse MapToResponse(FutureScenario scenario, int targetYear)
    {
        return new FutureScenarioResponse(
            scenario.Id,
            scenario.DreamId,
            scenario.Title,
            scenario.Intro,
            scenario.Morning,
            scenario.Daytime,
            scenario.Evening,
            scenario.EmotionalTone,
            scenario.ClosingReflection,
            scenario.CreatedAt,
            targetYear);
    }
}
