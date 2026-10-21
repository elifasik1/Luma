using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Domain.Entities;

namespace Luma.Application.Services;

public sealed class TalkToLumaService
{
    private const int MaxMessageLength = 2000;
    private const int MaxHistoryTurns = 12;
    private const int MaxHistoryMessageLength = 2000;
    private const int MaxReflections = 3;

    private readonly IDreamRepository _dreamRepository;
    private readonly IAIService _aiService;
    private readonly DailyPathService _dailyPathService;

    public TalkToLumaService(
        IDreamRepository dreamRepository,
        IAIService aiService,
        DailyPathService dailyPathService)
    {
        _dreamRepository = dreamRepository;
        _aiService = aiService;
        _dailyPathService = dailyPathService;
    }

    public async Task<TalkToLumaResponse?> SendMessageAsync(
        Guid dreamId,
        SendLumaMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("A message is required.", nameof(request));
        }

        var message = request.Message.Trim();
        if (message.Length > MaxMessageLength)
        {
            throw new ArgumentException($"Message must be {MaxMessageLength} characters or fewer.", nameof(request));
        }

        var dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        if (dream.DailyActions.Count == 0 && dream.Goals.Count > 0 && dream.FutureProfile is not null)
        {
            try
            {
                await _dailyPathService.GetOrCreateDailyPathAsync(dreamId, refresh: false, planDate: null, cancellationToken);
                dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken) ?? dream;
            }
            catch
            {
                // Proceed with existing dream context if daily path generation encounters an issue
            }
        }

        var history = (request.History ?? Array.Empty<LumaConversationTurn>())
            .Where(turn =>
                turn is not null &&
                (turn.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ||
                 turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrWhiteSpace(turn.Content))
            .TakeLast(MaxHistoryTurns)
            .Select(turn => new LumaConversationTurn(
                turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
                turn.Content.Trim()[..Math.Min(turn.Content.Trim().Length, MaxHistoryMessageLength)]))
            .ToList();

        var actions = dream.DailyActions
            .OrderBy(action => action.Order)
            .Take(20)
            .Select(action => new DailyActionConversationContext(
                action.Id,
                action.Title,
                action.GoalTitle,
                action.EstimatedMinutes,
                string.IsNullOrWhiteSpace(action.Status) ? "Pending" : action.Status,
                action.Intention))
            .ToList();

        var context = new LumaConversationContext(
            dream.FutureProfile is { } profile
                ? new FutureProfileConversationContext(
                    profile.Identity,
                    profile.Environment,
                    profile.Lifestyle,
                    profile.Aspirations,
                    profile.Values,
                    profile.Mood,
                    profile.Pace,
                    profile.Summary,
                    profile.TargetYear)
                : null,
            dream.FutureScenario is { } scenario
                ? new FutureScenarioConversationContext(
                    scenario.Title,
                    scenario.Intro,
                    scenario.Morning,
                    scenario.Daytime,
                    scenario.Evening,
                    scenario.EmotionalTone,
                    scenario.ClosingReflection)
                : null,
            dream.Goals
                .OrderBy(goal => goal.Order)
                .Take(12)
                .Select(goal => new GoalConversationContext(
                    goal.Title,
                    goal.DesiredOutcome,
                    goal.CurrentState,
                    goal.Gap,
                    goal.Status))
                .ToList(),
            actions,
            new ConversationProgressContext(
                dream.DailyActions.Count,
                dream.DailyActions.Count(action => !string.IsNullOrWhiteSpace(action.Status) && action.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)),
                dream.DailyActions.Count(action => !string.IsNullOrWhiteSpace(action.Status) && action.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase)),
                dream.DailyActions.Count(action => string.IsNullOrWhiteSpace(action.Status) || action.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))),
            dream.Reflections
                .OrderByDescending(reflection => reflection.CreatedAt)
                .Take(MaxReflections)
                .Select(reflection => new ReflectionConversationContext(
                    reflection.Mood,
                    Truncate(reflection.Notes, 500),
                    Truncate(reflection.Insight, 500),
                    reflection.CreatedAt))
                .ToList());

        var result = await _aiService.TalkToLumaAsync(
            new TalkToLumaRequest(
                dream.UserId,
                dream.Title,
                dream.Description,
                message,
                history,
                context),
            cancellationToken);

        return new TalkToLumaResponse(
            result.Reply,
            MapProposal(result.Proposal, dream.DailyActions));
    }

    private static DailyPathChangeProposal? MapProposal(
        TalkToLumaActionProposal? proposal,
        IReadOnlyCollection<DailyAction> actions)
    {
        if (proposal is null ||
            proposal.KeepActionId == Guid.Empty ||
            proposal.RestActionIds.Count == 0 ||
            proposal.RestActionIds.Contains(proposal.KeepActionId) ||
            proposal.RestActionIds.Distinct().Count() != proposal.RestActionIds.Count)
        {
            return null;
        }

        var pendingActions = actions
            .Where(action => string.IsNullOrWhiteSpace(action.Status) || action.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(action => action.Id);

        if (!pendingActions.TryGetValue(proposal.KeepActionId, out var keepAction) ||
            proposal.RestActionIds.Any(id => !pendingActions.ContainsKey(id)))
        {
            return null;
        }

        return new DailyPathChangeProposal(
            "suggest_plan_change",
            keepAction.Id,
            keepAction.Title,
            keepAction.EstimatedMinutes,
            proposal.RestActionIds);
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }
}
