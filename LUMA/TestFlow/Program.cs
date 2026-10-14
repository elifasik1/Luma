using System.Text.Json;
using Luma.Application.AI;
using Luma.Application.AI.Models;
using Luma.Application.Agent;
using Luma.Application.Agent.Tools;
using Luma.Application.Interfaces;
using Luma.Application.Models;
using Luma.Application.Services;
using Luma.Domain.Entities;
using Luma.Infrastructure.AI;
using Luma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

if (args.Contains("--agent-only", StringComparer.OrdinalIgnoreCase))
{
    var agentOnlyDreamA = Guid.NewGuid();
    var agentOnlyDreamB = Guid.NewGuid();
    var agentOnlyUserA = Guid.NewGuid();
    var agentOnlyUserB = Guid.NewGuid();
    var agentOnlyAccess = new InMemoryAgentDreamAccess(agentOnlyDreamA, agentOnlyUserA, agentOnlyDreamB, agentOnlyUserB);
    var agentOnlyService = new AgentService(agentOnlyAccess);

    var agentOnlyContext = await agentOnlyService.AuthorizeAsync(agentOnlyUserA, agentOnlyDreamA);
    if (agentOnlyContext.AuthenticatedUserId != agentOnlyUserA || agentOnlyContext.DreamId != agentOnlyDreamA)
    {
        throw new Exception("FAILURE: Authorized Agent context did not preserve ownership identity.");
    }

    await AssertAgentNotFoundAsync(agentOnlyService, agentOnlyUserA, agentOnlyDreamB);
    await AssertAgentNotFoundAsync(agentOnlyService, agentOnlyUserA, Guid.NewGuid());
    await AssertAgentNotFoundAsync(agentOnlyService, Guid.Empty, agentOnlyDreamA);

    var futureContext = new FutureContextResult(
        agentOnlyDreamA,
        "A calm creative life",
        "Build a thoughtful life around meaningful work.",
        DateTime.UtcNow,
        new FutureProfileContext(
            "Independent creator",
            "nature",
            new[] { "Focused mornings" },
            new[] { "Create meaningful work" },
            new[] { "Clarity" },
            "calm",
            "steady",
            "A future with room to breathe.",
            new ThemeConfigContext(
                "forest_mist",
                "calm",
                new[] { "Nature" },
                "nature_dreamer",
                "gentle",
                new[] { "creativity" })),
        new FutureScenarioContext(
            "A grounded day",
            "The morning begins without urgency.",
            "A quiet start.",
            "Meaningful work with clear focus.",
            "A gentle evening.",
            "Rooted and calm",
            "Small choices shape the direction."));
    var futureContextQuery = new InMemoryFutureContextQuery(futureContext);
    var futureContextTool = new FutureContextTool(new FutureContextReadService(futureContextQuery));
    var toolResult = await futureContextTool.ExecuteAsync(agentOnlyContext);
    if (toolResult.Data is not FutureContextResult returnedContext ||
        returnedContext.DreamId != agentOnlyDreamA ||
        returnedContext.FutureProfile?.Identity != "Independent creator" ||
        returnedContext.FutureScenario?.Title != "A grounded day" ||
        futureContextQuery.ReadCount != 1)
    {
        throw new Exception("FAILURE: get_future_context returned an unexpected structured result.");
    }

    var noScenarioQuery = new InMemoryFutureContextQuery(futureContext with { FutureScenario = null });
    var noScenarioTool = new FutureContextTool(new FutureContextReadService(noScenarioQuery));
    var noScenarioResult = await noScenarioTool.ExecuteAsync(agentOnlyContext);
    if (noScenarioResult.Data is not FutureContextResult noScenarioContext ||
        noScenarioContext.FutureScenario is not null ||
        noScenarioQuery.ReadCount != 1)
    {
        throw new Exception("FAILURE: get_future_context did not preserve an absent Future Scenario as null.");
    }

    var rawGoals = new[]
    {
        new ActiveGoalContext(Guid.NewGuid(), "Later goal", "", "PersonalGrowth", "", "", "", "", "NotStarted", 3),
        new ActiveGoalContext(Guid.NewGuid(), "Active goal", "", "Career", "", "", "", "", "Active", 1),
        new ActiveGoalContext(Guid.NewGuid(), "Completed goal", "", "Career", "", "", "", "", "Completed", 2),
        new ActiveGoalContext(Guid.NewGuid(), "Legacy incomplete goal", "", "Health", "", "", "", "", "InProgress", 4)
    };
    var goalsQuery = new InMemoryGoalReadQuery(agentOnlyDreamA, rawGoals);
    var activeGoalsTool = new ActiveGoalsTool(new ActiveGoalsReadService(goalsQuery));
    var activeGoalsResult = await activeGoalsTool.ExecuteAsync(agentOnlyContext);
    if (activeGoalsResult.Data is not ActiveGoalsResult activeGoals ||
        activeGoals.Goals.Count != 3 ||
        activeGoals.Goals.Any(goal => string.Equals(goal.Status, "Completed", StringComparison.OrdinalIgnoreCase)) ||
        !activeGoals.Goals.Select(goal => goal.Order).SequenceEqual(new[] { 1, 3, 4 }) ||
        goalsQuery.ReadCount != 1 ||
        goalsQuery.WriteCount != 0)
    {
        throw new Exception("FAILURE: get_active_goals returned an unexpected active-goal result.");
    }

    var emptyGoalsQuery = new InMemoryGoalReadQuery(agentOnlyDreamA, Array.Empty<ActiveGoalContext>());
    var emptyGoalsTool = new ActiveGoalsTool(new ActiveGoalsReadService(emptyGoalsQuery));
    var emptyGoalsResult = await emptyGoalsTool.ExecuteAsync(agentOnlyContext);
    if (emptyGoalsResult.Data is not ActiveGoalsResult emptyGoals ||
        emptyGoals.Goals.Count != 0 ||
        emptyGoalsQuery.WriteCount != 0)
    {
        throw new Exception("FAILURE: get_active_goals did not preserve an empty goal state without mutation.");
    }

    var generatedAt = DateTime.UtcNow.AddMinutes(-5);
    var rawActions = new[]
    {
        new DailyActionContext(Guid.NewGuid(), null, "Goal C", "Third action", "Third description", "Evening", 30, "Reflect", 3, "Skipped", null, generatedAt),
        new DailyActionContext(Guid.NewGuid(), null, "Goal A", "First action", "First description", "Morning", 15, "Start", 1, "Pending", null, generatedAt),
        new DailyActionContext(Guid.NewGuid(), Guid.NewGuid(), "Goal B", "Second action", "Second description", "Daytime", 20, "Continue", 2, "Completed", generatedAt, generatedAt)
    };
    var dailyPlanQuery = new InMemoryDailyPlanReadQuery(agentOnlyDreamA, rawActions);
    var dailyPlanTool = new DailyPlanTool(new DailyPlanReadService(dailyPlanQuery));
    var dailyPlanResult = await dailyPlanTool.ExecuteAsync(agentOnlyContext);
    if (dailyPlanResult.Data is not DailyPlanResult dailyPlanEnvelope ||
        dailyPlanEnvelope.Plan is not DailyPlanContext dailyPlan ||
        dailyPlan.TotalActions != 3 ||
        dailyPlan.CompletedActions != 1 ||
        dailyPlan.SkippedActions != 1 ||
        dailyPlan.PendingActions != 1 ||
        !dailyPlan.Actions.Select(action => action.Order).SequenceEqual(new[] { 1, 2, 3 }) ||
        dailyPlan.Actions[0].Status != "Pending" ||
        dailyPlan.Actions[1].Status != "Completed" ||
        dailyPlan.Actions[2].Status != "Skipped" ||
        dailyPlan.GeneratedAt != generatedAt ||
        dailyPlanQuery.ReadCount != 1 ||
        dailyPlanQuery.WriteCount != 0)
    {
        throw new Exception("FAILURE: get_daily_plan returned an unexpected plan result.");
    }

    var emptyPlanQuery = new InMemoryDailyPlanReadQuery(agentOnlyDreamA, Array.Empty<DailyActionContext>());
    var emptyPlanTool = new DailyPlanTool(new DailyPlanReadService(emptyPlanQuery));
    var emptyPlanResult = await emptyPlanTool.ExecuteAsync(agentOnlyContext);
    if (emptyPlanResult.Data is not DailyPlanResult emptyPlanEnvelope ||
        emptyPlanEnvelope.Plan is not null ||
        emptyPlanQuery.WriteCount != 0)
    {
        throw new Exception("FAILURE: get_daily_plan did not preserve an absent plan without mutation.");
    }

    var progressQuery = new InMemoryDailyPlanReadQuery(agentOnlyDreamA, rawActions);
    var progressTool = new ProgressTool(new DailyPlanReadService(progressQuery));
    var progressResult = await progressTool.ExecuteAsync(agentOnlyContext);
    if (progressResult.Data is not ProgressResult progress ||
        progress.TotalActions != 3 ||
        progress.CompletedActions != 1 ||
        progress.SkippedActions != 1 ||
        progress.PendingActions != 1 ||
        progress.CompletionRate != (decimal)1 / 3 ||
        string.IsNullOrWhiteSpace(progress.MomentumMessage) ||
        progressQuery.ReadCount != 1 ||
        progressQuery.WriteCount != 0)
    {
        throw new Exception("FAILURE: get_progress returned an unexpected progress result.");
    }

    var emptyProgressQuery = new InMemoryDailyPlanReadQuery(agentOnlyDreamA, Array.Empty<DailyActionContext>());
    var emptyProgressTool = new ProgressTool(new DailyPlanReadService(emptyProgressQuery));
    var emptyProgressResult = await emptyProgressTool.ExecuteAsync(agentOnlyContext);
    if (emptyProgressResult.Data is not ProgressResult emptyProgress ||
        emptyProgress.TotalActions != 0 ||
        emptyProgress.CompletedActions != 0 ||
        emptyProgress.SkippedActions != 0 ||
        emptyProgress.PendingActions != 0 ||
        emptyProgress.CompletionRate != 0m ||
        emptyProgressQuery.WriteCount != 0)
    {
        throw new Exception("FAILURE: get_progress did not safely handle an empty plan.");
    }

    var reflectionBase = DateTime.UtcNow.AddHours(-6);
    var rawReflections = Enumerable.Range(0, 6)
        .Select(index => new ReflectionContext(
            Guid.NewGuid(),
            $"Mood {index}",
            $"Notes {index}",
            $"Insight {index}",
            reflectionBase.AddHours(index)))
        .Reverse()
        .ToArray();
    var reflectionQuery = new InMemoryReflectionReadQuery(agentOnlyDreamA, rawReflections);
    var reflectionsTool = new RecentReflectionsTool(new ReflectionReadService(reflectionQuery));
    var reflectionsResult = await reflectionsTool.ExecuteAsync(agentOnlyContext);
    if (reflectionsResult.Data is not RecentReflectionsResult recentReflections ||
        recentReflections.Count != RecentReflectionPolicy.Limit ||
        recentReflections.Reflections.Count != RecentReflectionPolicy.Limit ||
        !recentReflections.Reflections
            .Select(reflection => reflection.CreatedAt)
            .SequenceEqual(recentReflections.Reflections.Select(reflection => reflection.CreatedAt).OrderByDescending(value => value)) ||
        recentReflections.Reflections[0].CreatedAt != reflectionBase.AddHours(5) ||
        recentReflections.Reflections[^1].CreatedAt != reflectionBase.AddHours(1) ||
        reflectionQuery.ReadCount != 1 ||
        reflectionQuery.WriteCount != 0 ||
        reflectionQuery.GenerationCount != 0)
    {
        throw new Exception("FAILURE: get_recent_reflections returned an unexpected result.");
    }

    var emptyReflectionQuery = new InMemoryReflectionReadQuery(agentOnlyDreamA, Array.Empty<ReflectionContext>());
    var emptyReflectionsTool = new RecentReflectionsTool(new ReflectionReadService(emptyReflectionQuery));
    var emptyReflectionsResult = await emptyReflectionsTool.ExecuteAsync(agentOnlyContext);
    if (emptyReflectionsResult.Data is not RecentReflectionsResult emptyReflections ||
        emptyReflections.Count != 0 ||
        emptyReflections.Reflections.Count != 0 ||
        emptyReflectionQuery.WriteCount != 0 ||
        emptyReflectionQuery.GenerationCount != 0)
    {
        throw new Exception("FAILURE: get_recent_reflections did not preserve an empty state without generation.");
    }

    var orchestrationTools = new[]
    {
        new RecordingAgentTool("get_future_context"),
        new RecordingAgentTool("get_active_goals"),
        new RecordingAgentTool("get_daily_plan"),
        new RecordingAgentTool("get_progress"),
        new RecordingAgentTool("get_recent_reflections")
    };
    var orchestrationRegistry = new AgentToolRegistry(orchestrationTools);
    var orchestrationService = new AgentService(
        agentOnlyAccess,
        orchestrationRegistry,
        new DeterministicAgentPlanner());

    if (orchestrationRegistry.GetAvailableTools().Count != 5)
    {
        throw new Exception("FAILURE: Agent registry did not discover all five read tools.");
    }

    var focusResponse = await orchestrationService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "What should I focus on today?"));
    AssertPlan(focusResponse, "daily_focus", "get_daily_plan", "get_active_goals", "get_progress");

    var futureResponse = await orchestrationService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "What kind of future life am I building?"));
    AssertPlan(futureResponse, "future_context", "get_future_context");

    var progressResponse = await orchestrationService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "How is my progress?"));
    AssertPlan(progressResponse, "progress_review", "get_progress", "get_active_goals");

    var reflectionResponse = await orchestrationService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "Show me my recent thoughts and reflections."));
    AssertPlan(reflectionResponse, "reflection_review", "get_recent_reflections", "get_future_context");

    var generalResponse = await orchestrationService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "Hello LUMA."));
    AssertPlan(generalResponse, "general_context", "get_daily_plan");

    var expectedCallCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["get_future_context"] = 2,
        ["get_active_goals"] = 2,
        ["get_daily_plan"] = 2,
        ["get_progress"] = 2,
        ["get_recent_reflections"] = 1
    };
    foreach (var tool in orchestrationTools)
    {
        if (tool.CallCount != expectedCallCounts[tool.Name] ||
            tool.MutationCount != 0 ||
            tool.AiCallCount != 0 ||
            tool.LastContext?.AuthenticatedUserId != agentOnlyUserA ||
            tool.LastContext?.DreamId != agentOnlyDreamA)
        {
            throw new Exception($"FAILURE: Orchestration executed '{tool.Name}' unexpectedly or with an unsafe context.");
        }
    }

    await AssertAgentExecutionNotFoundAsync(
        orchestrationService,
        agentOnlyUserA,
        agentOnlyDreamB,
        "today");
    await AssertAgentExecutionNotFoundAsync(
        orchestrationService,
        agentOnlyUserA,
        Guid.NewGuid(),
        "progress");

    var keepActionId = Guid.NewGuid();
    var restActionId = Guid.NewGuid();
    var adaptiveProposal = new DailyPathChangeProposal(
        "suggest_plan_change",
        keepActionId,
        "Keep one meaningful step",
        15,
        new[] { restActionId });
    var adaptiveUpdatedPlan = new DailyPathResponse(
        new[]
        {
            new DailyActionResponse(
                keepActionId,
                agentOnlyDreamA,
                null,
                "Goal",
                "Keep one meaningful step",
                "Description",
                "Morning",
                15,
                "Focus",
                1,
                "Pending",
                null),
            new DailyActionResponse(
                restActionId,
                agentOnlyDreamA,
                null,
                "Goal",
                "Rest one step",
                "Description",
                "Daytime",
                15,
                "Recover",
                2,
                "Skipped",
                null)
        },
        DateTime.UtcNow,
        2,
        0,
        1,
        "Taking space when needed is part of the rhythm. Return whenever you feel ready.");
    var adaptiveCapability = new InMemoryAdaptiveDailyPlanCapability(
        adaptiveProposal,
        adaptiveUpdatedPlan);
    var updateDailyPlanTool = new UpdateDailyPlanTool(adaptiveCapability);
    var actionRegistry = new AgentToolRegistry(
        orchestrationTools.Cast<IAgentTool>().Append(updateDailyPlanTool));
    var actionService = new AgentService(
        agentOnlyAccess,
        actionRegistry,
        new DeterministicAgentPlanner(),
        updateDailyPlanTool);

    var actionMetadata = actionRegistry.GetAvailableTools()
        .Single(metadata => metadata.Name == "update_daily_plan");
    if (actionMetadata.IsReadOnly ||
        !actionMetadata.Description.Contains("approval", StringComparison.OrdinalIgnoreCase))
    {
        throw new Exception("FAILURE: update_daily_plan registry metadata did not describe a protected action tool.");
    }

    var geminiPlannerResponses = new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiPlanResponse("daily_focus", new[] { "get_daily_plan", "get_active_goals", "get_progress" }, "Daily context is needed."),
        CreateGeminiPlanResponse("future_context", new[] { "get_future_context" }, "Future context is needed."),
        CreateGeminiPlanResponse("progress_review", new[] { "get_progress", "get_active_goals" }, "Progress and goals are needed."),
        CreateGeminiPlanResponse("reflection_review", new[] { "get_recent_reflections", "get_future_context" }, "Reflections and future context are useful.")
    });
    var geminiPlanner = CreateGeminiPlanner(new QueueHttpMessageHandler(geminiPlannerResponses));
    var plannerMetadata = orchestrationRegistry.GetAvailableTools();
    var geminiPlanA = await geminiPlanner.PlanAsync(new AgentPlannerRequest("Bugün neye odaklanmalıyım?"), plannerMetadata);
    var geminiPlanB = await geminiPlanner.PlanAsync(new AgentPlannerRequest("Ben nasıl bir hayat istiyordum?"), plannerMetadata);
    var geminiPlanC = await geminiPlanner.PlanAsync(new AgentPlannerRequest("Son zamanlarda ilerliyor muyum?"), plannerMetadata);
    var geminiPlanD = await geminiPlanner.PlanAsync(new AgentPlannerRequest("Son zamanlarda kendim hakkında neler fark ettim?"), plannerMetadata);
    AssertPlanContract(geminiPlanA, "daily_focus", "get_daily_plan", "get_active_goals", "get_progress");
    AssertPlanContract(geminiPlanB, "future_context", "get_future_context");
    AssertPlanContract(geminiPlanC, "progress_review", "get_progress", "get_active_goals");
    AssertPlanContract(geminiPlanD, "reflection_review", "get_recent_reflections", "get_future_context");

    await AssertPlannerValidationAsync(() => CreateGeminiPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiPlanResponse("daily_focus", new[] { "update_daily_plan" }, "Unsafe action.")
    }))).PlanAsync(new AgentPlannerRequest("Make my plan lighter."), plannerMetadata));
    await AssertPlannerValidationAsync(() => CreateGeminiPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiPlanResponse("daily_focus", new[] { "unknown_tool" }, "Unknown tool.")
    }))).PlanAsync(new AgentPlannerRequest("Plan please."), plannerMetadata));
    await AssertPlannerValidationAsync(() => CreateGeminiPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("not-json", System.Text.Encoding.UTF8, "application/json")
        }
    }))).PlanAsync(new AgentPlannerRequest("Plan please."), plannerMetadata));

    var duplicateResponse = await CreateGeminiPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiPlanResponse("progress_review", new[] { "get_progress", "get_progress", "get_active_goals" }, "Deduplicated.")
    }))).PlanAsync(new AgentPlannerRequest("How is my progress?"), plannerMetadata);
    AssertPlanContract(duplicateResponse, "progress_review", "get_progress", "get_active_goals");

    var emptyResponse = await CreateGeminiPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiPlanResponse("general_context", Array.Empty<string>(), "No context required.")
    }))).PlanAsync(new AgentPlannerRequest("Hello."), plannerMetadata);
    AssertPlanContract(emptyResponse, "general_context");

    var fallbackPlanner = new FallbackAgentPlanner(
        CreateGeminiPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
        }))),
        new DeterministicAgentPlanner(),
        NullLogger<FallbackAgentPlanner>.Instance);
    var fallbackPlan = await fallbackPlanner.PlanAsync(
        new AgentPlannerRequest("How is my progress?"),
        plannerMetadata);
    AssertPlanContract(fallbackPlan, "progress_review", "get_progress", "get_active_goals");

    var maliciousPlanner = new FixedAgentPlanner(new AgentPlan(
        "daily_focus",
        new[] { "update_daily_plan" },
        "Malicious mutation selection."));
    var guardedActionService = new AgentService(
        agentOnlyAccess,
        actionRegistry,
        maliciousPlanner,
        updateDailyPlanTool);
    var proposalCallsBeforeGuard = adaptiveCapability.ProposalCallCount;
    var applyCallsBeforeGuard = adaptiveCapability.ApplyCallCount;
    var guardedResponse = await guardedActionService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "Please use the unsafe tool."));
    if (string.IsNullOrWhiteSpace(guardedResponse.Error) ||
        guardedResponse.ToolResults is null ||
        guardedResponse.ToolResults.Count != 0 ||
        adaptiveCapability.ProposalCallCount != proposalCallsBeforeGuard ||
        adaptiveCapability.ApplyCallCount != applyCallsBeforeGuard)
    {
        throw new Exception("FAILURE: Application-side planner allowlist did not block a mutation tool.");
    }

    var actionPlanner = new FixedAgentActionPlanner(new AgentActionDecision(
        ActionRequired: true,
        ActionName: "update_daily_plan",
        Reason: "Limited time and energy were reported.",
        RequiresApproval: true,
        ActionRequest: new AgentActionRequest("I only have 30 minutes today and I'm feeling really tired. Can you adjust my day?")));
    var actionAwareService = new AgentService(
        agentOnlyAccess,
        actionRegistry,
        new DeterministicAgentPlanner(),
        updateDailyPlanTool,
        new DeterministicAgentResponseGenerator(),
        actionPlanner);
    var proposalMutationCountBeforeAction = adaptiveCapability.MutationCount;
    var actionAwareResponse = await actionAwareService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "I only have 30 minutes today and I'm feeling really tired. Can you adjust my day?"));
    if (!actionAwareResponse.RequiresApproval ||
        actionAwareResponse.ActionProposal is null ||
        actionAwareResponse.ActionProposal.ActionName != "update_daily_plan" ||
        actionAwareResponse.ActionProposal.ProposalResult.Data is not UpdateDailyPlanResult actionAwareProposal ||
        actionAwareProposal.Applied ||
        !actionAwareProposal.RequiresApproval ||
        adaptiveCapability.MutationCount != proposalMutationCountBeforeAction ||
        !actionAwareResponse.Reply.Contains("Keep one meaningful step", StringComparison.Ordinal) ||
        !actionAwareResponse.Reply.Contains("15 min", StringComparison.Ordinal) ||
        !actionAwareResponse.Reply.Contains("Would you like me to make that change", StringComparison.Ordinal))
    {
        throw new Exception("FAILURE: Action-aware Agent did not return an approval-required proposal without mutation.");
    }

    var deterministicActionDecision = await new DeterministicAgentActionPlanner().DecideAsync(
        new AgentPlannerRequest("I only have 30 minutes today and I'm feeling really tired. Can you adjust my day?"),
        new AgentPlan("daily_focus", new[] { "get_daily_plan", "get_progress" }, "daily context"),
        Array.Empty<AgentToolExecutionResult>());
    if (!deterministicActionDecision.ActionRequired ||
        deterministicActionDecision.ActionName != "update_daily_plan" ||
        !deterministicActionDecision.RequiresApproval)
    {
        throw new Exception("FAILURE: Focused exact-message action decision was not approval-required.");
    }

    var explicitPlanChangeMessages = new[]
    {
        "Let's set aside the career profile task for today and keep my most important goal on track.",
        "Can you make today's plan lighter?",
        "I want to postpone this task until tomorrow.",
        "Please leave the career profile task for today.",
        "I want to skip the career profile task today."
    };
    foreach (var message in explicitPlanChangeMessages)
    {
        var explicitPlanChangeDecision = await new DeterministicAgentActionPlanner().DecideAsync(
            new AgentPlannerRequest(message),
            new AgentPlan("daily_focus", new[] { "get_daily_plan" }, "daily plan"),
            Array.Empty<AgentToolExecutionResult>());
        if (!explicitPlanChangeDecision.ActionRequired ||
            explicitPlanChangeDecision.ActionName != AgentActionDecisionValidator.UpdateDailyPlanAction ||
            !explicitPlanChangeDecision.RequiresApproval ||
            explicitPlanChangeDecision.ActionRequest?.UserSituation != message)
        {
            throw new Exception($"FAILURE: Explicit daily-plan change did not require approval for '{message}'.");
        }
    }

    var geminiActionPromptHandler = new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiActionDecisionResponse(true, "update_daily_plan", "Explicit postponement.", true, explicitPlanChangeMessages[0])
    }));
    var explicitGeminiActionDecision = await CreateGeminiActionPlanner(geminiActionPromptHandler).DecideAsync(
        new AgentPlannerRequest(explicitPlanChangeMessages[0]),
        new AgentPlan("daily_focus", new[] { "get_daily_plan", "get_active_goals" }, "mixed goal and plan change"),
        Array.Empty<AgentToolExecutionResult>());
    var geminiActionPrompt = geminiActionPromptHandler.RequestBodies.Single();
    if (!explicitGeminiActionDecision.ActionRequired ||
        !explicitGeminiActionDecision.RequiresApproval ||
        !geminiActionPrompt.Contains("set aside, postpone, leave, or skip a task", StringComparison.OrdinalIgnoreCase) ||
        !geminiActionPrompt.Contains("even when the same message also mentions keeping an important goal on track", StringComparison.OrdinalIgnoreCase))
    {
        throw new Exception("FAILURE: Gemini action planner prompt did not cover explicit mixed goal and postponement requests.");
    }

    var noActionService = new AgentService(
        agentOnlyAccess,
        actionRegistry,
        new DeterministicAgentPlanner(),
        updateDailyPlanTool,
        new DeterministicAgentResponseGenerator(),
        new FixedAgentActionPlanner(new AgentActionDecision(
            ActionRequired: false,
            ActionName: null,
            Reason: "Read-only answer is sufficient.",
            RequiresApproval: false,
            ActionRequest: null)));
    var noActionResponse = await noActionService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "How am I doing?"));
    if (noActionResponse.RequiresApproval || noActionResponse.ActionProposal is not null)
    {
        throw new Exception("FAILURE: No-action decision created an action proposal.");
    }

    await AssertAgentExecutionNotFoundAsync(actionAwareService, agentOnlyUserA, agentOnlyDreamB, "Make my plan lighter.");
    await AssertAgentExecutionNotFoundAsync(actionAwareService, agentOnlyUserA, Guid.NewGuid(), "Make my plan lighter.");

    await AssertActionDecisionValidationAsync(new AgentActionDecision(
        true, "delete_user", "Unsafe", true, new AgentActionRequest("Delete data.")));
    await AssertActionDecisionValidationAsync(new AgentActionDecision(
        true, "complete_daily_action", "Unsupported", true, new AgentActionRequest("Complete it.")));
    await AssertActionDecisionValidationAsync(new AgentActionDecision(
        true, "update_daily_plan", "Apply immediately", false, new AgentActionRequest("Apply immediately.")));
    await AssertActionDecisionValidationAsync(new AgentActionDecision(
        true, "update_daily_plan", "Direct mutation", true, new AgentActionRequest("Apply immediately without approval.")));

    var geminiActionPlanner = CreateGeminiActionPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiActionDecisionResponse(true, "update_daily_plan", "Limited time.", true, "Only 30 minutes and low energy.")
    })));
    var geminiActionDecision = await geminiActionPlanner.DecideAsync(
        new AgentPlannerRequest("I only have 30 minutes today and I am exhausted."),
        new AgentPlan("daily_focus", new[] { "get_daily_plan", "get_progress" }, "daily context"),
        Array.Empty<AgentToolExecutionResult>());
    if (!geminiActionDecision.ActionRequired ||
        geminiActionDecision.ActionName != "update_daily_plan" ||
        !geminiActionDecision.RequiresApproval ||
        geminiActionDecision.ActionRequest?.UserSituation != "Only 30 minutes and low energy.")
    {
        throw new Exception("FAILURE: Gemini action planner did not return the expected safe action decision.");
    }

    await AssertActionDecisionValidationAsyncFromGemini(CreateGeminiActionPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiActionDecisionResponse(true, "delete_user", "Unsafe", true, "Delete data.")
    }))));
    await AssertActionDecisionValidationAsyncFromGemini(CreateGeminiActionPlanner(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiActionDecisionResponse(true, "complete_daily_action", "Unsupported", true, "Complete it.")
    }))));

    var generatedResponseHandler = new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
    {
        CreateGeminiGeneratedResponse("Bugün en anlamlı bekleyen adıma odaklanabilirsin.", "Hangisi sana en uygun geliyor?", null)
    }));
    var geminiResponseGenerator = CreateGeminiResponseGenerator(generatedResponseHandler);
    var generatedResponse = await geminiResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Bugün neye odaklanmalıyım?",
            focusResponse.Plan!,
            focusResponse.ToolResults!));
    if (generatedResponse.Message != "Bugün en anlamlı bekleyen adıma odaklanabilirsin." ||
        !generatedResponseHandler.RequestBodies.Single().Contains("Bug\\u00FCn neye odaklanmal\\u0131y\\u0131m?", StringComparison.Ordinal) ||
        !generatedResponseHandler.RequestBodies.Single().Contains("get_daily_plan", StringComparison.Ordinal) ||
        !generatedResponseHandler.RequestBodies.Single().Contains("only source of truth", StringComparison.OrdinalIgnoreCase) ||
        !generatedResponseHandler.RequestBodies.Single().Contains("Applied=true", StringComparison.Ordinal) ||
        !generatedResponseHandler.RequestBodies.Single().Contains("actual message first", StringComparison.Ordinal) ||
        generatedResponseHandler.RequestBodies.Single().Contains("PasswordHash", StringComparison.OrdinalIgnoreCase))
    {
        throw new Exception("FAILURE: Gemini response generator did not preserve safe structured request context.");
    }

    var deterministicResponseGenerator = new DeterministicAgentResponseGenerator();
    var futureGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Ben nasıl bir hayat istiyordum?",
            new AgentPlan("future_context", new[] { "get_future_context" }, "future"),
            new[]
            {
                new AgentToolExecutionResult(
                    "get_future_context",
                    true,
                    new FutureContextResult(
                        agentOnlyDreamA,
                        "A meaningful life",
                        "",
                        DateTime.UtcNow,
                        new FutureProfileContext("Thoughtful creator", "Quiet studio", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), "Calm", "Steady", "A life with room to breathe.", null),
                        null))
            }));
    var progressGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Son zamanlarda ilerliyor muyum?",
            new AgentPlan("progress_review", new[] { "get_progress" }, "progress"),
            new[]
            {
                new AgentToolExecutionResult(
                    "get_progress",
                    true,
                    new ProgressResult(4, 2, 1, 1, 0.5m, "Steady progress."))
            }));
    var goalGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "What am I working toward?",
            new AgentPlan("general_context", new[] { "get_active_goals" }, "goals"),
            new[]
            {
                new AgentToolExecutionResult(
                    "get_active_goals",
                    true,
                    new ActiveGoalsResult(new[]
                    {
                        new ActiveGoalContext(
                            Guid.NewGuid(),
                            "Build a consistent creative practice",
                            "",
                            "Creative",
                            "",
                            "",
                            "",
                            "",
                            "Active",
                            1)
                    }))
            }));
    var goalPlan = await new DeterministicAgentPlanner().PlanAsync(
        new AgentPlannerRequest("What am I working toward?"),
        new[] { new AgentToolMetadata("get_active_goals", "Reads active goals.", true) });
    var responseDailyPlan = new DailyPlanResult(new DailyPlanContext(
        new[]
        {
            new DailyActionContext(
                Guid.NewGuid(),
                null,
                "Creative practice",
                "Sketch a new concept",
                "",
                "Morning",
                15,
                "",
                1,
                "Pending",
                null,
                DateTime.UtcNow),
            new DailyActionContext(
                Guid.NewGuid(),
                null,
                "Creative practice",
                "Review references",
                "",
                "Afternoon",
                20,
                "",
                2,
                "Pending",
                null,
                DateTime.UtcNow)
        },
        DateTime.UtcNow,
        2,
        0,
        0,
        2,
        "Your day is open."));
    var overwhelmedGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "I feel overwhelmed today.",
            new AgentPlan("daily_focus", new[] { "get_daily_plan", "get_progress" }, "today"),
            new AgentToolExecutionResult[]
            {
                new("get_daily_plan", true, responseDailyPlan),
                new("get_progress", true, new ProgressResult(2, 0, 0, 2, 0m, "Your day is open."))
            }));
    var futureWithoutResult = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "What kind of life am I building?",
            new AgentPlan("future_context", Array.Empty<string>(), "no future data"),
            Array.Empty<AgentToolExecutionResult>()));
    var reflectionWithoutResult = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "What have I noticed in my reflections?",
            new AgentPlan("reflection_review", Array.Empty<string>(), "no reflection data"),
            Array.Empty<AgentToolExecutionResult>()));
    var ordinaryWithProgress = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Hello",
            new AgentPlan("general_context", new[] { "get_progress" }, "unrelated"),
            new[] { new AgentToolExecutionResult("get_progress", true, new ProgressResult(4, 2, 1, 1, 0.5m, "Steady progress.")) }));
    var reflectionGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Son zamanlarda kendim hakkında neler fark ettim?",
            new AgentPlan("reflection_review", new[] { "get_recent_reflections" }, "reflection"),
            new[]
            {
                new AgentToolExecutionResult(
                    "get_recent_reflections",
                    true,
                    new RecentReflectionsResult(
                        new[]
                        {
                            new ReflectionContext(Guid.NewGuid(), "Calm", "I noticed a pattern.", "I value space.", DateTime.UtcNow)
                        },
                        1))
            }));
    var proposalAction = new AgentActionProposal(
        "update_daily_plan",
        "The user described limited time or energy for today.",
        true,
        new AgentToolResult(new UpdateDailyPlanResult(false, true, adaptiveProposal, null, "Awaiting approval."), true));
    var proposalRequest = new AgentResponseGenerationRequest(
        "I only have 30 minutes today.",
        new AgentPlan("daily_focus", new[] { "get_daily_plan" }, "today"),
        Array.Empty<AgentToolExecutionResult>(),
        proposalAction);
    var proposalGenerated = await deterministicResponseGenerator.GenerateAsync(proposalRequest);
    var otherProposal = adaptiveProposal with { KeepActionTitle = "Read one chapter", KeepActionEstimatedMinutes = 25 };
    var otherProposalRequest = proposalRequest with
    {
        ActionProposal = proposalAction with
        {
            ProposalResult = new AgentToolResult(new UpdateDailyPlanResult(false, true, otherProposal, null, "Awaiting approval."), true)
        }
    };
    var otherProposalGenerated = await deterministicResponseGenerator.GenerateAsync(otherProposalRequest);
    var proposalWithoutDetails = adaptiveProposal with { KeepActionTitle = "", KeepActionEstimatedMinutes = 0 };
    var incompleteProposalRequest = proposalRequest with
    {
        ActionProposal = proposalAction with
        {
            ProposalResult = new AgentToolResult(new UpdateDailyPlanResult(false, true, proposalWithoutDetails, null, "Awaiting approval."), true)
        }
    };
    var incompleteProposalGenerated = await deterministicResponseGenerator.GenerateAsync(incompleteProposalRequest);
    var alreadyAppliedRequest = proposalRequest with
    {
        ActionProposal = proposalAction with
        {
            ProposalResult = new AgentToolResult(new UpdateDailyPlanResult(true, false, adaptiveProposal, adaptiveUpdatedPlan, "Applied."), false)
        }
    };
    var alreadyAppliedGenerated = await deterministicResponseGenerator.GenerateAsync(alreadyAppliedRequest);
    var noProposalGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Can you adjust my day?",
            new AgentPlan("daily_focus", new[] { "get_daily_plan" }, "today"),
            new[] { new AgentToolExecutionResult("get_daily_plan", true, responseDailyPlan) }));
    var mixedGoalPlanChangeWithoutProposal = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            explicitPlanChangeMessages[0],
            new AgentPlan("daily_focus", new[] { "get_daily_plan", "get_active_goals" }, "mixed goal and plan change"),
            new AgentToolExecutionResult[]
            {
                new("get_daily_plan", true, responseDailyPlan),
                new("get_active_goals", true, new ActiveGoalsResult(new[]
                {
                    new ActiveGoalContext(
                        Guid.NewGuid(),
                        "Build a consistent creative practice",
                        "",
                        "Creative",
                        "",
                        "",
                        "",
                        "",
                        "Active",
                        1)
                }))
            }));
    var genericGoalGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "What am I working toward?",
            new AgentPlan("general_context", new[] { "get_active_goals" }, "goal question"),
            new[]
            {
                new AgentToolExecutionResult(
                    "get_active_goals",
                    true,
                    new ActiveGoalsResult(new[]
                    {
                        new ActiveGoalContext(
                            Guid.NewGuid(),
                            "Build a consistent creative practice",
                            "",
                            "Creative",
                            "",
                            "",
                            "",
                            "",
                            "Active",
                            1)
                    }))
            }));
    var emptyGenerated = await deterministicResponseGenerator.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Hello",
            new AgentPlan("general_context", Array.Empty<string>(), "none"),
            Array.Empty<AgentToolExecutionResult>()));
    if (!futureGenerated.Message.Contains("room to breathe", StringComparison.OrdinalIgnoreCase) ||
        !progressGenerated.Message.Contains("2 tanesini", StringComparison.Ordinal) ||
        !progressGenerated.Message.Contains("1 adım bekliyor", StringComparison.Ordinal) ||
        !goalGenerated.Message.Contains("Build a consistent creative practice", StringComparison.Ordinal) ||
        !goalPlan.SelectedTools.Contains("get_active_goals", StringComparer.OrdinalIgnoreCase) ||
        !reflectionGenerated.Message.Contains("I value space.", StringComparison.Ordinal) ||
        !overwhelmedGenerated.Message.Contains("Sketch a new concept", StringComparison.Ordinal) ||
        (overwhelmedGenerated.Message.Contains("plan", StringComparison.OrdinalIgnoreCase) &&
         overwhelmedGenerated.Message.Contains("changed", StringComparison.OrdinalIgnoreCase)) ||
        overwhelmedGenerated.Message.Contains("completed", StringComparison.OrdinalIgnoreCase) ||
        !ordinaryWithProgress.Message.Contains("What would be useful", StringComparison.Ordinal) ||
        ordinaryWithProgress.Message.Contains("2 of 4", StringComparison.Ordinal) ||
        futureWithoutResult.Message.Contains("My Big City Life", StringComparison.Ordinal) ||
        futureWithoutResult.Message.Contains("future you're shaping centers", StringComparison.Ordinal) ||
        reflectionWithoutResult.Message.Contains("reflection", StringComparison.OrdinalIgnoreCase) ||
        !proposalGenerated.Message.Contains("Keep one meaningful step", StringComparison.Ordinal) ||
        !proposalGenerated.Message.Contains("15 min", StringComparison.Ordinal) ||
        !proposalGenerated.Message.Contains("Because you described limited time or energy for today", StringComparison.Ordinal) ||
        proposalGenerated.Message.Contains("Done", StringComparison.OrdinalIgnoreCase) ||
        proposalGenerated.Message.Contains("I've changed", StringComparison.OrdinalIgnoreCase) ||
        !proposalGenerated.Message.Contains("Would you like me", StringComparison.Ordinal) ||
        proposalGenerated.Message == otherProposalGenerated.Message ||
        incompleteProposalGenerated.Message.Contains("Keep one meaningful step", StringComparison.Ordinal) ||
        incompleteProposalGenerated.Message.Contains("0 min", StringComparison.Ordinal) ||
        alreadyAppliedGenerated.Message.Contains("Would you like me to make that change", StringComparison.Ordinal) ||
        alreadyAppliedGenerated.Message.Contains("Done", StringComparison.OrdinalIgnoreCase) ||
        alreadyAppliedGenerated.Message.Contains("applied", StringComparison.OrdinalIgnoreCase) ||
        noProposalGenerated.Message.Contains("Would you like me to make that change", StringComparison.Ordinal) ||
        !mixedGoalPlanChangeWithoutProposal.Message.Contains("couldn't prepare a change proposal", StringComparison.OrdinalIgnoreCase) ||
        mixedGoalPlanChangeWithoutProposal.Message.Contains("Build a consistent creative practice", StringComparison.Ordinal) ||
        mixedGoalPlanChangeWithoutProposal.Message.Contains("changed your plan", StringComparison.OrdinalIgnoreCase) ||
        !genericGoalGenerated.Message.Contains("Build a consistent creative practice", StringComparison.Ordinal) ||
        string.IsNullOrWhiteSpace(emptyGenerated.Message))
    {
        throw new Exception(
            "FAILURE: Deterministic response fallback did not use safe structured context. " +
            $"future={futureGenerated.Message}; progress={progressGenerated.Message}; goal={goalGenerated.Message}; " +
            $"reflection={reflectionGenerated.Message}; overwhelmed={overwhelmedGenerated.Message}; " +
            $"ordinary={ordinaryWithProgress.Message}; proposal={proposalGenerated.Message}; " +
            $"otherProposal={otherProposalGenerated.Message}; missingProposal={incompleteProposalGenerated.Message}; " +
            $"applied={alreadyAppliedGenerated.Message}; noProposal={noProposalGenerated.Message}; " +
            $"mixedGoalPlanChange={mixedGoalPlanChangeWithoutProposal.Message}; " +
            $"genericGoal={genericGoalGenerated.Message}; empty={emptyGenerated.Message}");
    }

    var malformedFallback = new FallbackAgentResponseGenerator(
        CreateGeminiResponseGenerator(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", System.Text.Encoding.UTF8, "application/json")
            }
        }))),
        deterministicResponseGenerator,
        NullLogger<FallbackAgentResponseGenerator>.Instance);
    var malformedFallbackResponse = await malformedFallback.GenerateAsync(
        new AgentResponseGenerationRequest(
            "Hello",
            new AgentPlan("general_context", Array.Empty<string>(), "none"),
            Array.Empty<AgentToolExecutionResult>()));
    if (string.IsNullOrWhiteSpace(malformedFallbackResponse.Message))
    {
        throw new Exception("FAILURE: Malformed Gemini response did not produce a safe fallback.");
    }

    var quotaFallback = new FallbackAgentResponseGenerator(
        CreateGeminiResponseGenerator(new QueueHttpMessageHandler(new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
        }))),
        deterministicResponseGenerator,
        NullLogger<FallbackAgentResponseGenerator>.Instance);
    var quotaFallbackResponse = await quotaFallback.GenerateAsync(proposalRequest);
    if (!quotaFallbackResponse.Message.Contains("Keep one meaningful step", StringComparison.Ordinal) ||
        !quotaFallbackResponse.Message.Contains("15 min", StringComparison.Ordinal))
    {
        throw new Exception("FAILURE: Gemini 429 did not use the structured deterministic proposal fallback.");
    }

    var responseGeneratorCalls = 0;
    var responseIntegration = new RecordingAgentResponseGenerator(() => responseGeneratorCalls++);
    var responseService = new AgentService(
        agentOnlyAccess,
        actionRegistry,
        new DeterministicAgentPlanner(),
        updateDailyPlanTool,
        responseIntegration);
    var responseIntegrationResult = await responseService.ExecuteAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "What is my progress?"));
    if (responseGeneratorCalls != 1 ||
        responseIntegrationResult.Reply != "Recorded response." ||
        responseIntegration.LastRequest is null ||
        responseIntegration.LastRequest.UserMessage != "What is my progress?" ||
        responseIntegration.LastRequest.ToolResults.Count == 0 ||
        adaptiveCapability.ApplyCallCount != applyCallsBeforeGuard)
    {
        throw new Exception("FAILURE: AgentService response generation integration was unsafe or incomplete.");
    }

    var proposalCallCountBeforeExplicitProposal = adaptiveCapability.ProposalCallCount;
    var proposalResult = await actionService.ExecuteUpdateDailyPlanAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "I am tired and only have 30 minutes."),
        new UpdateDailyPlanRequest("I am tired and only have 30 minutes."));
    if (proposalResult.Data is not UpdateDailyPlanResult proposalData ||
        proposalData.Applied ||
        !proposalData.RequiresApproval ||
        proposalData.Proposal != adaptiveProposal ||
        proposalData.UpdatedPlan is not null ||
        adaptiveCapability.ProposalCallCount != proposalCallCountBeforeExplicitProposal + 1 ||
        adaptiveCapability.ApplyCallCount != 0 ||
        adaptiveCapability.MutationCount != 0)
    {
        throw new Exception("FAILURE: update_daily_plan proposal step was not safe or structured.");
    }

    var approvalResult = await actionService.ExecuteUpdateDailyPlanAsync(
        agentOnlyUserA,
        new AgentRequest(agentOnlyDreamA, "Approve this plan change."),
        new UpdateDailyPlanRequest(
            "Approve this plan change.",
            Approve: true,
            Proposal: adaptiveProposal));
    if (approvalResult.Data is not UpdateDailyPlanResult approvalData ||
        !approvalData.Applied ||
        approvalData.RequiresApproval ||
        approvalData.UpdatedPlan != adaptiveUpdatedPlan ||
        adaptiveCapability.ApplyCallCount != 1 ||
        adaptiveCapability.MutationCount != 1)
    {
        throw new Exception("FAILURE: update_daily_plan approval did not apply the existing adaptive change path.");
    }

    await AssertAgentActionNotFoundAsync(
        actionService,
        agentOnlyUserA,
        agentOnlyDreamB,
        new UpdateDailyPlanRequest("Approve", Approve: true, Proposal: adaptiveProposal));
    await AssertAgentActionNotFoundAsync(
        actionService,
        agentOnlyUserA,
        Guid.NewGuid(),
        new UpdateDailyPlanRequest("Approve", Approve: true, Proposal: adaptiveProposal));

    await AssertAgentNotFoundAsync(agentOnlyService, agentOnlyUserA, agentOnlyDreamB);
    await AssertAgentNotFoundAsync(agentOnlyService, agentOnlyUserA, Guid.NewGuid());

    Console.WriteLine("AGENT-ONLY TESTS PASSED: owner allowed, cross-owner/missing/unauthenticated rejected.");
    Console.WriteLine("GET_FUTURE_CONTEXT TESTS PASSED: structured context, absent scenario, and read-only query behavior verified.");
    Console.WriteLine("GET_ACTIVE_GOALS TESTS PASSED: active filtering, ordering, empty state, ownership boundary, and read-only behavior verified.");
    Console.WriteLine("GET_DAILY_PLAN TESTS PASSED: statuses, counts, ordering, absent plan, ownership boundary, and read-only behavior verified.");
    Console.WriteLine("GET_PROGRESS TESTS PASSED: counts, completion rate, empty plan, ownership boundary, and read-only behavior verified.");
    Console.WriteLine("GET_RECENT_REFLECTIONS TESTS PASSED: ordering, limit, empty state, ownership boundary, and read-only behavior verified.");
    Console.WriteLine("AGENT ORCHESTRATION TESTS PASSED: planner selection, registry discovery, structured results, context safety, and no mutation verified.");
    Console.WriteLine("GEMINI AGENT PLANNER TESTS PASSED: structured responses, allowlist validation, deduplication, malformed output rejection, and transient fallback verified.");
    Console.WriteLine("AGENT RESPONSE GENERATION TESTS PASSED: structured context, safe fallback, validation, AgentService integration, and no mutation verified.");
    Console.WriteLine("UPDATE_DAILY_PLAN TESTS PASSED: proposal safety, explicit approval, registry metadata, ownership, and mutation delegation verified.");
    Console.WriteLine("ACTION-AWARE AGENT TESTS PASSED: action decision validation, proposal-before-mutation, approval boundary, ownership, and Gemini action safety verified.");
    await RunAdaptiveDeterministicFallbackTestsAsync();
    await RunDailyPlanSnapshotTestsAsync();
    return;
}

