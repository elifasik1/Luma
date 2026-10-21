using Luma.Application.AI.Models;
using Luma.Domain.Entities;

namespace Luma.Application.AI;

public sealed class DevelopmentAIService : IAIService
{
    public Task<AnalyzeDreamResult> AnalyzeDreamAsync(
        AnalyzeDreamRequest request,
        CancellationToken cancellationToken = default)
    {
        var text = (request.DreamText ?? string.Empty).Trim();
        var normalized = text.ToLowerInvariant();

        var environment = normalized.Contains("city") || normalized.Contains("office") || normalized.Contains("meeting")
            ? "big_city"
            : normalized.Contains("nature") || normalized.Contains("forest") || normalized.Contains("mountain")
                ? "forest"
                : normalized.Contains("ocean") || normalized.Contains("beach") || normalized.Contains("travel")
                    ? "ocean"
                    : "home";

        var imagery = new List<ThemeImagery>();
        if (environment == "big_city") imagery.Add(ThemeImagery.City);
        if (environment == "forest") imagery.Add(ThemeImagery.Nature);
        if (environment == "ocean") imagery.Add(ThemeImagery.Ocean);
        if (environment == "home") imagery.Add(ThemeImagery.Home);
        if (normalized.Contains("travel") && !imagery.Contains(ThemeImagery.Travel)) imagery.Add(ThemeImagery.Travel);
        if (normalized.Contains("campus")) imagery.Add(ThemeImagery.Campus);
        if (normalized.Contains("workspace") || normalized.Contains("office")) imagery.Add(ThemeImagery.Workspace);
        if (normalized.Contains("bakery")) imagery.Add(ThemeImagery.Bakery);
        if (normalized.Contains("book")) imagery.Add(ThemeImagery.Books);
        if (normalized.Contains("technology") || normalized.Contains("software")) imagery.Add(ThemeImagery.Technology);

        var lifestyle = new List<string>();
        if (normalized.Contains("ambitious") || normalized.Contains("career") || normalized.Contains("growth")) lifestyle.Add("ambitious");
        if (normalized.Contains("community") || normalized.Contains("social") || normalized.Contains("friends")) lifestyle.Add("social");
        if (normalized.Contains("calm") || normalized.Contains("balance") || normalized.Contains("family")) lifestyle.Add("balanced");
        if (lifestyle.Count == 0) lifestyle.Add("ambitious");

        var aspirations = new List<string>();
        if (normalized.Contains("travel")) aspirations.Add("travel");
        if (normalized.Contains("software") || normalized.Contains("engineering") || normalized.Contains("build")) aspirations.Add("software_engineering");
        if (normalized.Contains("finance") || normalized.Contains("money") || normalized.Contains("independent")) aspirations.Add("financial_independence");
        if (normalized.Contains("freedom") || normalized.Contains("purpose")) aspirations.Add("freedom");
        if (normalized.Contains("family") || normalized.Contains("home")) aspirations.Add("family_life");
        if (aspirations.Count == 0) aspirations.Add("growth");

        var values = new List<string>();
        if (normalized.Contains("freedom")) values.Add("freedom");
        if (normalized.Contains("growth")) values.Add("growth");
        if (normalized.Contains("purpose")) values.Add("purpose");
        if (normalized.Contains("family")) values.Add("family");
        if (values.Count == 0) values.Add("clarity");

        var mood = normalized.Contains("tired") || normalized.Contains("overwhelmed")
            ? "calm"
            : normalized.Contains("creative")
                ? "creative"
                : "hopeful";

        var pace = normalized.Contains("slow") || normalized.Contains("rest")
            ? "steady"
            : "active";

        var palette = environment switch
        {
            "forest" => ThemePalette.ForestMist,
            "ocean" => ThemePalette.OceanLight,
            _ => mood switch
            {
                "calm" => ThemePalette.OceanLight,
                "creative" => ThemePalette.LavenderMilk,
                "hopeful" => ThemePalette.SunsetCream,
                _ => ThemePalette.WarmDream
            }
        };

        var atmosphere = pace == "steady" ? ThemeAtmosphere.Calm : ThemeAtmosphere.Energetic;
        var illustration = environment switch
        {
            "big_city" => ThemeIllustration.UrbanDreamer,
            "forest" => ThemeIllustration.NatureDreamer,
            "ocean" => ThemeIllustration.Dreamer,
            _ => ThemeIllustration.CreativeDreamer
        };

        var result = new AnalyzeDreamResult(
            FutureProfile: new FutureProfileAnalysis(
                Identity: string.IsNullOrWhiteSpace(request.DreamText) ? "Dreamer" : "Dreamer",
                Environment: environment,
                Lifestyle: lifestyle,
                Aspirations: aspirations,
                Values: values,
                Mood: mood,
                Pace: pace),
            ThemeConfig: new ThemeConfigAnalysis(
                Palette: palette,
                Atmosphere: atmosphere,
                Illustration: illustration,
                Motion: ThemeMotion.Soft,
                Imagery: imagery),
            Summary: $"This future is shaped around a {environment.Replace('_', ' ')} lifestyle, meaningful {string.Join(", ", aspirations)}, and a {mood} pace.");

        return Task.FromResult(result);
    }

