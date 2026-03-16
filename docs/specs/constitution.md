# Event Management Portal Constitution

## Core Principles

### I. Human-Gated AI Actions (NON-NEGOTIABLE)

All AI-generated content MUST pass through a human approval gate before any
external action is taken. This applies without exception to social media posts,
SMS campaigns, PDF reports, and every other AI output that leaves the system.

Rules:
- Zero auto-publishing: no AI agent may publish or send content autonomously.
- Raw AI output MUST be stored separately from admin-edited content.
- Structured JSON schema output is mandatory for all agents; schema validation
  failure MUST cause the job to fail — not silently degrade.
- Prompt templates MUST be version-controlled in source code, and the prompt
  version suffix MUST be logged with every AI invocation.
- An append-only audit log MUST record every approve, send, publish, and export
  action with actor identity, timestamp, and content reference.

Rationale: The portal acts on behalf of event organisers with real-world
consequences (published posts, SMS to attendees). Any autonomous publishing
failure could cause reputational or legal harm. The approval gate is the
primary safeguard.

### II. Security-First Architecture (NON-NEGOTIABLE)

Every architectural decision MUST preserve strict tier isolation and minimal
privilege. No shortcut that reduces the security boundary is acceptable.

Rules:
- Internet → App Service → Azure SQL is the only permitted data-access path;
  Azure SQL MUST never be exposed publicly.
- JWT access tokens MUST be held in memory only — never written to
  `localStorage` or `sessionStorage`.
- Refresh tokens MUST be delivered as `HttpOnly` cookies with rotation on
  every use.
- All secrets MUST be stored exclusively in Azure Key Vault; source code and
  `appsettings` files MUST NOT contain credentials or connection strings.
- Managed Identity MUST be used between Azure services wherever supported.
- Microsoft Entra External ID is the sole authentication provider; custom
  credential storage is prohibited.

Rationale: A breach of attendee data or AI publish credentials would directly
damage CMFI Miracle Centre's reputation and may carry regulatory consequences.
Defense-in-depth is not optional.

### III. Modular Monolith Architecture

The backend MUST be organised as a modular monolith; each module owns its
domain and exposes it only through well-defined service interfaces.

Rules:
- Modules: Auth, Events, Registrations, Campaigns, SocialPosts, Sessions,
  Reports, AuditLogs, Shared.
- Cross-module communication MUST go through service interfaces — no module
  may reach directly into another module's DbSet or repository.
- Shared infrastructure (persistence, security, jobs) lives in the Shared
  module and is consumed by all other modules.
- New modules require a documented rationale; module count MUST NOT grow
  without a clear domain boundary.

Rationale: A modular monolith delivers the deployment simplicity of a monolith
with the domain-isolation benefits of microservices, appropriate for a
single-team portfolio project deployed to Azure App Service.

### IV. Spec-Driven Development

All non-trivial features MUST begin with a specification before implementation.
Code written without a corresponding spec is out of compliance.

Rules:
- The spec workflow is: constitution → `/speckit.specify` → `/speckit.plan` →
  `/speckit.tasks` → implementation.
- Every sprint MUST produce a `spec.md` and `plan.md` before tasks begin.
- Acceptance criteria in `spec.md` MUST map 1-to-1 to test cases.
- Architecture Decision Records (ADRs) MUST document every significant
  technology or design decision.
- The constitution supersedes all other documentation; no spec or plan may
  contradict a principle stated here.

Rationale: Spec-driven development is both a portfolio demonstration objective
and a discipline that prevents scope creep and unverifiable requirements.

### V. Observability

Every operation that matters to the business MUST produce a traceable,
structured log entry.

Rules:
- Serilog structured logging is mandatory in the backend; plain-text log
  lines are not acceptable for operational events.
- Every HTTP request and background job MUST carry a correlation ID through
  the full call chain.
- Application Insights MUST receive telemetry from both the frontend (React)
  and backend (App Service).
- Background job visibility (Hangfire dashboard) MUST be secured and retained
  across environments.
- Alerts MUST be configured in Log Analytics Workspace for job failure and
  error-rate thresholds.

Rationale: AI agent jobs, Eventbrite syncs, and SMS dispatches are
asynchronous and long-running. Without structured observability, diagnosing
failures in production is impractical.

### VI. Infrastructure as Code

All Azure resources MUST be provisioned exclusively via Terraform; no resource
MUST be created or modified through the Azure portal in production.