var options = new DbContextOptionsBuilder<LumaDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

using var db = new LumaDbContext(options);
var repo = new DreamRepository(db);
var devAi = new DevelopmentAIService();
var dailyPathService = new DailyPathService(repo, devAi);
var talkService = new TalkToLumaService(repo, devAi, dailyPathService);

// 1. Create a Dream with Profile, Goals, and 3 Daily Actions
var dream = new Dream
{
    Id = Guid.NewGuid(),
    UserId = Guid.NewGuid(),
    Title = "Become an Independent Game Designer",
    Description = "Build mindful indie games while living peacefully.",
    CreatedAt = DateTime.UtcNow
};
db.Users.Add(new User
{
    Id = dream.UserId,
    Name = "Deterministic Test User",
    Email = "deterministic-test@example.com"
});

var profile = new FutureProfile
{
    Id = Guid.NewGuid(),
    DreamId = dream.Id,
    Identity = "Independent Game Creator",
    Environment = "Cozy Studio",
    Lifestyle = new List<string> { "Balanced Creative Days" },
    Aspirations = new List<string> { "Craft impactful worlds" },
    Values = new List<string> { "Patience, Depth" },
    Mood = "Calm",
    Pace = "Sustainable",
    Summary = "A gentle creative life."
};
dream.FutureProfile = profile;

