namespace Luma.Domain.Entities;

public class Dream
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FutureProfile? FutureProfile { get; set; }
    public FutureScenario? FutureScenario { get; set; }
    public List<Goal> Goals { get; set; } = new();
    public List<DailyAction> DailyActions { get; set; } = new();
    public List<DailyPlanSnapshot> DailyPlanSnapshots { get; set; } = new();
    public List<DreamReflection> Reflections { get; set; } = new();
}
