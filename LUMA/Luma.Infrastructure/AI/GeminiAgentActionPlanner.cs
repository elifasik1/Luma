using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Luma.Application.Agent;
using Luma.Infrastructure.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Luma.Infrastructure.AI;

public sealed class GeminiAgentActionPlanner : IAgentActionPlanner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAgentActionPlanner> _logger;

    public GeminiAgentActionPlanner(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAgentActionPlanner> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentActionDecision> DecideAsync(
        AgentPlannerRequest request,
        AgentPlan plan,
        IReadOnlyList<AgentToolExecutionResult> toolResults,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured for Agent action planning.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";
        var contextJson = SanitizeContextForModel(plan, toolResults);
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
                        new() { Text = $"User message:\n{request.Message}\n\nStructured read context:\n{contextJson}" }
                    }
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
            _logger.LogError("Gemini Agent action planner returned HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429 || statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException("Gemini Agent action planning is temporarily unavailable.", statusCode);
            }

            if (statusCode is 401 or 403)
            {
                throw new InvalidOperationException("Gemini Agent action planning authentication failed.");
            }

            throw new InvalidOperationException($"Gemini Agent action planner returned an error ({statusCode}).");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            throw new AgentActionDecisionValidationException("Gemini returned malformed action response envelope JSON.");
        }

        if (geminiResponse?.Error is { } error)
        {
            if (error.Code is 429 or 500 or 502 or 503 or 504 ||
                string.Equals(error.Status, "UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(error.Status, "RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException("Gemini Agent action planning is temporarily unavailable.", error.Code);
            }

            throw new InvalidOperationException("Gemini Agent action planner returned an API error.");
        }

        var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new AgentActionDecisionValidationException("Gemini returned an empty action decision.");
        }

        GeminiAgentActionDecisionOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiAgentActionDecisionOutput>(rawJson, JsonOptions);
        }
        catch (JsonException)
        {
            throw new AgentActionDecisionValidationException("Gemini returned malformed action decision JSON.");
        }

        if (output is null)
        {
            throw new AgentActionDecisionValidationException("Gemini returned no action decision.");
        }

        var decision = new AgentActionDecision(
            output.ActionRequired,
            output.ActionName,
            output.Reason,
            output.RequiresApproval,
            output.ActionRequest is null
                ? null
                : new AgentActionRequest(output.ActionRequest.UserSituation ?? string.Empty));

        return AgentActionDecisionValidator.Validate(decision);
    }

    private static string BuildSystemPrompt() => """
You are the action-decision component of LUMA.
Read the user's message and the structured read-only context, then decide whether an actionable proposal is needed.
You are not allowed to mutate data or apply any change.
The only supported action is update_daily_plan. It creates a proposal and always requires explicit user approval.
For informational questions, status requests, future context, goal questions, progress, or reflections that do not ask to change today's plan, return actionRequired false.
An explicit request to set aside, postpone, leave, or skip a task for today, or to make today's plan lighter, is an actionable daily-plan change request. Return actionRequired true for update_daily_plan with requiresApproval true, even when the same message also mentions keeping an important goal on track. A goal reference does not cancel an explicit request to change today's plan.
Never select complete_daily_action, create_reflection, delete_user, database operations, SQL, repository calls, or arbitrary IDs.
Do not provide UserId, DreamId, DailyAction IDs, or persistence instructions.
If update_daily_plan is selected, actionRequest must contain only userSituation describing the user's stated situation.
Example: when a user explicitly asks to postpone a named task until tomorrow while keeping an important goal on track, return actionRequired true for update_daily_plan with requiresApproval true.
If the user only asks for information or reflection, return actionRequired false.
Return JSON only.
""";

    private static string SanitizeContextForModel(
        AgentPlan plan,
        IReadOnlyList<AgentToolExecutionResult> toolResults)
    {
        var json = JsonSerializer.SerializeToNode(new { plan, toolResults }, JsonOptions)
            ?? new JsonObject();
        RemoveInternalIdentifiers(json);
        return json.ToJsonString(JsonOptions);
    }

    private static void RemoveInternalIdentifiers(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (property.Key.Contains("id", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(property.Key, "intent", StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject.Remove(property.Key);
                    continue;
                }

                if (property.Value is not null)
                {
                    RemoveInternalIdentifiers(property.Value);
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray.Where(item => item is not null).Cast<JsonNode>())
            {
                RemoveInternalIdentifiers(item);
            }
        }
    }

    private static object BuildResponseSchema() => new
    {
        type = "OBJECT",
        properties = new
        {
            actionRequired = new { type = "BOOLEAN" },
            actionName = new { type = "STRING", nullable = true, @enum = new[] { AgentActionDecisionValidator.UpdateDailyPlanAction } },
            reason = new { type = "STRING", nullable = true },
            requiresApproval = new { type = "BOOLEAN" },
            actionRequest = new
            {
                type = "OBJECT",
                nullable = true,
                properties = new { userSituation = new { type = "STRING" } },
                required = new[] { "userSituation" }
            }
        },
        required = new[] { "actionRequired", "requiresApproval" }
    };

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

                throw new GeminiTransientException("Failed to communicate with Gemini Agent action planner.", ex);
            }

            if (!IsTransientStatusCode(response.StatusCode) || attempt == maxAttempts)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(initialDelaySeconds * Math.Pow(2, attempt - 1), 10)), cancellationToken);
        }

        throw new GeminiTransientException("Gemini Agent action planner retry loop ended unexpectedly.");
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.TooManyRequests or
        HttpStatusCode.InternalServerError or
        HttpStatusCode.BadGateway or
        HttpStatusCode.ServiceUnavailable or
        HttpStatusCode.GatewayTimeout;
}
