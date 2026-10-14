namespace Luma.Application.Agent;

public sealed record AgentToolMetadata(
    string Name,
    string Description,
    bool IsReadOnly);

public interface IAgentToolRegistry
{
    IReadOnlyList<AgentToolMetadata> GetAvailableTools();

    IAgentTool? Get(string name);
}
