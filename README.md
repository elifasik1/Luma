# LUMA — Your Future, Taking Shape

**See the life you want. Build it, one day at a time.**

LUMA is an AI-powered life companion that helps you turn your vision of the future into meaningful goals, personalized daily actions, and measurable progress.

Instead of leaving your aspirations as distant dreams, LUMA connects the future you imagine with the steps you can take today.

## ✨ The Vision

Your future is more than a destination. It is a life you shape through everyday decisions.

LUMA brings together future visualization, goal planning, daily action, and reflection in one guided experience.

- **Discover your future** — Explore the life you want to create.
- **Set meaningful goals** — Turn your vision into concrete objectives.
- **Take action today** — Follow a daily path aligned with your goals.
- **Adapt as you grow** — Reconsider your plan when your priorities change.
- **Reflect on your journey** — Review your progress and learn from your experiences.
- **Talk to LUMA** — Use a conversational interface to explore your plans and next steps.

## 🧭 How It Works

`Future → Goals → Today → Progress → Reflection → Future`

LUMA connects long-term aspirations with everyday actions, creating a continuous cycle of planning, doing, and reflecting.

## 🛠️ Built With

| Layer | Technologies |
|---|---|
| Frontend | Next.js, React, TypeScript, Tailwind CSS |
| Backend | ASP.NET Core, C# |
| Architecture | Domain-driven separation, layered application architecture |
| Data & persistence | Entity Framework Core, PostgreSQL |
| AI integration | Gemini API, structured outputs, deterministic fallback services |

## 🏗️ Project Structure

```text
Luma/
├── LUMA/
│   ├── Luma.Api/             # HTTP API and authentication
│   ├── Luma.Application/     # Application services and contracts
│   ├── Luma.Domain/          # Domain entities
│   ├── Luma.Infrastructure/  # Persistence and AI integrations
│   └── TestFlow/              # Executable flow-validation harness
├── luma-frontend/
│   └── src/                   # Next.js pages, components and API routes
├── .gitignore
└── README.md
```

## 🚀 Getting Started

### Prerequisites

- .NET SDK compatible with the backend target framework
- Node.js and npm
- PostgreSQL for persistent local data, or the configured in-memory development option

### 1. Configure the backend

```powershell
cd LUMA
```

Configure the required settings using .NET User Secrets. Set a random `Jwt:Secret` of at least 32 bytes. For a local demo without Gemini API calls, configure:

```text
AI:Provider = Development
```

Gemini integration requires a server-side API key. Never expose it in frontend code or commit it to Git.

See [`LUMA/README.md`](./LUMA/README.md) for backend configuration details.

### 2. Start the API

```powershell
dotnet run --project Luma.Api
```

### 3. Configure and start the frontend

Open a second terminal:

```powershell
cd luma-frontend
Copy-Item .env.example .env.local
npm install
npm run dev
```

Ensure `.env.local` points `BACKEND_URL` to the running API. The example configuration uses `http://localhost:5192`.

Open [http://localhost:3000](http://localhost:3000) in your browser.

For the local demo, the backend must run in Development and its demo data must be available. Demo login is not intended for production use.

## 🔐 Security

- Keep API keys, database credentials, JWT secrets, and personal data out of source control.
- Use .NET User Secrets locally and an appropriate secret manager in deployment.
- Keep demo authentication disabled in production unless a deliberately secured demo environment is required.
- Review HTTPS, authentication, rate limiting, logging, and deployment configuration before production use.

## 🧪 Testing & Validation

The repository includes an executable `TestFlow` harness for exercising application flows. It is not currently a conventional .NET test project, so a successful `dotnet test` command alone should not be interpreted as proof that automated tests ran.

## 📌 Project Status

LUMA is under active development. Features and integrations may evolve as the product grows. The local demo and development configuration are intended for evaluation and development, not as a production deployment template.

## 🌱 What's Next

LUMA's long-term direction is to become a more adaptive companion that connects personal aspirations with realistic plans and supports users as their priorities and circumstances evolve.

---

**LUMA — Your future doesn't happen all at once. It takes shape, one day at a time.**