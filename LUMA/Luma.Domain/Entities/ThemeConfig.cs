namespace Luma.Domain.Entities;

public class ThemeConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FutureProfileId { get; set; }
    public FutureProfile? FutureProfile { get; set; }
    public string Palette { get; set; } = "lavender_milk";
    public string Atmosphere { get; set; } = "calm";
    public string Illustration { get; set; } = "dreamer";
    public string Motion { get; set; } = "soft";
    public List<ThemeImagery> Imagery { get; set; } = new();
    public List<string> CoreAnchorKeys { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
