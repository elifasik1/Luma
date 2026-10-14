using Luma.Application.AI;
using Luma.Application.AI.Models;
using Microsoft.Extensions.Logging;

namespace Luma.Infrastructure.AI;

public sealed class FallbackAIService : IAIService
{
    private readonly GeminiAIService _primaryService;
    private readonly DevelopmentAIService _fallbackService;
    private readonly ILogger<FallbackAIService> _logger;

    public FallbackAIService(
        GeminiAIService primaryService,
        DevelopmentAIService fallbackService,
        ILogger<FallbackAIService> logger)
    {
        _primaryService = primaryService;
        _fallbackService = fallbackService;
        _logger = logger;
    }

    public async Task<AnalyzeDreamResult> AnalyzeDreamAsync(
        AnalyzeDreamRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.AnalyzeDreamAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini unavailable ({ErrorType}). Falling back to DevelopmentAIService.", ex.GetType().Name);
            return await _fallbackService.AnalyzeDreamAsync(request, cancellationToken);
        }
    }

    public async Task<AnalyzeDreamResult> EvolveFutureAsync(
        EvolveFutureRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.EvolveFutureAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini unavailable while evolving a future ({ErrorType}). Falling back to DevelopmentAIService.", ex.GetType().Name);
            return await _fallbackService.EvolveFutureAsync(request, cancellationToken);
        }
    }

    public async Task<GenerateFutureScenarioResult> GenerateFutureScenarioAsync(
        GenerateFutureScenarioRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.GenerateFutureScenarioAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini unavailable ({ErrorType}). Falling back to DevelopmentAIService.", ex.GetType().Name);
            return await _fallbackService.GenerateFutureScenarioAsync(request, cancellationToken);
        }
    }

    public async Task<GenerateGoalsResult> GenerateGoalsAsync(
        GenerateGoalsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.GenerateGoalsAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini unavailable ({ErrorType}). Falling back to DevelopmentAIService.", ex.GetType().Name);
            return await _fallbackService.GenerateGoalsAsync(request, cancellationToken);
        }
    }

    public async Task<GenerateDailyPathResult> GenerateDailyPathAsync(
        GenerateDailyPathRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.GenerateDailyPathAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini unavailable ({ErrorType}). Falling back to DevelopmentAIService.", ex.GetType().Name);
            return await _fallbackService.GenerateDailyPathAsync(request, cancellationToken);
        }
    }

    public async Task<GenerateReflectionInsightResult> GenerateReflectionInsightAsync(
        GenerateReflectionInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.GenerateReflectionInsightAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini unavailable ({ErrorType}). Falling back to DevelopmentAIService.", ex.GetType().Name);
            return await _fallbackService.GenerateReflectionInsightAsync(request, cancellationToken);
        }
    }

    public async Task<TalkToLumaResult> TalkToLumaAsync(
        TalkToLumaRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryService.TalkToLumaAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini unavailable ({ErrorType}). Falling back to DevelopmentAIService.", ex.GetType().Name);
            return await _fallbackService.TalkToLumaAsync(request, cancellationToken);
        }
    }
}
