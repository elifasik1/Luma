using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Domain.Entities;

namespace Luma.Application.Services;

public sealed class GoalService
{
    private readonly IDreamRepository _dreamRepository;
    private readonly IAIService _aiService;

    public GoalService(IDreamRepository dreamRepository, IAIService aiService)
    {
        _dreamRepository = dreamRepository;
        _aiService = aiService;
    }

    public async Task<IReadOnlyList<GoalResponse>?> GetOrCreateGoalsAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        var dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        if (dream.Goals.Count > 0)
        {
            return dream.Goals.OrderBy(g => g.Order).Select(MapToResponse).ToList();
        }

        var profile = dream.FutureProfile;
        if (profile is null)
        {
            throw new InvalidOperationException("Future profile must be analyzed before generating goals.");
        }

        var scenario = dream.FutureScenario;
        var request = new GenerateGoalsRequest(
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
            ScenarioTitle: scenario?.Title,
            ScenarioIntro: scenario?.Intro,
            ScenarioDaytime: scenario?.Daytime,
            ScenarioEmotionalTone: scenario?.EmotionalTone);

        var aiResult = await _aiService.GenerateGoalsAsync(request, cancellationToken);

        var order = 1;
        var goalEntities = aiResult.Goals.Select(item => new Goal
        {
            DreamId = dream.Id,
            Title = item.Title,
            Description = item.Description,
            Category = string.IsNullOrWhiteSpace(item.Category) ? "PersonalGrowth" : item.Category,
            WhyItMatters = item.WhyItMatters,
            DesiredOutcome = item.DesiredOutcome,
            CurrentState = item.CurrentState,
            Gap = item.Gap,
            Status = string.IsNullOrWhiteSpace(item.Status) ? "NotStarted" : item.Status,
            Order = item.Order > 0 ? item.Order : order++,
        }).ToList();

        var savedGoals = await _dreamRepository.AddGoalsAsync(goalEntities, cancellationToken);

        return savedGoals.OrderBy(g => g.Order).Select(MapToResponse).ToList();
    }

    private static GoalResponse MapToResponse(Goal goal)
    {
        return new GoalResponse(
            goal.Id,
            goal.DreamId,
            goal.Title,
            goal.Description,
            goal.Category,
            goal.WhyItMatters,
            goal.DesiredOutcome,
            goal.CurrentState,
            goal.Gap,
            goal.Status,
            goal.Order,
            goal.CreatedAt);
    }
}
