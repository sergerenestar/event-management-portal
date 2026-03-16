# Research: Sprint 0 — Foundation Scaffold

All technical choices for Sprint 0 are fully specified in the approved spec documents.
No NEEDS CLARIFICATION items exist. This document records the decisions and rationale
for the key scaffold choices.

---

## Decision 1: ASP.NET Core Project Layout

**Decision**: Single `EventPortal.Api` Web API project inside `backend/src/`, with a
companion test project at `backend/tests/EventPortal.Tests/`.

**Rationale**: Modular monolith — one deployable unit, multiple internal module folders.
No need for multiple csproj files (Campaigns.csproj, Events.csproj, etc.) in Sprint 0.
Modules are separated by folder, not by project boundary. This is the simplest structure
that satisfies Principle III (Modular Monolith).

**Alternatives considered**:
- Multi-project solution with one csproj per module: rejected — premature complexity for Sprint 0.
  Module-per-project is a migration path if the team scales, not a starting point.
- Minimal API instead of Controllers: rejected — spec 04 shows controller-based routing;
  FluentValidation integration is cleaner with controllers.

---

## Decision 2: EF Core Migration Scope

**Decision**: Initial migration (`InitialCreate`) creates only the `AdminUsers` table.
No other entities are included.

**Rationale**: User constraint. All other entities will be added in their respective
feature sprints. This keeps the database schema change minimal and verifiable in Sprint 0.

**Migration command**:
```bash
dotnet ef migrations add InitialCreate --project src/EventPortal.Api --output-dir Modules/Auth/Migrations
```

**Alternatives considered**:
- Create all tables upfront: rejected — violates Sprint 0 scope. Entities must be implemented
  alongside their modules to keep module ownership clear.

---

## Decision 3: Vite React Setup

**Decision**: `npm create vite@latest frontend -- --template react` as the scaffold baseline.
Material UI v5 installed separately. No TypeScript in frontend (spec 07 specifies JavaScript).

**Rationale**: Spec 07 explicitly uses `.jsx` and `.js` extensions. Vite 5 + React 18 is
the specified combination. MUI v5 is the single UI component library constraint.

**Key packages**:
```
@mui/material @emotion/react @emotion/styled
@mui/icons-material
react-router-dom@6
zustand
axios
recharts
```

**Alternatives considered**:
- TypeScript: rejected — spec is explicit about `.js`/`.jsx`.
- Create React App: rejected — deprecated; Vite is the specified tool.

---

## Decision 4: Terraform Module Structure

**Decision**: `infra/modules/<service>/` with stub `main.tf`, `variables.tf`, `outputs.tf`
for each of the 6 Azure services. `infra/env/dev/` and `infra/env/prod/` as environment
entry points referencing the modules.

**Rationale**: Spec 08 defines this structure exactly. Stubs allow the Terraform directory
structure to be committed and CI-validated without requiring real Azure credentials in Sprint 0.

**6 modules**: `app_service`, `static_web_app`, `sql_database`, `key_vault`, `storage`, `monitoring`

**Alternatives considered**:
- Single flat `infra/main.tf`: rejected — contradicts IaC principle requiring reusable,
  composable modules with clear input/output contracts.

---

## Decision 5: GitHub Actions Pipeline Split

**Decision**: Three separate workflow files per the README and spec 08:
- `pr-check.yml` — triggers on PR to `main`/`develop`; runs .NET build + tests + React build + lint
- `dev-deploy.yml` — triggers on push to `develop`; runs Terraform plan + deploy
- `prod-deploy.yml` — triggers on push to `main`; requires manual approval environment gate

**Rationale**: Separation of concerns. PR validation is fast and must not deploy.
Dev deploy is automatic. Prod deploy requires explicit human gate — aligns with Principle VI (IaC).

**Alternatives considered**:
- Single workflow with conditional jobs: rejected — makes pipeline harder to read and audit.

---

## Decision 6: Docker Compose for Local Dev

**Decision**: `docker-compose.yml` at repo root defines three services:
- `api` — ASP.NET Core backend (Dockerfile in `backend/`)
- `frontend` — React dev server (Dockerfile in `frontend/`)
- `db` — SQL Server 2022 (mcr.microsoft.com/mssql/server:2022-latest)

**Rationale**: Spec 08 and README specify Docker for local dev only. App Service does not
use Docker — it uses source deploy. A `docker-compose.yml` is a Sprint 0 deliverable.

---

## Decision 7: Serilog + App Insights Bootstrap

**Decision**: Wire Serilog in `Program.cs` with:
- Console sink (dev)
- Application Insights sink (placeholder — `APPLICATIONINSIGHTS_CONNECTION_STRING` from config)
- Correlation ID middleware registered before other middleware

**Rationale**: Principle V requires structured Serilog and App Insights from day one, even
in the scaffold sprint. The connection string will be empty in local dev — Serilog handles
this gracefully (telemetry simply won't be sent).

---

## Summary: No NEEDS CLARIFICATION Items

All stack choices, folder structures, naming conventions, pipeline triggers, and migration
scope are fully defined by the approved spec documents and user constraints. Phase 1 design
can proceed immediately.
