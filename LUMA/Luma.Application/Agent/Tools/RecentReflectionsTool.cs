using Luma.Application.Services;

namespace Luma.Application.Agent.Tools;

public sealed class RecentReflectionsTool : IAgentTool
{
    private readonly ReflectionReadService _reflectionReader;

    public RecentReflectionsTool(ReflectionReadService reflectionReader)
    {
        _reflectionReader = reflectionReader;
    }

    public string Name => "get_recent_reflections";

    public string Description =>
        "Reads the most recent persisted reflections for the owned Dream without creating or changing data.";

    public async Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _reflectionReader.GetRecentAsync(context, cancellationToken);
        if (result is null)
        {
            throw new AgentDreamNotFoundException(context.DreamId);
        }

        return new AgentToolResult(result);
    }
}
