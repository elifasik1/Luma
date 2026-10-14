using Luma.Application.AI.Models;

namespace Luma.Application.AI;

public interface IAIService
{
    Task<AnalyzeDreamResult> AnalyzeDreamAsync(
        AnalyzeDreamRequest request,
        CancellationToken cancellationToken = default);

    Task<AnalyzeDreamResult> EvolveFutureAsync(
        EvolveFutureRequest request,
        CancellationToken cancellationToken = default);

    Task<GenerateFutureScenarioResult> GenerateFutureScenarioAsync(
        GenerateFutureScenarioRequest request,
        CancellationToken cancellationToken = default);

    Task<GenerateGoalsResult> GenerateGoalsAsync(
        GenerateGoalsRequest request,
        CancellationToken cancellationToken = default);

    Task<GenerateDailyPathResult> GenerateDailyPathAsync(
        GenerateDailyPathRequest request,
        CancellationToken cancellationToken = default);

    Task<GenerateReflectionInsightResult> GenerateReflectionInsightAsync(
        GenerateReflectionInsightRequest request,
        CancellationToken cancellationToken = default);

    Task<TalkToLumaResult> TalkToLumaAsync(
        TalkToLumaRequest request,
        CancellationToken cancellationToken = default);
}