Rules:
- Remote state MUST be stored in Azure Storage with environment-isolated state
  files (`dev` vs `prod` — never shared).
- Terraform modules MUST be reusable and composable with clear input/output
  contracts.
- Environment-specific `.tfvars` files MUST be committed to source (with
  `.example` templates); actual secret values MUST NOT be committed.
- The `dev` environment MUST be reproducible from scratch using Terraform
  without manual steps.
- IaC changes to `prod` MUST pass a `terraform plan` review before `apply`.

Rationale: Manual portal changes in production create configuration drift that
cannot be audited, version-controlled, or reliably recreated.

### VII. Test Coverage

Every user story MUST have tests that verify its acceptance criteria before the
story is marked complete.

Rules:
- Unit tests MUST cover business logic in service classes and domain models.
- Integration tests MUST cover: Eventbrite sync contracts, Mailchimp dispatch,
  inter-module service calls, and EF Core query behaviour against a test DB.
- Each acceptance scenario in `spec.md` MUST map to at least one test case.
- Tests MUST be organised to allow any single user story to be tested
  independently of others.
- The PR check pipeline (`pr-check.yml`) MUST fail if any test fails or if
  the .NET build or React lint produces errors.

Rationale: A portfolio project without tests cannot credibly demonstrate
production-grade engineering discipline. Tests are documentation of intent and
a regression safety net.

## Technology Stack & Platform Constraints

The following choices are fixed for the lifetime of this project. Changes
require a formal ADR and a constitution amendment.

| Layer | Technology |
|---|---|
| Frontend | React · JavaScript · Material UI · Recharts · Zustand |
| Backend | ASP.NET Core Web API · .NET 8 · C# · Modular Monolith |
| Database | Azure SQL Database · Entity Framework Core (code-first) |
| Background Jobs | Hangfire |
| Authentication | Microsoft Entra External ID · MSAL React · JWT |
| AI | Azure OpenAI (GPT-4.1 · GPT-4o) |
| Infrastructure | Terraform (modular, remote state on Azure Storage) |
| CI/CD | GitHub Actions (3 pipelines: pr-check, dev-deploy, prod-deploy) |
| Secrets | Azure Key Vault (Managed Identity access from App Service) |
| Observability | Application Insights · Serilog · Log Analytics Workspace |
| Containers | Docker (local dev only — App Service uses source deploy) |

External integrations (Eventbrite, Mailchimp, Meta Graph API, YouTube Data API)
are integration boundaries, not architectural layers. All integration logic
lives in the corresponding backend module.

## Development Workflow

- **Branch strategy**: feature branches off `develop`; `develop` → `main` for
  production releases.
- **PR gate**: every PR to `develop` MUST pass the `pr-check.yml` pipeline
  (.NET build + unit tests + React build + lint).
- **Dev deploy**: automatic on merge to `develop`; Terraform apply + EF Core
  migration + App Service deploy.
- **Prod deploy**: manual approval gate required; EF Core migration dry-run
  before apply; staging slot swap for zero-downtime release.
- **EF Core migrations**: auto-applied on deploy in both environments; a
  dry-run step is mandatory before prod apply.
- **Secrets in dev**: developers hold Key Vault Secrets Officer role; pipeline
  holds Managed Identity access only.
- **Docker**: used for local full-stack development via `docker compose up`;
  not used for Azure App Service deployment.

## Governance

This constitution is the highest-ranking document in the project. All specs,
plans, and implementation decisions MUST conform to it. Where a conflict
exists, this document wins.

**Amendment procedure**:
1. Propose the amendment in a GitHub issue with the label `constitution`.
2. Document the rationale, the principle being changed, and the impact on
   existing specs.
3. Update this file with the new content and bump the version accordingly.
4. Run `/speckit.constitution` to propagate changes to dependent templates.
5. Update `docs/specs/constitution.md` to keep the human-readable copy in sync.
6. Record the change in an ADR under `docs/adr/`.

**Versioning policy** (semantic):
- MAJOR: Backward-incompatible governance changes — principle removal or
  fundamental redefinition.
- MINOR: New principle or section added, or materially expanded guidance.
- PATCH: Clarifications, wording fixes, non-semantic refinements.

**Compliance review**: Every PR description MUST include a "Constitution Check"
confirming no principles are violated. The `/speckit.plan` command enforces
this as a gate before Phase 0 research.

**Version**: 1.0.0 | **Ratified**: 2026-03-16 | **Last Amended**: 2026-03-16
