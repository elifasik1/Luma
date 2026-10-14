namespace Luma.Domain.Entities;

public class DailyPlanActionSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DailyPlanSnapshotId { get; set; }
    public DailyPlanSnapshot? DailyPlanSnapshot { get; set; }
    public Guid? SourceDailyActionId { get; set; }
    public Guid? GoalId { get; set; }
    public string GoalTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeOfDay { get; set; } = "Morning";
    public int EstimatedMinutes { get; set; } = 15;
    public string Intention { get; set; } = string.Empty;
    public int Order { get; set; } = 1;
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAt { get; set; }
}