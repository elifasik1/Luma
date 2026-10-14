using Luma.Application.AI.Models;

namespace Luma.Application.Models;

public sealed record SendLumaMessageRequest(
    string Message,
    IReadOnlyList<LumaConversationTurn>? History);
