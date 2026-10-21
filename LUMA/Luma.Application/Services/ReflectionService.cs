using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Domain.Entities;

namespace Luma.Application.Services;

public sealed class ReflectionService
{
    private readonly IDreamRepository _dreamRepository;
    private readonly IAIService _aiService;
    private readonly TimeProvider _timeProvider;

    public ReflectionService(
        IDreamRepository dreamRepository,
        IAIService aiService,
        TimeProvider? timeProvider = null)
    {
        _dreamRepository = dreamRepository;
        _aiService = aiService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ReflectionResponse?> CreateReflectionAsync(
        Guid dreamId,
        CreateReflectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        var planDate = request.PlanDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var dailyPlanSnapshot = await _dreamRepository.GetDailyPlanSnapshotAsync(
            dreamId,
            planDate,
            cancellationToken);

        var profile = dream.FutureProfile;
        var identity = profile?.Identity ?? "Dreamer";

        // A reflection may be saved without a plan snapshot, but it must not borrow
        // actions from another day to create a made-up historical progress record.
        IEnumerable<DailyPlanActionSnapshot> planActions = dailyPlanSnapshot?.Actions
            ?? Enumerable.Empty<DailyPlanActionSnapshot>();

        var completedActions = planActions
            .Where(action => string.Equals(action.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .Select(action => action.Title)
            .ToList();

        var skippedActions = planActions
            .Where(action => string.Equals(action.Status, "Skipped", StringComparison.OrdinalIgnoreCase))
            .Select(action => action.Title)
            .ToList();

        var aiRequest = new GenerateReflectionInsightRequest(
            UserId: dream.UserId,
            DreamTitle: dream.Title,
            Identity: identity,
            ReflectionMood: request.Mood,
            ReflectionNotes: request.Notes,
            CompletedActions: completedActions,
            SkippedActions: skippedActions);

        var aiResult = await _aiService.GenerateReflectionInsightAsync(aiRequest, cancellationToken);

        var entity = new DreamReflection
        {
            DreamId = dreamId,
            DailyPlanSnapshotId = dailyPlanSnapshot?.Id,
            PlanDate = planDate,
            Mood = string.IsNullOrWhiteSpace(request.Mood) ? "Reflective" : request.Mood.Trim(),
            Notes = request.Notes?.Trim() ?? string.Empty,
            Insight = aiResult.Insight,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _dreamRepository.AddReflectionAsync(entity, cancellationToken);

        if (profile is not null &&
            TryGetMeaningfulEnvironmentShift(profile.Environment, entity.Notes, out var direction))
        {
            var evolution = await _aiService.EvolveFutureAsync(
                new EvolveFutureRequest(
                    dream.UserId,
                    dream.Title,
                    dream.Description,
                    entity.Notes,
                    profile.Identity,
                    profile.Environment,
                    profile.Summary ?? string.Empty,
                    profile.Mood,
                    profile.Pace,
                    profile.Lifestyle,
                    profile.Aspirations,
                    profile.Values),
                cancellationToken);

            profile.Identity = evolution.FutureProfile.Identity;
            profile.Environment = evolution.FutureProfile.Environment;
            profile.Lifestyle = evolution.FutureProfile.Lifestyle.ToList();
            profile.Aspirations = evolution.FutureProfile.Aspirations.ToList();
            profile.Values = evolution.FutureProfile.Values.ToList();
            profile.Mood = evolution.FutureProfile.Mood;
            profile.Pace = evolution.FutureProfile.Pace;
            profile.Summary = evolution.Summary ?? $"Your future is evolving toward {direction}.";

            if (profile.ThemeConfig is null)
            {
                profile.ThemeConfig = new ThemeConfig
                {
                    FutureProfileId = profile.Id,
                    Palette = evolution.ThemeConfig.Palette.ToString(),
                    Atmosphere = evolution.ThemeConfig.Atmosphere.ToString(),
                    Illustration = evolution.ThemeConfig.Illustration.ToString(),
                    Motion = evolution.ThemeConfig.Motion.ToString(),
                    Imagery = evolution.ThemeConfig.Imagery.ToList()
                };
            }
            else
            {
                // Preserve user-selected theme palette, atmosphere, illustration, and motion.
                // Evolving reflections must not overwrite the user's active theme choice.
                if (evolution.ThemeConfig.Imagery.Count > 0 && profile.ThemeConfig.Imagery.Count == 0)
                {
                    profile.ThemeConfig.Imagery = evolution.ThemeConfig.Imagery.ToList();
                }
            }

            await _dreamRepository.SaveFutureProfileAsync(profile, cancellationToken);
        }

        return MapToResponse(saved);
    }

    public async Task<IReadOnlyList<ReflectionResponse>> GetReflectionsAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        var reflections = await _dreamRepository.GetReflectionsAsync(dreamId, cancellationToken);
        return reflections.Select(MapToResponse).ToList();
    }

    private static ReflectionResponse MapToResponse(DreamReflection r)
    {
        return new ReflectionResponse(
            r.Id,
            r.DreamId,
            r.Mood,
            r.Notes,
            r.Insight,
            r.PlanDate,
            r.CreatedAt);
    }

    public async Task<ReflectionResponse?> UpdateReflectionAsync(
        Guid dreamId,
        Guid reflectionId,
        UpdateReflectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _dreamRepository.UpdateReflectionAsync(
            dreamId,
            reflectionId,
            string.IsNullOrWhiteSpace(request.Mood) ? "Reflective" : request.Mood.Trim(),
            request.Notes?.Trim() ?? string.Empty,
            cancellationToken);
        return updated is null ? null : MapToResponse(updated);
    }

    private static bool TryGetMeaningfulEnvironmentShift(
        string currentEnvironment,
        string reflection,
        out string direction)
    {
        direction = string.Empty;
        if (string.IsNullOrWhiteSpace(reflection))
        {
            return false;
        }

        var normalized = reflection.ToLowerInvariant();
        var hasDirectionChange = new[]
        {
            "instead",
            "rather than",
            "no longer",
            "realized",
            "realised",
            "i want to live",
            "i'd rather",
            "i would rather",
            "moving to",
            "move to",
            "my future",
            "future direction"
        }.Any(normalized.Contains);
        if (!hasDirectionChange)
        {
            return false;
        }

        direction = normalized.Contains("nature") || normalized.Contains("forest") || normalized.Contains("greenery")
            ? "nature"
            : normalized.Contains("ocean") || normalized.Contains("coast") || normalized.Contains("sea")
                ? "ocean"
                : normalized.Contains("city") || normalized.Contains("urban")
                    ? "city"
                    : normalized.Contains("quiet home") || normalized.Contains("rural")
                        ? "home"
                        : string.Empty;
        if (string.IsNullOrEmpty(direction))
        {
            return false;
        }

        var current = currentEnvironment.ToLowerInvariant();
        var currentDirection = current.Contains("forest") || current.Contains("nature")
            ? "nature"
            : current.Contains("ocean") || current.Contains("coast")
                ? "ocean"
                : current.Contains("city") || current.Contains("urban")
                    ? "city"
                    : current.Contains("home") || current.Contains("rural")
                        ? "home"
                        : current;

        return !string.Equals(direction, currentDirection, StringComparison.Ordinal);
    }
}
