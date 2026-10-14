using Luma.Application.Agent;
using Luma.Application.Interfaces;

namespace Luma.Application.Services;

public sealed class AgentService
{
    private readonly IAgentDreamAccess _dreamAccess;
    private readonly IAgentToolRegistry _toolRegistry;
    private readonly IAgentPlanner _planner;
    private readonly IAgentActionPlanner? _actionPlanner;
    private readonly IUpdateDailyPlanTool? _updateDailyPlanTool;
    private readonly IAgentResponseGenerator? _responseGenerator;

    public AgentService(
        IAgentDreamAccess dreamAccess,
        IAgentToolRegistry toolRegistry,
        IAgentPlanner planner,
        IUpdateDailyPlanTool? updateDailyPlanTool = null,
        IAgentResponseGenerator? responseGenerator = null,
        IAgentActionPlanner? actionPlanner = null)
    {
        _dreamAccess = dreamAccess;
        _toolRegistry = toolRegistry;
        _planner = planner;
        _actionPlanner = actionPlanner;
        _updateDailyPlanTool = updateDailyPlanTool;
        _responseGenerator = responseGenerator;
    }

    public async Task<AgentToolResult> ExecuteUpdateDailyPlanAsync(
        Guid authenticatedUserId,
        AgentRequest request,
        UpdateDailyPlanRequest actionRequest,
        CancellationToken cancellationToken = default)
    {
        var context = await AuthorizeAsync(authenticatedUserId, request.DreamId, cancellationToken);
        if (_updateDailyPlanTool is null)
        {
            throw new InvalidOperationException("The update_daily_plan action tool is not registered.");
        }

        return await _updateDailyPlanTool.ExecuteAsync(context, actionRequest, cancellationToken);
    }

    public AgentService(IAgentDreamAccess dreamAccess)
        : this(dreamAccess, new AgentToolRegistry(Array.Empty<IAgentTool>()), new DeterministicAgentPlanner())
    {
    }

    public async Task<AgentResponse> ExecuteAsync(
        Guid authenticatedUserId,
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = await AuthorizeAsync(authenticatedUserId, request.DreamId, cancellationToken);
        Console.WriteLine($"[AGENT_TRACE] ExecutionContextDreamId={context.DreamId}");
        var availableTools = _toolRegistry.GetAvailableTools();
        AgentPlan plan;
        try
        {
            plan = AgentPlanValidator.Validate(
                await _planner.PlanAsync(
                    new AgentPlannerRequest(request.Message),
                    availableTools,
                    cancellationToken),
                availableTools);
        }
        catch (AgentPlannerValidationException ex)
        {
            return new AgentResponse(
                Reply: string.Empty,
                RequiresApproval: false,
                Plan: null,
                ToolResults: Array.Empty<AgentToolExecutionResult>(),
                Error: ex.Message);
        }
        var results = new List<AgentToolExecutionResult>();

        foreach (var toolName in plan.SelectedTools)
        {
            var metadata = availableTools.FirstOrDefault(tool =>
                string.Equals(tool.Name, toolName, StringComparison.OrdinalIgnoreCase));
            var tool = _toolRegistry.Get(toolName);

            if (metadata is null || tool is null || !metadata.IsReadOnly)
            {
                results.Add(new AgentToolExecutionResult(
                    toolName,
                    Success: false,
                    Data: null,
                    Error: "Tool is not available as a read-only capability."));
                continue;
            }

            try
            {
                var toolResult = await tool.ExecuteAsync(context, cancellationToken);
                results.Add(new AgentToolExecutionResult(
                    toolName,
                    Success: true,
                    toolResult.Data,
                    RequiresApproval: toolResult.RequiresApproval));
            }
            catch (AgentDreamNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                results.Add(new AgentToolExecutionResult(
                    toolName,
                    Success: false,
                    Data: null,
                    Error: ex.Message));
            }
        }

        AgentActionProposal? actionProposal = null;
        if (_actionPlanner is not null)
        {
            AgentActionDecision decision;
            try
            {
                decision = AgentActionDecisionValidator.Validate(
                    await _actionPlanner.DecideAsync(
                        new AgentPlannerRequest(request.Message),
                        plan,
                        results,
                        cancellationToken));
            }
            catch (AgentActionDecisionValidationException ex)
            {
                return new AgentResponse(
                    Reply: string.Empty,
                    RequiresApproval: false,
                    Plan: plan,
                    ToolResults: results,
                    Error: ex.Message);
            }

            if (decision.ActionRequired)
            {
                if (_updateDailyPlanTool is null || decision.ActionRequest is null)
                {
                    return new AgentResponse(
                        Reply: string.Empty,
                        RequiresApproval: false,
                        Plan: plan,
                        ToolResults: results,
                        Error: "The requested Agent action is not available.");
                }

                AgentToolResult proposalResult;
                try
                {
                    proposalResult = await _updateDailyPlanTool.ExecuteAsync(
                        context,
                        new UpdateDailyPlanRequest(decision.ActionRequest.UserSituation),
                        cancellationToken);
                }
                catch (AgentDreamNotFoundException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return new AgentResponse(
                        Reply: string.Empty,
                        RequiresApproval: false,
                        Plan: plan,
                        ToolResults: results,
                        Error: ex.Message);
                }

                if (proposalResult.Data is not UpdateDailyPlanResult updateResult ||
                    updateResult.Applied ||
                    !updateResult.RequiresApproval)
                {
                    return new AgentResponse(
                        Reply: string.Empty,
                        RequiresApproval: false,
                        Plan: plan,
                        ToolResults: results,
                        Error: "Agent action did not return an approval-required proposal.");
                }

                actionProposal = new AgentActionProposal(
                    AgentActionDecisionValidator.UpdateDailyPlanAction,
                    decision.Reason ?? decision.ActionRequest.UserSituation,
                    RequiresApproval: true,
                    proposalResult);
            }
        }

        if (_responseGenerator is null)
        {
            return new AgentResponse(
                Reply: string.Empty,
                RequiresApproval: actionProposal is not null,
                ActionProposal: actionProposal,
                Plan: plan,
                ToolResults: results);
        }

        try
        {
            var generated = await _responseGenerator.GenerateAsync(
                new AgentResponseGenerationRequest(request.Message, plan, results, actionProposal),
                cancellationToken);
            return new AgentResponse(
                Reply: generated.Message,
                RequiresApproval: actionProposal is not null,
                ActionProposal: actionProposal,
                Plan: plan,
                ToolResults: results);
        }
        catch (AgentResponseGenerationValidationException ex)
        {
            return new AgentResponse(
                Reply: string.Empty,
                RequiresApproval: false,
                Plan: plan,
                ToolResults: results,
                Error: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new AgentResponse(
                Reply: string.Empty,
                RequiresApproval: false,
                Plan: plan,
                ToolResults: results,
                Error: ex.Message);
        }
    }

    public async Task<AgentExecutionContext> AuthorizeAsync(
        Guid authenticatedUserId,
        Guid dreamId,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty ||
            !await _dreamAccess.IsOwnedByAsync(dreamId, authenticatedUserId, cancellationToken))
        {
            throw new AgentDreamNotFoundException(dreamId);
        }

        return new AgentExecutionContext(authenticatedUserId, dreamId);
    }
}
