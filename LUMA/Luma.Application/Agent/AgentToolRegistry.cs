namespace Luma.Application.Agent;

public sealed class AgentToolRegistry : IAgentToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAgentTool> _tools;

    public AgentToolRegistry(IEnumerable<IAgentTool> tools)
    {
        _tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AgentToolMetadata> GetAvailableTools()
    {
        return _tools.Values
            .OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tool => new AgentToolMetadata(
                tool.Name,
                tool.Description,
                IsReadOnly: tool is not IAgentActionTool))
            .ToArray();
    }

    public IAgentTool? Get(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }
}
