using Luma.Application.Services;

namespace Luma.Application.Agent.Tools;

public sealed class ActiveGoalsTool : IAgentTool
{
    private readonly ActiveGoalsReadService _goalsReader;

    public ActiveGoalsTool(ActiveGoalsReadService goalsReader)
    {
        _goalsReader = goalsReader;
    }

    public string Name => "get_active_goals";

    public string Description =>
        "Reads the owned Dream's incomplete goals without creating or changing data.";

    public async Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _goalsReader.GetAsync(context, cancellationToken);
        if (result is null)
        {
            throw new AgentDreamNotFoundException(context.DreamId);
        }

        return new AgentToolResult(result);
    }
}
