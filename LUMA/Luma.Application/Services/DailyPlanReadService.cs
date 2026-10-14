using Luma.Application.Agent;
using Luma.Application.Interfaces;

namespace Luma.Application.Services;

public sealed class DailyPlanReadService
{
    private readonly IDailyPlanReadQuery _query;

    public DailyPlanReadService(IDailyPlanReadQuery query)
    {
        _query = query;
    }

    public async Task<DailyPlanResult?> GetAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[AGENT_TRACE] DailyPlanDreamId={context.DreamId}");
        var result = await _query.GetAsync(context.DreamId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        if (result.Actions.Count == 0)
        {
            return new DailyPlanResult(null);
        }

        var actions = result.Actions
            .OrderBy(action => action.Order)
            .Select(NormalizeAction)
            .ToArray();

        var total = actions.Length;
        var completed = actions.Count(action => string.Equals(action.Status, "Completed", StringComparison.OrdinalIgnoreCase));
        var skipped = actions.Count(action => string.Equals(action.Status, "Skipped", StringComparison.OrdinalIgnoreCase));
        var pending = actions.Count(action => string.Equals(action.Status, "Pending", StringComparison.OrdinalIgnoreCase));

        return new DailyPlanResult(new DailyPlanContext(
            actions,
            actions[0].GeneratedAt,
            total,
            completed,
            skipped,
            pending,
            GenerateMomentumMessage(total, completed, skipped)));
    }

    private static DailyActionContext NormalizeAction(DailyActionContext action)
    {
        return action with
        {
            Status = string.IsNullOrWhiteSpace(action.Status) ? "Pending" : action.Status
        };
    }

    private static string GenerateMomentumMessage(int total, int completed, int skipped)
    {
        if (total == 0)
        {
            return "Your day is open and ready for intentional direction.";
        }

        if (completed == total)
        {
            return "You have honored every intention for today. Your future is taking root in small, quiet ways.";
        }

        if (completed + skipped == total && completed > 0)
        {
            return "You took thoughtful steps and honored your boundaries today. Steady progress is calm progress.";
        }

        if (completed > 0)
        {
            return $"You have completed {completed} of {total} steps today. Each small action bridges today to tomorrow.";
        }

        if (skipped > 0)
        {
            return "Taking space when needed is part of the rhythm. Return whenever you feel ready.";
        }

        return "Every journey begins with a single intentional breath. Choose one gentle step to start.";
    }
}
