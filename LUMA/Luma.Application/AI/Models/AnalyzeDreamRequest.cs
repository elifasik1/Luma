namespace Luma.Application.AI.Models;

public sealed record AnalyzeDreamRequest(
    Guid UserId,
    string DreamText,
    IReadOnlyDictionary<string, string>? DiscoveryAnswers = null);
