namespace Luma.Application.Agent;

public static class RecentReflectionPolicy
{
    // The existing reflection API has no recent-window limit. Agent context uses
    // a deliberately small deterministic window to keep context bounded.
    public const int Limit = 5;
}

public sealed record RecentReflectionsResult(
    IReadOnlyList<ReflectionContext> Reflections,
    int Count);

public sealed record ReflectionContext(
    Guid Id,
    string Mood,
    string Notes,
    string Insight,
    DateTime CreatedAt);

public sealed record ReflectionReadResult(
    IReadOnlyList<ReflectionContext> Reflections);
