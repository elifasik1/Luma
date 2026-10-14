namespace Luma.Application.Models;

public sealed record ReflectionResponse(
    Guid Id,
    Guid DreamId,
    string Mood,
    string Notes,
    string Insight,
    DateTime CreatedAt);
