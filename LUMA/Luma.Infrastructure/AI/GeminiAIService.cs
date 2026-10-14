using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Domain.Entities;
using Luma.Infrastructure.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Luma.Infrastructure.AI;

public sealed class GeminiAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAIService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GeminiAIService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AnalyzeDreamResult> AnalyzeDreamAsync(
        AnalyzeDreamRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Gemini API key is not configured. Configure 'Gemini:ApiKey' via User Secrets or environment variables, or set 'AI:Provider' to 'Development'.");
            throw new InvalidOperationException("Gemini API key is not configured. Please set 'Gemini:ApiKey' or configure 'AI:Provider' to 'Development'.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        _logger.LogInformation("Starting dream analysis with Gemini model {Model} for User {UserId}", model, request.UserId);

        var requestPayload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = BuildSystemInstruction() } }
            },
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = BuildUserPrompt(request) } }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7,
                ResponseMimeType = "application/json",
                ResponseSchema = BuildResponseSchema()
            }
        };

        var requestJson = JsonSerializer.Serialize(requestPayload, JsonOptions);
        using var response = await SendWithRetryAsync(endpoint, requestJson, request.UserId, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API returned HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429)
            {
                throw new GeminiTransientException("Gemini AI rate limit or quota exceeded. Please wait a moment and try again.", statusCode);
            }

            if (statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException($"Gemini AI service is temporarily unavailable (HTTP {statusCode}).", statusCode);
            }

            if (statusCode == 401 || statusCode == 403)
            {
                throw new InvalidOperationException("Gemini AI authentication failed. Please verify the configured API key.");
            }

            throw new InvalidOperationException($"Gemini AI service returned an error ({statusCode}). Please try again later.");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse top-level Gemini API response for User {UserId}", request.UserId);
            throw new InvalidOperationException("Received an unreadable response from Gemini AI.", ex);
        }

        if (geminiResponse?.Error != null)
        {
            _logger.LogError("Gemini API error in response payload: {Message} (Code: {Code}, Status: {Status})",
                geminiResponse.Error.Message, geminiResponse.Error.Code, geminiResponse.Error.Status);

            var code = geminiResponse.Error.Code;
            var status = geminiResponse.Error.Status ?? string.Empty;

            if (code is 429 or 500 or 502 or 503 or 504 ||
                status.Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException($"Gemini AI service error: {geminiResponse.Error.Message}", code);
            }

            throw new InvalidOperationException($"Gemini AI service error: {geminiResponse.Error.Message}");
        }

        var candidate = geminiResponse?.Candidates?.FirstOrDefault();
        var rawJson = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogError("Gemini API returned an empty candidate/content payload for User {UserId}. FinishReason: {FinishReason}",
                request.UserId, candidate?.FinishReason ?? "unknown");
            throw new InvalidOperationException("Gemini AI returned an empty response.");
        }

        GeminiDreamAnalysisOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiDreamAnalysisOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize structured dream analysis JSON from Gemini for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to parse structured dream analysis from Gemini AI.", ex);
        }

        if (output == null)
        {
            _logger.LogError("Deserialized dream analysis output was null for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to process dream analysis from Gemini AI.");
        }

        _logger.LogInformation("Gemini dream analysis completed successfully for User {UserId}", request.UserId);

        return MapToAnalyzeDreamResult(output);
    }

    public Task<AnalyzeDreamResult> EvolveFutureAsync(
        EvolveFutureRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = new Dictionary<string, string>
        {
            ["futureEvolution"] = "true",
            ["reflectionDirection"] = request.ReflectionNotes,
            ["currentIdentity"] = request.CurrentIdentity,
            ["currentEnvironment"] = request.CurrentEnvironment,
            ["currentSummary"] = request.CurrentSummary,
            ["currentMood"] = request.CurrentMood,
            ["currentPace"] = request.CurrentPace,
            ["currentLifestyle"] = string.Join(", ", request.CurrentLifestyle),
            ["currentAspirations"] = string.Join(", ", request.CurrentAspirations),
            ["currentValues"] = string.Join(", ", request.CurrentValues)
        };

        return AnalyzeDreamAsync(
            new AnalyzeDreamRequest(
                request.UserId,
                $"The user is intentionally evolving the future described in this reflection: {request.ReflectionNotes}",
                context),
            cancellationToken);
    }

    public async Task<TalkToLumaResult> TalkToLumaAsync(
        TalkToLumaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Gemini API key is not configured for LUMA conversation.");
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";
        var contents = request.History
            .Select(turn => new GeminiContent
            {
                Role = turn.Role == "assistant" ? "model" : "user",
                Parts = new List<GeminiPart> { new() { Text = turn.Content } }
            })
            .ToList();

        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new List<GeminiPart> { new() { Text = request.Message } }
        });

        var payload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart>
                {
                    new()
                    {
                        Text = BuildConversationSystemInstruction(request)
                    }
                }
            },
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7,
                ResponseMimeType = "application/json",
                ResponseSchema = BuildConversationResponseSchema()
            }
        };

        var requestJson = JsonSerializer.Serialize(payload, JsonOptions);
        using var response = await SendWithRetryAsync(endpoint, requestJson, request.UserId, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini conversation API returned HTTP {StatusCode}: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429 || statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException(
                    $"Gemini AI service is temporarily unavailable (HTTP {statusCode}).",
                    statusCode);
            }

            if (statusCode is 401 or 403)
            {
                throw new InvalidOperationException("Gemini AI authentication failed. Please verify the configured API key.");
            }

            throw new InvalidOperationException($"Gemini AI service returned an error ({statusCode}). Please try again later.");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini conversation response for User {UserId}", request.UserId);
            throw new InvalidOperationException("Received an unreadable response from Gemini AI.", ex);
        }

        if (geminiResponse?.Error is { } error)
        {
            _logger.LogError(
                "Gemini conversation API error: {Message} (Code: {Code}, Status: {Status})",
                error.Message,
                error.Code,
                error.Status);

            if (error.Code is 429 or 500 or 502 or 503 or 504 ||
                error.Status is "UNAVAILABLE" or "RESOURCE_EXHAUSTED")
            {
                throw new GeminiTransientException(
                    "Gemini AI service is temporarily unavailable. Please try again shortly.",
                    error.Code);
            }

            throw new InvalidOperationException("Gemini AI could not complete this conversation turn.");
        }

        var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new InvalidOperationException("Gemini AI returned an empty response.");
        }

        GeminiConversationOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiConversationOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize structured Gemini conversation response for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to parse the LUMA response.", ex);
        }

        if (string.IsNullOrWhiteSpace(output?.Reply))
        {
            throw new InvalidOperationException("Gemini AI returned an empty response.");
        }

        TalkToLumaActionProposal? proposal = null;
        if (string.Equals(output.ActionProposalType, "suggest_plan_change", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(output.KeepActionId, out var keepActionId) &&
            output.RestActionIds is { Count: > 0 } &&
            output.RestActionIds.All(id => Guid.TryParse(id, out _)))
        {
            proposal = new TalkToLumaActionProposal(
                keepActionId,
                output.RestActionIds.Select(Guid.Parse).ToList());
        }

        return new TalkToLumaResult(output.Reply.Trim(), proposal);
    }

    private static string BuildConversationSystemInstruction(TalkToLumaRequest request)
    {
        var contextJson = JsonSerializer.Serialize(request.Context, JsonOptions);
        return $"""
You are LUMA, a calm and thoughtful companion helping someone build a life that matters to them.
You are not a generic chatbot, therapist, fortune teller, or task manager. Speak warmly and naturally,
ground each reply in the user's LUMA context, and keep replies concise. Do not promise outcomes.
When the user explicitly has limited time (e.g. 30 minutes) and/or low energy / tiredness, respond with empathy.
If there are at least two Pending actions in today's path, you should propose keeping one suitable Pending action
(preferably <= 30 minutes or the shortest step) and resting the remaining Pending actions.
When proposing a plan change:
- set actionProposalType to "suggest_plan_change"
- set keepActionId to the exact UUID of the Pending action to keep
- set restActionIds to the array of exact UUIDs of the other Pending actions to let rest/skip
Use only the exact action IDs supplied in context. Never select completed or already skipped actions.
This is only a proposal: do not change, create, or claim to have changed anything in your text reply.
For all other messages, or when there are fewer than 2 Pending actions, return actionProposalType "none", keepActionId "", and restActionIds [].

Dream: {request.DreamTitle}
Original intention: {request.DreamDescription}
Current LUMA context (future profile, possible scenario, goals and gaps, today's path and progress, reflections):
{contextJson}

Return JSON with "reply", "actionProposalType" ("none" or "suggest_plan_change"), "keepActionId",
and "restActionIds".
""";
    }

    private static object BuildConversationResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                reply = new
                {
                    type = "STRING",
                    description = "A concise, empathetic, context-aware response from LUMA."
                },
                actionProposalType = new
                {
                    type = "STRING",
                    @enum = new[] { "none", "suggest_plan_change" },
                    description = "Whether LUMA is proposing a bounded Daily Path adjustment."
                },
                keepActionId = new
                {
                    type = "STRING",
                    description = "The ID of the current Pending Daily Path action to keep, or an empty string."
                },
                restActionIds = new
                {
                    type = "ARRAY",
                    items = new { type = "STRING" },
                    description = "IDs of current Pending Daily Path actions proposed for resting; empty when there is no proposal."
                }
            },
            required = new[] { "reply", "actionProposalType", "keepActionId", "restActionIds" }
        };
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string endpoint,
        string requestJson,
        Guid userId,
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
                _logger.LogWarning("Gemini dream analysis request was cancelled for User {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Network or connectivity failure calling Gemini API for User {UserId}", userId);
                if (attempt < maxAttempts)
                {
                    var delaySeconds = Math.Min(initialDelaySeconds * Math.Pow(2, attempt - 1), 10);
                    _logger.LogWarning(
                        "Gemini API call failed with network/timeout error for User {UserId}; retrying attempt {NextAttempt}/{MaxAttempts} after {DelaySeconds} seconds",
                        userId, attempt + 1, maxAttempts, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }

                throw new GeminiTransientException("Failed to communicate with the Gemini AI service. Please check network connectivity or try again later.", ex);
            }

            if (!IsTransientStatusCode(response.StatusCode) || attempt == maxAttempts)
            {
                return response;
            }

            var delay = Math.Min(initialDelaySeconds * Math.Pow(2, attempt - 1), 10);
            _logger.LogWarning(
                "Gemini API returned transient HTTP {StatusCode} for User {UserId}; retrying attempt {NextAttempt}/{MaxAttempts} after {DelaySeconds} seconds",
                (int)response.StatusCode, userId, attempt + 1, maxAttempts, delay);
            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
        }

        throw new GeminiTransientException("Gemini retry loop ended unexpectedly.");
    }

    private static bool IsTransientStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode is System.Net.HttpStatusCode.TooManyRequests
            or System.Net.HttpStatusCode.InternalServerError
            or System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout;
    }

    private static string BuildSystemInstruction()
    {
        return """
You are LUMA, a future-oriented AI companion.

Core philosophy:
1. LUMA is NOT a fortune teller. It does NOT predict the future or make guarantees.
2. It creates a plausible, grounded future possibility based directly on what the user shares.
3. Keep the output grounded in the user's dream title, description, and discovery answers.
4. Avoid generic motivational clichés (e.g., 'reach for the stars', 'unlimited potential', 'anything is possible').
5. Tone: Empathetic, grounded, thoughtful, clear. The output should feel like: "This could be a version of your life."
6. You MUST strictly adhere to the controlled theme system. Choose semantic keys from the provided allowed enum options only. Never produce raw CSS, hex colors, or styling classes.
""";
    }

    private static string BuildUserPrompt(AnalyzeDreamRequest request)
    {
        var sb = new StringBuilder();
        if (request.DiscoveryAnswers?.TryGetValue("futureEvolution", out var isEvolution) == true &&
            string.Equals(isEvolution, "true", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("This is an intentional future-direction evolution, not a new discovery.");
            sb.AppendLine("Prioritize the user's reflectionDirection over the current profile when they conflict.");
            sb.AppendLine("Preserve compatible identity, values, and aspirations; update environment and controlled theme keys to reflect the new direction.");
            sb.AppendLine();
        }

        sb.AppendLine("Analyze the following dream to generate a structured future profile and theme configuration:");
        sb.AppendLine();
        sb.AppendLine($"Dream Description:\n{request.DreamText}");

        if (request.DiscoveryAnswers != null && request.DiscoveryAnswers.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Discovery Answers & Context:");
            foreach (var (key, value) in request.DiscoveryAnswers)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    sb.AppendLine($"- {key}: {value}");
                }
            }

            if (request.DiscoveryAnswers.TryGetValue("coreAnchorKeys", out var coreAnchorKeys) &&
                !string.IsNullOrWhiteSpace(coreAnchorKeys))
            {
                sb.AppendLine("Treat these Core Anchors as the user's explicit priorities and reflect them in future-profile values and aspirations.");
                sb.AppendLine("Do not infer or change palette, atmosphere, or motion from Core Anchors; the backend composes those from the controlled World and Rhythm selections.");
            }
        }

        return sb.ToString();
    }

    private static object BuildResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                futureProfile = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        identity = new
                        {
                            type = "STRING",
                            description = "Archetype or identity label representing the user in this future possibility (e.g., 'Creative Founder', 'Mindful Technologist', 'Coastal Artisan')."
                        },
                        environment = new
                        {
                            type = "STRING",
                            description = "Primary physical/social environment (e.g., 'city', 'nature', 'ocean', 'home', 'workspace', 'campus')."
                        },
                        lifestyle = new
                        {
                            type = "ARRAY",
                            items = new { type = "STRING" },
                            description = "3 to 5 key lifestyle rhythms or traits."
                        },
                        aspirations = new
                        {
                            type = "ARRAY",
                            items = new { type = "STRING" },
                            description = "3 to 5 core aspirations grounded in the dream."
                        },
                        values = new
                        {
                            type = "ARRAY",
                            items = new { type = "STRING" },
                            description = "3 to 5 guiding values."
                        },
                        mood = new
                        {
                            type = "STRING",
                            description = "The emotional atmosphere (e.g., 'calm', 'hopeful', 'creative', 'energetic', 'focused')."
                        },
                        pace = new
                        {
                            type = "STRING",
                            description = "The pace of life (e.g., 'steady', 'active', 'reflective', 'gentle')."
                        }
                    },
                    required = new[] { "identity", "environment", "lifestyle", "aspirations", "values", "mood", "pace" }
                },
                themeConfig = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        palette = new
                        {
                            type = "STRING",
                            @enum = new[] { "WarmDream", "LavenderMilk", "SunsetCream", "CityNight", "ForestMist", "OceanLight" },
                            description = "Controlled theme color palette token matching the mood."
                        },
                        atmosphere = new
                        {
                            type = "STRING",
                            @enum = new[] { "Airy", "Cozy", "Energetic", "Calm", "Creative", "Focused" },
                            description = "Controlled atmosphere styling token."
                        },
                        illustration = new
                        {
                            type = "STRING",
                            @enum = new[] { "Dreamer", "UrbanDreamer", "CreativeDreamer", "NatureDreamer", "ProfessionalDreamer" },
                            description = "Controlled illustration archetype."
                        },
                        motion = new
                        {
                            type = "STRING",
                            @enum = new[] { "Soft", "Gentle", "Floating", "Still" },
                            description = "Controlled motion dynamic."
                        },
                        imagery = new
                        {
                            type = "ARRAY",
                            items = new
                            {
                                type = "STRING",
                                @enum = new[] { "city", "nature", "ocean", "campus", "workspace", "bakery", "travel", "home", "books", "technology" }
                            },
                            description = "Controlled visual imagery motifs strictly selected from allowed enum values."
                        }
                    },
                    required = new[] { "palette", "atmosphere", "illustration", "motion", "imagery" }
                },
                summary = new
                {
                    type = "STRING",
                    description = "A concise 1-2 sentence grounded, vivid synthesis of this future possibility. Avoid clichés."
                }
            },
            required = new[] { "futureProfile", "themeConfig", "summary" }
        };
    }

    private static AnalyzeDreamResult MapToAnalyzeDreamResult(GeminiDreamAnalysisOutput output)
    {
        var fp = output.FutureProfile;
        var tc = output.ThemeConfig;

        var futureProfileAnalysis = new FutureProfileAnalysis(
            Identity: string.IsNullOrWhiteSpace(fp?.Identity) ? "Dreamer" : fp.Identity.Trim(),
            Environment: string.IsNullOrWhiteSpace(fp?.Environment) ? "city" : fp.Environment.Trim(),
            Lifestyle: fp?.Lifestyle is { Count: > 0 }
                ? fp.Lifestyle.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList()
                : new List<string> { "ambitious", "balanced" },
            Aspirations: fp?.Aspirations is { Count: > 0 }
                ? fp.Aspirations.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList()
                : new List<string> { "growth" },
            Values: fp?.Values is { Count: > 0 }
                ? fp.Values.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList()
                : new List<string> { "clarity", "purpose" },
            Mood: string.IsNullOrWhiteSpace(fp?.Mood) ? "hopeful" : fp.Mood.Trim(),
            Pace: string.IsNullOrWhiteSpace(fp?.Pace) ? "steady" : fp.Pace.Trim()
        );

        var themeConfigAnalysis = new ThemeConfigAnalysis(
            Palette: ParsePalette(tc?.Palette),
            Atmosphere: ParseAtmosphere(tc?.Atmosphere),
            Illustration: ParseIllustration(tc?.Illustration),
            Motion: ParseMotion(tc?.Motion),
            Imagery: ParseImagery(tc?.Imagery, futureProfileAnalysis.Environment)
        );

        var summary = string.IsNullOrWhiteSpace(output.Summary)
            ? $"This future is shaped around a {futureProfileAnalysis.Environment.Replace('_', ' ')} lifestyle, meaningful {string.Join(", ", futureProfileAnalysis.Aspirations)}, and a {futureProfileAnalysis.Mood} pace."
            : output.Summary.Trim();

        return new AnalyzeDreamResult(futureProfileAnalysis, themeConfigAnalysis, summary);
    }

    private static ThemePalette ParsePalette(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ThemePalette.WarmDream;

        var cleaned = value.Trim().Replace("_", "").Replace(" ", "");
        if (Enum.TryParse<ThemePalette>(cleaned, ignoreCase: true, out var result))
        {
            return result;
        }

        return ThemePalette.WarmDream;
    }

    private static ThemeAtmosphere ParseAtmosphere(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ThemeAtmosphere.Calm;

        var cleaned = value.Trim().Replace("_", "").Replace(" ", "");
        if (Enum.TryParse<ThemeAtmosphere>(cleaned, ignoreCase: true, out var result))
        {
            return result;
        }

        return ThemeAtmosphere.Calm;
    }

    private static ThemeIllustration ParseIllustration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ThemeIllustration.Dreamer;

        var cleaned = value.Trim().Replace("_", "").Replace(" ", "");
        if (Enum.TryParse<ThemeIllustration>(cleaned, ignoreCase: true, out var result))
        {
            return result;
        }

        return ThemeIllustration.Dreamer;
    }

    private static ThemeMotion ParseMotion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ThemeMotion.Soft;

        var cleaned = value.Trim().Replace("_", "").Replace(" ", "");
        if (Enum.TryParse<ThemeMotion>(cleaned, ignoreCase: true, out var result))
        {
            return result;
        }

        return ThemeMotion.Soft;
    }

    private static IReadOnlyList<ThemeImagery> ParseImagery(IEnumerable<string>? values, string environment)
    {
        var list = new List<ThemeImagery>();

        if (values != null)
        {
            foreach (var val in values)
            {
                if (string.IsNullOrWhiteSpace(val)) continue;
                var cleaned = val.Trim().Replace("_", "").Replace(" ", "");
                if (Enum.TryParse<ThemeImagery>(cleaned, ignoreCase: true, out var imagery) && !list.Contains(imagery))
                {
                    list.Add(imagery);
                }
            }
        }

        if (list.Count == 0)
        {
            var envCleaned = environment.Trim().Replace("_", "").Replace(" ", "");
            if (Enum.TryParse<ThemeImagery>(envCleaned, ignoreCase: true, out var envImagery))
            {
                list.Add(envImagery);
            }
            else
            {
                list.Add(ThemeImagery.City);
            }
        }

        return list;
    }

    public async Task<GenerateFutureScenarioResult> GenerateFutureScenarioAsync(
        GenerateFutureScenarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Gemini API key is not configured. Configure 'Gemini:ApiKey' via User Secrets or environment variables, or set 'AI:Provider' to 'Development'.");
            throw new InvalidOperationException("Gemini API key is not configured. Please set 'Gemini:ApiKey' or configure 'AI:Provider' to 'Development'.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        _logger.LogInformation("Starting future scenario generation with Gemini model {Model} for User {UserId}", model, request.UserId);

        var requestPayload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = BuildScenarioSystemInstruction() } }
            },
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = BuildScenarioUserPrompt(request) } }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7,
                ResponseMimeType = "application/json",
                ResponseSchema = BuildScenarioResponseSchema()
            }
        };

        var requestJson = JsonSerializer.Serialize(requestPayload, JsonOptions);
        using var response = await SendWithRetryAsync(endpoint, requestJson, request.UserId, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API returned HTTP {StatusCode} during scenario generation: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429)
            {
                throw new GeminiTransientException("Gemini AI rate limit or quota exceeded. Please wait a moment and try again.", statusCode);
            }

            if (statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException($"Gemini AI service is temporarily unavailable (HTTP {statusCode}).", statusCode);
            }

            if (statusCode == 401 || statusCode == 403)
            {
                throw new InvalidOperationException("Gemini AI authentication failed. Please verify the configured API key.");
            }

            throw new InvalidOperationException($"Gemini AI service returned an error ({statusCode}). Please try again later.");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse top-level Gemini API response for scenario generation for User {UserId}", request.UserId);
            throw new InvalidOperationException("Received an unreadable response from Gemini AI.", ex);
        }

        if (geminiResponse?.Error != null)
        {
            _logger.LogError("Gemini API error in scenario response payload: {Message} (Code: {Code}, Status: {Status})",
                geminiResponse.Error.Message, geminiResponse.Error.Code, geminiResponse.Error.Status);

            var code = geminiResponse.Error.Code;
            var status = geminiResponse.Error.Status ?? string.Empty;

            if (code is 429 or 500 or 502 or 503 or 504 ||
                status.Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException($"Gemini AI service error: {geminiResponse.Error.Message}", code);
            }

            throw new InvalidOperationException($"Gemini AI service error: {geminiResponse.Error.Message}");
        }

        var candidate = geminiResponse?.Candidates?.FirstOrDefault();
        var rawJson = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogError("Gemini API returned an empty candidate/content payload for scenario generation for User {UserId}. FinishReason: {FinishReason}",
                request.UserId, candidate?.FinishReason ?? "unknown");
            throw new InvalidOperationException("Gemini AI returned an empty response.");
        }

        GeminiFutureScenarioOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiFutureScenarioOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize structured future scenario JSON from Gemini for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to parse structured future scenario from Gemini AI.", ex);
        }

        if (output == null)
        {
            _logger.LogError("Deserialized future scenario output was null for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to process future scenario from Gemini AI.");
        }

        _logger.LogInformation("Gemini future scenario generation completed successfully for User {UserId}", request.UserId);

        return MapToGenerateFutureScenarioResult(output, request);
    }

    private static string BuildScenarioSystemInstruction()
    {
        return """
You are LUMA, a future-oriented AI companion.

Core philosophy:
1. LUMA is NOT a fortune teller. It does NOT predict the future or make guarantees.
2. The scenario MUST be framed as "A possible future" — a vivid, grounded day in a possible version of the user's life.
3. Tone: Emotionally warm, thoughtful, sensory, grounded. Avoid generic motivational clichés (e.g., 'reach for the stars', 'unlimited potential', 'anything is possible').
4. The scenario must be specific to the user's FutureProfile, aspirations, values, environment, mood, and pace.
5. Avoid exaggerated success fantasies (e.g., 'you are rich and famous'). Focus on authentic, everyday moments: waking up, working with intention, enjoying the evening, feeling grounded.
6. The user should read it and feel: "I can imagine myself there."
""";
    }

    private static string BuildScenarioUserPrompt(GenerateFutureScenarioRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate a concrete, plausible day scenario for this user's possible future:");
        sb.AppendLine();
        sb.AppendLine($"Dream Title: {request.DreamTitle}");
        sb.AppendLine($"Dream Description: {request.DreamDescription}");
        sb.AppendLine($"Identity: {request.Identity}");
        sb.AppendLine($"Environment: {request.Environment}");
        sb.AppendLine($"Lifestyle: {string.Join(", ", request.Lifestyle)}");
        sb.AppendLine($"Aspirations: {string.Join(", ", request.Aspirations)}");
        sb.AppendLine($"Values: {string.Join(", ", request.Values)}");
        sb.AppendLine($"Mood: {request.Mood}");
        sb.AppendLine($"Pace: {request.Pace}");
        sb.AppendLine($"Atmosphere: {request.Atmosphere}");
        if (request.Imagery.Count > 0)
        {
            sb.AppendLine($"Imagery motifs: {string.Join(", ", request.Imagery)}");
        }
        return sb.ToString();
    }

    private static object BuildScenarioResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                title = new
                {
                    type = "STRING",
                    description = "An evocative, poetic title for this possible day (e.g., 'A Tuesday in Your Possible Future', 'A Steady Morning by the Coast')."
                },
                intro = new
                {
                    type = "STRING",
                    description = "1-2 atmospheric, grounding sentences setting the scene for this possible day."
                },
                morning = new
                {
                    type = "STRING",
                    description = "A sensory paragraph describing waking up and the morning rhythm in this future environment."
                },
                daytime = new
                {
                    type = "STRING",
                    description = "A paragraph describing the daytime flow, meaningful pursuits, and focus aligned with their aspirations and identity."
                },
                evening = new
                {
                    type = "STRING",
                    description = "A paragraph describing winding down, quiet connection, or resting in their space as dusk falls."
                },
                emotionalTone = new
                {
                    type = "STRING",
                    description = "A short phrase describing the emotional resonance (e.g., 'Grounded, peaceful, and quietly energized')."
                },
                closingReflection = new
                {
                    type = "STRING",
                    description = "A gentle closing thought reinforcing that this is an open possibility shaped by their choices."
                }
            },
            required = new[] { "title", "intro", "morning", "daytime", "evening", "emotionalTone", "closingReflection" }
        };
    }

    private static GenerateFutureScenarioResult MapToGenerateFutureScenarioResult(
        GeminiFutureScenarioOutput output,
        GenerateFutureScenarioRequest request)
    {
        var title = string.IsNullOrWhiteSpace(output.Title)
            ? $"A Possible Day in Your {request.Environment.Replace('_', ' ')} Future"
            : output.Title.Trim();

        var intro = string.IsNullOrWhiteSpace(output.Intro)
            ? $"A quiet morning in a life shaped by what matters to you."
            : output.Intro.Trim();

        var morning = string.IsNullOrWhiteSpace(output.Morning)
            ? "The day begins gently, with space to breathe and focus."
            : output.Morning.Trim();

        var daytime = string.IsNullOrWhiteSpace(output.Daytime)
            ? "Your hours are dedicated to meaningful pursuits aligned with your values."
            : output.Daytime.Trim();

        var evening = string.IsNullOrWhiteSpace(output.Evening)
            ? "As evening arrives, you find time to unwind in a calm atmosphere."
            : output.Evening.Trim();

        var emotionalTone = string.IsNullOrWhiteSpace(output.EmotionalTone)
            ? $"Rooted and {request.Mood}"
            : output.EmotionalTone.Trim();

        var closingReflection = string.IsNullOrWhiteSpace(output.ClosingReflection)
            ? "A possible future, not a prediction. One small step at a time."
            : output.ClosingReflection.Trim();

        return new GenerateFutureScenarioResult(
            Title: title,
            Intro: intro,
            Morning: morning,
            Daytime: daytime,
            Evening: evening,
            EmotionalTone: emotionalTone,
            ClosingReflection: closingReflection);
    }

    public async Task<GenerateGoalsResult> GenerateGoalsAsync(
        GenerateGoalsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Gemini API key is not configured. Configure 'Gemini:ApiKey' via User Secrets or environment variables, or set 'AI:Provider' to 'Development'.");
            throw new InvalidOperationException("Gemini API key is not configured. Please set 'Gemini:ApiKey' or configure 'AI:Provider' to 'Development'.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        _logger.LogInformation("Starting goal generation with Gemini model {Model} for User {UserId}", model, request.UserId);

        var requestPayload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = BuildGoalsSystemInstruction() } }
            },
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = BuildGoalsUserPrompt(request) } }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7,
                ResponseMimeType = "application/json",
                ResponseSchema = BuildGoalsResponseSchema()
            }
        };

        var requestJson = JsonSerializer.Serialize(requestPayload, JsonOptions);
        using var response = await SendWithRetryAsync(endpoint, requestJson, request.UserId, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API returned HTTP {StatusCode} during goal generation: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429)
            {
                throw new GeminiTransientException("Gemini AI rate limit or quota exceeded. Please wait a moment and try again.", statusCode);
            }

            if (statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException($"Gemini AI service is temporarily unavailable (HTTP {statusCode}).", statusCode);
            }

            if (statusCode == 401 || statusCode == 403)
            {
                throw new InvalidOperationException("Gemini AI authentication failed. Please verify the configured API key.");
            }

            throw new InvalidOperationException($"Gemini AI service returned an error ({statusCode}). Please try again later.");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse top-level Gemini API response for goal generation for User {UserId}", request.UserId);
            throw new InvalidOperationException("Received an unreadable response from Gemini AI.", ex);
        }

        if (geminiResponse?.Error != null)
        {
            _logger.LogError("Gemini API error in goal response payload: {Message} (Code: {Code}, Status: {Status})",
                geminiResponse.Error.Message, geminiResponse.Error.Code, geminiResponse.Error.Status);

            var code = geminiResponse.Error.Code;
            var status = geminiResponse.Error.Status ?? string.Empty;

            if (code is 429 or 500 or 502 or 503 or 504 ||
                status.Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException($"Gemini AI service error: {geminiResponse.Error.Message}", code);
            }

            throw new InvalidOperationException($"Gemini AI service error: {geminiResponse.Error.Message}");
        }

        var candidate = geminiResponse?.Candidates?.FirstOrDefault();
        var rawJson = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogError("Gemini API returned an empty candidate/content payload for goal generation for User {UserId}. FinishReason: {FinishReason}",
                request.UserId, candidate?.FinishReason ?? "unknown");
            throw new InvalidOperationException("Gemini AI returned an empty response.");
        }

        GeminiGoalsOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiGoalsOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize structured goals JSON from Gemini for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to parse structured goals from Gemini AI.", ex);
        }

        if (output?.Goals == null || output.Goals.Count == 0)
        {
            _logger.LogError("Deserialized goals output was null or empty for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to process goals from Gemini AI.");
        }

        _logger.LogInformation("Gemini goal generation completed successfully with {Count} goals for User {UserId}", output.Goals.Count, request.UserId);

        return MapToGenerateGoalsResult(output, request);
    }

    private static string BuildGoalsSystemInstruction()
    {
        return """
You are LUMA, a future-oriented AI companion.

Core philosophy:
1. LUMA connects the user's possible future to meaningful life paths and bridges.
2. Goals represent meaningful life direction (3 to 4 goals strictly), NOT a generic todo list or tiny tasks (no 'drink water', no 'open laptop', no 'work for 30 minutes').
3. Personalize every goal directly to the user's FutureProfile, aspirations, values, environment, and scenario.
4. Do NOT invent facts about the user's current life (e.g., do not assume unemployment, specific relationship status, or unmentioned facts). If unknown, use cautious, uncertainty-respecting formulations for CurrentState (e.g., 'Your starting point is a foundation ready for focused development').
5. Gap is NOT a negative judgment or criticism. Frame it with hope and empowerment (e.g., 'The next bridge could be...', 'The distance between today and this future may involve dedicating...').
6. Allowed categories: 'Career', 'Lifestyle', 'Creativity', 'PersonalGrowth', 'Financial', 'Health', 'Travel', 'Relationships'.
7. Status should be 'NotStarted'.
""";
    }

    private static string BuildGoalsUserPrompt(GenerateGoalsRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate 3 to 4 meaningful, directional goals and gaps connecting today to this possible future:");
        sb.AppendLine();
        sb.AppendLine($"Dream Title: {request.DreamTitle}");
        sb.AppendLine($"Dream Description: {request.DreamDescription}");
        sb.AppendLine($"Identity: {request.Identity}");
        sb.AppendLine($"Environment: {request.Environment}");
        sb.AppendLine($"Lifestyle: {string.Join(", ", request.Lifestyle)}");
        sb.AppendLine($"Aspirations: {string.Join(", ", request.Aspirations)}");
        sb.AppendLine($"Values: {string.Join(", ", request.Values)}");
        sb.AppendLine($"Mood: {request.Mood}");
        sb.AppendLine($"Pace: {request.Pace}");
        if (!string.IsNullOrWhiteSpace(request.ScenarioTitle))
        {
            sb.AppendLine($"Possible Future Day: {request.ScenarioTitle}");
            sb.AppendLine($"Scenario Intro: {request.ScenarioIntro}");
            sb.AppendLine($"Daytime Focus: {request.ScenarioDaytime}");
            sb.AppendLine($"Emotional Tone: {request.ScenarioEmotionalTone}");
        }
        return sb.ToString();
    }

    private static object BuildGoalsResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                goals = new
                {
                    type = "ARRAY",
                    description = "3 to 4 meaningful life-direction goals bridging current state to the possible future.",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            title = new { type = "STRING", description = "A clear, directional goal title." },
                            description = new { type = "STRING", description = "1-2 sentences elaborating the goal's focus." },
                            category = new { type = "STRING", @enum = new[] { "Career", "Lifestyle", "Creativity", "PersonalGrowth", "Financial", "Health", "Travel", "Relationships" }, description = "Core life category." },
                            whyItMatters = new { type = "STRING", description = "Why this goal is foundational for the user's possible future." },
                            desiredOutcome = new { type = "STRING", description = "The tangible future state once this bridge is built." },
                            currentState = new { type = "STRING", description = "Cautious formulation of starting point without assuming unstated facts." },
                            gap = new { type = "STRING", description = "Hopeful, empowering description of the distance to bridge." },
                            status = new { type = "STRING", @enum = new[] { "NotStarted", "Active", "Completed" } }
                        },
                        required = new[] { "title", "description", "category", "whyItMatters", "desiredOutcome", "currentState", "gap", "status" }
                    }
                }
            },
            required = new[] { "goals" }
        };
    }

    private static GenerateGoalsResult MapToGenerateGoalsResult(
        GeminiGoalsOutput output,
        GenerateGoalsRequest request)
    {
        var items = new List<GoalAnalysisItem>();
        var order = 1;

        if (output.Goals != null)
        {
            foreach (var g in output.Goals.Take(4))
            {
                items.Add(new GoalAnalysisItem(
                    Title: string.IsNullOrWhiteSpace(g.Title) ? $"Goal {order}" : g.Title.Trim(),
                    Description: string.IsNullOrWhiteSpace(g.Description) ? "Take meaningful steps toward this future." : g.Description.Trim(),
                    Category: string.IsNullOrWhiteSpace(g.Category) ? "PersonalGrowth" : g.Category.Trim(),
                    WhyItMatters: string.IsNullOrWhiteSpace(g.WhyItMatters) ? "This supports your long-term life vision." : g.WhyItMatters.Trim(),
                    DesiredOutcome: string.IsNullOrWhiteSpace(g.DesiredOutcome) ? "Closer alignment with your desired future." : g.DesiredOutcome.Trim(),
                    CurrentState: string.IsNullOrWhiteSpace(g.CurrentState) ? "Your starting point is ready for focused growth." : g.CurrentState.Trim(),
                    Gap: string.IsNullOrWhiteSpace(g.Gap) ? "Dedicate regular focus to building this foundation." : g.Gap.Trim(),
                    Status: string.IsNullOrWhiteSpace(g.Status) ? "NotStarted" : g.Status.Trim(),
                    Order: order++));
            }
        }

        if (items.Count == 0)
        {
            items.Add(new GoalAnalysisItem(
                Title: "Build real-world experience",
                Description: "Focus on meaningful skill development aligned with your aspirations.",
                Category: "Career",
                WhyItMatters: "Anchors your future daytime pursuits.",
                DesiredOutcome: "Confidence and autonomy in your chosen area.",
                CurrentState: "Your current foundation is open for growth.",
                Gap: "Dedicating protected time to focused practice.",
                Status: "NotStarted",
                Order: 1));
        }

        return new GenerateGoalsResult(items);
    }

    public async Task<GenerateDailyPathResult> GenerateDailyPathAsync(
        GenerateDailyPathRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Gemini API key is not configured.");
            throw new InvalidOperationException("Gemini API key is not configured. Please set 'Gemini:ApiKey' or configure 'AI:Provider' to 'Development'.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        _logger.LogInformation("Starting daily path generation with Gemini model {Model} for User {UserId}", model, request.UserId);

        var requestPayload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = BuildDailyPathSystemInstruction() } }
            },
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = BuildDailyPathUserPrompt(request) } }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.8,
                ResponseMimeType = "application/json",
                ResponseSchema = BuildDailyPathResponseSchema()
            }
        };

        var requestJson = JsonSerializer.Serialize(requestPayload, JsonOptions);
        using var response = await SendWithRetryAsync(endpoint, requestJson, request.UserId, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API returned HTTP {StatusCode} during daily path generation: {ErrorBody}", statusCode, errorBody);

            if (statusCode == 429)
            {
                throw new GeminiTransientException("Gemini AI rate limit or quota exceeded. Please wait a moment and try again.", statusCode);
            }

            if (statusCode is 500 or 502 or 503 or 504)
            {
                throw new GeminiTransientException($"Gemini AI service is temporarily unavailable (HTTP {statusCode}).", statusCode);
            }

            if (statusCode == 401 || statusCode == 403)
            {
                throw new InvalidOperationException("Gemini AI authentication failed. Please verify the configured API key.");
            }

            throw new InvalidOperationException($"Gemini AI service returned an error ({statusCode}). Please try again later.");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse top-level Gemini API response for daily path generation for User {UserId}", request.UserId);
            throw new InvalidOperationException("Received an unreadable response from Gemini AI.", ex);
        }

        if (geminiResponse?.Error != null)
        {
            _logger.LogError("Gemini API error in daily path response payload: {Message} (Code: {Code}, Status: {Status})",
                geminiResponse.Error.Message, geminiResponse.Error.Code, geminiResponse.Error.Status);

            var code = geminiResponse.Error.Code;
            var status = geminiResponse.Error.Status ?? string.Empty;

            if (code is 429 or 500 or 502 or 503 or 504 ||
                status.Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException($"Gemini AI service error: {geminiResponse.Error.Message}", code);
            }

            throw new InvalidOperationException($"Gemini AI service error: {geminiResponse.Error.Message}");
        }

        var candidate = geminiResponse?.Candidates?.FirstOrDefault();
        var rawJson = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogError("Gemini API returned an empty candidate/content payload for daily path generation for User {UserId}. FinishReason: {FinishReason}",
                request.UserId, candidate?.FinishReason ?? "unknown");
            throw new InvalidOperationException("Gemini AI returned an empty response.");
        }

        GeminiDailyPathOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiDailyPathOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize structured daily path JSON from Gemini for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to parse structured daily path from Gemini AI.", ex);
        }

        if (output?.Actions == null || output.Actions.Count == 0)
        {
            _logger.LogError("Deserialized daily path output was null or empty for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to process daily path from Gemini AI.");
        }

        _logger.LogInformation("Gemini daily path generation completed successfully with {Count} actions for User {UserId}", output.Actions.Count, request.UserId);

        return MapToGenerateDailyPathResult(output);
    }

    private static string BuildDailyPathSystemInstruction()
    {
        return """
You are LUMA, a future-oriented AI companion.

Core philosophy for daily path generation:
1. Convert the user's life-direction goals into small, realistic, concrete actions for today.
2. Each action should take 5 to 45 minutes. Never suggest all-day tasks.
3. Generate 3 to 5 actions, spread across Morning, Afternoon, and Evening.
4. Actions must be specific enough to start immediately (e.g., 'Write 300 words about your ideal workspace' not 'Think about your future').
5. Each action should clearly map back to one of the user's existing goals by referencing its title in goalTitle.
6. Intention is a short, motivating sentence explaining how this small step connects to the bigger life direction.
7. Allowed timeOfDay values: 'Morning', 'Afternoon', 'Evening'.
8. Tone: Warm, grounded, practical. No generic self-help clichés.
9. Do NOT invent new goals. Only derive actions from the provided goals.
""";
    }

    private static string BuildDailyPathUserPrompt(GenerateDailyPathRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate 3 to 5 small, concrete daily actions for today based on these life goals:");
        sb.AppendLine();
        sb.AppendLine($"Dream: {request.DreamTitle}");
        sb.AppendLine($"Identity: {request.Identity}");
        sb.AppendLine($"Environment: {request.Environment}");
        sb.AppendLine($"Mood: {request.Mood}");
        sb.AppendLine($"Pace: {request.Pace}");
        sb.AppendLine();
        sb.AppendLine("Goals:");
        foreach (var goal in request.Goals)
        {
            sb.AppendLine($"- Title: {goal.Title}");
            sb.AppendLine($"  Category: {goal.Category}");
            sb.AppendLine($"  Gap: {goal.Gap}");
        }
        return sb.ToString();
    }

    private static object BuildDailyPathResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                actions = new
                {
                    type = "ARRAY",
                    description = "3 to 5 small, concrete daily actions for today.",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            goalTitle = new { type = "STRING", description = "The exact title of the goal this action bridges toward." },
                            title = new { type = "STRING", description = "A short, specific action title (e.g., 'Sketch your ideal morning routine')." },
                            description = new { type = "STRING", description = "1-2 sentences describing the concrete action." },
                            timeOfDay = new { type = "STRING", @enum = new[] { "Morning", "Afternoon", "Evening" }, description = "When to do this action." },
                            estimatedMinutes = new { type = "INTEGER", description = "Estimated duration in minutes (5 to 45)." },
                            intention = new { type = "STRING", description = "Short motivating sentence connecting this step to the bigger life direction." }
                        },
                        required = new[] { "goalTitle", "title", "description", "timeOfDay", "estimatedMinutes", "intention" }
                    }
                }
            },
            required = new[] { "actions" }
        };
    }

    private static GenerateDailyPathResult MapToGenerateDailyPathResult(GeminiDailyPathOutput output)
    {
        var items = new List<DailyActionItem>();
        var order = 1;

        if (output.Actions != null)
        {
            foreach (var a in output.Actions.Take(5))
            {
                items.Add(new DailyActionItem(
                    GoalTitle: string.IsNullOrWhiteSpace(a.GoalTitle) ? "General" : a.GoalTitle.Trim(),
                    Title: string.IsNullOrWhiteSpace(a.Title) ? $"Action {order}" : a.Title.Trim(),
                    Description: string.IsNullOrWhiteSpace(a.Description) ? "Take a small meaningful step today." : a.Description.Trim(),
                    TimeOfDay: string.IsNullOrWhiteSpace(a.TimeOfDay) ? "Morning" : a.TimeOfDay.Trim(),
                    EstimatedMinutes: a.EstimatedMinutes is > 0 and <= 60 ? a.EstimatedMinutes : 15,
                    Intention: string.IsNullOrWhiteSpace(a.Intention) ? "Every small step matters." : a.Intention.Trim(),
                    Order: order++));
            }
        }

        if (items.Count == 0)
        {
            items.Add(new DailyActionItem(
                GoalTitle: "Self-reflection",
                Title: "Spend 10 minutes journaling about your future direction",
                Description: "Write freely about what today's small progress means for the life you are building.",
                TimeOfDay: "Evening",
                EstimatedMinutes: 10,
                Intention: "Staying grounded in your long-term vision.",
                Order: 1));
        }

        return new GenerateDailyPathResult(items);
    }

    public async Task<GenerateReflectionInsightResult> GenerateReflectionInsightAsync(
        GenerateReflectionInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Gemini API key is not configured.");
            throw new InvalidOperationException("Gemini API key is not configured. Please set 'Gemini:ApiKey' or configure 'AI:Provider' to 'Development'.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model.Trim();
        var endpoint = $"/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        _logger.LogInformation("Starting reflection insight generation with Gemini model {Model} for User {UserId}", model, request.UserId);

        var requestPayload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart> { new() { Text = BuildReflectionSystemInstruction() } }
            },
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new() { Text = BuildReflectionUserPrompt(request) } }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7,
                ResponseMimeType = "application/json",
                ResponseSchema = BuildReflectionResponseSchema()
            }
        };

        var requestJson = JsonSerializer.Serialize(requestPayload, JsonOptions);
        using var response = await SendWithRetryAsync(endpoint, requestJson, request.UserId, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = (int)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API returned HTTP {StatusCode} during reflection insight generation: {ErrorBody}", statusCode, errorBody);

            if (statusCode is 429 or 500 or 502 or 503 or 504)
            {
                var message = statusCode == 429
                    ? "Gemini AI rate limit or quota exceeded. Please wait a moment and try again."
                    : $"Gemini AI service is temporarily unavailable ({statusCode}). Please try again shortly.";
                throw new GeminiTransientException(message, statusCode);
            }

            throw new InvalidOperationException($"Gemini AI service returned an error ({statusCode}). Please try again later.");
        }

        GeminiGenerateContentResponse? geminiResponse;
        try
        {
            geminiResponse = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse top-level Gemini API response for reflection insight for User {UserId}", request.UserId);
            throw new InvalidOperationException("Received an unreadable response from Gemini AI.", ex);
        }

        if (geminiResponse?.Error != null)
        {
            var code = geminiResponse.Error.Code;
            var status = geminiResponse.Error.Status ?? string.Empty;

            if (code is 429 or 500 or 502 or 503 or 504 ||
                status.Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiTransientException($"Gemini AI service error: {geminiResponse.Error.Message}", code);
            }

            throw new InvalidOperationException($"Gemini AI service error: {geminiResponse.Error.Message}");
        }

        var candidate = geminiResponse?.Candidates?.FirstOrDefault();
        var rawJson = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogError("Gemini API returned an empty candidate payload for reflection insight for User {UserId}", request.UserId);
            throw new InvalidOperationException("Gemini AI returned an empty response.");
        }

        GeminiReflectionOutput? output;
        try
        {
            output = JsonSerializer.Deserialize<GeminiReflectionOutput>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize structured reflection JSON from Gemini for User {UserId}", request.UserId);
            throw new InvalidOperationException("Failed to parse structured reflection from Gemini AI.", ex);
        }

        if (string.IsNullOrWhiteSpace(output?.Insight))
        {
            throw new InvalidOperationException("Failed to process reflection insight from Gemini AI.");
        }

        _logger.LogInformation("Gemini reflection insight generation completed successfully for User {UserId}", request.UserId);
        return new GenerateReflectionInsightResult(output.Insight.Trim());
    }

    private static string BuildReflectionSystemInstruction()
    {
        return """
You are LUMA, a future-oriented AI companion.

Core philosophy for user progress reflection:
1. Provide a thoughtful, grounded, 2 to 3 sentence reflection connecting today's lived experience with their emerging future.
2. Acknowledge the emotional tone of the user's day (their mood and notes) without excessive flattery or toxic positivity.
3. Observe how small daily actions (completed or paused) are actively shaping and refining who they are becoming.
4. Tone: Calm, poetic yet grounded, gentle, deeply observant.
5. Avoid generic productivity advice. Keep the focus on human feeling and future alignment.
""";
    }

    private static string BuildReflectionUserPrompt(GenerateReflectionInsightRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Reflect on the user's day and how it connects to their future:");
        sb.AppendLine();
        sb.AppendLine($"Dream: {request.DreamTitle}");
        sb.AppendLine($"Identity: {request.Identity}");
        sb.AppendLine($"How today felt (Mood): {request.ReflectionMood}");
        if (!string.IsNullOrWhiteSpace(request.ReflectionNotes))
        {
            sb.AppendLine($"User notes: {request.ReflectionNotes}");
        }
        sb.AppendLine($"Completed actions: {(request.CompletedActions.Count > 0 ? string.Join(", ", request.CompletedActions) : "None today")}");
        sb.AppendLine($"Skipped/rested actions: {(request.SkippedActions.Count > 0 ? string.Join(", ", request.SkippedActions) : "None")}");
        return sb.ToString();
    }

    private static object BuildReflectionResponseSchema()
    {
        return new
        {
            type = "OBJECT",
            properties = new
            {
                insight = new { type = "STRING", description = "A grounded 2-3 sentence reflection connecting today's progress to the user's future evolution." }
            },
            required = new[] { "insight" }
        };
    }
}
