using Luma.Application.Services;

namespace Luma.Application.Agent.Tools;

public sealed class FutureContextTool : IAgentTool
{
    private readonly FutureContextReadService _contextReader;

    public FutureContextTool(FutureContextReadService contextReader)
    {
        _contextReader = contextReader;
    }

    public string Name => "get_future_context";

    public string Description =>
        "Reads the owned Dream's future profile and optional future scenario without creating or changing data.";

    public async Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _contextReader.GetAsync(context, cancellationToken);
        if (result is null)
        {
            throw new AgentDreamNotFoundException(context.DreamId);
        }

        return new AgentToolResult(result);
    }
}
