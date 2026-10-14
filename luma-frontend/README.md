# LUMA Frontend

LUMA is a guided reflection experience that helps users turn discovery answers into a personal world, goals, daily actions, future scenarios, reflections, and conversations with Luma.

## User flow

Users register or log in, complete the discovery flow, review their generated world and goals, follow a daily path, and return to reflect or talk with Luma. In local Development, the demo login can obtain a seeded account from the backend.

## Technology and architecture

- Next.js 16 App Router with React 19 and TypeScript
- Tailwind CSS for the visual system
- Server-side Next.js route handlers as a same-origin proxy to the ASP.NET Core API
- HttpOnly `luma_access_token` cookie for browser sessions

The browser does not receive backend secrets. `BACKEND_URL` is read only by server-side route handlers; do not use a `NEXT_PUBLIC_` secret or put API keys in client components.

## Local setup

1. Install Node.js and npm.
2. Install dependencies with `npm install`.
3. Copy `.env.example` to `.env.local` and set `BACKEND_URL` to the local API URL (normally `http://localhost:5192`). `.env.local` is ignored by Git.
4. Start the backend, then run `npm run dev`.
5. Open `http://localhost:3000`.

## Demo account

The development login route calls the backend demo endpoint. It is available during local Development when the backend is running in Development. In a production Next.js process, the route is disabled unless `LUMA_DEMO_ENABLED=true` is deliberately configured, and the backend must independently allow its demo endpoint.

## Environment variables and security

- `BACKEND_URL`: server-only URL for the API. It is an example value in `.env.example`.
- `LUMA_DEMO_ENABLED`: optional explicit production opt-in for the frontend demo route; leave unset for normal deployments.

Gemini keys, database passwords, JWT secrets, and user data belong in the backend's secret store or deployment environment. Never commit `.env.local`, credentials, generated `.next` output, or real connection strings.

## Current status and limitations

This is an active development project. The frontend requires the matching backend API and its configured authentication, database, and AI services. The demo flow is intended for local evaluation, not as a production identity system. Production readiness still requires deployment-specific HTTPS, cookie, CORS/CSRF, rate-limit, monitoring, and secret-management review.
