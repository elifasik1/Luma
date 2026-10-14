using Luma.Application.Services;

namespace Luma.Application.Agent.Tools;

public sealed class DailyPlanTool : IAgentTool
{
    private readonly DailyPlanReadService _planReader;

    public DailyPlanTool(DailyPlanReadService planReader)
    {
        _planReader = planReader;
    }

    public string Name => "get_daily_plan";

    public string Description =>
        "Reads the owned Dream's existing daily plan without creating or changing actions.";

    public async Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _planReader.GetAsync(context, cancellationToken);
        if (result is null)
        {
            throw new AgentDreamNotFoundException(context.DreamId);
        }

        return new AgentToolResult(result);
    }
}
