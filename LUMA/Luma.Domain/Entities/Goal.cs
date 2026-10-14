namespace Luma.Domain.Entities;

public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DreamId { get; set; }
    public Dream? Dream { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "PersonalGrowth";
    public string WhyItMatters { get; set; } = string.Empty;
    public string DesiredOutcome { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string Gap { get; set; } = string.Empty;
    public string Status { get; set; } = "NotStarted";
    public int Order { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
