using Luma.Application.Agent;
using Luma.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Luma.Infrastructure.Persistence;

public sealed class FutureContextQuery : IFutureContextQuery
{
    private readonly LumaDbContext _dbContext;

    public FutureContextQuery(LumaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FutureContextResult?> GetAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        var dream = await _dbContext.Dreams
            .AsNoTracking()
            .Include(item => item.FutureProfile)
                .ThenInclude(profile => profile!.ThemeConfig)
            .Include(item => item.FutureScenario)
            .FirstOrDefaultAsync(item => item.Id == dreamId, cancellationToken);

        if (dream is null)
        {
            return null;
        }

        var profile = dream.FutureProfile;
        var theme = profile?.ThemeConfig;

        return new FutureContextResult(
            dream.Id,
            dream.Title,
            dream.Description,
            dream.CreatedAt,
            profile is null
                ? null
                : new FutureProfileContext(
                    profile.Identity,
                    profile.Environment,
                    profile.Lifestyle.ToArray(),
                    profile.Aspirations.ToArray(),
                    profile.Values.ToArray(),
                    profile.Mood,
                    profile.Pace,
                    profile.Summary,
                    theme is null
                        ? null
                        : new ThemeConfigContext(
                            theme.Palette,
                            theme.Atmosphere,
                            theme.Imagery.Select(item => item.ToString()).ToArray(),
                            theme.Illustration,
                            theme.Motion,
                            theme.CoreAnchorKeys.ToArray()),
                    profile.TargetYear),
            dream.FutureScenario is null
                ? null
                : new FutureScenarioContext(
                    dream.FutureScenario.Title,
                    dream.FutureScenario.Intro,
                    dream.FutureScenario.Morning,
                    dream.FutureScenario.Daytime,
                    dream.FutureScenario.Evening,
                    dream.FutureScenario.EmotionalTone,
                    dream.FutureScenario.ClosingReflection));
    }
}
