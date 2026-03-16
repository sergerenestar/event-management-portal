# Implementation Plan: Sprint 0 — Foundation Scaffold

**Branch**: `001-foundation-scaffold` | **Date**: 2026-03-16 | **Spec**: N/A (scaffold sprint — no feature spec)
**Input**: Sprint 0 constraints: backend skeleton, frontend skeleton, EF Core + AdminUsers migration, Terraform module stubs, GitHub Actions pipelines

## Summary

Stand up the full project skeleton: ASP.NET Core .NET 8 modular monolith (9 modules scaffolded
empty), React + Vite + MUI frontend (feature-based structure), EF Core code-first with a single
`AdminUsers` initial migration, Terraform module stubs for 6 Azure services, and three GitHub
Actions pipelines (pr-check, dev-deploy, prod-deploy). No business logic is delivered. The sprint
ends when the project compiles, the migration runs, and the CI pipeline goes green.

## Technical Context

**Language/Version**: C# .NET 8 (backend) · JavaScript ES2022 (frontend)
**Primary Dependencies**:
- Backend: ASP.NET Core Web API · EF Core 8 · Hangfire · Serilog · FluentValidation · xUnit
- Frontend: React 18 · Vite 5 · Material UI v5 · Zustand · React Router v6 · Axios · Recharts
**Storage**: Azure SQL Database — EF Core code-first migrations
**Testing**: xUnit + Moq (backend project scaffold) · Vitest (frontend — no tests in Sprint 0)
**Target Platform**: Azure App Service .NET 8 on Linux (backend) · Azure Static Web Apps (frontend)
**Project Type**: Web service (REST API) + SPA
**Performance Goals**: N/A — scaffold sprint only
**Constraints**: No business features. All modules scaffolded empty. Only `AdminUsers` table in
the initial EF migration.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|---|---|---|
| I. Human-Gated AI Actions | No AI agents in Sprint 0 | ✅ N/A — Pass |
| II. Security-First Architecture | JWT config scaffolded; Key Vault bootstrap in Shared; HTTPS enforced in Terraform | ✅ Pass |
| III. Modular Monolith | All 9 modules folder-scaffolded per spec 02 folder layouts | ✅ Pass |
| IV. Spec-Driven Development | Plan written before implementation — compliant | ✅ Pass |
| V. Observability | Serilog + App Insights + CorrelationId middleware registered in Program.cs | ✅ Pass |
| VI. Infrastructure as Code | 6 Terraform module stubs + dev/prod env roots created | ✅ Pass |
| VII. Test Coverage | xUnit project scaffolded; no test authoring required in Sprint 0 | ✅ Pass |

**No violations. Phase 0 research approved.**

## Project Structure

### Documentation (this feature)

```text
specs/001-foundation-scaffold/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output — AdminUsers entity only
├── quickstart.md        # Phase 1 output — local dev startup guide
└── contracts/
    └── health.md        # Health check endpoint contract
```

### Source Code (repository root)

```text
backend/
├── src/EventPortal.Api/
│   ├── Modules/
│   │   ├── Auth/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   ├── Events/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Integrations/
│   │   │   ├── Jobs/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   ├── Registrations/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Queries/
│   │   │   ├── Jobs/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   ├── Campaigns/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Integrations/
│   │   │   ├── Jobs/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   ├── SocialPosts/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Agents/
│   │   │   ├── Integrations/
│   │   │   ├── Jobs/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   ├── Sessions/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Agents/
│   │   │   ├── Integrations/
│   │   │   ├── Jobs/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   ├── Reports/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Agents/
│   │   │   ├── Pdf/
│   │   │   ├── Jobs/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   ├── AuditLogs/
│   │   │   ├── Controllers/
│   │   │   ├── Services/
│   │   │   ├── Dtos/
│   │   │   └── Entities/
│   │   └── Shared/
│   │       ├── Infrastructure/
│   │       ├── Persistence/
│   │       ├── Security/
│   │       ├── BackgroundJobs/
│   │       └── Observability/
│   ├── Program.cs
│   ├── EventPortal.Api.csproj
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.Development.json.example
└── tests/EventPortal.Tests/
    ├── EventPortal.Tests.csproj
    └── PlaceholderTest.cs

frontend/
├── src/
│   ├── app/
│   │   ├── router/AppRouter.jsx
│   │   ├── providers/AppProviders.jsx
│   │   └── store/useAppStore.js
│   ├── features/
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── events/
│   │   ├── registrations/
│   │   ├── communications/
│   │   ├── social/
│   │   ├── content/
│   │   └── reports/
│   ├── components/
│   │   ├── layout/
│   │   ├── charts/
│   │   ├── forms/
│   │   ├── tables/
│   │   └── feedback/
│   ├── services/apiClient.js
│   └── utils/
├── index.html
├── package.json
├── vite.config.js
├── .env.example
└── staticwebapp.config.json

infra/
├── env/
│   ├── dev/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── terraform.tfvars.example
│   └── prod/
│       ├── main.tf
│       ├── variables.tf
│       └── terraform.tfvars.example
└── modules/
    ├── app_service/         # main.tf · variables.tf · outputs.tf
    ├── static_web_app/      # main.tf · variables.tf · outputs.tf
    ├── sql_database/        # main.tf · variables.tf · outputs.tf
    ├── key_vault/           # main.tf · variables.tf · outputs.tf
    ├── storage/             # main.tf · variables.tf · outputs.tf
    └── monitoring/          # main.tf · variables.tf · outputs.tf

.github/workflows/
├── pr-check.yml
├── dev-deploy.yml
└── prod-deploy.yml

docker-compose.yml
.gitignore
```

**Structure Decision**: Web application — `backend/` + `frontend/` at repo root, `infra/` for
Terraform, `.github/workflows/` for CI/CD. Matches specs 02, 07, and 08 exactly.

## Complexity Tracking

No constitution violations requiring justification.
