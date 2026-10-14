namespace Luma.Application.Agent;

public static class AgentActionDecisionValidator
{
    public const string UpdateDailyPlanAction = "update_daily_plan";

    public static AgentActionDecision Validate(AgentActionDecision decision)
    {
        if (decision is null)
        {
            throw new AgentActionDecisionValidationException("Action planner returned no decision.");
        }

        if (!decision.ActionRequired)
        {
            if (!string.IsNullOrWhiteSpace(decision.ActionName) ||
                decision.ActionRequest is not null ||
                decision.RequiresApproval)
            {
                throw new AgentActionDecisionValidationException(
                    "A no-action decision cannot contain an action or approval requirement.");
            }

            return decision with { Reason = NormalizeOptional(decision.Reason) };
        }

        if (!string.Equals(decision.ActionName, UpdateDailyPlanAction, StringComparison.OrdinalIgnoreCase))
        {
            throw new AgentActionDecisionValidationException(
                $"Unsupported Agent action '{decision.ActionName}'.");
        }

        if (!decision.RequiresApproval)
        {
            throw new AgentActionDecisionValidationException(
                "update_daily_plan requires explicit approval before application.");
        }

        if (decision.ActionRequest is null ||
            string.IsNullOrWhiteSpace(decision.ActionRequest.UserSituation))
        {
            throw new AgentActionDecisionValidationException(
                "update_daily_plan requires a non-empty user situation.");
        }

        var userSituation = decision.ActionRequest.UserSituation.Trim();
        if (userSituation.Length > 2000)
        {
            throw new AgentActionDecisionValidationException(
                "The update_daily_plan user situation is too long.");
        }

        var normalizedSituation = userSituation.ToLowerInvariant();
        if (normalizedSituation.Contains("apply immediately", StringComparison.Ordinal) ||
            normalizedSituation.Contains("immediately apply", StringComparison.Ordinal) ||
            normalizedSituation.Contains("without approval", StringComparison.Ordinal) ||
            normalizedSituation.Contains("skip approval", StringComparison.Ordinal))
        {
            throw new AgentActionDecisionValidationException(
                "Direct mutation instructions are not valid Agent action requests.");
        }

        return decision with
        {
            ActionName = UpdateDailyPlanAction,
            Reason = string.IsNullOrWhiteSpace(decision.Reason)
                ? userSituation
                : decision.Reason.Trim(),
            ActionRequest = new AgentActionRequest(userSituation)
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class AgentActionDecisionValidationException : Exception
{
    public AgentActionDecisionValidationException(string message)
        : base(message)
    {
    }
}
