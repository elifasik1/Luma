# LUMA Backend

LUMA is a guided reflection application. It turns a user's discovery answers into a personal dream, goals, daily actions, future scenarios, reflections, and conversations with Luma.

## User flow

1. The user completes the discovery flow in the Next.js frontend.
2. The frontend sends authenticated requests to this ASP.NET Core API.
3. The API stores users, dreams, goals, plans, and reflections through EF Core.
4. An AI provider generates analysis and responses. The deterministic development provider can run without an external AI account.

## Technology and architecture

- ASP.NET Core minimal APIs on .NET 10
- EF Core with an in-memory database by default and PostgreSQL when configured
- JWT authentication with password hashing
- Gemini REST integration behind application service interfaces
- Swagger/OpenAPI in Development only

The solution is split into `Luma.Api`, `Luma.Application`, `Luma.Domain`, and `Luma.Infrastructure`. Secrets are read from User Secrets or environment variables and are not required in tracked files.

## Local setup

1. Install the .NET SDK required by `LUMA.slnx`.
2. From this directory, initialize and set local secrets for the API project:

   ```bash
   dotnet user-secrets init --project Luma.Api
   dotnet user-secrets set "Jwt:Secret" "use-a-random-secret-at-least-32-bytes" --project Luma.Api
   dotnet user-secrets set "Jwt:Issuer" "LUMA" --project Luma.Api
   dotnet user-secrets set "Jwt:Audience" "LUMA" --project Luma.Api
   ```

3. For an offline run, set `AI:Provider` to `Development`. For Gemini, set `Gemini:ApiKey` in User Secrets or the process environment.
4. Run the API with `dotnet run --project Luma.Api`. The Development profile uses `http://localhost:5192`.

The tracked `.env.example` contains placeholders only. Do not copy real credentials into tracked JSON, `.env` files, or source code.

## Configuration

- `Jwt__Secret`: required, at least 32 bytes; keep it in User Secrets or a deployment secret store.
- `Jwt__Issuer` and `Jwt__Audience`: values used to validate tokens.
- `ConnectionStrings__DefaultConnection`: optional PostgreSQL connection string. Empty means in-memory storage.
- `AI__Provider`: `Development` or `Gemini`.
- `Gemini__ApiKey`: server-side Gemini key only.
- `Demo__Enabled`: leave unset or `false` in production. The demo token endpoint is available only in Development or when deliberately enabled.

## Demo account

When the API runs in Development, `GET /auth/dev-token` seeds a deterministic demo user and returns a short-lived token. The frontend's development login route uses this endpoint. Do not expose it in a public production deployment unless the demo is intentionally enabled and isolated.

## Current status and limitations

This repository is an active development project. The default in-memory database is suitable for local demos only; data is lost when the process stops. Production deployments need a managed database, secret storage, HTTPS, operational logging, rate limiting, and a reviewed AI provider configuration. External Gemini usage requires a valid account, API key, quotas, and rate-limit review.

Build and test status depends on the local SDK, database, and secret configuration. Do not treat a local demo as production validation.
