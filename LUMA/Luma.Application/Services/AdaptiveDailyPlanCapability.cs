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
        return CreateDeterministicProposal(existingPlan.Plan);
    }

    private static DailyPathChangeProposal? CreateDeterministicProposal(
        DailyPlanContext plan)
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

        var keepAction = eligibleActions[0];

        var restActionIds = eligibleActions
            .Skip(1)
            .Select(action => action.Id)
            .ToArray();

        return new DailyPathChangeProposal(
            "suggest_plan_change",
            keepAction.Id,
            keepAction.Title,
            keepAction.EstimatedMinutes,
            restActionIds);
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
            cancellationToken);
    }
}