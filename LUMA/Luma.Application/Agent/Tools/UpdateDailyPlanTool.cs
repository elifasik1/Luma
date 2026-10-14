using Luma.Application.Interfaces;

namespace Luma.Application.Agent.Tools;

public sealed class UpdateDailyPlanTool : IUpdateDailyPlanTool
{
    private readonly IAdaptiveDailyPlanCapability _dailyPlanCapability;

    public UpdateDailyPlanTool(IAdaptiveDailyPlanCapability dailyPlanCapability)
    {
        _dailyPlanCapability = dailyPlanCapability;
    }

    public string Name => "update_daily_plan";

    public string Description =>
        "Proposes an adaptive daily-plan change and applies it only after explicit approval.";

    public Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "update_daily_plan requires an explicit proposal or approval request.");
    }

    public async Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        UpdateDailyPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Approve)
        {
            var proposal = await _dailyPlanCapability.ProposeAsync(
                context,
                request.UserSituation,
                cancellationToken);

            return new AgentToolResult(new UpdateDailyPlanResult(
                Applied: false,
                RequiresApproval: true,
                Proposal: proposal,
                UpdatedPlan: null,
                Message: proposal is null
                    ? "No eligible pending actions are available for an adaptive proposal."
                    : "A daily-plan change proposal is ready for explicit approval."));
        }

        if (request.Proposal is null)
        {
            throw new ArgumentException(
                "An existing adaptive daily-plan proposal is required for approval.",
                nameof(request));
        }

        var updatedPlan = await _dailyPlanCapability.ApplyAsync(
            context,
            request.Proposal,
            cancellationToken);

        if (updatedPlan is null)
        {
            throw new AgentDreamNotFoundException(context.DreamId);
        }

        return new AgentToolResult(new UpdateDailyPlanResult(
            Applied: true,
            RequiresApproval: false,
            Proposal: request.Proposal,
            UpdatedPlan: updatedPlan,
            Message: "The approved daily-plan change was applied."));
    }
}
