namespace Luma.Application.Models;

public record DreamAnalysisResult(
    Guid DreamId,
    Guid FutureProfileId,
    Guid ThemeConfigId,
    string Title,
    string Summary,
    string Environment,
    string Mood,
    string Palette,
    string Atmosphere,
    string Illustration,
    string Motion);
