namespace Luma.Application.Agent;

public static class AgentPlanValidator
{
    public static IReadOnlyList<string> AllowedReadTools { get; } = new[]
    {
        "get_future_context",
        "get_active_goals",
        "get_daily_plan",
        "get_progress",
        "get_recent_reflections"
    };

    private static readonly IReadOnlySet<string> AllowedIntents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "daily_focus",
        "future_context",
        "progress_review",
        "reflection_review",
        "general_context"
    };

    public static AgentPlan Validate(
        AgentPlan plan,
        IReadOnlyList<AgentToolMetadata> availableTools)
    {
        if (plan is null || string.IsNullOrWhiteSpace(plan.Intent))
        {
            throw new AgentPlannerValidationException("Planner returned an invalid intent.");
        }

        if (!AllowedIntents.Contains(plan.Intent))
        {
            throw new AgentPlannerValidationException($"Planner returned unsupported intent '{plan.Intent}'.");
        }

        var availableReadTools = availableTools
            .Where(tool => tool.IsReadOnly)
            .Select(tool => tool.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedTools = plan.SelectedTools ?? Array.Empty<string>();
        var invalidTools = selectedTools
            .Where(tool => !AllowedReadTools.Contains(tool, StringComparer.OrdinalIgnoreCase) ||
                           !availableReadTools.Contains(tool))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (invalidTools.Length > 0)
        {
            throw new AgentPlannerValidationException(
                $"Planner returned disallowed or unavailable tool(s): {string.Join(", ", invalidTools)}.");
        }

        var selected = selectedTools
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return plan with { SelectedTools = selected };
    }
}
