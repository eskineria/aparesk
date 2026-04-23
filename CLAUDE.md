# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Frontend (`/frontend`)

```bash
npm install          # Install dependencies
npm run dev          # Start dev server → http://localhost:5173
npm run build        # TypeScript check + production Vite bundle
npm run lint         # Run ESLint
npm run preview      # Preview production build locally
```

### Backend (`/backend`)

```bash
dotnet build                           # Build all projects
dotnet run --project Eskineria.WebApi  # Start API server → http://localhost:5285

# EF Core migrations (run from /backend)
dotnet ef migrations add <Name> -p Eskineria.Persistence -s Eskineria.WebApi
dotnet ef database update -p Eskineria.Persistence -s Eskineria.WebApi
dotnet ef migrations list -p Eskineria.Persistence -s Eskineria.WebApi
```

### Infrastructure

SQL Server and Redis must be running before starting the backend. The default connection targets `localhost:1433` with credentials `sa / aA123456` and auto-applies migrations on startup.

## Architecture

**Stack:** React 18 + TypeScript + Vite (frontend) / .NET 10 ASP.NET Core (backend) / SQL Server + Redis

### Backend Layers

```
Domain → Core → Application → Persistence → WebApi
```

- **Domain** — entity definitions
- **Core** — cross-cutting concerns: JWT auth, Redis caching, email (MailKit + Scriban templates), file storage, rate limiting, audit logging, Serilog, FluentValidation, Mapster
- **Application** — business logic organized by domain feature (e.g., `Features/Products/`)
- **Persistence** — EF Core DbContext + repositories
- **WebApi** — controllers, middleware pipeline, `Program.cs` DI setup, `appsettings.json`

Each layer has a `ServiceCollectionExtensions.cs` that registers its own services. Features in Application follow a co-located pattern: DTO + Validator + Service + Abstraction in one folder.

### Frontend Structure

- `src/api/` — Axios instance with interceptors (adds `Authorization`, `Accept-Language`, `X-Workspace-Id` headers; redirects to `/auth/login` on 401)
- `src/services/` — API service classes per domain
- `src/context/` — Auth, Layout, and Branding React contexts
- `src/pages/` — route-level components
- `src/components/` — reusable components
- `src/locales/` — i18n JSON files (en-US and others); server-side keys live in `backend/Eskineria.WebApi/Localization/`

### Frontend–Backend Connection

The API base URL is `${VITE_API_BASE_URL}/api/${VITE_API_VERSION}`, defaulting to `http://localhost:5285/api/v1` in development (`.env.development`). CORS is configured in `Program.cs`.

### Authentication Flow

1. `POST /api/v1/auth/login` → JWT access token (60 min) + refresh token (7 days)
2. JWT stored locally; Axios interceptor injects `Authorization: Bearer {token}` on every request
3. Refresh token rotation on expiry; 5 failed logins trigger a 15-minute lockout
4. Email verification is required; MFA is supported; max 5 concurrent sessions

### Caching

Hybrid strategy: L1 in-memory (60s TTL) backed by L2 Redis. Cache keys are prefixed with `Eskineria:`.

### Rate Limiting

Per-endpoint policies defined in `appsettings.json` (e.g., login: 10/min, register: 5/5 min). Limits differ for authenticated vs. anonymous users.

### Audit Logging

SQL-based audit trail with PII masking, diff snapshots, and 365-day retention. Configured in `appsettings.json` under the `Audit` section.

### Storage

Default provider is local (`wwwroot/uploads`). AWS S3 and Azure Blob are alternative providers toggled via config. Allowed file extensions and a 1 GB per-file cap are enforced server-side.

### Email

MailKit over SMTP with Scriban templates located in `backend/Eskineria.WebApi/EmailTemplates/`. Daily send limit: 5,000.

### API Documentation

Scalar UI is available at `/scalar` when the backend is running (OpenAPI v1).
