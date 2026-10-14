namespace Luma.Application.Models;

public sealed record GoalResponse(
    Guid Id,
    Guid DreamId,
    string Title,
    string Description,
    string Category,
    string WhyItMatters,
    string DesiredOutcome,
    string CurrentState,
    string Gap,
    string Status,
    int Order,
    DateTime CreatedAt);
