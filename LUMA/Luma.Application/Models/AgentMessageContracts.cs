namespace Luma.Application.Models;

public sealed record AgentMessageRequest(string Message);

public sealed record AgentMessageResponse(
    string Message,
    IReadOnlyList<string> UsedTools,
    AgentActionProposalResponse? ActionProposal = null);

public sealed record AgentActionProposalResponse(
    string ActionName,
    string Reason,
    bool RequiresApproval,
    DailyPathChangeProposal? Proposal,
    string Message);
