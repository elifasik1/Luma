using System.Text.Json;
using System.Text.Json.Serialization;
using Luma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Luma.Infrastructure.Persistence;

public class LumaDbContext : DbContext
{
    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        value => value.ToList());
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    private static readonly ValueComparer<List<ThemeImagery>> ThemeImageryListComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
        value => value.ToList());

    public LumaDbContext(DbContextOptions<LumaDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Dream> Dreams => Set<Dream>();
    public DbSet<FutureProfile> FutureProfiles => Set<FutureProfile>();
    public DbSet<ThemeConfig> ThemeConfigs => Set<ThemeConfig>();
    public DbSet<FutureScenario> FutureScenarios => Set<FutureScenario>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<DailyAction> DailyActions => Set<DailyAction>();
    public DbSet<DailyPlanSnapshot> DailyPlanSnapshots => Set<DailyPlanSnapshot>();
    public DbSet<DailyPlanActionSnapshot> DailyPlanActionSnapshots => Set<DailyPlanActionSnapshot>();
    public DbSet<DreamReflection> Reflections => Set<DreamReflection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .Property(user => user.PasswordHash)
            .IsRequired(false);

        modelBuilder.Entity<User>()
            .Property(user => user.ThemePreference)
            .IsRequired(false);

        modelBuilder.Entity<User>()
            .HasMany(user => user.Dreams)
            .WithOne(dream => dream.User)
            .HasForeignKey(dream => dream.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dream>()
            .HasOne(dream => dream.FutureProfile)
            .WithOne(futureProfile => futureProfile.Dream)
            .HasForeignKey<FutureProfile>(futureProfile => futureProfile.DreamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dream>()
            .HasOne(dream => dream.FutureScenario)
            .WithOne(futureScenario => futureScenario.Dream)
            .HasForeignKey<FutureScenario>(futureScenario => futureScenario.DreamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dream>()
            .HasMany(dream => dream.Goals)
            .WithOne(goal => goal.Dream)
            .HasForeignKey(goal => goal.DreamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dream>()
            .HasMany(dream => dream.DailyActions)
            .WithOne(action => action.Dream)
            .HasForeignKey(action => action.DreamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dream>()
            .HasMany(dream => dream.DailyPlanSnapshots)
            .WithOne(snapshot => snapshot.Dream)
            .HasForeignKey(snapshot => snapshot.DreamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DailyPlanSnapshot>()
            .Property(snapshot => snapshot.PlanDate)
            .HasColumnType("date");

        modelBuilder.Entity<DailyAction>()
            .Property(action => action.ScheduledDate)
            .HasColumnType("date");

        modelBuilder.Entity<FutureProfile>()
            .Property(profile => profile.TargetDate)
            .HasColumnType("date")
            .HasDefaultValueSql("CURRENT_DATE");

        modelBuilder.Entity<DailyPlanSnapshot>()
            .HasIndex(snapshot => new { snapshot.DreamId, snapshot.PlanDate })
            .IsUnique();

        modelBuilder.Entity<DailyPlanSnapshot>()
            .HasMany(snapshot => snapshot.Actions)
            .WithOne(action => action.DailyPlanSnapshot)
            .HasForeignKey(action => action.DailyPlanSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DailyPlanActionSnapshot>()
            .HasIndex(action => new { action.DailyPlanSnapshotId, action.Order });

        modelBuilder.Entity<Dream>()
            .HasMany(dream => dream.Reflections)
            .WithOne(reflection => reflection.Dream)
            .HasForeignKey(reflection => reflection.DreamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DailyPlanSnapshot>()
            .HasMany(snapshot => snapshot.Reflections)
            .WithOne(reflection => reflection.DailyPlanSnapshot)
            .HasForeignKey(reflection => reflection.DailyPlanSnapshotId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DreamReflection>()
            .HasIndex(reflection => reflection.DailyPlanSnapshotId);
        modelBuilder.Entity<DreamReflection>()
            .HasIndex(reflection => reflection.DreamId)
            .HasDatabaseName("IX_Reflections_DreamId");
        modelBuilder.Entity<DreamReflection>()
            .HasIndex(reflection => new { reflection.DreamId, reflection.PlanDate });

        modelBuilder.Entity<FutureProfile>()
            .HasOne(futureProfile => futureProfile.ThemeConfig)
            .WithOne(themeConfig => themeConfig.FutureProfile)
            .HasForeignKey<ThemeConfig>(themeConfig => themeConfig.FutureProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureStringListProperty(modelBuilder, futureProfile => futureProfile.Lifestyle);
        ConfigureStringListProperty(modelBuilder, futureProfile => futureProfile.Aspirations);
        ConfigureStringListProperty(modelBuilder, futureProfile => futureProfile.Values);

        modelBuilder.Entity<ThemeConfig>()
            .Property(themeConfig => themeConfig.Imagery)
            .HasColumnType("jsonb")
            .HasConversion(
                values => JsonSerializer.Serialize(values, JsonOptions),
                value => JsonSerializer.Deserialize<List<ThemeImagery>>(value, JsonOptions) ?? new List<ThemeImagery>())
            .Metadata.SetValueComparer(ThemeImageryListComparer);

        modelBuilder.Entity<ThemeConfig>()
            .Property(themeConfig => themeConfig.CoreAnchorKeys)
            .HasColumnType("jsonb")
            .HasConversion(
                values => JsonSerializer.Serialize(values),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>())
            .Metadata.SetValueComparer(StringListComparer);
    }

    private static void ConfigureStringListProperty(
        ModelBuilder modelBuilder,
        System.Linq.Expressions.Expression<Func<FutureProfile, List<string>>> propertyExpression)
    {
        modelBuilder.Entity<FutureProfile>()
            .Property(propertyExpression)
            .HasColumnType("jsonb")
            .HasConversion(
                values => JsonSerializer.Serialize(values),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>())
            .Metadata.SetValueComparer(StringListComparer);
    }
}
