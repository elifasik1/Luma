namespace Luma.Application.Models;

public sealed record TalkToLumaResponse(
    string Reply,
    DailyPathChangeProposal? Proposal = null);

public sealed record DailyPathChangeProposal(
    string Type,
    Guid KeepActionId,
    string KeepActionTitle,
    int KeepActionEstimatedMinutes,
    IReadOnlyList<Guid> RestActionIds);
