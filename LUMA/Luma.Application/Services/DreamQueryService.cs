using System.Text.Json;
using Luma.Application.Interfaces;
using Luma.Application.Models;

namespace Luma.Application.Services;

public sealed class DreamQueryService
{
    private readonly IDreamRepository _dreamRepository;

    public DreamQueryService(IDreamRepository dreamRepository)
    {
        _dreamRepository = dreamRepository;
    }

    public async Task<DreamDetailsResponse?> GetDreamDetailsAsync(
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        var dream = await _dreamRepository.GetByIdAsync(dreamId, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        var futureProfile = dream.FutureProfile;

        return new DreamDetailsResponse(
            dream.Id,
            dream.Title,
            dream.Description,
            dream.CreatedAt,
            dream.UserId,
            futureProfile is null
                ? null
                : new FutureProfileDetailsResponse(
                    futureProfile.Id,
                    futureProfile.Identity,
                    futureProfile.Environment,
                    futureProfile.Lifestyle.ToArray(),
                    futureProfile.Aspirations.ToArray(),
                    futureProfile.Values.ToArray(),
                    futureProfile.Mood,
                    futureProfile.Pace,
                    futureProfile.Summary,
                    futureProfile.TargetYear,
                    futureProfile.TargetDate,
                    futureProfile.ThemeConfig is null
                        ? null
                        : new ThemeConfigDetailsResponse(
                            futureProfile.ThemeConfig.Id,
                            futureProfile.ThemeConfig.Palette,
                            futureProfile.ThemeConfig.Atmosphere,
                            futureProfile.ThemeConfig.Imagery
                                .Select(imagery => JsonNamingPolicy.CamelCase.ConvertName(imagery.ToString()))
                                .ToArray(),
                            futureProfile.ThemeConfig.Illustration,
                            futureProfile.ThemeConfig.Motion,
                            futureProfile.ThemeConfig.CoreAnchorKeys.ToArray())));
    }
}
