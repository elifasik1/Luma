using System.Text.Json.Serialization;

namespace Luma.Infrastructure.AI.Models;

internal sealed class GeminiGenerateContentRequest
{
    [JsonPropertyName("systemInstruction")]
    public GeminiContent? SystemInstruction { get; set; }

    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

internal sealed class GeminiContent
{
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; set; }

    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

internal sealed class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal sealed class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("responseMimeType")]
    public string? ResponseMimeType { get; set; }

    [JsonPropertyName("responseSchema")]
    public object? ResponseSchema { get; set; }
}

internal sealed class GeminiGenerateContentResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }

    [JsonPropertyName("error")]
    public GeminiError? Error { get; set; }
}

internal sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

internal sealed class GeminiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

internal sealed class GeminiDreamAnalysisOutput
{
    [JsonPropertyName("futureProfile")]
    public GeminiFutureProfileOutput? FutureProfile { get; set; }

    [JsonPropertyName("themeConfig")]
    public GeminiThemeConfigOutput? ThemeConfig { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

internal sealed class GeminiFutureProfileOutput
{
    [JsonPropertyName("identity")]
    public string? Identity { get; set; }

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    [JsonPropertyName("lifestyle")]
    public List<string>? Lifestyle { get; set; }

    [JsonPropertyName("aspirations")]
    public List<string>? Aspirations { get; set; }

    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }

    [JsonPropertyName("mood")]
    public string? Mood { get; set; }

    [JsonPropertyName("pace")]
    public string? Pace { get; set; }
}

internal sealed class GeminiThemeConfigOutput
{
    [JsonPropertyName("palette")]
    public string? Palette { get; set; }

    [JsonPropertyName("atmosphere")]
    public string? Atmosphere { get; set; }

    [JsonPropertyName("illustration")]
    public string? Illustration { get; set; }

    [JsonPropertyName("motion")]
    public string? Motion { get; set; }

    [JsonPropertyName("imagery")]
    public List<string>? Imagery { get; set; }
}

internal sealed class GeminiFutureScenarioOutput
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("intro")]
    public string? Intro { get; set; }

    [JsonPropertyName("morning")]
    public string? Morning { get; set; }

    [JsonPropertyName("daytime")]
    public string? Daytime { get; set; }

    [JsonPropertyName("evening")]
    public string? Evening { get; set; }

    [JsonPropertyName("emotionalTone")]
    public string? EmotionalTone { get; set; }

    [JsonPropertyName("closingReflection")]
    public string? ClosingReflection { get; set; }
}

internal sealed class GeminiGoalsOutput
{
    [JsonPropertyName("goals")]
    public List<GeminiGoalItemOutput>? Goals { get; set; }
}

internal sealed class GeminiGoalItemOutput
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("whyItMatters")]
    public string? WhyItMatters { get; set; }

    [JsonPropertyName("desiredOutcome")]
    public string? DesiredOutcome { get; set; }

    [JsonPropertyName("currentState")]
    public string? CurrentState { get; set; }

    [JsonPropertyName("gap")]
    public string? Gap { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

internal sealed class GeminiDailyPathOutput
{
    [JsonPropertyName("actions")]
    public List<GeminiDailyActionItemOutput>? Actions { get; set; }
}

internal sealed class GeminiDailyActionItemOutput
{
    [JsonPropertyName("goalTitle")]
    public string? GoalTitle { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("timeOfDay")]
    public string? TimeOfDay { get; set; }

    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; }

    [JsonPropertyName("intention")]
    public string? Intention { get; set; }
}

internal sealed class GeminiReflectionOutput
{
    [JsonPropertyName("insight")]
    public string? Insight { get; set; }
}

internal sealed class GeminiConversationOutput
{
    [JsonPropertyName("reply")]
    public string? Reply { get; set; }

    [JsonPropertyName("actionProposalType")]
    public string? ActionProposalType { get; set; }

    [JsonPropertyName("keepActionId")]
    public string? KeepActionId { get; set; }

    [JsonPropertyName("restActionIds")]
    public List<string>? RestActionIds { get; set; }
}

internal sealed class GeminiAgentPlanOutput
{
    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("selectedTools")]
    public List<string>? SelectedTools { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

internal sealed class GeminiAgentGeneratedResponseOutput
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("followUpQuestion")]
    public string? FollowUpQuestion { get; set; }

    [JsonPropertyName("suggestedNextStep")]
    public string? SuggestedNextStep { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GeminiAgentActionDecisionOutput
{
    [JsonPropertyName("actionRequired")]
    public bool ActionRequired { get; set; }

    [JsonPropertyName("actionName")]
    public string? ActionName { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("requiresApproval")]
    public bool RequiresApproval { get; set; }

    [JsonPropertyName("actionRequest")]
    public GeminiAgentActionRequestOutput? ActionRequest { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed class GeminiAgentActionRequestOutput
{
    [JsonPropertyName("userSituation")]
    public string? UserSituation { get; set; }
}
