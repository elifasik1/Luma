using Luma.Application.Agent;
using Microsoft.Extensions.Logging;

namespace Luma.Infrastructure.AI;

public sealed class FallbackAgentResponseGenerator : IAgentResponseGenerator
{
    private readonly GeminiAgentResponseGenerator _primaryGenerator;
    private readonly DeterministicAgentResponseGenerator _fallbackGenerator;
    private readonly ILogger<FallbackAgentResponseGenerator> _logger;

    public FallbackAgentResponseGenerator(
        GeminiAgentResponseGenerator primaryGenerator,
        DeterministicAgentResponseGenerator fallbackGenerator,
        ILogger<FallbackAgentResponseGenerator> logger)
    {
        _primaryGenerator = primaryGenerator;
        _fallbackGenerator = fallbackGenerator;
        _logger = logger;
    }

    public async Task<AgentGeneratedResponse> GenerateAsync(
        AgentResponseGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryGenerator.GenerateAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini response generator unavailable ({ErrorType}). Using safe deterministic fallback.", ex.GetType().Name);
            return await _fallbackGenerator.GenerateAsync(request, cancellationToken);
        }
    }
}
