namespace Luma.Domain.Entities;

public class FutureProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DreamId { get; set; }
    public Dream? Dream { get; set; }
    public string Identity { get; set; } = "Dreamer";
    public string Environment { get; set; } = "city";
    public List<string> Lifestyle { get; set; } = new();
    public List<string> Aspirations { get; set; } = new();
    public List<string> Values { get; set; } = new();
    public string Mood { get; set; } = "hopeful";
    public string Pace { get; set; } = "active";
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ThemeConfig? ThemeConfig { get; set; }
}
