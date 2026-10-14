namespace Luma.Application.Models;

public record CreateDreamRequest(
    string Title,
    string Description,
    string? VisualWorldKey = null,
    IReadOnlyList<string>? DailyRhythmKeys = null,
    IReadOnlyList<string>? CoreAnchorKeys = null);
