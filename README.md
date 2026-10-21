# LUMA — Your Future, Taking Shape

**See the life you want. Build it, one day at a time.**

LUMA is an AI-powered life companion that helps people turn a vision of their future into meaningful goals, personalized daily actions, and reflection. It connects long-term aspirations with practical steps users can take today, then gives them a place to review progress and adapt their plans as life changes.

Rather than treating a future vision as a distant dream, LUMA links the future a person wants to create with the choices and actions that can move them toward it.

> **Hackathon context:** LUMA is designed to demonstrate an Alexa+-inspired, web-based simulated agentic experience. It is an independent project, not an official Amazon or Alexa product. This README does not claim that the application is a production Alexa+ Skill or a compliant MCP server.

---

## Table of Contents

- [The Problem](#the-problem)
- [The Vision](#the-vision)
- [How LUMA Works](#how-luma-works)
- [Core Features](#core-features)
- [Screenshots and Product Walkthrough](#screenshots-and-product-walkthrough)
- [Alexa+-Inspired Experience](#alexa-inspired-experience)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Repository Structure](#repository-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database and Migrations](#database-and-migrations)
- [Testing and Validation](#testing-and-validation)
- [Security Notes](#security-notes)
- [Current Status and Limitations](#current-status-and-limitations)
- [Hackathon Demo](#hackathon-demo)
- [License](#license)

## The Problem

People often know the kind of life they want but struggle to translate that idea into achievable goals and consistent daily action. Plans can become disconnected from day-to-day decisions, and progress can be difficult to review meaningfully.

LUMA is built around one continuous loop: imagine a future, define goals, take action, reflect on what happened, and adapt the plan.

## The Vision

Your future is more than a destination. It is a life you shape through everyday decisions.

LUMA brings future visualization, goal planning, daily action, progress tracking, and reflection into one guided experience. The product is designed to help users connect the person they want to become with what they can do now.

## How LUMA Works

```text
Future → Goals → Today → Progress → Reflection → Future
```

1. **Discover a future vision.** The user describes the future they want and builds a personalized future profile.
2. **Choose a target date.** The user selects a target date using day, month, and year. The date is saved as a date-only value to avoid time-zone day shifts.
3. **Shape the future world.** LUMA uses the profile and controlled visual theme options to represent the user's future vision.
4. **Set meaningful goals.** The vision is connected to objectives that can guide action.
5. **Follow a daily path.** The user reviews daily actions, tracks progress, and can schedule actions for a future date.
6. **Reflect on the journey.** Calendar-based reflections let the user record and revisit experiences for specific dates.
7. **Adapt through conversation.** The Talk to LUMA experience provides a conversational way to explore the plan and work with supported application capabilities.

## Core Features

### Future discovery and personalization

A guided onboarding flow helps the user articulate the life they want to build. LUMA stores a future profile and uses it to personalize the future-world experience.

### Full target-date selection

Users can select a complete target date in `DD/MM/YYYY` format rather than only choosing a year. Past dates and invalid calendar dates should be rejected. The selected date is persisted and can be updated later. `TargetYear` remains synchronized with the year component of `TargetDate` for backward compatibility.

### Personalized future world

The future-world view presents the user's future vision with a controlled visual theme. Theme choices are stored so that the user's explicit selection can persist across reloads and sessions.

### Goal planning

LUMA connects a future vision to goals that make the larger ambition easier to act on. Goals are part of the broader planning experience rather than isolated checklist items.

### Daily Path and scheduled actions

The Daily Path presents actions intended to help the user make progress toward their goals. Actions support progress tracking, and an action can be scheduled for a selected date. A scheduled date is stored separately from timestamps so that calendar dates do not shift because of time-zone conversion.

### Journey calendar and reflections

The Journey calendar provides a date-based view of the user's path. Users can create, update, and revisit reflections for specific dates, including dates that do not have a daily-plan snapshot. Existing reflection dates are preserved when the target date changes.

### Talk to LUMA

Talk to LUMA provides a conversational interface for exploring the user's future, goals, daily actions, progress, and reflections. The agent-oriented design can coordinate supported application capabilities instead of being limited to a one-turn question-and-answer interaction.

### Development AI provider and optional Gemini integration

The backend supports a deterministic `Development` provider for local demos and testing without making paid model API calls. A Gemini provider is also available through server-side configuration and structured responses. The provider is selected by configuration; API credentials must never be placed in frontend code or committed to the repository.

## Screenshots and Product Walkthrough

The screenshots below document the current LUMA web experience across onboarding, future visualization, goals, daily actions, reflections, and the Alexa+-inspired web simulator. The simulator is a web demo; these screenshots do not imply an official or production Alexa+ integration.

### Product landing and Alexa+-inspired demo

<table><tbody>
<tr>
<td width="50%" valign="top">
<strong>LUMA landing page</strong><br />
<img src="docs/screenshots/landing-hero.png" alt="Product introduction and entry points into the LUMA experience." width="100%" />
</td>
<td width="50%" valign="top">
<strong>How LUMA builds your future</strong><br />
<img src="docs/screenshots/product-architecture.png" alt="The three-part product story: imagine, plan, and adapt." width="100%" />
</td>
</tr>
<tr>
<td width="50%" valign="top">
<strong>Alexa+-inspired simulator introduction</strong><br />
<img src="docs/screenshots/alexa-simulator-intro.png" alt="Overview of the web-based voice and agent simulator experience." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Landing page closing section</strong><br />
<img src="docs/screenshots/landing-closing-cta.png" alt="Closing call to action for starting a Life Canvas or demo session." width="100%" />
</td>
</tr>
<tr>
<td width="50%" valign="top">
<strong>Alexa+ companion simulator chat</strong><br />
<img src="docs/screenshots/alexa-companion-chat.png" alt="The web-based Alexa+-inspired companion responds to a request about today’s focus." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Adaptive plan recommendation</strong><br />
<img src="docs/screenshots/alexa-adaptive-plan-approval.png" alt="The simulator proposes a plan adjustment and waits for explicit approval." width="100%" />
</td>
</tr>
</tbody></table>

### Account access and onboarding

<table><tbody>
<tr>
<td width="50%" valign="top">
<strong>Create account</strong><br />
<img src="docs/screenshots/signup.png" alt="Registration form and optional theme aesthetic selection." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Log in</strong><br />
<img src="docs/screenshots/login.png" alt="Login screen for returning users." width="100%" />
</td>
</tr>
<tr>
<td width="50%" valign="top">
<strong>Onboarding: environment</strong><br />
<img src="docs/screenshots/onboarding-environment.png" alt="The user selects the kind of life environment they imagine." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Onboarding: daily rhythm</strong><br />
<img src="docs/screenshots/onboarding-daily-rhythm.png" alt="The user chooses how they want their days to feel." width="100%" />
</td>
</tr>
<tr>
<td width="50%" valign="top">
<strong>Onboarding: core values</strong><br />
<img src="docs/screenshots/onboarding-core-values.png" alt="The user selects values that will shape future priorities." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Onboarding: personal voice</strong><br />
<img src="docs/screenshots/onboarding-personal-voice.png" alt="The user describes their aspirations in their own words." width="100%" />
</td>
</tr>
</tbody></table>

### Your World and themes

<table><tbody>
<tr>
<td width="50%" valign="top">
<strong>Your World: future vision</strong><br />
<img src="docs/screenshots/your-world-future-vision.png" alt="Personalized future-world scene and future-profile summary." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Your World: target date and progress</strong><br />
<img src="docs/screenshots/your-world-target-date.png" alt="Target-date control alongside progress and goal summaries." width="100%" />
</td>
</tr>
<tr>
<td width="50%" valign="top">
<strong>Your World: goals and progress</strong><br />
<img src="docs/screenshots/your-world-goals-and-progress.png" alt="Daily progress and important goals on the Your World page." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Theme selector</strong><br />
<img src="docs/screenshots/theme-selector.png" alt="Theme options for changing the visual atmosphere across LUMA." width="100%" />
</td>
</tr>
</tbody></table>

### Goals & Gaps

<table><tbody>
<tr>
<td width="50%" valign="top">
<strong>Goals & Gaps: career path</strong><br />
<img src="docs/screenshots/goals-career-path.png" alt="A future path with context, current starting point, bridge, and desired outcome." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Goals & Gaps: lifestyle path</strong><br />
<img src="docs/screenshots/goals-lifestyle-path.png" alt="A second goal path and the daily action connected to it." width="100%" />
</td>
</tr>
<tr>
<td width="50%" valign="top">
<strong>Goals & Gaps: next step</strong><br />
<img src="docs/screenshots/goals-next-step.png" alt="The page’s next-step call to action." width="100%" />
</td>
<td width="50%"></td>
</tr>
</tbody></table>

### Daily Path and reflections

<table><tbody>
<tr>
<td width="50%" valign="top">
<strong>Daily Path overview</strong><br />
<img src="docs/screenshots/daily-path-overview.png" alt="Daily progress summary and the planned actions grouped by time of day." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Daily Path: actions</strong><br />
<img src="docs/screenshots/daily-path-actions.png" alt="Action cards with status, duration, and schedule controls." width="100%" />
</td>
</tr>
<tr>
<td width="50%" valign="top">
<strong>Daily Path: action states</strong><br />
<img src="docs/screenshots/daily-path-statuses.png" alt="Examples of pending, completed, and rested action states." width="100%" />
</td>
<td width="50%" valign="top">
<strong>Daily reflection</strong><br />
<img src="docs/screenshots/daily-reflection.png" alt="Reflection prompts for noticing how the day shaped the user’s future." width="100%" />
</td>
</tr>
</tbody></table>

### My Journey

<table><tbody>
<tr>
<td width="50%" valign="top">
<strong>My Journey calendar</strong><br />
<img src="docs/screenshots/journey-calendar.png" alt="Date-based calendar view with snapshot and selected-day indicators." width="100%" />
</td>
<td width="50%" valign="top">
<strong>My Journey: selected day</strong><br />
<img src="docs/screenshots/journey-selected-day.png" alt="Selected-day reflection editor and actions recorded for the date." width="100%" />
</td>
</tr>
</tbody></table>

## Alexa+-Inspired Experience

LUMA's hackathon presentation is focused on a web-based simulated agentic experience inspired by the Alexa+ track. The experience is intended to show how a conversational companion can use user context and application capabilities to help connect a long-term vision with everyday planning.

The simulation must be shown functioning in the submitted demo. Unless a separate, working Agent Skill or self-hosted MCP server is present and verified in this repository, do not describe LUMA as a live Alexa+ integration or claim that it implements the MCP Streamable HTTP transport. The web-based simulated-experience path and a production Alexa+ integration are different deliverables.

## Technology Stack

| Layer | Technologies | Responsibility |
|---|---|---|
| Frontend | Next.js, React, TypeScript, Tailwind CSS | Guided onboarding, future-world UI, goals, Daily Path, Journey, and Talk to LUMA |
| API | ASP.NET Core, C# | HTTP endpoints, authentication, and application request handling |
| Application | C# application services and interfaces | Use cases, request/response models, and service contracts |
| Domain | C# domain entities | Core concepts such as dreams, future profiles, goals, daily actions, and reflections |
| Persistence | Entity Framework Core, Npgsql, PostgreSQL | Relational persistence and schema migrations |
| AI integration | Provider abstraction, deterministic Development provider, optional Gemini REST integration | Configurable assistant behavior and structured AI output |
| Validation | TestFlow executable harness, .NET builds, frontend lint/type/build checks | Flow, access-boundary, persistence, and regression validation |

## Architecture

LUMA separates the HTTP API, application services, domain model, and infrastructure concerns into distinct projects.

- **Luma.Api** exposes HTTP endpoints and configures dependency injection, authentication, AI provider selection, and persistence.
- **Luma.Application** contains application-level services, contracts, request models, and response models.
- **Luma.Domain** contains the core domain entities and concepts.
- **Luma.Infrastructure** contains Entity Framework Core persistence, repository implementations, migrations, and external AI provider implementation.
- **luma-frontend** contains the Next.js user experience and server-side API proxy routes.
- **TestFlow** is an executable flow-validation harness. It is not a conventional xUnit test project; use the documented `dotnet run` command to execute its checks.

### Data and persistence

The PostgreSQL schema is managed through Entity Framework Core migrations. Important date-only values, including target dates, reflection plan dates, and daily-action scheduled dates, use PostgreSQL `date` columns where appropriate.

The API can use an in-memory development database if no `ConnectionStrings:DefaultConnection` is configured. In-memory mode is useful for quick local evaluation, but data is not durable across application restarts. Use a dedicated local PostgreSQL database to validate persistent behavior.

## Repository Structure

```text
Luma/
├── LUMA/
│   ├── Luma.Api/              # ASP.NET Core API and authentication
│   ├── Luma.Application/      # Application services, contracts, DTOs
│   ├── Luma.Domain/           # Domain entities
│   ├── Luma.Infrastructure/   # EF Core, PostgreSQL, repositories, AI providers
│   ├── TestFlow/              # Executable flow-validation harness
│   └── LUMA.slnx              # .NET solution
├── luma-frontend/
│   ├── src/app/               # Next.js App Router and API proxy routes
│   ├── src/components/        # UI components and product views
│   ├── src/lib/               # API clients, types, and helpers
│   └── public/                # Static assets
├── .gitignore
└── README.md
```

## Getting Started

These instructions target a local Windows/PowerShell development setup. Commands may need small adjustments for macOS or Linux. Use a dedicated local database for development; do not point local migrations at a production or shared database.

### Prerequisites

- Git
- .NET SDK compatible with the API project (the project targets .NET 10)
- Node.js 22.x and npm
- PostgreSQL for durable local persistence, or the configured Development in-memory option
- Docker Desktop if you prefer to run PostgreSQL in a local container

### 1. Clone the public repository

Replace the placeholder with the repository's actual public GitHub URL:

```powershell
git clone <YOUR_PUBLIC_REPOSITORY_URL>
cd Luma
```

### 2. Configure backend secrets

The API uses .NET User Secrets for local-only settings. From the repository root, set a unique random JWT secret of at least 32 bytes and select the deterministic Development AI provider for a no-paid-API local demo:

```powershell
dotnet user-secrets set "Jwt:Secret" "<YOUR_LOCAL_RANDOM_SECRET_AT_LEAST_32_BYTES>" --project .\LUMA\Luma.Api\Luma.Api.csproj
dotnet user-secrets set "AI:Provider" "Development" --project .\LUMA\Luma.Api\Luma.Api.csproj
```

Replace the placeholder before running the command. Do not commit secrets or share them in issues, screenshots, logs, or recordings. The repository already has a User Secrets ID configured; if you use a different checkout or project, confirm that a User Secrets ID exists before setting values.

### 3. Choose a database mode

#### Option A — Quick local evaluation with in-memory storage

If `ConnectionStrings:DefaultConnection` is absent, the API's Development configuration can fall back to an in-memory database. This is useful for a quick UI walkthrough, but data does not persist across API restarts. Skip the PostgreSQL setup and migration steps if you intentionally choose this mode.

#### Option B — Local PostgreSQL for persistent behavior

Create a dedicated local PostgreSQL database and user. One possible approach is a local Docker container. Replace the sample password with a unique local development password; do not reuse a production or shared database password.

```powershell
docker run --name luma-postgres-dev `
  -e POSTGRES_DB=luma_dev `
  -e POSTGRES_USER=luma_dev `
  -e POSTGRES_PASSWORD=<YOUR_LOCAL_DB_PASSWORD> `
  -p 127.0.0.1:5432:5432 `
  -v luma_pgdata:/var/lib/postgresql/data `
  -d postgres:16-alpine
```

Configure the connection string using User Secrets. Replace the password placeholder with the password used by your local PostgreSQL container:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=luma_dev;Username=luma_dev;Password=<YOUR_LOCAL_DB_PASSWORD>" --project .\LUMA\Luma.Api\Luma.Api.csproj
```

Before applying migrations, make sure this connection points only to your dedicated local `luma_dev` database. Never use a production or shared database for local migration commands.

If you do not have the EF Core CLI installed, install a version matching the project's EF Core major version. Then apply migrations to your local database:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef database update `
  --project .\LUMA\Luma.Infrastructure\Luma.Infrastructure.csproj `
  --startup-project .\LUMA\Luma.Api\Luma.Api.csproj `
  --context LumaDbContext
```

If `dotnet-ef` is already installed, do not install it again. Always confirm the active connection target before running schema changes.

### 4. Start the API

Open a terminal at the repository root:

```powershell
dotnet run --project .\LUMA\Luma.Api\Luma.Api.csproj
```

The local development launch profile is expected to expose the API at `http://localhost:5192`. Confirm the current `launchSettings.json` if the port differs. If available, the health endpoint can be checked at:

```text
http://localhost:5192/health
```

Keep the API terminal running.

### 5. Configure and start the frontend

Open a second terminal:

```powershell
cd .\luma-frontend
```

Install dependencies:

```powershell
npm install
```

Create `.env.local` from the example if it does not exist, then make sure it points to the local API:

```powershell
if (-not (Test-Path .env.local)) {
  Copy-Item .env.example .env.local
}
```

In `luma-frontend/.env.local`, set:

```dotenv
BACKEND_URL=http://localhost:5192
```

Do not overwrite an existing `.env.local` file blindly; preserve any local-only settings it already contains. Start the frontend:

```powershell
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### 6. Demo login

The development demo-login flow is intended for local development and evaluation only. It must not be enabled or exposed in production. If demo login fails, check the API terminal first: a backend exception may be the real cause even when the frontend displays a generic login error.

## Configuration

### AI provider

For a free local walkthrough without paid model API calls, use:

```text
AI:Provider = Development
```

To use the optional Gemini provider, configure the provider and API key in the backend's User Secrets. Keep the key server-side and do not place it in `luma-frontend/.env.local` or any `NEXT_PUBLIC_*` variable:

```powershell
dotnet user-secrets set "AI:Provider" "Gemini" --project .\LUMA\Luma.Api\Luma.Api.csproj
dotnet user-secrets set "Gemini:ApiKey" "<YOUR_GEMINI_API_KEY>" --project .\LUMA\Luma.Api\Luma.Api.csproj
```

The Gemini integration supports configurable model settings and structured JSON output. Model access, provider quotas, and costs depend on your Google AI configuration. The Development provider should be used for a deterministic local demo when no model API call is intended.

### Frontend API proxy

The frontend uses server-side proxy routes to forward requests to `BACKEND_URL`. Keep this variable server-side; it should not be exposed to the browser as a public variable unless the application specifically requires that behavior.

## Database and Migrations

Migrations are stored in `LUMA/Luma.Infrastructure`. The schema includes entities for future profiles, scenarios, goals, daily actions, plan snapshots, user theme preferences, and date-based reflections.

When setting up a fresh local PostgreSQL database, apply the migrations as described in [Getting Started](#getting-started). If an existing database contains important data, inspect the target database and migration history before applying changes. Never use destructive reset commands against a shared or production database.

Current date-related fields include:

- `FutureProfiles.TargetDate` — PostgreSQL `date`, used for the user's target future date.
- `FutureProfiles.TargetYear` — maintained for compatibility and synchronized with `TargetDate.Year`.
- `Reflections.PlanDate` — PostgreSQL `date`, associates a reflection with a calendar date.
- `DailyActions.ScheduledDate` — PostgreSQL `date`, stores the planned date of a daily action.

These values are date-only rather than timestamps so they can represent calendar dates without time-zone conversion shifting the day.

## Testing and Validation

From the repository root, build the .NET solution:

```powershell
dotnet build .\LUMA\LUMA.slnx
dotnet build .\LUMA\TestFlow\TestFlow.csproj
```

Run the executable flow-validation harness:

```powershell
dotnet run --project .\LUMA\TestFlow\TestFlow.csproj
```

For agent-focused checks, the harness also supports:

```powershell
dotnet run --project .\LUMA\TestFlow\TestFlow.csproj -- --agent-only
```

For frontend checks:

```powershell
cd .\luma-frontend
npx tsc --noEmit
npm run lint
npm run build
```

The TestFlow harness has been used to exercise application flows and boundaries, including goal/daily-action flows, owner isolation, agent boundaries, daily-plan snapshots, and theme persistence. Check the output of the specific command you run; a successful build alone is not the same as a passing runtime test.

## Security Notes

- Never commit database connection strings, passwords, JWT signing secrets, Gemini API keys, real user data, database dumps, or local logs containing personal data.
- Use .NET User Secrets for local backend configuration and an appropriate secret manager for deployed environments.
- Keep `.env.local` and other local environment files out of Git. Verify `.gitignore` before pushing.
- Use a dedicated local database for development and testing. Confirm the connection target before applying migrations.
- Demo authentication and the deterministic Development provider are intended for evaluation, not production deployment.
- Before production use, review authentication, authorization, HTTPS, rate limiting, error handling, logging, privacy, backups, and secret management.
- Do not enter sensitive personal information into a shared demo instance.

## Current Status and Limitations

LUMA is under active development. The local development and demo configuration is intended for evaluation, not as a production deployment template. UI behavior, integrations, and supported workflows can evolve.

The project supports a configurable Development AI provider and an optional Gemini integration. The presence of an AI provider abstraction does not mean that every local run calls an external model; the selected configuration determines which provider is used.

The hackathon's Alexa+ simulated-experience path is distinct from shipping a production Alexa+ Skill or MCP server. If the submission relies on the simulation path, the demo video should clearly show the working web experience and the repository should include the source code needed to evaluate it.

## Hackathon Demo

For the Amazon Developer Hackathon submission, prepare a concise demonstration that shows the product working. The hackathon rules require a public YouTube or Vimeo demo video in English that is **under three minutes**. Lead with the strongest product experience and show real interactions rather than relying only on narration or static slides.

A possible demo sequence:

1. Introduce LUMA and the problem it addresses.
2. Complete the future-discovery flow and select a target date.
3. Show the personalized future world and a persisted theme choice.
4. Show a goal and a daily action, including date scheduling where appropriate.
5. Open Journey, navigate to the target date, and create or revisit a reflection.
6. Demonstrate Talk to LUMA as part of the simulated agentic experience, if this flow is functioning in the recorded build.

Use only footage, images, audio, and music that you own or are authorized to use. All submission materials must be in English, or have English translations as required by the hackathon rules.

## License

This repository is intended to be public. For the hackathon's public-repository requirement, the repository must also include an open-source license file at its root; public visibility alone does not grant an open-source license. **Choose and add a license (for example, MIT, if that matches your intentions) before submission.** Do not claim a license until the corresponding `LICENSE` file has been added.

---

**LUMA — Your future doesn't happen all at once. It takes shape, one day at a time.**