    public async Task<AnalyzeDreamResult> EvolveFutureAsync(
        EvolveFutureRequest request,
        CancellationToken cancellationToken = default)
    {
        var reflection = request.ReflectionNotes.Trim();
        var normalized = reflection.ToLowerInvariant();
        var targetDescription = normalized.Contains("nature") || normalized.Contains("forest") || normalized.Contains("green")
            ? "nature forest slow calm"
            : normalized.Contains("ocean") || normalized.Contains("coast") || normalized.Contains("sea")
                ? "ocean calm"
                : normalized.Contains("city") || normalized.Contains("urban")
                    ? "city active"
                    : normalized.Contains("home") || normalized.Contains("quiet")
                        ? "home calm"
                        : reflection;

        var evolved = await AnalyzeDreamAsync(
            new AnalyzeDreamRequest(
                request.UserId,
                targetDescription,
                new Dictionary<string, string>
                {
                    ["title"] = request.DreamTitle,
                    ["reflectionDirection"] = reflection
                }),
            cancellationToken);

        return evolved with
        {
            FutureProfile = evolved.FutureProfile with
            {
                Identity = string.Equals(evolved.FutureProfile.Identity, "Dreamer", StringComparison.OrdinalIgnoreCase)
                    ? request.CurrentIdentity
                    : evolved.FutureProfile.Identity,
                Lifestyle = request.CurrentLifestyle.Concat(evolved.FutureProfile.Lifestyle).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Aspirations = request.CurrentAspirations.Concat(evolved.FutureProfile.Aspirations).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Values = request.CurrentValues.Concat(evolved.FutureProfile.Values).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            },
            Summary = $"Your future is evolving from {request.CurrentEnvironment.Replace('_', ' ')} toward {evolved.FutureProfile.Environment.Replace('_', ' ')}, with more room for the direction you described."
        };
    }

    public Task<GenerateFutureScenarioResult> GenerateFutureScenarioAsync(
        GenerateFutureScenarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var env = (request.Environment ?? "home").Replace('_', ' ');
        var identity = string.IsNullOrWhiteSpace(request.Identity) ? "Dreamer" : request.Identity.Trim();
        var mood = string.IsNullOrWhiteSpace(request.Mood) ? "hopeful" : request.Mood.Trim();
        var pace = string.IsNullOrWhiteSpace(request.Pace) ? "steady" : request.Pace.Trim();
        var aspirations = request.Aspirations is { Count: > 0 }
            ? string.Join(", ", request.Aspirations.Select(a => a.Replace('_', ' ')))
            : "meaningful growth";
        var values = request.Values is { Count: > 0 }
            ? string.Join(" and ", request.Values.Select(v => v.Replace('_', ' ')))
            : "clarity";

        var title = $"A {char.ToUpper(pace[0])}{pace[1..]} Day in Your {char.ToUpper(env[0])}{env[1..]} Life in {request.TargetYear}";
        var intro = $"You step into an ordinary morning where the noise has quieted down. The spaces around you reflect your rhythm, rooted in {values}.";
        var morning = $"The morning begins without urgency. Light filters into your {env} space as you prepare for the day. There is room to breathe before diving into what matters, with time for a warm cup and clear thoughts.";
        var daytime = $"During the day, your focus is dedicated to {aspirations}. You are working as a {identity}, making deliberate progress without feeling stretched thin. The hours carry a {pace} momentum where each task has purpose.";
        var evening = $"As dusk settles, the atmosphere turns {request.Atmosphere.ToLowerInvariant()}. You wind down in a space that feels deeply personal, closing out the day with a sense of quiet contentment and connection.";
        var emotionalTone = $"Rooted, {mood}, and unhurried";
        var closingReflection = $"This is not a fixed promise, but an open horizon. A possible shape of your life that begins with the intentions you nurture today.";

        var result = new GenerateFutureScenarioResult(
            Title: title,
            Intro: intro,
            Morning: morning,
            Daytime: daytime,
            Evening: evening,
            EmotionalTone: emotionalTone,
            ClosingReflection: closingReflection);

        return Task.FromResult(result);
    }

