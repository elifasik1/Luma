namespace Luma.Domain.Entities;

public class FutureScenario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DreamId { get; set; }
    public Dream? Dream { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public string Morning { get; set; } = string.Empty;
    public string Daytime { get; set; } = string.Empty;
    public string Evening { get; set; } = string.Empty;
    public string EmotionalTone { get; set; } = string.Empty;
    public string ClosingReflection { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
