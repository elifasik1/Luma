using System.Globalization;
using Luma.Application.Models;

namespace Luma.Application.Agent;

public sealed class DeterministicAgentResponseGenerator : IAgentResponseGenerator
{
    public Task<AgentGeneratedResponse> GenerateAsync(
        AgentResponseGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isTurkish = IsTurkish(request.UserMessage);
        var intent = DetectIntent(request.UserMessage);
        var toolResults = request.ToolResults
            .Where(result => result.Success)
            .Select(result => result.Data)
            .ToArray();

        if (TryGetPendingProposal(request.ActionProposal, out var proposal))
        {
            return Task.FromResult(new AgentGeneratedResponse(
                BuildProposalResponse(request.ActionProposal!, proposal, isTurkish)));
        }

        switch (intent)
        {
            case ResponseIntent.DailyPlanChange:
            {
                var plan = toolResults
                    .OfType<DailyPlanResult>()
                    .Select(result => result.Plan)
                    .FirstOrDefault(result => result is not null);
                if (plan is null)
                {
                    return Task.FromResult(new AgentGeneratedResponse(
                        isTurkish
                            ? "Bugünkü planını değiştirmek için mevcut bir günlük plana erişemiyorum. Herhangi bir değişiklik yapmadım."
                            : "I can't propose a change because I don't have an existing daily plan to adjust. I haven't changed anything."));
                }

                var pendingActions = plan.Actions.Where(IsPending).ToArray();
                if (pendingActions.Length < 2)
                {
                    return Task.FromResult(new AgentGeneratedResponse(
                        isTurkish
                            ? "Bugünkü planda en az iki bekleyen adım olmadığı için şu anda bir adımı erteleme önerisi sunamıyorum. Herhangi bir değişiklik yapmadım."
                            : "I can't propose setting aside a step because today's plan has fewer than two pending actions. I haven't changed anything."));
                }

                return Task.FromResult(new AgentGeneratedResponse(
                    isTurkish
                        ? "Bugünkü planında bir değişiklik henüz öneremedim. Herhangi bir değişiklik yapmadım; hangi adımı ertelemek istediğini belirtebilirsin."
                        : "I couldn't prepare a change proposal for today's plan. I haven't changed anything; you can tell me which step you'd like to set aside."));
            }
            case ResponseIntent.Progress:
            {
                var progress = toolResults.OfType<ProgressResult>().FirstOrDefault();
                if (progress is not null)
                {
                    return Task.FromResult(new AgentGeneratedResponse(BuildProgressResponse(progress, isTurkish)));
                }
                break;
            }
            case ResponseIntent.Goals:
            {
                var goal = toolResults
                    .OfType<ActiveGoalsResult>()
                    .SelectMany(result => result.Goals)
                    .FirstOrDefault(result => !string.IsNullOrWhiteSpace(result.Title));
                if (goal is not null)
                {
                    return Task.FromResult(new AgentGeneratedResponse(
                        isTurkish
                            ? "Şu anda üzerinde çalıştığın hedef: “" + goal.Title + "”."
                            : "One goal you're working toward is “" + goal.Title + "”."));
                }
                break;
            }
            case ResponseIntent.Future:
            {
                var future = toolResults.OfType<FutureContextResult>().FirstOrDefault();
                var response = future is null ? null : BuildFutureResponse(future, isTurkish);
                if (response is not null)
                {
                    return Task.FromResult(new AgentGeneratedResponse(response));
                }
                break;
            }
            case ResponseIntent.Reflection:
            {
                var reflection = toolResults
                    .OfType<RecentReflectionsResult>()
                    .SelectMany(result => result.Reflections)
                    .OrderByDescending(item => item.CreatedAt)
                    .FirstOrDefault();
                if (reflection is not null)
                {
                    var detail = FirstNonEmpty(reflection.Insight, reflection.Notes);
                    if (detail is not null)
                    {
                        return Task.FromResult(new AgentGeneratedResponse(
                            isTurkish
                                ? "Yakın tarihli bir yansımada şunu not etmiştin: “" + Truncate(detail, 180) + "”"
                                : "In a recent reflection, you noted: “" + Truncate(detail, 180) + "”"));
                    }
                }
                break;
            }
            case ResponseIntent.Emotional:
            {
                var plan = toolResults
                    .OfType<DailyPlanResult>()
                    .Select(result => result.Plan)
                    .FirstOrDefault(result => result is not null);
                var nextAction = plan?.Actions
                    .Where(IsPending)
                    .OrderBy(action => action.Order)
                    .FirstOrDefault();

                if (nextAction is not null && !string.IsNullOrWhiteSpace(nextAction.Title))
                {
                    return Task.FromResult(new AgentGeneratedResponse(
                        isTurkish
                            ? "Bugünün ağır geldiğini duyuyorum. Önünde “" + nextAction.Title + "” var; bir sonraki adımı küçük tutabiliriz."
                            : "I hear that today feels heavy. You have “" + nextAction.Title + "” ahead; we can keep the next step small."));
                }

                return Task.FromResult(new AgentGeneratedResponse(
                    isTurkish
                        ? "Bugünün ağır geldiğini duyuyorum. İstersen yalnızca şu anki en küçük adıma odaklanabiliriz."
                        : "I hear that today feels heavy. We can focus on just the smallest next step."));
            }
            case ResponseIntent.DailyPlan:
            {
                var plan = toolResults
                    .OfType<DailyPlanResult>()
                    .Select(result => result.Plan)
                    .FirstOrDefault(result => result is not null);
                if (plan is not null)
                {
                    var nextAction = plan.Actions
                        .Where(IsPending)
                        .OrderBy(action => action.Order)
                        .FirstOrDefault();
                    var response = nextAction is not null && !string.IsNullOrWhiteSpace(nextAction.Title)
                        ? BuildNextActionResponse(nextAction, isTurkish)
                        : isTurkish
                            ? "Bugünkü planında bekleyen bir adım görünmüyor."
                            : "There are no pending actions in today's plan.";
                    return Task.FromResult(new AgentGeneratedResponse(response));
                }
                break;
            }
        }

        return Task.FromResult(new AgentGeneratedResponse(
            isTurkish
                ? "Sana yardımcı olmak için buradayım. Şu anda neye odaklanmak istersin?"
                : "I'm here to help. What would be useful to focus on right now?"));
    }