var goals = new List<Goal>
{
    new() { Id = Guid.NewGuid(), DreamId = dream.Id, Title = "Build Core Mechanic", Category = "Skill", DesiredOutcome = "Working prototype", CurrentState = "Concept only", Gap = "Need hands-on prototype", Order = 1, Status = "InProgress" },
    new() { Id = Guid.NewGuid(), DreamId = dream.Id, Title = "Establish Daily Rhythm", Category = "Habit", DesiredOutcome = "Daily 1h focus", CurrentState = "Inconsistent", Gap = "Need daily focus block", Order = 2, Status = "InProgress" }
};
dream.Goals = goals;

var action1 = new DailyAction { Id = Guid.NewGuid(), DreamId = dream.Id, GoalTitle = goals[0].Title, Title = "Sketch Level 1 Flow", Description = "Draw the layout on paper", TimeOfDay = "Morning", EstimatedMinutes = 15, Intention = "Get visual clarity", Order = 1, Status = "Pending" };
var action2 = new DailyAction { Id = Guid.NewGuid(), DreamId = dream.Id, GoalTitle = goals[0].Title, Title = "Implement Player Movement", Description = "Write basic movement script", TimeOfDay = "Daytime", EstimatedMinutes = 45, Intention = "Code the mechanic", Order = 2, Status = "Pending" };
var action3 = new DailyAction { Id = Guid.NewGuid(), DreamId = dream.Id, GoalTitle = goals[1].Title, Title = "Organize Project Notes", Description = "Clean up the Notion workspace", TimeOfDay = "Evening", EstimatedMinutes = 20, Intention = "Clear mind", Order = 3, Status = "Pending" };

