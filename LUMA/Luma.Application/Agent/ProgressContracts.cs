namespace Luma.Application.Agent;

public sealed record ProgressResult(
    int TotalActions,
    int CompletedActions,
    int SkippedActions,
    int PendingActions,
    decimal CompletionRate,
    string MomentumMessage);
