namespace Luma.Application.Agent;

/// <summary>
/// Temporary development planner. It produces an explicit plan from intent
/// signals so a future LLM planner can replace this class without changing
/// tool contracts or orchestration.
/// </summary>
public sealed class DeterministicAgentPlanner : IAgentPlanner
{
    private static readonly string[] FocusSignals =
    {
        "today", "focus", "plan", "daily", "action", "schedule", "what should i do"
    };

    private static readonly string[] FutureSignals =
    {
        "future", "life", "dream", "aspiration", "values", "vision", "imagine", "become"
    };

    private static readonly string[] ProgressSignals =
    {
        "progress", "completed", "complete", "done", "status", "momentum", "how am i doing"
    };

    private static readonly string[] ReflectionSignals =
    {
        "reflection", "reflections", "reflected", "thought", "thoughts", "notes", "insight", "past"
    };

    private static readonly string[] GoalSignals =
    {
        "goal", "goals", "working toward", "working towards", "what am i working on", "aim", "target", "objective",
        "hedef", "hedefler", "ne için çalışıyorum"
    };

    public Task<AgentPlan> PlanAsync(
        AgentPlannerRequest request,
        IReadOnlyList<AgentToolMetadata> availableTools,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = request.Message?.Trim() ?? string.Empty;
        var normalized = message.ToLowerInvariant();
        var selected = new List<string>();
        string intent;
        string reason;

        var focusScore = Score(normalized, FocusSignals);
        var futureScore = Score(normalized, FutureSignals);
        var progressScore = Score(normalized, ProgressSignals);
        var reflectionScore = Score(normalized, ReflectionSignals);
        var goalScore = Score(normalized, GoalSignals);

        if (focusScore > 0 && focusScore >= futureScore && focusScore >= progressScore && focusScore >= reflectionScore)
        {
            intent = "daily_focus";
            selected.AddRange(new[] { "get_daily_plan", "get_active_goals", "get_progress" });
            reason = "Daily focus requires the existing plan, active goals, and current progress.";
        }
        else if (progressScore > 0 && progressScore >= reflectionScore)
        {
            intent = "progress_review";
            selected.AddRange(new[] { "get_progress", "get_active_goals" });
            reason = "Progress review requires current progress and the goals that define it.";
        }
        else if (goalScore > 0)
        {
            intent = "general_context";
            selected.Add("get_active_goals");
            reason = "The message is about the user's goals and what they are working toward.";
        }
        else if (futureScore > 0 && futureScore >= reflectionScore)
        {
            intent = "future_context";
            selected.Add("get_future_context");
            reason = "The message is primarily about the user's desired future and life context.";
        }
        else if (reflectionScore > 0)
        {
            intent = "reflection_review";
            selected.AddRange(new[] { "get_recent_reflections", "get_future_context" });
            reason = "Reflection review uses persisted reflections and the future context they relate to.";
        }
        else
        {
            intent = "general_context";
            selected.Add("get_daily_plan");
            reason = "No specific intent signal was detected; use the minimum useful current context.";
        }

        var available = availableTools
            .Where(tool => tool.IsReadOnly)
            .Select(tool => tool.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var supportedTools = selected.Where(available.Contains).ToArray();

        return Task.FromResult(new AgentPlan(intent, supportedTools, reason));
    }

    private static int Score(string message, IEnumerable<string> signals)
    {
        return signals.Count(message.Contains);
    }
}
