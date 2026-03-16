# Event Management Portal — Constitution

> This document establishes the non-negotiable principles, architectural constraints,
> and engineering conventions that govern every specification, plan, and implementation
> decision in this project. All AI agents and contributors must comply with this
> constitution before writing any code or generating any artifact.

---

## 1. Project Identity

- **Name:** Event Management Portal
- **Type:** Internal admin operations platform
- **Users:** One role only — `Admin`. There is no public-facing interface, no
  multi-tenant model, and no guest or viewer role. If a future requirement
  introduces additional roles, the constitution must be updated first.
- **Purpose:** Centralized platform for event registration analytics,
  SMS communications, AI-assisted social media marketing, YouTube session
  summarization, and PDF report generation.

---

## 2. Architecture Principles

### 2.1 Modular Monolith First

- The backend is a **modular monolith**, not microservices.
- Each domain module owns its own controllers, services, DTOs, entities,
  and integration clients.
- Modules must not directly reference each other's internal types. Shared
  contracts go in `Modules/Shared/`.
- Do not introduce microservice boundaries until scale explicitly demands it.

### 2.2 Module Structure

Every backend module follows this internal layout:

```
Modules/<ModuleName>/
├── Controllers/
├── Services/
├── Dtos/
├── Entities/
├── Integrations/   (only if external API calls exist)
├── Agents/         (only if AI orchestration exists)
└── Jobs/           (only if background workers exist)
```

### 2.3 Clean Separation of Concerns

- Controllers handle HTTP routing and input validation only.
- Services contain business logic.
- Entities are EF Core models — never returned directly from controllers.
- DTOs are the public contract between API and frontend.
- Integration clients are isolated behind interfaces. Business logic must
  never call external APIs directly.

### 2.4 Async by Default for Long-Running Work

- All external API calls (Eventbrite sync, Mailchimp send, YouTube ingestion,
  Meta publish, PDF compilation) must be handled asynchronously.
- Use **Hangfire** for background job scheduling and retry management.
- Background jobs must be observable — job status must be queryable from
  the admin UI.
- No request thread should block on an AI generation or external publish call.

---

## 3. Technology Stack (Non-Negotiable)

### Backend
- **Runtime:** ASP.NET Core Web API, .NET 8
- **ORM:** Entity Framework Core
- **Validation:** FluentValidation
- **Logging:** Serilog with structured output
- **Background Jobs:** Hangfire
- **API Documentation:** OpenAPI / Swagger

### Frontend
- **Framework:** React with JavaScript (not TypeScript unless explicitly decided)
- **Routing:** React Router
- **State:** Context API or Zustand
- **UI Components:** Material UI or Ant Design (pick one per project, do not mix)
- **Charts:** Recharts or Chart.js
- **HTTP:** Axios

### Database
- **Primary:** Azure SQL Database
- **Access:** Entity Framework Core only — no raw ADO.NET unless justified

### Cloud (Azure Only)
- **API Hosting:** Azure App Service
- **Frontend Hosting:** Azure Static Web Apps
- **Database:** Azure SQL Database
- **File Storage:** Azure Blob Storage (PDF exports, generated media metadata)
- **Secrets:** Azure Key Vault — no secrets in source code, appsettings, or
  environment variables outside of Key Vault references
- **Observability:** Application Insights
- **AI:** Azure OpenAI only — no direct OpenAI API calls

### AI Models (Azure OpenAI)
| Task | Model |
|------|-------|
| Session summary generation | GPT-4.1 |
| Quote and structured JSON extraction | GPT-4.1 |
| Marketing caption and post generation | GPT-4.1 or GPT-4o |
| PDF narrative compilation | GPT-4.1 |
| Future multimodal / image-aware features | GPT-4o |

- Always require **JSON schema outputs** for agent steps.
- Log all prompts and AI responses via Application Insights.
- A smaller model tier may be introduced later for lightweight tasks
  (hashtag cleanup, title rewrites) — add to this table when decided.

---

## 4. Authentication and Security