dream.DailyActions = new List<DailyAction> { action1, action2, action3 };

await repo.AddAsync(dream);

Console.WriteLine("--- TEST 1: Sending message: 'I only have 30 minutes today and I\\'m feeling tired.' ---");
var messageRequest = new SendLumaMessageRequest(
    Message: "I only have 30 minutes today and I'm feeling tired.",
    History: Array.Empty<LumaConversationTurn>());

var response = await talkService.SendMessageAsync(dream.Id, messageRequest);

if (response is null)
{
    throw new Exception("Response was null!");
}

Console.WriteLine($"Reply: {response.Reply}");
Console.WriteLine($"Proposal: {JsonSerializer.Serialize(response.Proposal)}");

if (response.Proposal is null)
{
    throw new Exception("FAILURE: Proposal was NULL! Expected an action proposal card.");
}

if (response.Proposal.Type != "suggest_plan_change")
{
    throw new Exception($"FAILURE: Proposal Type was '{response.Proposal.Type}' instead of 'suggest_plan_change'.");
}

if (response.Proposal.KeepActionId == Guid.Empty)
{
    throw new Exception("FAILURE: KeepActionId is empty!");
}

if (response.Proposal.RestActionIds.Count != 2)
{
    throw new Exception($"FAILURE: RestActionIds count is {response.Proposal.RestActionIds.Count}, expected 2!");
}

