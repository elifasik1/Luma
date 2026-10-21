using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Domain.Entities;

namespace Luma.Application.Services;

public sealed class DailyPathService
{
    private readonly IDreamRepository _dreamRepository;
    private readonly IAIService _aiService;
    private readonly TimeProvider _timeProvider;

    public DailyPathService(
        IDreamRepository dreamRepository,
        IAIService aiService,
        TimeProvider? timeProvider = null)
    {
        _dreamRepository = dreamRepository;
        _aiService = aiService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DailyPathResponse?> GetOrCreateDailyPathAsync(
        Guid dreamId,
        bool refresh = false,
        DateOnly? planDate = null,
        CancellationToken cancellationToken = default)
    {
        var dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        // Use provided planDate, or default to today
        var effectivePlanDate = planDate ?? GetCurrentPlanDate();
        var currentSnapshot = await _dreamRepository.GetDailyPlanSnapshotAsync(
            dreamId,
            effectivePlanDate,
            cancellationToken);

        if (!refresh && currentSnapshot is not null)
        {
            return MapToResponse(dream.DailyActions);
        }

        if (!refresh && dream.DailyActions.Count > 0)
        {
            var latestSnapshot = await _dreamRepository.GetLatestDailyPlanSnapshotAsync(
                dreamId,
                cancellationToken);

            if (latestSnapshot is null)
            {
                await _dreamRepository.AddDailyPlanSnapshotAsync(
                    CreateSnapshot(dream.Id, effectivePlanDate, dream.DailyActions),
                    cancellationToken);
                return MapToResponse(dream.DailyActions);
            }

            if (latestSnapshot.PlanDate >= effectivePlanDate)
            {
                return MapToResponse(dream.DailyActions);
            }
        }

        if (dream.Goals.Count == 0)
        {
            throw new InvalidOperationException("Goals must be generated before creating a daily path.");
        }

        var profile = dream.FutureProfile;
        if (profile is null)
        {
            throw new InvalidOperationException("Future profile must be analyzed before creating a daily path.");
        }

        var goalSummaries = dream.Goals
            .OrderBy(g => g.Order)
            .Select(g => new GoalSummary(g.Id, g.Title, g.Category, g.Gap))
            .ToList();

        var request = new GenerateDailyPathRequest(
            UserId: dream.UserId,
            DreamTitle: dream.Title,
            Identity: profile.Identity,
            Environment: profile.Environment,
            Mood: profile.Mood,
            Pace: profile.Pace,
            Goals: goalSummaries);

        var aiResult = await _aiService.GenerateDailyPathAsync(request, cancellationToken);

        var order = 1;
        var actionEntities = aiResult.Actions.Select(item => new DailyAction
        {
            DreamId = dream.Id,
            GoalId = goalSummaries.FirstOrDefault(g =>
                g.Title.Equals(item.GoalTitle, StringComparison.OrdinalIgnoreCase))?.GoalId,
            GoalTitle = item.GoalTitle,
            Title = item.Title,
            Description = item.Description,
            TimeOfDay = string.IsNullOrWhiteSpace(item.TimeOfDay) ? "Morning" : item.TimeOfDay,
            EstimatedMinutes = item.EstimatedMinutes > 0 ? item.EstimatedMinutes : 15,
            Intention = item.Intention,
            Order = item.Order > 0 ? item.Order : order++,
            ScheduledDate = effectivePlanDate,
        }).ToList();

        var snapshot = CreateSnapshot(dream.Id, effectivePlanDate, actionEntities);
        var saved = await _dreamRepository.ReplaceDailyActionsAndSnapshotAsync(
            snapshot,
            actionEntities,
            cancellationToken);

        return MapToResponse(saved);
    }

    public async Task<DailyActionResponse?> UpdateActionStatusAsync(
        Guid dreamId,
        Guid actionId,
        string status,
        DateOnly? scheduledDate = null,
        DateOnly? planDate = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeStatus(status);
        var effectivePlanDate = planDate ?? GetCurrentPlanDate();
        var updated = await _dreamRepository.UpdateDailyActionStatusAsync(
            dreamId,
            actionId,
            normalizedStatus,
            scheduledDate,
            effectivePlanDate,
            cancellationToken);

        if (updated is null)
        {
            return null;
        }

        return MapToActionResponse(updated);
    }

    public async Task<DailyPathResponse?> ApplyAdaptiveChangeAsync(
        Guid dreamId,
        ApplyDailyPathChangeRequest request,
        DateOnly? planDate = null,
        CancellationToken cancellationToken = default)
    {
        if (request.KeepActionId == Guid.Empty ||
            request.RestActionIds is null ||
            request.RestActionIds.Count == 0 ||
            request.RestActionIds.Contains(request.KeepActionId) ||
            request.RestActionIds.Distinct().Count() != request.RestActionIds.Count)
        {
            throw new ArgumentException("Choose one action to keep and at least one different action to rest.", nameof(request));
        }

        var dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        var pendingIds = dream.DailyActions
            .Where(action => string.IsNullOrWhiteSpace(action.Status) || action.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            .Select(action => action.Id)
            .ToHashSet();
        if (!pendingIds.Contains(request.KeepActionId) ||
            request.RestActionIds.Any(id => !pendingIds.Contains(id)))
        {
            throw new ArgumentException("The proposed actions are no longer available in today's path.", nameof(request));
        }

        var effectivePlanDate = planDate ?? GetCurrentPlanDate();
        var updatedActions = await _dreamRepository.ApplyDailyPathChangeAsync(
            dreamId,
            request.KeepActionId,
            request.RestActionIds,
            effectivePlanDate,
            cancellationToken);
        if (updatedActions is null)
        {
            throw new InvalidOperationException("Today's path changed before the adjustment could be applied. Please review the current path and try again.");
        }

        return MapToResponse(updatedActions);
    }

    private DateOnly GetCurrentPlanDate()
    {
        return DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
    }

    private DailyPlanSnapshot CreateSnapshot(
        Guid dreamId,
        DateOnly planDate,
        IEnumerable<DailyAction> actions)
    {
        var snapshot = new DailyPlanSnapshot
        {
            DreamId = dreamId,
            PlanDate = planDate,
            GeneratedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        snapshot.Actions = actions
            .OrderBy(action => action.Order)
            .Select(action => new DailyPlanActionSnapshot
            {
                DailyPlanSnapshotId = snapshot.Id,
                DailyPlanSnapshot = snapshot,
                SourceDailyActionId = action.Id,
                GoalId = action.GoalId,
                GoalTitle = action.GoalTitle,
                Title = action.Title,
                Description = action.Description,
                TimeOfDay = action.TimeOfDay,
                EstimatedMinutes = action.EstimatedMinutes,
                Intention = action.Intention,
                Order = action.Order,
                Status = string.IsNullOrWhiteSpace(action.Status) ? "Pending" : action.Status,
                CompletedAt = action.CompletedAt
            })
            .ToList();

        return snapshot;
    }

    private static string NormalizeStatus(string status)
    {
        if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            return "Completed";
        if (string.Equals(status, "Skipped", StringComparison.OrdinalIgnoreCase))
            return "Skipped";
        return "Pending";
    }

    private static DailyPathResponse MapToResponse(IEnumerable<DailyAction> actions)
    {
        var ordered = actions.OrderBy(a => a.Order).ToList();
        var generatedAt = ordered.FirstOrDefault()?.GeneratedAt ?? DateTime.UtcNow;

        var total = ordered.Count;
        var completed = ordered.Count(a => string.Equals(a.Status, "Completed", StringComparison.OrdinalIgnoreCase));
        var skipped = ordered.Count(a => string.Equals(a.Status, "Skipped", StringComparison.OrdinalIgnoreCase));

        var momentumMessage = GenerateMomentumMessage(total, completed, skipped);

        return new DailyPathResponse(
            Actions: ordered.Select(MapToActionResponse).ToList(),
            GeneratedAt: generatedAt,
            TotalActions: total,
            CompletedActions: completed,
            SkippedActions: skipped,
            MomentumMessage: momentumMessage);
    }

    private static DailyActionResponse MapToActionResponse(DailyAction a)
    {
        return new DailyActionResponse(
            a.Id,
            a.DreamId,
            a.GoalId,
            a.GoalTitle,
            a.Title,
            a.Description,
            a.TimeOfDay,
            a.EstimatedMinutes,
            a.Intention,
            a.Order,
            string.IsNullOrWhiteSpace(a.Status) ? "Pending" : a.Status,
            a.CompletedAt,
            a.ScheduledDate);
    }

    private static string GenerateMomentumMessage(int total, int completed, int skipped)
    {
        if (total == 0)
        {
            return "Your day is open and ready for intentional direction.";
        }

        if (completed == total)
        {
            return "You have honored every intention for today. Your future is taking root in small, quiet ways.";
        }

        if (completed + skipped == total && completed > 0)
        {
            return "You took thoughtful steps and honored your boundaries today. Steady progress is calm progress.";
        }

        if (completed > 0)
        {
            return $"You have completed {completed} of {total} steps today. Each small action bridges today to tomorrow.";
        }

        if (skipped > 0)
        {
            return "Taking space when needed is part of the rhythm. Return whenever you feel ready.";
        }

        return "Every journey begins with a single intentional breath. Choose one gentle step to start.";
    }
}