    public Task<GenerateGoalsResult> GenerateGoalsAsync(
        GenerateGoalsRequest request,
        CancellationToken cancellationToken = default)
    {
        var env = (request.Environment ?? "home").Replace('_', ' ');
        var identity = string.IsNullOrWhiteSpace(request.Identity) ? "Dreamer" : request.Identity.Trim();
        var primaryAspiration = request.Aspirations is { Count: > 0 }
            ? request.Aspirations[0].Replace('_', ' ')
            : "meaningful life work";
        var secondaryAspiration = request.Aspirations is { Count: > 1 }
            ? request.Aspirations[1].Replace('_', ' ')
            : "creative independence";
        var primaryValue = request.Values is { Count: > 0 }
            ? request.Values[0].Replace('_', ' ')
            : "clarity";

        var goals = new List<GoalAnalysisItem>
        {
            new(
                Title: $"Deepen craftsmanship in {primaryAspiration}",
                Description: $"Build deliberate expertise and portfolio depth aligned with becoming a {identity}.",
                Category: "Career",
                WhyItMatters: $"This anchors your daytime flow in meaningful work and provides the technical foundation for your {env} life.",
                DesiredOutcome: $"A recognized body of thoughtful work and the confidence to operate autonomously as a {identity}.",
                CurrentState: "Your current skills and background form a starting foundation that is ready for focused refinement.",
                Gap: "The distance between today and this future may involve dedicating protected blocks of time to deep practice and real-world projects.",
                Status: "NotStarted",
                Order: 1),
            new(
                Title: $"Design a {request.Pace} daily rhythm rooted in {primaryValue}",
                Description: "Create sustainable routines that protect mental clarity and prevent cognitive overload.",
                Category: "Lifestyle",
                WhyItMatters: "Your possible future is built on an unhurried, intentional pace rather than constant urgency.",
                DesiredOutcome: $"A calm daily schedule where mornings, focused work, and evenings have clear boundaries in a {env} setting.",
                CurrentState: "Your current days may carry competing demands and external friction.",
                Gap: "The next bridge could be gradually eliminating non-essential distractions and establishing quiet morning and evening boundaries.",
                Status: "NotStarted",
                Order: 2),
            new(
                Title: $"Cultivate sustainable independence for {secondaryAspiration}",
                Description: "Establish the financial and operational stability needed to pursue your aspirations freely.",
                Category: "PersonalGrowth",
                WhyItMatters: "Financial and personal resilience allows you to make choices aligned with your values without pressure.",
                DesiredOutcome: "Greater autonomy over your time, projects, and living environment.",
                CurrentState: "Your long-term independence is an evolving path that benefits from steady, intentional steps.",
                Gap: "The area to strengthen could be aligning current resources, savings, and side projects with your long-term vision.",
                Status: "NotStarted",
                Order: 3)
        };

        return Task.FromResult(new GenerateGoalsResult(goals));
    }

    public Task<GenerateDailyPathResult> GenerateDailyPathAsync(
        GenerateDailyPathRequest request,
        CancellationToken cancellationToken = default)
    {
        var identity = string.IsNullOrWhiteSpace(request.Identity) ? "Dreamer" : request.Identity.Trim();
        var pace = string.IsNullOrWhiteSpace(request.Pace) ? "steady" : request.Pace.Trim();

        var actions = new List<DailyActionItem>();
        var order = 1;

        foreach (var goal in request.Goals.Take(4))
        {
            var timeOfDay = order switch
            {
                1 => "Morning",
                2 => "Afternoon",
                3 => "Afternoon",
                _ => "Evening"
            };

            var minutes = order switch
            {
                1 => 20,
                2 => 30,
                _ => 15
            };

            actions.Add(new DailyActionItem(
                GoalTitle: goal.Title,
                Title: $"Take one small step toward: {goal.Title.ToLowerInvariant()}",
                Description: $"As a {identity}, dedicate {minutes} focused minutes to bridging the gap in {goal.Category.ToLowerInvariant()}. Start with what feels most approachable.",
                TimeOfDay: timeOfDay,
                EstimatedMinutes: minutes,
                Intention: $"Moving closer to your future self at a {pace} pace.",
                Order: order++));
        }

        if (actions.Count < 3)
        {
            actions.Add(new DailyActionItem(
                GoalTitle: "Self-reflection",
                Title: "Take five minutes to reflect on today's direction",
                Description: "Before winding down, revisit your possible future. Notice how even small steps matter.",
                TimeOfDay: "Evening",
                EstimatedMinutes: 5,
                Intention: "Staying connected to your long-term vision.",
                Order: order));
        }

        return Task.FromResult(new GenerateDailyPathResult(actions));
    }

