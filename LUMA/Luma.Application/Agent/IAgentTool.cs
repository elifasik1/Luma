namespace Luma.Application.Agent;

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }

    Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}
