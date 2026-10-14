using Luma.Application.Services;

namespace Luma.Application.Agent.Tools;

public sealed class ProgressTool : IAgentTool
{
    private readonly DailyPlanReadService _dailyPlanReader;

    public ProgressTool(DailyPlanReadService dailyPlanReader)
    {
        _dailyPlanReader = dailyPlanReader;
    }

    public string Name => "get_progress";

    public string Description =>
        "Reads progress calculated from the owned Dream's existing daily actions without changing data.";

    public async Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var dailyPlanResult = await _dailyPlanReader.GetAsync(context, cancellationToken);
        if (dailyPlanResult is null)
        {
            throw new AgentDreamNotFoundException(context.DreamId);
        }

        var plan = dailyPlanResult.Plan;
        var totalActions = plan?.TotalActions ?? 0;
        var completedActions = plan?.CompletedActions ?? 0;

        return new AgentToolResult(new ProgressResult(
            totalActions,
            completedActions,
            plan?.SkippedActions ?? 0,
            plan?.PendingActions ?? 0,
            totalActions == 0 ? 0m : (decimal)completedActions / totalActions,
            plan?.MomentumMessage ?? "Your day is open and ready for intentional direction."));
    }
}