    public Task<GenerateReflectionInsightResult> GenerateReflectionInsightAsync(
        GenerateReflectionInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        var identity = string.IsNullOrWhiteSpace(request.Identity) ? "Dreamer" : request.Identity.Trim();
        var mood = string.IsNullOrWhiteSpace(request.ReflectionMood) ? "Reflective" : request.ReflectionMood.Trim();
        var completedCount = request.CompletedActions.Count;
        var skippedCount = request.SkippedActions.Count;

        string insight;
        if (completedCount > 0 && !string.IsNullOrWhiteSpace(request.ReflectionNotes))
        {
            insight = $"Honoring today with a {mood.ToLowerInvariant()} presence and completing {completedCount} intentional steps is quietly reshaping your reality. Your reflection shows that your future as a {identity} is not just an idea, but a lived experience taking shape in the present.";
        }
        else if (completedCount > 0)
        {
            insight = $"Each completed action today bridges your current reality to the future you envisioned. Moving forward with a {mood.ToLowerInvariant()} mindset grounds your aspirations in real, gentle progress.";
        }
        else if (skippedCount > 0)
        {
            insight = $"Pausing and listening to what your body and mind need today is a vital part of sustainable growth. The path toward your future as a {identity} honors rest and patience just as much as action.";
        }
        else
        {
            insight = $"Taking a moment to pause and reflect creates clarity. Every day offers new space to align your actions with your imagined future.";
        }

        return Task.FromResult(new GenerateReflectionInsightResult(insight));
    }

    public Task<TalkToLumaResult> TalkToLumaAsync(
        TalkToLumaRequest request,
        CancellationToken cancellationToken = default)
    {
        var identity = request.Context.FutureProfile?.Identity;
        var pendingActions = request.Context.TodayActions
            .Where(action => string.IsNullOrWhiteSpace(action.Status) || action.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (pendingActions.Count < 2 && request.Context.TodayActions.Count >= 2)
        {
            var nonCompleted = request.Context.TodayActions
                .Where(action => !string.Equals(action.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (nonCompleted.Count >= 2)
            {
                pendingActions = nonCompleted;
            }
        }

        var pendingAction = pendingActions.FirstOrDefault();
        var message = request.Message;
        var hasTimeLimit = message.Contains("minute", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("min", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("half an hour", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("30 minutes", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("limited time", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("little time", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("short time", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("time", StringComparison.OrdinalIgnoreCase);

        var hasLowEnergy = message.Contains("tired", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("exhausted", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("low energy", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("fatigue", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("drained", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("overwhelmed", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("heavy", StringComparison.OrdinalIgnoreCase) ||
                           message.Contains("weary", StringComparison.OrdinalIgnoreCase);

        string reply;
        TalkToLumaActionProposal? proposal = null;
        if ((hasTimeLimit || hasLowEnergy) && pendingActions.Count >= 2)
        {
            var keepAction = pendingActions
                .Where(action => action.EstimatedMinutes <= 30)
                .OrderBy(action => action.EstimatedMinutes)
                .FirstOrDefault() ?? pendingActions.OrderBy(action => action.EstimatedMinutes).First();
            var restActions = pendingActions
                .Where(action => action.Id != keepAction.Id)
                .Select(action => action.Id)
                .ToList();

            proposal = new TalkToLumaActionProposal(
                keepAction.Id,
                restActions);
            reply = $"You have {pendingActions.Count} steps planned, and your energy deserves care. I’d keep one focused step—“{keepAction.Title}”—and let the rest wait for another day. Nothing changes unless you choose it.";
        }
        else if (message.Contains("tired", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("overwhelmed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("too much", StringComparison.OrdinalIgnoreCase))
        {
            reply = $"It makes sense to listen to your energy today. Your path toward {(string.IsNullOrWhiteSpace(identity) ? "the future you described" : $"the life you imagine as {identity}")} doesn't depend on doing everything at once. You could choose one gentle step—or let rest be enough for today. Nothing in your path has been changed.";
        }
        else if (message.Contains("30 minutes", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("half an hour", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("limited time", StringComparison.OrdinalIgnoreCase))
        {
            reply = pendingAction is null
                ? "Thirty minutes can still hold one small, meaningful step. Your current path has no pending actions, so it may be a good moment to pause or reflect instead. I haven't changed anything in your plan."
                : $"Thirty minutes is enough to make a little progress without rushing. Your next pending step is “{pendingAction.Title}” ({pendingAction.EstimatedMinutes} minutes). You could focus on just that and leave the rest for another day; your path remains unchanged.";
        }
        else if (request.Context.Goals.Count > 0)
        {
            var goal = request.Context.Goals[0];
            reply = $"Let's connect this to what you're building. One of your goals is “{goal.Title},” and its current gap is {goal.Gap}. What feels like the smallest useful part of that for you right now?";
        }
        else
        {
            reply = $"I'm here with you as you shape “{request.DreamTitle}.” What part of that possible future feels most important to talk through today?";
        }

        return Task.FromResult(new TalkToLumaResult(reply, proposal));
    }
}
