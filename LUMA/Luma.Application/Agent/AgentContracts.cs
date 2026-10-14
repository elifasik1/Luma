namespace Luma.Application.Agent;

public sealed record AgentRequest(
    Guid DreamId,
    string Message);

public sealed record AgentPlannerRequest(
    string Message);

public sealed record AgentResponse(
    string Reply,
    bool RequiresApproval,
    AgentAction? Action = null,
    AgentPlan? Plan = null,
    IReadOnlyList<AgentToolExecutionResult>? ToolResults = null,
    string? Error = null,
    AgentActionProposal? ActionProposal = null);

public sealed record AgentAction(
    string Name,
    IReadOnlyDictionary<string, string> Arguments);

public sealed record AgentActionRequest(
    string UserSituation);

public sealed record AgentActionDecision(
    bool ActionRequired,
    string? ActionName,
    string? Reason,
    bool RequiresApproval,
    AgentActionRequest? ActionRequest);

public sealed record AgentActionProposal(
    string ActionName,
    string Reason,
    bool RequiresApproval,
    AgentToolResult ProposalResult);

public sealed class AgentExecutionContext
{
    internal AgentExecutionContext(Guid authenticatedUserId, Guid dreamId)
    {
        AuthenticatedUserId = authenticatedUserId;
        DreamId = dreamId;
    }

    public Guid AuthenticatedUserId { get; }
    public Guid DreamId { get; }
}

public sealed record AgentToolResult(
    object? Data,
    bool RequiresApproval = false);

public sealed record AgentPlan(
    string Intent,
    IReadOnlyList<string> SelectedTools,
    string Reason);

public sealed record AgentToolExecutionResult(
    string ToolName,
    bool Success,
    object? Data,
    string? Error = null,
    bool RequiresApproval = false);

public sealed class AgentDreamNotFoundException : Exception
{
    public AgentDreamNotFoundException(Guid dreamId)
        : base($"Dream '{dreamId}' was not found.")
    {
        DreamId = dreamId;
    }

    public Guid DreamId { get; }
}

public sealed class AgentPlannerValidationException : Exception
{
    public AgentPlannerValidationException(string message)
        : base(message)
    {
    }
}
