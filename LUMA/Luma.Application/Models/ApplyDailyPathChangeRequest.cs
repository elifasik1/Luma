namespace Luma.Application.Models;

public sealed record ApplyDailyPathChangeRequest(
    Guid KeepActionId,
    IReadOnlyList<Guid> RestActionIds);
