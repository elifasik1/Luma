namespace Luma.Application.Agent;

public sealed class DeterministicAgentActionPlanner : IAgentActionPlanner
{
    public Task<AgentActionDecision> DecideAsync(
        AgentPlannerRequest request,
        AgentPlan plan,
        IReadOnlyList<AgentToolExecutionResult> toolResults,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = request.Message ?? string.Empty;
        var lowerMessage = message.ToLowerInvariant();
        var asksForExplicitPlanChange =
            lowerMessage.Contains("set aside", StringComparison.Ordinal) ||
            lowerMessage.Contains("postpone", StringComparison.Ordinal) ||
            (lowerMessage.Contains("leave ", StringComparison.Ordinal) &&
             lowerMessage.Contains(" for today", StringComparison.Ordinal)) ||
            lowerMessage.Contains("plan lighter", StringComparison.Ordinal) ||
            lowerMessage.Contains("day lighter", StringComparison.Ordinal) ||
            lowerMessage.Contains("adjust my day", StringComparison.Ordinal) ||
            lowerMessage.Contains("adjust today", StringComparison.Ordinal) ||
            (lowerMessage.Contains("skip ", StringComparison.Ordinal) &&
             lowerMessage.Contains(" today", StringComparison.Ordinal));
        var asksForLowEnergyPlanChange =
            lowerMessage.Contains("30 minutes", StringComparison.Ordinal) ||
            lowerMessage.Contains("30 dakika", StringComparison.Ordinal) ||
            lowerMessage.Contains("tired", StringComparison.Ordinal) ||
            lowerMessage.Contains("exhausted", StringComparison.Ordinal) ||
            lowerMessage.Contains("yorgun", StringComparison.Ordinal) ||
            lowerMessage.Contains("bitkin", StringComparison.Ordinal);
        var asksForPlanChange = asksForExplicitPlanChange || asksForLowEnergyPlanChange;

        var decision = asksForPlanChange
            ? new AgentActionDecision(
                ActionRequired: true,
                ActionName: AgentActionDecisionValidator.UpdateDailyPlanAction,
                Reason: asksForLowEnergyPlanChange
                    ? "The user described limited time or energy for today."
                    : "The user explicitly asked to modify today's plan.",
                RequiresApproval: true,
                ActionRequest: new AgentActionRequest(message.Trim()))
            : new AgentActionDecision(
                ActionRequired: false,
                ActionName: null,
                Reason: "The request can be answered from read-only context.",
                RequiresApproval: false,
                ActionRequest: null);

        return Task.FromResult(AgentActionDecisionValidator.Validate(decision));
    }
}
