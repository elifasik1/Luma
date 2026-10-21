using Luma.Application.Agent;
using Luma.Application.Interfaces;
using Luma.Application.Models;

namespace Luma.Application.Services;

public sealed class AdaptiveDailyPlanCapability : IAdaptiveDailyPlanCapability
{
    private readonly DailyPlanReadService _dailyPlanReader;
    private readonly DailyPathService _dailyPath;

    public AdaptiveDailyPlanCapability(
        DailyPlanReadService dailyPlanReader,
        DailyPathService dailyPath)
    {
        _dailyPlanReader = dailyPlanReader;
        _dailyPath = dailyPath;
    }

    public async Task<DailyPathChangeProposal?> ProposeAsync(
        AgentExecutionContext context,
        string userSituation,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[AGENT_TRACE] AdaptiveCapabilityDreamId={context.DreamId}");
        if (string.IsNullOrWhiteSpace(userSituation))
        {
            throw new ArgumentException(
                "A user situation is required.",
                nameof(userSituation));
        }

        // Read the existing daily plan.
        // This capability must not create a new plan.
        var existingPlan = await _dailyPlanReader.GetAsync(
            context,
            cancellationToken);

        if (existingPlan?.Plan is null)
        {
            return null;
        }

        // Create a deterministic adaptive proposal from the
        // existing pending actions.
        return CreateDeterministicProposal(existingPlan.Plan, userSituation);
    }

    private static DailyPathChangeProposal? CreateDeterministicProposal(
        DailyPlanContext plan,
        string? userSituation = null)
    {
        var eligibleActions = plan.Actions
            .Where(action =>
                string.IsNullOrWhiteSpace(action.Status) ||
                action.Status.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(action => action.Order)
            .ThenBy(action => action.Id)
            .ToArray();

        // We need at least two pending actions:
        // one action to keep and at least one action to rest.
        if (eligibleActions.Length < 2)
        {
            return null;
        }

        var maxMinutes = ExtractMaxMinutes(userSituation);
        DailyActionContext keepAction;

        if (maxMinutes.HasValue)
        {
            var fitting = eligibleActions.FirstOrDefault(action =>
                action.EstimatedMinutes > 0 && action.EstimatedMinutes <= maxMinutes.Value);

            if (fitting is null)
            {
                // No eligible action fits within the user's explicit time constraint.
                // We must not invent a suitable action.
                return null;
            }

            keepAction = fitting;
        }
        else
        {
            keepAction = eligibleActions[0];
        }

        var restActionIds = eligibleActions
            .Where(action => action.Id != keepAction.Id)
            .Select(action => action.Id)
            .ToArray();

        if (restActionIds.Length == 0)
        {
            return null;
        }

        return new DailyPathChangeProposal(
            "suggest_plan_change",
            keepAction.Id,
            keepAction.Title,
            keepAction.EstimatedMinutes,
            restActionIds);
    }

    private static int? ExtractMaxMinutes(string? userSituation)
    {
        if (string.IsNullOrWhiteSpace(userSituation))
        {
            return null;
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            userSituation,
            @"\b(\d+)\s*(?:minutes?|dakika|min|dk)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success && int.TryParse(match.Groups[1].Value, out var minutes) && minutes > 0)
        {
            return minutes;
        }

        return null;
    }

    public Task<DailyPathResponse?> ApplyAsync(
        AgentExecutionContext context,
        DailyPathChangeProposal proposal,
        CancellationToken cancellationToken = default)
    {
        return _dailyPath.ApplyAdaptiveChangeAsync(
            context.DreamId,
            new ApplyDailyPathChangeRequest(
                proposal.KeepActionId,
                proposal.RestActionIds),
            null,
            cancellationToken);
    }
}