- **Identity Provider:** Microsoft Entra External ID
- **Supported Login Methods:** Microsoft account, Google (federated)
- **App Token:** Short-lived JWT issued after successful Entra authentication
- **Refresh:** Refresh token rotation enabled
- **Secrets:** All API keys and connection strings in Azure Key Vault only
- **Transport:** HTTPS only — no HTTP in any environment
- **CSRF:** Protection required wherever cookie auth is used
- **Rate Limiting:** Applied to all AI-triggering and external-publish endpoints
- **Audit Log:** Required for all send, publish, approve, and report generation actions

---

## 5. External Integration Rules

### General
- All integration clients implement an interface (e.g., `IEventbriteClient`).
- Integration clients handle HTTP, error mapping, and retry logic only.
- Business logic never calls integration clients directly — always via service layer.
- Credentials for all integrations are stored in Azure Key Vault.

### Eventbrite
- Data is synced via background jobs, not live on every request.
- Dashboard queries hit stored snapshots (`DailyRegistrationSnapshots`),
  not live aggregations from Eventbrite.

### Mailchimp SMS
- Campaign drafts are always stored locally before any send command is issued.
- Recipient segment eligibility must be validated before send.
- Admin must explicitly confirm before a send is dispatched to Mailchimp.
- Provider response IDs and send statuses must be persisted after every send.

### Meta (Facebook / Instagram)
- Publishing only to connected professional/business accounts.
- No post may be published without passing through the approval workflow:
  `Draft → Reviewed → Approved → Published`.
- Background jobs handle the actual publish call, not request threads.
- Publish failures must be logged and surfaced in the admin UI.

### YouTube
- Ingestion is triggered by an admin-provided YouTube URL.
- Transcript retrieval and summary generation are background jobs.
- Session summaries must be stored and approved before being included
  in any PDF or social snippet export.

---

## 6. AI Agent Guardrails

- AI agents must never publish directly to external platforms.
- All AI-generated content requires human approval before any external action.
- Raw AI output is stored separately from approved/final output.
- Prompt templates are version-controlled as part of the codebase.
- AI failures must be caught, logged, and surfaced — never silently dropped.

---

## 7. Data Model Conventions

- All entity primary keys are `int Id` (identity, auto-increment).
- All entities include `CreatedAt` (datetime, UTC).
- Mutable entities include `UpdatedAt` (datetime, UTC).
- External IDs from third-party systems are stored as separate string fields
  (e.g., `ExternalEventbriteId`) — never used as primary keys.
- JSON data stored in the database uses `*Json` column name suffix
  (e.g., `KeyTakeawaysJson`, `MetadataJson`).

---

## 8. Frontend Conventions

- Features are organized under `src/features/<domain>/`.
- Each feature owns its own components, hooks, and service calls.
- Shared UI components live in `src/components/`.
- API communication is centralized in `src/services/`.
- No feature may call `fetch` or `axios` directly — always via a service module.
- Auth state is managed globally via Context or Zustand store.
- All route changes require authentication guard validation.

---

## 9. DevOps and Environment Strategy

- Environments: `dev` and `prod` (minimum). `test` added when CI pipeline matures.
- Infrastructure defined as code in `infra/` using Terraform or Bicep.
- CI/CD via GitHub Actions.
- No manual deployments to `prod` — all changes go through pipeline.
- Environment-specific secrets injected via Azure Key Vault references only.

```
infra/
├── env/
│   ├── dev/
│   └── prod/
└── modules/
    ├── app_service/
    ├── static_web_app/
    ├── sql_database/
    ├── key_vault/
    ├── storage/
    └── monitoring/
```

---

## 10. Observability Requirements

- Structured logs via Serilog, forwarded to Application Insights.
- Every request carries a correlation ID, propagated through background jobs.
- Metrics tracked: sync counts, publish counts, AI failures, SMS send failures.
- Dashboard queries must include performance telemetry.
- Failed background jobs must be visible in the admin UI — not just in logs.

---

## 11. What This Project Is Not

- Not a multi-tenant SaaS product.
- Not a public-facing website or attendee portal.
- Not a microservices system (yet).
- Not a real-time event system — all external data is synced and cached locally.
- Not a general-purpose CMS or email marketing tool.

---

*This constitution was generated from the Event Management Portal README
and architecture definition. Update this document before changing any
non-negotiable principle. All agents must re-read this document at the
start of every new spec or plan session.*
