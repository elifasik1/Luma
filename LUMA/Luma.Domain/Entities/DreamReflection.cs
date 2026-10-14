namespace Luma.Domain.Entities;

public class DreamReflection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DreamId { get; set; }
    public Dream? Dream { get; set; }
    public Guid? DailyPlanSnapshotId { get; set; }
    public DailyPlanSnapshot? DailyPlanSnapshot { get; set; }
    public string Mood { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Insight { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