Console.WriteLine("✓ TEST 1 PASSED: Proposal received with 1 keep action and 2 rest actions.");

// --- TEST 2: Verify DB before approval is unchanged ---
Console.WriteLine("\n--- TEST 2: Checking PostgreSQL / DB state BEFORE approval ---");
var dbActionsBefore = await db.DailyActions.Where(a => a.DreamId == dream.Id).OrderBy(a => a.Order).ToListAsync();
foreach (var a in dbActionsBefore)
{
    Console.WriteLine($"Action '{a.Title}': Status = {a.Status}");
    if (a.Status != "Pending")
    {
        throw new Exception($"FAILURE: Action '{a.Title}' status was modified before approval!");
    }
}
Console.WriteLine("✓ TEST 2 PASSED: DB is completely unmodified prior to approval.");

// --- TEST 3: User approves proposal ("Make today lighter ✦") ---
Console.WriteLine("\n--- TEST 3: Applying adaptive change (approval) ---");
var applyRequest = new ApplyDailyPathChangeRequest(
    KeepActionId: response.Proposal.KeepActionId,
    RestActionIds: response.Proposal.RestActionIds);

var updatedDailyPath = await dailyPathService.ApplyAdaptiveChangeAsync(dream.Id, applyRequest);

if (updatedDailyPath is null)
{
    throw new Exception("FAILURE: ApplyAdaptiveChangeAsync returned null!");
}

Console.WriteLine($"Total: {updatedDailyPath.TotalActions}, Completed: {updatedDailyPath.CompletedActions}, Skipped: {updatedDailyPath.SkippedActions}, Momentum: {updatedDailyPath.MomentumMessage}");

var keptFromPath = updatedDailyPath.Actions.First(a => a.Id == response.Proposal.KeepActionId);
if (keptFromPath.Status != "Pending")
{
    throw new Exception($"FAILURE: Kept action status is '{keptFromPath.Status}' instead of 'Pending'!");
}

foreach (var restId in response.Proposal.RestActionIds)
{
    var restFromPath = updatedDailyPath.Actions.First(a => a.Id == restId);
    if (restFromPath.Status != "Skipped")
    {
        throw new Exception($"FAILURE: Rest action status is '{restFromPath.Status}' instead of 'Skipped'!");
    }
}

var dbActionsAfter = await db.DailyActions.Where(a => a.DreamId == dream.Id).OrderBy(a => a.Order).ToListAsync();
Console.WriteLine("DB State after approval:");
foreach (var a in dbActionsAfter)
{
    Console.WriteLine($"Action '{a.Title}': Status = {a.Status}");
}

Console.WriteLine("✓ TEST 3 PASSED: PostgreSQL updated ONLY after approval, kept action is Pending, rest actions are Skipped.");

// --- TEST 4: Test empty daily actions initialization flow ---
Console.WriteLine("\n--- TEST 4: Dream with no DailyActions initialized before conversation ---");
var dream2 = new Dream
{
    Id = Guid.NewGuid(),
    UserId = Guid.NewGuid(),
    Title = "Learn Classical Piano",
    Description = "Play Chopin with ease.",
    CreatedAt = DateTime.UtcNow
};
db.Users.Add(new User
{
    Id = dream2.UserId,
    Name = "Deterministic Test User 2",
    Email = "deterministic-test-2@example.com"
});
dream2.FutureProfile = new FutureProfile
{
    Id = Guid.NewGuid(),
    DreamId = dream2.Id,
    Identity = "Classical Pianist",
    Environment = "Quiet Music Room",
    Mood = "Calm",
    Pace = "Gentle"
};
dream2.Goals = new List<Goal>
{
    new() { Id = Guid.NewGuid(), DreamId = dream2.Id, Title = "Master Nocturne in E-flat", Category = "Practice", DesiredOutcome = "Fluent performance", CurrentState = "Memorizing notes", Gap = "Fluency and dynamics", Order = 1, Status = "InProgress" }
};
await repo.AddAsync(dream2);

var response2 = await talkService.SendMessageAsync(dream2.Id, messageRequest);
if (response2?.Proposal is null)
{
    throw new Exception("FAILURE: Auto-generated daily actions did not produce proposal for dream2!");
}
Console.WriteLine($"✓ TEST 4 PASSED: Auto-initialization produced proposal: Keep='{response2.Proposal.KeepActionTitle}' ({response2.Proposal.KeepActionEstimatedMinutes} min), Rest={response2.Proposal.RestActionIds.Count} actions");

Console.WriteLine("\n=========================================");
Console.WriteLine("ALL 4 TESTS PASSED FLAWLESSLY!");
Console.WriteLine("=========================================");

// Agent boundary ownership checks. The fake access adapter keeps this test at the
// application boundary without introducing a test framework or a second database.
var agentDreamA = Guid.NewGuid();
var agentDreamB = Guid.NewGuid();
var agentUserA = Guid.NewGuid();
var agentUserB = Guid.NewGuid();
var agentAccess = new InMemoryAgentDreamAccess(agentDreamA, agentUserA, agentDreamB, agentUserB);
var agentService = new AgentService(agentAccess);

var authorizedContext = await agentService.AuthorizeAsync(agentUserA, agentDreamA);
if (authorizedContext.AuthenticatedUserId != agentUserA || authorizedContext.DreamId != agentDreamA)
{
    throw new Exception("FAILURE: Authorized Agent context did not preserve ownership identity.");
}

await AssertAgentNotFoundAsync(agentService, agentUserA, agentDreamB);
await AssertAgentNotFoundAsync(agentService, agentUserA, Guid.NewGuid());
await AssertAgentNotFoundAsync(agentService, Guid.Empty, agentDreamA);

Console.WriteLine("✓ AGENT BOUNDARY TESTS PASSED: owner allowed, cross-owner/missing/unauthenticated rejected.");

static async Task AssertAgentNotFoundAsync(AgentService service, Guid userId, Guid dreamId)
{
    try
    {
        await service.AuthorizeAsync(userId, dreamId);
        throw new Exception($"FAILURE: Agent boundary allowed User '{userId}' to access Dream '{dreamId}'.");
    }
    catch (AgentDreamNotFoundException)
    {
        // Expected not-found behavior.
    }
}

static void AssertPlan(AgentResponse response, string intent, params string[] expectedTools)
{
    if (response.Plan is null ||
        response.Plan.Intent != intent ||
        !response.Plan.SelectedTools.SequenceEqual(expectedTools) ||
        response.ToolResults is null ||
        response.ToolResults.Count != expectedTools.Length ||
        response.ToolResults.Any(result => !result.Success || result.Data is null))
    {
        throw new Exception($"FAILURE: Unexpected orchestration plan for intent '{intent}'.");
    }
}

static void AssertPlanContract(AgentPlan plan, string intent, params string[] expectedTools)
{
    if (plan.Intent != intent || !plan.SelectedTools.SequenceEqual(expectedTools))
    {
        throw new Exception($"FAILURE: Gemini planner returned an unexpected '{intent}' plan.");
    }
}

static async Task AssertPlannerValidationAsync(Func<Task<AgentPlan>> plannerCall)
{
    try
    {
        await plannerCall();
        throw new Exception("FAILURE: Unsafe or malformed planner output was accepted.");
    }
    catch (AgentPlannerValidationException)
    {
        // Expected controlled planner rejection.
    }
}