    private static ResponseIntent DetectIntent(string message)
    {
        var normalized = message.ToLowerInvariant();
        if (ContainsAny(
                normalized,
                "set aside",
                "postpone",
                "plan lighter",
                "day lighter",
                "make my day lighter",
                "adjust my day",
                "adjust today's plan",
                "adjust today’s plan") ||
            (normalized.Contains("leave ", StringComparison.Ordinal) &&
             normalized.Contains(" for today", StringComparison.Ordinal)) ||
            (normalized.Contains("skip ", StringComparison.Ordinal) &&
             normalized.Contains(" today", StringComparison.Ordinal)))
        {
            return ResponseIntent.DailyPlanChange;
        }

        if (ContainsAny(normalized, "progress", "how am i doing", "completed", "completion", "momentum", "ilerleme", "ilerliyor", "nasıl gidiyor", "ne kadar ilerledim", "tamamladım"))
        {
            return ResponseIntent.Progress;
        }

        if (ContainsAny(normalized, "overwhelmed", "tired", "exhausted", "stressed", "stress", "anxious", "drained", "bunaldım", "bunalmış", "yorgun", "stresli", "kaygılı"))
        {
            return ResponseIntent.Emotional;
        }

        if (ContainsAny(normalized, "goal", "goals", "working toward", "working towards", "what am i working on", "aim", "hedef", "hedefler", "ne için çalışıyorum"))
        {
            return ResponseIntent.Goals;
        }

        if (ContainsAny(normalized, "future", "what kind of life", "life am i building", "future profile", "identity", "lifestyle", "dream", "hayat kuruyorum", "nasıl bir hayat", "gelecek", "kim ol"))
        {
            return ResponseIntent.Future;
        }

        if (ContainsAny(normalized, "reflection", "reflect", "past experience", "what have i noticed", "what did i learn", "insight", "reflections", "geçmiş", "yansıma", "fark ettim", "ne öğrendim"))
        {
            return ResponseIntent.Reflection;
        }

        if (ContainsAny(normalized, "today", "daily plan", "my plan", "my day", "what should i do", "next action", "bugün", "planım", "günüm", "hangi adım"))
        {
            return ResponseIntent.DailyPlan;
        }

        return ResponseIntent.General;
    }

