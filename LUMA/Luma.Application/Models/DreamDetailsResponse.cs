namespace Luma.Application.Models;

public sealed record DreamDetailsResponse(
    Guid Id,
    string Title,
    string RawInput,
    DateTime CreatedAt,
    Guid UserId,
    FutureProfileDetailsResponse? FutureProfile);

public sealed record FutureProfileDetailsResponse(
    Guid Id,
    string Identity,
    string Environment,
    IReadOnlyList<string> Lifestyle,
    IReadOnlyList<string> Aspirations,
    IReadOnlyList<string> Values,
    string Mood,
    string Pace,
    string? Summary,
    int TargetYear,
    DateOnly TargetDate,
    ThemeConfigDetailsResponse? ThemeConfig);

public sealed record ThemeConfigDetailsResponse(
    Guid Id,
    string Palette,
    string Atmosphere,
    IReadOnlyList<string> Imagery,
    string Illustration,
    string Motion,
    IReadOnlyList<string> CoreAnchorKeys);