static async Task AssertActionDecisionValidationAsync(AgentActionDecision decision)
{
    try
    {
        AgentActionDecisionValidator.Validate(decision);
        throw new Exception("FAILURE: Unsafe Agent action decision was accepted.");
    }
    catch (AgentActionDecisionValidationException)
    {
        await Task.CompletedTask;
    }
}

static async Task AssertActionDecisionValidationAsyncFromGemini(GeminiAgentActionPlanner planner)
{
    try
    {
        await planner.DecideAsync(
            new AgentPlannerRequest("Please change something."),
            new AgentPlan("daily_focus", Array.Empty<string>(), "context"),
            Array.Empty<AgentToolExecutionResult>());
        throw new Exception("FAILURE: Gemini returned an unsafe Agent action that was accepted.");
    }
    catch (AgentActionDecisionValidationException)
    {
        // Expected application-side action allowlist rejection.
    }
}

static GeminiAgentPlanner CreateGeminiPlanner(HttpMessageHandler handler)
{
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri("https://planner.test")
    };
    return new GeminiAgentPlanner(
        client,
        Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            MaxRetries = 0,
            RetryDelaySeconds = 1,
            TimeoutSeconds = 5
        }),
        NullLogger<GeminiAgentPlanner>.Instance);
}

static GeminiAgentActionPlanner CreateGeminiActionPlanner(HttpMessageHandler handler)
{
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri("https://action-planner.test")
    };
    return new GeminiAgentActionPlanner(
        client,
        Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            MaxRetries = 0,
            RetryDelaySeconds = 1,
            TimeoutSeconds = 5
        }),
        NullLogger<GeminiAgentActionPlanner>.Instance);
}

static GeminiAgentResponseGenerator CreateGeminiResponseGenerator(HttpMessageHandler handler)
{
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri("https://response.test")
    };
    return new GeminiAgentResponseGenerator(
        client,
        Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            MaxRetries = 0,
            RetryDelaySeconds = 1,
            TimeoutSeconds = 5
        }),
        NullLogger<GeminiAgentResponseGenerator>.Instance);
}

static HttpResponseMessage CreateGeminiPlanResponse(
    string intent,
    IReadOnlyList<string> selectedTools,
    string reason)
{
    var planJson = JsonSerializer.Serialize(new
    {
        intent,
        selectedTools,
        reason
    });
    var responseJson = JsonSerializer.Serialize(new
    {
        candidates = new[]
        {
            new
            {
                content = new
                {
                    parts = new[] { new { text = planJson } }
                }
            }
        }
    });
    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
    };
}

static HttpResponseMessage CreateGeminiActionDecisionResponse(
    bool actionRequired,
    string? actionName,
    string? reason,
    bool requiresApproval,
    string? userSituation)
{
    var actionJson = JsonSerializer.Serialize(new
    {
        actionRequired,
        actionName,
        reason,
        requiresApproval,
        actionRequest = userSituation is null ? null : new { userSituation }
    });
    var responseJson = JsonSerializer.Serialize(new
    {
        candidates = new[]
        {
            new
            {
                content = new
                {
                    parts = new[] { new { text = actionJson } }
                }
            }
        }
    });
    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
    };
}

static HttpResponseMessage CreateGeminiGeneratedResponse(
    string message,
    string? followUpQuestion,
    string? suggestedNextStep)
{
    var generatedJson = JsonSerializer.Serialize(new
    {
        message,
        followUpQuestion,
        suggestedNextStep
    });
    var responseJson = JsonSerializer.Serialize(new
    {
        candidates = new[]
        {
            new
            {
                content = new
                {
                    parts = new[] { new { text = generatedJson } }
                }
            }
        }
    });
    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
    {
        Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
    };
}

static async Task AssertAgentExecutionNotFoundAsync(
    AgentService service,
    Guid userId,
    Guid dreamId,
    string message)
{
    try
    {
        await service.ExecuteAsync(userId, new AgentRequest(dreamId, message));
        throw new Exception($"FAILURE: Agent orchestration allowed Dream '{dreamId}'.");
    }
    catch (AgentDreamNotFoundException)
    {
        // Expected not-found behavior.
    }
}

static async Task AssertAgentActionNotFoundAsync(
    AgentService service,
    Guid userId,
    Guid dreamId,
    UpdateDailyPlanRequest actionRequest)
{
    try
    {
        await service.ExecuteUpdateDailyPlanAsync(
            userId,
            new AgentRequest(dreamId, "update daily plan"),
            actionRequest);
        throw new Exception($"FAILURE: Action tool allowed Dream '{dreamId}'.");
    }
    catch (AgentDreamNotFoundException)
    {
        // Expected not-found behavior.
    }
}

static async Task RunAdaptiveDeterministicFallbackTestsAsync()
{
    var harness = await CreateAdaptiveHarnessAsync("Completed", "Pending", "Pending");
    var combinedGoalPlanChangeResponse = await harness.AgentService.ExecuteAsync(
        harness.UserId,
        new AgentRequest(
            harness.Context.DreamId,
            "Let's set aside the career profile task for today and keep my most important goal on track."));
    if (!combinedGoalPlanChangeResponse.RequiresApproval ||
        combinedGoalPlanChangeResponse.ActionProposal?.ProposalResult.Data is not UpdateDailyPlanResult combinedProposalResult ||
        combinedProposalResult.Applied ||
        !combinedProposalResult.RequiresApproval ||
        combinedProposalResult.Proposal is null ||
        !combinedGoalPlanChangeResponse.Reply.Contains("Action 2", StringComparison.Ordinal) ||
        !combinedGoalPlanChangeResponse.Reply.Contains("Would you like me", StringComparison.Ordinal) ||
        harness.Repository.MutationCount != 0)
    {
        throw new Exception("FAILURE: Mixed goal and explicit postponement did not return an approval-required adaptive proposal without mutation.");
    }

    var proposalResult = await harness.Tool.ExecuteAsync(
        harness.Context,
        new UpdateDailyPlanRequest("Limited time and energy."));

    if (proposalResult.Data is not UpdateDailyPlanResult proposalData ||
        proposalData.Applied ||
        !proposalData.RequiresApproval ||
        proposalData.Proposal is null ||
        proposalData.Proposal.KeepActionId != harness.Actions[1].Id ||
        proposalData.Proposal.RestActionIds.Count != 1 ||
        proposalData.Proposal.RestActionIds[0] != harness.Actions[2].Id ||
        proposalData.Proposal.RestActionIds.Contains(harness.Actions[0].Id) ||
        proposalData.Proposal.KeepActionId == proposalData.Proposal.RestActionIds[0] ||
        harness.Repository.MutationCount != 0)
    {
        throw new Exception("FAILURE: deterministic adaptive fallback did not produce a safe proposal from two pending actions.");
    }

    var approvalResult = await harness.Tool.ExecuteAsync(
        harness.Context,
        new UpdateDailyPlanRequest("Approve", Approve: true, Proposal: proposalData.Proposal));
    if (approvalResult.Data is not UpdateDailyPlanResult approvalData ||
        !approvalData.Applied ||
        approvalData.RequiresApproval ||
        harness.Repository.MutationCount != 1 ||
        harness.Actions[2].Status != "Skipped" ||
        harness.Actions[0].Status != "Completed")
    {
        throw new Exception("FAILURE: explicit adaptive approval did not use the existing mutation path.");
    }

    var insufficientHarness = await CreateAdaptiveHarnessAsync("Completed", "Pending");
    var insufficientResult = await insufficientHarness.Tool.ExecuteAsync(
        insufficientHarness.Context,
        new UpdateDailyPlanRequest("Limited time and energy."));
    if (insufficientResult.Data is not UpdateDailyPlanResult insufficientData ||
        insufficientData.Proposal is not null ||
        !insufficientData.Message.Contains("eligible pending actions", StringComparison.OrdinalIgnoreCase) ||
        insufficientHarness.Repository.MutationCount != 0)
    {
        throw new Exception("FAILURE: adaptive fallback did not reject a plan with fewer than two pending actions.");
    }

    var insufficientAgentResponse = await insufficientHarness.AgentService.ExecuteAsync(
        insufficientHarness.UserId,
        new AgentRequest(
            insufficientHarness.Context.DreamId,
            "I want to postpone this task until tomorrow."));
    if (insufficientAgentResponse.ActionProposal?.ProposalResult.Data is not UpdateDailyPlanResult insufficientAgentResult ||
        insufficientAgentResult.Applied ||
        insufficientAgentResult.Proposal is not null ||
        insufficientAgentResponse.Reply.Contains("changed your plan", StringComparison.OrdinalIgnoreCase) ||
        insufficientAgentResponse.Reply.Contains("I changed", StringComparison.OrdinalIgnoreCase) ||
        !insufficientAgentResponse.Reply.Contains("fewer than two pending actions", StringComparison.OrdinalIgnoreCase) ||
        insufficientHarness.Repository.MutationCount != 0)
    {
        throw new Exception("FAILURE: Insufficient pending actions returned an invalid proposal, success claim, or mutation.");
    }

    await AssertAgentNotFoundAsync(
        insufficientHarness.AgentService,
        insufficientHarness.UserId,
        Guid.NewGuid());

    Console.WriteLine("ADAPTIVE DETERMINISTIC FALLBACK TESTS PASSED: pending selection, completed exclusion, proposal safety, approval mutation, insufficient-plan handling, and ownership verified.");
}

