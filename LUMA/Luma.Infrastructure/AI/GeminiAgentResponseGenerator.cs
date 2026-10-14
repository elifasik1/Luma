using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Luma.Application.Agent;
using Luma.Infrastructure.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Luma.Infrastructure.AI;

public sealed class GeminiAgentResponseGenerator : IAgentResponseGenerator
{
    private const int MaxMessageLength = 4000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAgentResponseGenerator> _logger;

    public GeminiAgentResponseGenerator(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAgentResponseGenerator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentGeneratedResponse> GenerateAsync(
        AgentResponseGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured for Agent response generation.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";
        var contextJson = JsonSerializer.Serialize(new
        {
            request.Plan,
            request.ToolResults,
            request.ActionProposal
        }, JsonOptions);

        var payload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = BuildSystemPrompt() } }
            },
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart>
                    {
                        new() { Text = $"User message:\n{request.UserMessage}\n\nStructured LUMA context:\n{contextJson}" }
                    }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.4,
                ResponseMimeType = "application/json",
                ResponseSchema = BuildResponseSchema()
            }
        };

        var requestJson = JsonSerializer.Serialize(payload, JsonOptions);
        using var response = await SendWithRetryAsync(endpoint, requestJson, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini Agent response generator returned HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429 || statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException(
                    $"Gemini Agent response generation is temporarily unavailable (HTTP {statusCode}).",
                    statusCode);
            }

            if (statusCode is 401 or 403)
            {
                throw new InvalidOperationException("Gemini Agent response generation authentication failed.");
            }

            throw new InvalidOperationException($"Gemini Agent response generation returned an error ({statusCode}).");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            throw new AgentResponseGenerationValidationException("Gemini returned malformed response envelope JSON.");
        }

        if (geminiResponse?.Error is { } error)
        {
            if (error.Code is 429 or 500 or 502 or 503 or 504 ||
                string.Equals(error.Status, "UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(error.Status, "RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException(
                    "Gemini Agent response generation is temporarily unavailable.",
                    error.Code);
            }

            throw new InvalidOperationException("Gemini Agent response generation returned an API error.");
        }

        var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new AgentResponseGenerationValidationException("Gemini returned an empty response.");
        }

        GeminiAgentGeneratedResponseOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiAgentGeneratedResponseOutput>(rawJson, JsonOptions);
        }
        catch (JsonException)
        {
            throw new AgentResponseGenerationValidationException("Gemini returned malformed structured response JSON.");
        }

        if (output is null || string.IsNullOrWhiteSpace(output.Message))
        {
            throw new AgentResponseGenerationValidationException("Gemini returned an empty response message.");
        }

        var message = output.Message.Trim();
        if (message.Length > MaxMessageLength)
        {
            throw new AgentResponseGenerationValidationException("Gemini response message exceeded the allowed length.");
        }

        return new AgentGeneratedResponse(
            message,
            NormalizeOptional(output.FollowUpQuestion),
            NormalizeOptional(output.SuggestedNextStep));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildSystemPrompt()
    {
        return """
You are LUMA, a calm, personal, future-oriented AI companion.
The supplied tool results are the only source of truth for user-specific facts.
Never invent action, goal, progress, or Future Profile details.
Answer the user's actual message first, using only relevant provided context; do not mention every tool result.
Avoid generic motivational filler. Keep the response concise, natural, grounded, and useful.
Preserve LUMA's calm, personal tone and respond in the user's language.
Do not claim an action was completed or changed unless the supplied structured result proves it.
Only say an action was completed or applied when the supplied UpdateDailyPlanResult has Applied=true.
An approval-required action proposal is not an applied change: describe it accurately as a proposal and ask for explicit approval.
Do not claim to have changed a database. Do not make predictions or guarantees about the user's future;
a Future Scenario is only a plausible possibility.
Do not mention internal tool names, AgentPlan, registry, Gemini, databases, or implementation details.
This is interpretation only. Do not select or perform actions.

Return JSON only with:
- message: the natural-language answer
- followUpQuestion: optional question or null
- suggestedNextStep: optional safe next step or null
""";
    }

    private static object BuildResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                message = new { type = "STRING" },
                followUpQuestion = new { type = "STRING", nullable = true },
                suggestedNextStep = new { type = "STRING", nullable = true }
            },
            required = new[] { "message" }
        };
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string endpoint,
        string requestJson,
        CancellationToken cancellationToken)
    {
        var maxRetries = Math.Clamp(_options.MaxRetries, 0, 5);
        var initialDelaySeconds = Math.Clamp(_options.RetryDelaySeconds, 1, 5);
        var maxAttempts = maxRetries + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(initialDelaySeconds * Math.Pow(2, attempt - 1), 10)), cancellationToken);
                    continue;
                }

                throw new GeminiTransientException("Failed to communicate with Gemini Agent response generator.", ex);
            }

            if (!IsTransientStatusCode(response.StatusCode) || attempt == maxAttempts)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(initialDelaySeconds * Math.Pow(2, attempt - 1), 10)), cancellationToken);
        }

        throw new GeminiTransientException("Gemini Agent response generator retry loop ended unexpectedly.");
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}
