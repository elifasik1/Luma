namespace Luma.Application.Agent;

public sealed record AgentResponseGenerationRequest(
    string UserMessage,
    AgentPlan Plan,
    IReadOnlyList<AgentToolExecutionResult> ToolResults,
    AgentActionProposal? ActionProposal = null);

public sealed record AgentGeneratedResponse(
    string Message,
    string? FollowUpQuestion = null,
    string? SuggestedNextStep = null);

public sealed class AgentResponseGenerationValidationException : Exception
{
    public AgentResponseGenerationValidationException(string message)
        : base(message)
    {
    }
}