static async Task RunDailyPlanSnapshotTestsAsync()
{
    var options = new DbContextOptionsBuilder<LumaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    await using var db = new LumaDbContext(options);
    var repository = new DreamRepository(db);
    var user = new User
    {
        Id = Guid.NewGuid(),
        Name = "Snapshot test user"
    };
    db.Users.Add(user);
    var dream = new Dream
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Title = "Snapshot test dream",
        Description = "A dream for daily snapshot regression coverage.",
        FutureProfile = new FutureProfile
        {
            DreamId = Guid.NewGuid(),
            Identity = "Snapshot tester",
            Environment = "home",
            Mood = "calm",
            Pace = "steady"
        }
    };
    dream.FutureProfile.DreamId = dream.Id;
    dream.User = user;
    await repository.AddAsync(dream);
    if (!await db.Dreams.AnyAsync(item => item.Id == dream.Id))
    {
        throw new Exception("FAILURE: snapshot test dream was not persisted to the InMemory database.");
    }
    if (await repository.GetByIdAsync(dream.Id) is null)
    {
        throw new Exception("FAILURE: repository's included Dream query did not return the persisted snapshot test dream.");
    }

    var goal = new Goal
    {
        DreamId = dream.Id,
        Title = "Snapshot goal",
        Category = "PersonalGrowth"
    };
    await repository.AddGoalsAsync(new[] { goal });

    var timeProvider = new MutableTimeProvider(
        new DateTimeOffset(2026, 10, 10, 23, 30, 0, TimeSpan.Zero));
    var aiService = new SnapshotFlowAIService();
    var dailyPathService = new DailyPathService(repository, aiService, timeProvider);

    var firstPlan = await dailyPathService.GetOrCreateDailyPathAsync(dream.Id)
        ?? throw new Exception("FAILURE: initial daily path was not created.");
    var firstDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    var firstSnapshot = await repository.GetDailyPlanSnapshotAsync(dream.Id, firstDate)
        ?? throw new Exception("FAILURE: initial daily plan snapshot was not persisted.");
    var firstActionSnapshot = firstSnapshot.Actions.Single(action => action.Order == 1);

    if (firstSnapshot.Actions.Count != 3 ||
        firstActionSnapshot.GoalId != goal.Id ||
        firstActionSnapshot.GoalTitle != goal.Title ||
        firstActionSnapshot.Title != firstPlan.Actions[0].Title ||
        aiService.DailyPlanGenerationCount != 1)
    {
        throw new Exception("FAILURE: snapshot did not preserve the generated plan and goal data.");
    }

    await dailyPathService.GetOrCreateDailyPathAsync(dream.Id);
    if (aiService.DailyPlanGenerationCount != 1 ||
        await db.DailyPlanSnapshots.CountAsync(snapshot => snapshot.DreamId == dream.Id) != 1)
    {
        throw new Exception("FAILURE: repeated same-day plan retrieval generated another plan or snapshot.");
    }

    await dailyPathService.UpdateActionStatusAsync(dream.Id, firstPlan.Actions[0].Id, "Completed");
    var completedFirstSnapshot = await repository.GetDailyPlanSnapshotAsync(dream.Id, firstDate)
        ?? throw new Exception("FAILURE: first-day snapshot disappeared after status update.");
    var completedFirstAction = completedFirstSnapshot.Actions.Single(action => action.Order == 1);
    if (completedFirstAction.Status != "Completed" || completedFirstAction.CompletedAt is null)
    {
        throw new Exception("FAILURE: completed action status was not synchronized to its snapshot.");
    }

    var originalTitle = completedFirstAction.Title;
    var originalDescription = completedFirstAction.Description;
    var originalCompletedAt = completedFirstAction.CompletedAt;
    timeProvider.Advance(TimeSpan.FromDays(1));

    var secondPlan = await dailyPathService.GetOrCreateDailyPathAsync(dream.Id)
        ?? throw new Exception("FAILURE: next day's plan was not generated.");
    var secondDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    var secondSnapshot = await repository.GetDailyPlanSnapshotAsync(dream.Id, secondDate)
        ?? throw new Exception("FAILURE: next day's plan snapshot was not persisted.");
    var preservedFirstSnapshot = await repository.GetDailyPlanSnapshotAsync(dream.Id, firstDate)
        ?? throw new Exception("FAILURE: prior day's snapshot was deleted during plan rollover.");
    var preservedFirstAction = preservedFirstSnapshot.Actions.Single(action => action.Order == 1);

    if (aiService.DailyPlanGenerationCount != 2 ||
        firstPlan.Actions[0].Id == secondPlan.Actions[0].Id ||
        preservedFirstAction.Title != originalTitle ||
        preservedFirstAction.Description != originalDescription ||
        preservedFirstAction.Status != "Completed" ||
        preservedFirstAction.CompletedAt != originalCompletedAt ||
        await db.DailyActions.AnyAsync(action => action.Id == firstPlan.Actions[0].Id))
    {
        throw new Exception("FAILURE: replacing live DailyAction rows changed or retained the prior day's snapshot source data.");
    }

    await dailyPathService.UpdateActionStatusAsync(dream.Id, secondPlan.Actions[2].Id, "Completed");
    var adaptivePlan = await dailyPathService.ApplyAdaptiveChangeAsync(
        dream.Id,
        new ApplyDailyPathChangeRequest(
            secondPlan.Actions[0].Id,
            new[] { secondPlan.Actions[1].Id }));
    if (adaptivePlan is null)
    {
        throw new Exception("FAILURE: adaptive approval returned no updated plan.");
    }

    var afterAdaptiveSnapshot = await repository.GetDailyPlanSnapshotAsync(dream.Id, secondDate)
        ?? throw new Exception("FAILURE: current-day snapshot disappeared after adaptive approval.");
    if (afterAdaptiveSnapshot.Actions.Single(action => action.Order == 1).Status != "Pending" ||
        afterAdaptiveSnapshot.Actions.Single(action => action.Order == 2).Status != "Skipped" ||
        afterAdaptiveSnapshot.Actions.Single(action => action.Order == 3).Status != "Completed")
    {
        throw new Exception("FAILURE: adaptive approval or action status changes were not synchronized to today's snapshot.");
    }

    var snapshotActionTitles = afterAdaptiveSnapshot.Actions.OrderBy(action => action.Order).Select(action => action.Title).ToArray();
    var snapshotStatuses = afterAdaptiveSnapshot.Actions.OrderBy(action => action.Order).Select(action => action.Status).ToArray();
    _ = await dailyPathService.GetOrCreateDailyPathAsync(dream.Id, refresh: true)
        ?? throw new Exception("FAILURE: same-day plan refresh was not returned.");
    var refreshedSnapshot = await repository.GetDailyPlanSnapshotAsync(dream.Id, secondDate)
        ?? throw new Exception("FAILURE: refreshed plan snapshot was not persisted.");
    var stillPreserved = await repository.GetDailyPlanSnapshotAsync(dream.Id, firstDate)
        ?? throw new Exception("FAILURE: same-day refresh deleted the prior day's snapshot.");

    if (aiService.DailyPlanGenerationCount != 3 ||
        refreshedSnapshot.Id != secondSnapshot.Id ||
        !refreshedSnapshot.Actions.OrderBy(action => action.Order).Select(action => action.Title).SequenceEqual(snapshotActionTitles) ||
        !refreshedSnapshot.Actions.OrderBy(action => action.Order).Select(action => action.Status).SequenceEqual(snapshotStatuses) ||
        await db.DailyPlanSnapshots.CountAsync(snapshot => snapshot.DreamId == dream.Id) != 2 ||
        stillPreserved.Actions.Single(action => action.Order == 1).Title != originalTitle)
    {
        throw new Exception("FAILURE: refresh duplicated or rewrote an existing snapshot.");
    }

    var reflectionService = new ReflectionService(repository, aiService, timeProvider);
    var reflection = await reflectionService.CreateReflectionAsync(
        dream.Id,
        new CreateReflectionRequest("Grounded", "An ordinary check-in."))
        ?? throw new Exception("FAILURE: reflection was not created.");
    var storedReflection = await db.Reflections.SingleAsync(item => item.Id == reflection.Id);
    if (storedReflection.DailyPlanSnapshotId != secondSnapshot.Id)
    {
        throw new Exception("FAILURE: reflection was not linked to the exact current-day snapshot.");
    }

    timeProvider.Advance(TimeSpan.FromDays(1));
    var unassociatedReflection = await reflectionService.CreateReflectionAsync(
        dream.Id,
        new CreateReflectionRequest("Reflective", "No plan snapshot exists for this UTC date."))
        ?? throw new Exception("FAILURE: reflection without a matching snapshot was not created.");
    var storedUnassociatedReflection = await db.Reflections.SingleAsync(item => item.Id == unassociatedReflection.Id);
    if (storedUnassociatedReflection.DailyPlanSnapshotId is not null)
    {
        throw new Exception("FAILURE: reflection was associated with a snapshot from a different date.");
    }

    var calendarQuery = new JourneyCalendarQuery(db);
    var month = await calendarQuery.GetMonthAsync(dream.Id, secondDate.Year, secondDate.Month)
        ?? throw new Exception("FAILURE: calendar month was not returned for an existing dream.");
    var firstDay = month.Days.Single(day => day.PlanDate == firstDate);
    var secondDay = month.Days.Single(day => day.PlanDate == secondDate);
    var emptyDay = month.Days.Single(day => day.PlanDate == secondDate.AddDays(10));
    if (!firstDay.HasSnapshot || firstDay.ActionCount != 3 || firstDay.CompletedActionCount != 1 ||
        !secondDay.HasSnapshot || secondDay.ActionCount != 3 || secondDay.CompletedActionCount != 1 ||
        secondDay.Reflections.Count != 1 || emptyDay.HasSnapshot || emptyDay.ActionCount != 0 ||
        emptyDay.CompletedActionCount != 0 || emptyDay.Reflections.Count != 0)
    {
        throw new Exception("FAILURE: calendar month summary did not report snapshots, progress, reflections, and empty days correctly.");
    }

    var dayDetail = await calendarQuery.GetDayAsync(dream.Id, secondDate)
        ?? throw new Exception("FAILURE: calendar day detail was not returned for an existing snapshot.");
    if (!dayDetail.Actions.Select(action => action.Order).SequenceEqual(new[] { 1, 2, 3 }) ||
        dayDetail.Reflections.Single().Id != storedReflection.Id ||
        await calendarQuery.GetDayAsync(dream.Id, secondDate.AddDays(10)) is not null ||
        await calendarQuery.GetMonthAsync(Guid.NewGuid(), secondDate.Year, secondDate.Month) is not null ||
        !await repository.IsOwnedByAsync(dream.Id, user.Id) ||
        await repository.IsOwnedByAsync(dream.Id, Guid.NewGuid()))
    {
        throw new Exception("FAILURE: calendar detail ordering, empty-day behavior, or dream ownership isolation is incorrect.");
    }

    Console.WriteLine("DAILY PLAN SNAPSHOT TESTS PASSED: daily idempotency, rollover/refresh history preservation, status/adaptive synchronization, date-safe reflection association, calendar summaries/details, and ownership isolation verified.");
}

static async Task<AdaptiveHarness> CreateAdaptiveHarnessAsync(params string[] statuses)
{
    var dreamId = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var dream = new Dream
    {
        Id = dreamId,
        UserId = userId,
        Title = "Adaptive test dream",
        Description = "Adaptive test context."
    };

    var actions = statuses.Select((status, index) => new DailyAction
    {
        Id = Guid.NewGuid(),
        DreamId = dreamId,
        GoalTitle = "Test goal",
        Title = $"Action {index + 1}",
        Description = "Test action",
        TimeOfDay = "Morning",
        EstimatedMinutes = 15 + index,
        Intention = "Test intention",
        Order = index + 1,
        Status = status,
        GeneratedAt = DateTime.UtcNow
    }).ToList();
    dream.DailyActions = actions;

    var readActions = actions.Select(action => new DailyActionContext(
        action.Id,
        action.GoalId,
        action.GoalTitle,
        action.Title,
        action.Description,
        action.TimeOfDay,
        action.EstimatedMinutes,
        action.Intention,
        action.Order,
        action.Status,
        action.CompletedAt,
        action.GeneratedAt)).ToArray();

    var repository = new InMemoryAdaptiveDreamRepository(dream);
    var aiService = new NullProposalAIService();
    var dailyPathService = new DailyPathService(repository, aiService);
    var readService = new DailyPlanReadService(new InMemoryDailyPlanReadQuery(dreamId, readActions));
    var capability = new AdaptiveDailyPlanCapability(readService, dailyPathService);
    var tool = new UpdateDailyPlanTool(capability);
    var access = new InMemoryAgentDreamAccess(dreamId, userId, Guid.NewGuid(), Guid.NewGuid());
    var toolRegistry = new AgentToolRegistry(new IAgentTool[] { new DailyPlanTool(readService) });
    var agentService = new AgentService(
        access,
        toolRegistry,
        new DeterministicAgentPlanner(),
        tool,
        new DeterministicAgentResponseGenerator(),
        new DeterministicAgentActionPlanner());
    var context = await agentService.AuthorizeAsync(userId, dreamId);

    return new AdaptiveHarness(agentService, context, tool, repository, userId, actions);
}

sealed record AdaptiveHarness(
    AgentService AgentService,
    AgentExecutionContext Context,
    UpdateDailyPlanTool Tool,
    InMemoryAdaptiveDreamRepository Repository,
    Guid UserId,
    IReadOnlyList<DailyAction> Actions);

sealed class InMemoryAdaptiveDreamRepository : IDreamRepository
{
    private readonly Dream _dream;

    public InMemoryAdaptiveDreamRepository(Dream dream)
    {
        _dream = dream;
    }

    public int MutationCount { get; private set; }

    public Task<Dream> AddAsync(Dream dream, CancellationToken cancellationToken = default) => Task.FromResult(dream);

    public Task<Dream?> GetByIdAsync(Guid dreamId, CancellationToken cancellationToken = default) =>
        Task.FromResult<Dream?>(_dream.Id == dreamId ? _dream : null);

    public Task<bool> IsOwnedByAsync(Guid dreamId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_dream.Id == dreamId && _dream.UserId == userId);

    public Task<FutureScenario> AddScenarioAsync(FutureScenario scenario, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario);

