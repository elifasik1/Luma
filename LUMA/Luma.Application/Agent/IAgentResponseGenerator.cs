namespace Luma.Application.Agent;

public interface IAgentResponseGenerator
{
    Task<AgentGeneratedResponse> GenerateAsync(
        AgentResponseGenerationRequest request,
        CancellationToken cancellationToken = default);
}
