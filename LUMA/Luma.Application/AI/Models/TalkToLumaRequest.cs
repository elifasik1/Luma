namespace Luma.Application.AI.Models;

public sealed record TalkToLumaRequest(
    Guid UserId,
    string DreamTitle,
    string DreamDescription,
    string Message,
    IReadOnlyList<LumaConversationTurn> History,
    LumaConversationContext Context);

public sealed record LumaConversationTurn(string Role, string Content);

public sealed record LumaConversationContext(
    FutureProfileConversationContext? FutureProfile,
    FutureScenarioConversationContext? FutureScenario,
    IReadOnlyList<GoalConversationContext> Goals,
    IReadOnlyList<DailyActionConversationContext> TodayActions,
    ConversationProgressContext Progress,
    IReadOnlyList<ReflectionConversationContext> Reflections);

public sealed record FutureProfileConversationContext(
    string Identity,
    string Environment,
    IReadOnlyList<string> Lifestyle,
    IReadOnlyList<string> Aspirations,
    IReadOnlyList<string> Values,
    string Mood,
    string Pace,
    string? Summary,
    int TargetYear);

public sealed record FutureScenarioConversationContext(
    string Title,
    string Intro,
    string Morning,
    string Daytime,
    string Evening,
    string EmotionalTone,
    string ClosingReflection);

public sealed record GoalConversationContext(
    string Title,
    string DesiredOutcome,
    string CurrentState,
    string Gap,
    string Status);

public sealed record DailyActionConversationContext(
    Guid Id,
    string Title,
    string GoalTitle,
    int EstimatedMinutes,
    string Status,
    string Intention);

public sealed record ConversationProgressContext(
    int TotalActions,
    int CompletedActions,
    int SkippedActions,
    int PendingActions);

public sealed record ReflectionConversationContext(
    string Mood,
    string Notes,
    string Insight,
    DateTime CreatedAt);

public sealed record TalkToLumaActionProposal(
    Guid KeepActionId,
    IReadOnlyList<Guid> RestActionIds);

public sealed record TalkToLumaResult(
    string Reply,
    TalkToLumaActionProposal? Proposal = null);