    private static bool TryGetPendingProposal(
        AgentActionProposal? actionProposal,
        out DailyPathChangeProposal proposal)
    {
        proposal = null!;
        if (actionProposal is null ||
            !string.Equals(actionProposal.ActionName, AgentActionDecisionValidator.UpdateDailyPlanAction, StringComparison.OrdinalIgnoreCase) ||
            !actionProposal.RequiresApproval ||
            actionProposal.ProposalResult.Data is not UpdateDailyPlanResult result ||
            result.Applied ||
            !result.RequiresApproval ||
            result.Proposal is null ||
            !string.Equals(result.Proposal.Type, "suggest_plan_change", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        proposal = result.Proposal;
        return true;
    }

    private static string BuildProposalResponse(
        AgentActionProposal actionProposal,
        DailyPathChangeProposal proposal,
        bool isTurkish)
    {
        var reason = NormalizeProposalReason(actionProposal.Reason, isTurkish);
        var action = string.IsNullOrWhiteSpace(proposal.KeepActionTitle)
            ? null
            : proposal.KeepActionTitle.Trim();
        var minutes = proposal.KeepActionEstimatedMinutes > 0
            ? proposal.KeepActionEstimatedMinutes.ToString(CultureInfo.InvariantCulture)
            : null;

        if (isTurkish)
        {
            var reasonText = reason is null ? string.Empty : reason + " ";
            var turkishStep = action is null ? "bugünkü adımlarından birini" : "“" + action + "” adımını";
            if (minutes is not null)
            {
                turkishStep += " (" + minutes + " dk)";
            }
            var turkishRestCount = proposal.RestActionIds.Count;
            var turkishRest = turkishRestCount == 1 ? "diğer adımı" : turkishRestCount + " diğer adımı";
            return reasonText + turkishStep + " bugünün tek küçük adımı olarak bırakıp " + turkishRest +
                   " dinlenmeye alabilirim. İstersen bu değişikliği onaylayabilirsin.";
        }

        var step = action is null ? "one of today's steps" : "“" + action + "”";
        if (minutes is not null)
        {
            step += " (" + minutes + " min)";
        }
        var restCount = proposal.RestActionIds.Count;
        var rest = restCount == 1 ? "the other step" : restCount.ToString(CultureInfo.InvariantCulture) + " other steps";
        var proposalText = "I can keep " + step + " as today's one small step and let " + rest + " rest.";
        var reasonPrefix = reason is null ? string.Empty : reason + " ";
        return reasonPrefix + proposalText + " Would you like me to make that change?";
    }

    private static string? NormalizeProposalReason(string reason, bool isTurkish)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var normalized = reason.Trim().TrimEnd('.');
        if (normalized.StartsWith("The user described ", StringComparison.OrdinalIgnoreCase))
        {
            var detail = normalized["The user described ".Length..];
            if (isTurkish && detail.Equals("limited time or energy for today", StringComparison.OrdinalIgnoreCase))
            {
                return "Bugün için sınırlı zamanın veya enerjin olduğunu paylaştın;";
            }

            return isTurkish
                ? "Paylaştığın durumu dikkate alarak"
                : "Because you described " + detail + ",";
        }

        return isTurkish
            ? "Paylaştığın durumu dikkate alarak"
            : normalized;
    }

    private static string BuildProgressResponse(ProgressResult progress, bool isTurkish)
    {
        return isTurkish
            ? "Bugün " + progress.TotalActions.ToString(CultureInfo.InvariantCulture) + " adımdan " +
              progress.CompletedActions.ToString(CultureInfo.InvariantCulture) + " tanesini tamamladın; " +
              progress.PendingActions.ToString(CultureInfo.InvariantCulture) + " adım bekliyor. " + progress.MomentumMessage
            : "You've completed " + progress.CompletedActions.ToString(CultureInfo.InvariantCulture) + " of " +
              progress.TotalActions.ToString(CultureInfo.InvariantCulture) + " actions, with " +
              progress.PendingActions.ToString(CultureInfo.InvariantCulture) + " pending. " + progress.MomentumMessage;
    }

    private static string? BuildFutureResponse(FutureContextResult future, bool isTurkish)
    {
        var profile = future.FutureProfile;
        var identity = profile is null ? null : FirstNonEmpty(profile.Identity, null);
        var summary = profile is null ? null : FirstNonEmpty(profile.Summary, null);
        var title = FirstNonEmpty(future.Title, null);
        var scenarioTitle = future.FutureScenario?.Title;

        if (title is not null && identity is not null && summary is not null)
        {
            return isTurkish
                ? "“" + title + "” içinde “" + identity + "” olarak şekillendirdiğin gelecek: " + summary
                : "In “" + title + ",” you're shaping a future as “" + identity + "”: " + summary;
        }

        if (title is not null && identity is not null)
        {
            return isTurkish
                ? "“" + title + "” içinde şekillendirdiğin gelecek “" + identity + "” kimliğini taşıyor."
                : "In “" + title + ",” the future you're shaping centers on “" + identity + "”.";
        }

        if (title is not null && summary is not null)
        {
            return isTurkish
                ? "“" + title + "”: " + summary
                : "“" + title + "”: " + summary;
        }

        if (identity is not null && summary is not null)
        {
            return isTurkish
                ? "Şekillendirdiğin gelecek “" + identity + "” kimliğini taşıyor: " + summary
                : "The future you're shaping centers on “" + identity + "”: " + summary;
        }

        if (identity is not null)
        {
            return isTurkish
                ? "Şekillendirdiğin gelecek “" + identity + "” kimliğini taşıyor."
                : "The future you're shaping centers on “" + identity + "”.";
        }

        if (summary is not null)
        {
            return summary;
        }

        if (!string.IsNullOrWhiteSpace(scenarioTitle))
        {
            return isTurkish
                ? "Olası gelecek senaryonun adı “" + scenarioTitle + "”."
                : "One possible future scenario is titled “" + scenarioTitle + "”.";
        }

        if (title is not null)
        {
            return isTurkish
                ? "Şekillendirdiğin olası gelecek “" + title + "” olarak adlandırılmış."
                : "The possible future you're shaping is titled “" + title + "”.";
        }

        return null;
    }

    private static string BuildNextActionResponse(DailyActionContext action, bool isTurkish)
    {
        var duration = action.EstimatedMinutes > 0
            ? " (" + action.EstimatedMinutes.ToString(CultureInfo.InvariantCulture) + (isTurkish ? " dk)" : " min)")
            : string.Empty;
        return isTurkish
            ? "Bugünkü sıradaki adımın “" + action.Title + "”" + duration + ". İstersen yalnızca buna odaklanabilirsin."
            : "Your next step today is “" + action.Title + "”" + duration + ". You can focus on just this for now.";
    }

    private static bool IsPending(DailyActionContext action) =>
        string.IsNullOrWhiteSpace(action.Status) ||
        string.Equals(action.Status, "Pending", StringComparison.OrdinalIgnoreCase);

    private static bool IsTurkish(string message) =>
        ContainsAny(message.ToLowerInvariant(), "bugün", "bunaldım", "bunalmış", "yorgun", "hedef", "gelecek", "yansıma", "ilerleme", "ilerliyor", "son zamanlarda") ||
        message.IndexOfAny(new[] { 'ı', 'İ', 'ş', 'Ş', 'ğ', 'Ğ', 'ü', 'Ü', 'ö', 'Ö', 'ç', 'Ç' }) >= 0;

    private static bool ContainsAny(string message, params string[] phrases) =>
        phrases.Any(message.Contains);

    private static string? FirstNonEmpty(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first) ? first.Trim() :
        !string.IsNullOrWhiteSpace(second) ? second.Trim() :
        null;

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength] + "…";

    private enum ResponseIntent
    {
        General,
        DailyPlanChange,
        Progress,
        Goals,
        Future,
        Reflection,
        Emotional,
        DailyPlan
    }
}
