using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Luma.Application.Agent;
using Luma.Infrastructure.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Luma.Infrastructure.AI;

public sealed class GeminiAgentPlanner : IAgentPlanner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAgentPlanner> _logger;

    public GeminiAgentPlanner(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAgentPlanner> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentPlan> PlanAsync(
        AgentPlannerRequest request,
        IReadOnlyList<AgentToolMetadata> availableTools,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured for Agent planning.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";
        var readTools = availableTools
            .Where(tool => tool.IsReadOnly && AgentPlanValidator.AllowedReadTools.Contains(tool.Name, StringComparer.OrdinalIgnoreCase))
            .Select(tool => new { name = tool.Name, description = tool.Description })
            .ToArray();

        var payload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = BuildSystemPrompt(readTools) } }
            },
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = request.Message ?? string.Empty } }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.1,
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
            _logger.LogError("Gemini Agent planner returned HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429 || statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException(
                    $"Gemini Agent planner is temporarily unavailable (HTTP {statusCode}).",
                    statusCode);
            }

            if (statusCode is 401 or 403)
            {
                throw new InvalidOperationException("Gemini Agent planner authentication failed.");
            }

            throw new InvalidOperationException($"Gemini Agent planner returned an error ({statusCode}).");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            throw new AgentPlannerValidationException("Gemini Agent planner returned malformed API JSON.");
        }

        if (geminiResponse?.Error is { } error)
        {
            if (error.Code is 429 or 500 or 502 or 503 or 504 ||
                string.Equals(error.Status, "UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(error.Status, "RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException(
                    "Gemini Agent planner is temporarily unavailable.",
                    error.Code);
            }

            throw new InvalidOperationException("Gemini Agent planner returned an API error.");
        }

        var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new AgentPlannerValidationException("Gemini Agent planner returned an empty plan.");
        }

        GeminiAgentPlanOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiAgentPlanOutput>(rawJson, JsonOptions);
        }
        catch (JsonException)
        {
            throw new AgentPlannerValidationException("Gemini Agent planner returned malformed plan JSON.");
        }

        if (output is null)
        {
            throw new AgentPlannerValidationException("Gemini Agent planner returned an empty plan object.");
        }

        var plan = new AgentPlan(
            output.Intent ?? string.Empty,
            output.SelectedTools?.ToArray() ?? Array.Empty<string>(),
            output.Reason ?? string.Empty);

        return AgentPlanValidator.Validate(plan, availableTools);
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

                throw new GeminiTransientException("Failed to communicate with Gemini Agent planner.", ex);
            }

            if (!IsTransientStatusCode(response.StatusCode) || attempt == maxAttempts)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(initialDelaySeconds * Math.Pow(2, attempt - 1), 10)), cancellationToken);
        }

        throw new GeminiTransientException("Gemini Agent planner retry loop ended unexpectedly.");
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static string BuildSystemPrompt(object[] readTools)
    {
        var toolJson = JsonSerializer.Serialize(readTools, JsonOptions);
        return $"""
You are the planning component of LUMA.
Your job is NOT to answer the user. Determine the minimum read-only context tools needed to answer the user's message.
Do not perform actions, mutate data, or select mutation tools. Return structured JSON only.

Available read tools:
- get_future_context: the user's desired future, Future Profile, and Future Scenario.
- get_active_goals: active goals and current gaps.
- get_daily_plan: today's/current Daily Path actions and statuses.
- get_progress: progress and completion information from Daily Actions.
- get_recent_reflections: recent persisted user reflections.

Distinguish semantic intent carefully:
- WHO/WHERE future context uses future profile and scenario.
- GOALS uses active goals and gaps.
- TODAY'S PLAN uses the daily plan and action state.
- PROGRESS uses completion/progress and usually its related goals.
- REFLECTIONS uses recent reflections and may use future context when useful.

Select the minimum useful set. Do not select every tool by default. Do not invent tools.
The application will reject any tool outside its read-only allowlist.

Read tool metadata:
{toolJson}
""";
    }

    private static object BuildResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                intent = new
                {
                    type = "STRING",
                    @enum = new[] { "daily_focus", "future_context", "progress_review", "reflection_review", "general_context" }
                },
                selectedTools = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "STRING",
                        @enum = AgentPlanValidator.AllowedReadTools.ToArray()
                    }
                },
                reason = new
                {
                    type = "STRING"
                }
            },
            required = new[] { "intent", "selectedTools", "reason" }
        };
    }
}
