namespace Luma.Domain.Entities;

public class DailyPlanSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DreamId { get; set; }
    public Dream? Dream { get; set; }
    public DateOnly PlanDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<DailyPlanActionSnapshot> Actions { get; set; } = new();
    public List<DreamReflection> Reflections { get; set; } = new();
}