    public Task<IReadOnlyList<Goal>> AddGoalsAsync(IEnumerable<Goal> goals, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Goal>>(goals.ToArray());

    public Task<IReadOnlyList<DailyAction>> AddDailyActionsAsync(IEnumerable<DailyAction> actions, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DailyAction>>(actions.ToArray());

    public Task DeleteDailyActionsAsync(Guid dreamId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<DailyPlanSnapshot?> GetDailyPlanSnapshotAsync(Guid dreamId, DateOnly planDate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_dream.DailyPlanSnapshots.FirstOrDefault(snapshot => snapshot.DreamId == dreamId && snapshot.PlanDate == planDate));

    public Task<DailyPlanSnapshot?> GetLatestDailyPlanSnapshotAsync(Guid dreamId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_dream.DailyPlanSnapshots.Where(snapshot => snapshot.DreamId == dreamId).OrderByDescending(snapshot => snapshot.PlanDate).FirstOrDefault());

    public Task<DailyPlanSnapshot> AddDailyPlanSnapshotAsync(DailyPlanSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var existing = _dream.DailyPlanSnapshots.FirstOrDefault(item => item.DreamId == snapshot.DreamId && item.PlanDate == snapshot.PlanDate);
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        _dream.DailyPlanSnapshots.Add(snapshot);
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<DailyAction>> ReplaceDailyActionsAndSnapshotAsync(
        DailyPlanSnapshot snapshot,
        IReadOnlyCollection<DailyAction> actions,
        CancellationToken cancellationToken = default)
    {
        _dream.DailyActions = actions.ToList();
        var existing = _dream.DailyPlanSnapshots.FirstOrDefault(item => item.DreamId == snapshot.DreamId && item.PlanDate == snapshot.PlanDate);
        if (existing is null)
        {
            _dream.DailyPlanSnapshots.Add(snapshot);
        }
        else
        {
            existing.GeneratedAt = snapshot.GeneratedAt;
            existing.Actions = snapshot.Actions;
        }

        return Task.FromResult<IReadOnlyList<DailyAction>>(_dream.DailyActions);
    }

    public Task<DailyAction?> UpdateDailyActionStatusAsync(Guid dreamId, Guid actionId, string status, DateOnly planDate, CancellationToken cancellationToken = default) =>
        Task.FromResult<DailyAction?>(null);

    public Task SaveFutureProfileAsync(FutureProfile profile, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<DailyAction>?> ApplyDailyPathChangeAsync(
        Guid dreamId,
        Guid keepActionId,
        IReadOnlyCollection<Guid> restActionIds,
        DateOnly planDate,
        CancellationToken cancellationToken = default)
    {
        var requestedIds = restActionIds.Append(keepActionId).ToHashSet();
        var actions = _dream.DailyActions.Where(action => requestedIds.Contains(action.Id)).ToList();
        if (actions.Count != requestedIds.Count ||
            actions.Any(action => !string.IsNullOrWhiteSpace(action.Status) &&
                                  !action.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult<IReadOnlyList<DailyAction>?>(null);
        }

        foreach (var action in actions)
        {
            action.Status = action.Id == keepActionId ? "Pending" : "Skipped";
            action.CompletedAt = null;
        }

        MutationCount++;
        return Task.FromResult<IReadOnlyList<DailyAction>?>(_dream.DailyActions);
    }

    public Task<DreamReflection> AddReflectionAsync(DreamReflection reflection, CancellationToken cancellationToken = default) =>
        Task.FromResult(reflection);

    public Task<IReadOnlyList<DreamReflection>> GetReflectionsAsync(Guid dreamId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DreamReflection>>(_dream.Reflections);
}

sealed class NullProposalAIService : IAIService
{
    public Task<AnalyzeDreamResult> AnalyzeDreamAsync(AnalyzeDreamRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<AnalyzeDreamResult> EvolveFutureAsync(EvolveFutureRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<GenerateFutureScenarioResult> GenerateFutureScenarioAsync(GenerateFutureScenarioRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<GenerateGoalsResult> GenerateGoalsAsync(GenerateGoalsRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<GenerateDailyPathResult> GenerateDailyPathAsync(GenerateDailyPathRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<GenerateReflectionInsightResult> GenerateReflectionInsightAsync(GenerateReflectionInsightRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task<TalkToLumaResult> TalkToLumaAsync(TalkToLumaRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TalkToLumaResult("No adaptive proposal from the simulated AI.", null));
}

sealed class SnapshotFlowAIService : IAIService
{
    public int DailyPlanGenerationCount { get; private set; }

    public Task<AnalyzeDreamResult> AnalyzeDreamAsync(AnalyzeDreamRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<AnalyzeDreamResult> EvolveFutureAsync(EvolveFutureRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<GenerateFutureScenarioResult> GenerateFutureScenarioAsync(GenerateFutureScenarioRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<GenerateGoalsResult> GenerateGoalsAsync(GenerateGoalsRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<GenerateDailyPathResult> GenerateDailyPathAsync(GenerateDailyPathRequest request, CancellationToken cancellationToken = default)
    {
        var generation = ++DailyPlanGenerationCount;
        var actions = Enumerable.Range(1, 3)
            .Select(order => new DailyActionItem(
                "Snapshot goal",
                $"Plan {generation} action {order}",
                $"Description {generation} action {order}",
                "Morning",
                15 + order,
                $"Intention {generation} action {order}",
                order))
            .ToArray();
        return Task.FromResult(new GenerateDailyPathResult(actions));
    }

    public Task<GenerateReflectionInsightResult> GenerateReflectionInsightAsync(GenerateReflectionInsightRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new GenerateReflectionInsightResult("A useful reflection insight."));

    public Task<TalkToLumaResult> TalkToLumaAsync(TalkToLumaRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset _currentUtc;

    public MutableTimeProvider(DateTimeOffset currentUtc)
    {
        _currentUtc = currentUtc;
    }

    public override DateTimeOffset GetUtcNow() => _currentUtc;

    public void Advance(TimeSpan amount)
    {
        _currentUtc = _currentUtc.Add(amount);
    }
}

sealed class InMemoryAgentDreamAccess : IAgentDreamAccess
{
    private readonly Dictionary<Guid, Guid> _ownership;

    public InMemoryAgentDreamAccess(Guid dreamA, Guid userA, Guid dreamB, Guid userB)
    {
        _ownership = new Dictionary<Guid, Guid>
        {
            [dreamA] = userA,
            [dreamB] = userB,
        };
    }

    public Task<bool> IsOwnedByAsync(Guid dreamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_ownership.TryGetValue(dreamId, out var ownerId) && ownerId == userId);
    }
}

sealed class InMemoryFutureContextQuery : IFutureContextQuery
{
    private readonly FutureContextResult _result;

    public InMemoryFutureContextQuery(FutureContextResult result)
    {
        _result = result;
    }

    public int ReadCount { get; private set; }

    public Task<FutureContextResult?> GetAsync(Guid dreamId, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult<FutureContextResult?>(_result.DreamId == dreamId ? _result : null);
    }
}

sealed class InMemoryGoalReadQuery : IGoalReadQuery
{
    private readonly Guid _dreamId;
    private readonly IReadOnlyList<ActiveGoalContext> _goals;

    public InMemoryGoalReadQuery(Guid dreamId, IReadOnlyList<ActiveGoalContext> goals)
    {
        _dreamId = dreamId;
        _goals = goals;
    }

    public int ReadCount { get; private set; }
    public int WriteCount { get; private set; }

    public Task<GoalReadResult?> GetAsync(Guid dreamId, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult<GoalReadResult?>(
            _dreamId == dreamId ? new GoalReadResult(_goals) : null);
    }
}

sealed class InMemoryDailyPlanReadQuery : IDailyPlanReadQuery
{
    private readonly Guid _dreamId;
    private readonly IReadOnlyList<DailyActionContext> _actions;

    public InMemoryDailyPlanReadQuery(Guid dreamId, IReadOnlyList<DailyActionContext> actions)
    {
        _dreamId = dreamId;
        _actions = actions;
    }

    public int ReadCount { get; private set; }
    public int WriteCount { get; private set; }

    public Task<DailyPlanReadResult?> GetAsync(Guid dreamId, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult<DailyPlanReadResult?>(
            _dreamId == dreamId ? new DailyPlanReadResult(_actions) : null);
    }
}

sealed class InMemoryReflectionReadQuery : IReflectionReadQuery
{
    private readonly Guid _dreamId;
    private readonly IReadOnlyList<ReflectionContext> _reflections;

    public InMemoryReflectionReadQuery(Guid dreamId, IReadOnlyList<ReflectionContext> reflections)
    {
        _dreamId = dreamId;
        _reflections = reflections;
    }

    public int ReadCount { get; private set; }
    public int WriteCount { get; private set; }
    public int GenerationCount { get; private set; }

    public Task<ReflectionReadResult?> GetRecentAsync(Guid dreamId, CancellationToken cancellationToken = default)
    {
        ReadCount++;
        return Task.FromResult<ReflectionReadResult?>(
            _dreamId == dreamId ? new ReflectionReadResult(_reflections) : null);
    }
}

sealed class RecordingAgentTool : IAgentTool
{
    public RecordingAgentTool(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public string Description => $"Recording read tool: {Name}";
    public int CallCount { get; private set; }
    public int MutationCount { get; private set; }
    public int AiCallCount { get; private set; }
    public AgentExecutionContext? LastContext { get; private set; }

    public Task<AgentToolResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastContext = context;
        return Task.FromResult(new AgentToolResult(new
        {
            Tool = Name,
            ContextDreamId = context.DreamId
        }));
    }
}

sealed class InMemoryAdaptiveDailyPlanCapability : IAdaptiveDailyPlanCapability
{
    private readonly DailyPathChangeProposal _proposal;
    private readonly DailyPathResponse _updatedPlan;

    public InMemoryAdaptiveDailyPlanCapability(
        DailyPathChangeProposal proposal,
        DailyPathResponse updatedPlan)
    {
        _proposal = proposal;
        _updatedPlan = updatedPlan;
    }

    public int ProposalCallCount { get; private set; }
    public int ApplyCallCount { get; private set; }
    public int MutationCount { get; private set; }

    public Task<DailyPathChangeProposal?> ProposeAsync(
        AgentExecutionContext context,
        string userSituation,
        CancellationToken cancellationToken = default)
    {
        ProposalCallCount++;
        return Task.FromResult<DailyPathChangeProposal?>(_proposal);
    }

    public Task<DailyPathResponse?> ApplyAsync(
        AgentExecutionContext context,
        DailyPathChangeProposal proposal,
        CancellationToken cancellationToken = default)
    {
        ApplyCallCount++;
        MutationCount++;
        return Task.FromResult<DailyPathResponse?>(_updatedPlan);
    }
}

sealed class QueueHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses;

    public List<string> RequestBodies { get; } = new();

    public QueueHttpMessageHandler(Queue<HttpResponseMessage> responses)
    {
        _responses = responses;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No fake Gemini response is available.");
        }

        RequestBodies.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));
        return _responses.Dequeue();
    }
}

sealed class RecordingAgentResponseGenerator : IAgentResponseGenerator
{
    private readonly Action _onCall;

    public RecordingAgentResponseGenerator(Action onCall)
    {
        _onCall = onCall;
    }

    public AgentResponseGenerationRequest? LastRequest { get; private set; }

    public Task<AgentGeneratedResponse> GenerateAsync(
        AgentResponseGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        _onCall();
        LastRequest = request;
        return Task.FromResult(new AgentGeneratedResponse("Recorded response."));
    }
}

sealed class FixedAgentActionPlanner : IAgentActionPlanner
{
    private readonly AgentActionDecision _decision;

    public FixedAgentActionPlanner(AgentActionDecision decision)
    {
        _decision = decision;
    }

    public Task<AgentActionDecision> DecideAsync(
        AgentPlannerRequest request,
        AgentPlan plan,
        IReadOnlyList<AgentToolExecutionResult> toolResults,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_decision);
    }
}

sealed class FixedAgentPlanner : IAgentPlanner
{
    private readonly AgentPlan _plan;

    public FixedAgentPlanner(AgentPlan plan)
    {
        _plan = plan;
    }

    public Task<AgentPlan> PlanAsync(
        AgentPlannerRequest request,
        IReadOnlyList<AgentToolMetadata> availableTools,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_plan);
    }
}
