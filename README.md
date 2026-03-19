# Event Management Portal

> Enterprise-grade event operations platform for registration analytics, AI-assisted marketing, SMS communications, YouTube session summarization, and PDF report generation. Built as a portfolio project demonstrating full-stack engineering, cloud infrastructure, AI agent orchestration, and spec-driven development from real-world business requirements.

This project covers the full engineering lifecycle — from specification and system design through implementation, testing, and production deployment on **Azure** — following practices used in real enterprise environments, including **GitHub Spec Kit** for structured AI-assisted development.

---

## 🏗 Architecture Overview

### 🌐 Cloud Layer
Azure App Service · Azure Static Web Apps · Azure SQL Database · Azure Blob Storage · Azure Key Vault · Application Insights · Microsoft Entra External ID

### 🎨 Frontend
React SPA · Material UI · Recharts · Axios · React Router · Zustand · Azure Static Web Apps

### ⚙️ Backend
ASP.NET Core Web API · .NET 8 · Modular Monolith · Entity Framework Core · Hangfire · Serilog · Azure App Service

### 🗄 Database
Azure SQL Database · Private access only · EF Core code-first migrations · No public exposure

### 🤖 AI Layer
Azure OpenAI (GPT-4.1 · GPT-4o) · Session Summary Agent · Marketing Agent · PDF Narrative Agent · Structured JSON schema outputs · Human approval before any external action

### 📊 Observability
Application Insights · Log Analytics Workspace · Serilog structured logging · Correlation IDs · Background job visibility · CloudWatch-style alerting

---

## 🧱 Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React · JavaScript · Material UI · Recharts · Zustand |
| Backend | ASP.NET Core (.NET 8) · C# · Modular Monolith |
| Database | Azure SQL Database · Entity Framework Core |
| Background Jobs | Hangfire |
| Authentication | Microsoft Entra External ID · MSAL React · JWT |
| AI | Azure OpenAI · GPT-4.1 · GPT-4o |
| Infrastructure | Terraform (modular) |
| CI/CD | GitHub Actions |
| Containers | Docker (local dev) |
| Secrets | Azure Key Vault |
| Observability | Application Insights · Serilog |
| Spec Tooling | GitHub Spec Kit · Specify CLI |

---

## 🔌 External Integrations

| Integration | Purpose |
|---|---|
| **Eventbrite** | Event sync · Ticket types · Attendee & registration data |
| **Mailchimp** | SMS audience segments · Campaign dispatch · Delivery tracking |
| **Meta (Facebook / Instagram)** | AI-generated post publishing · Approval workflow · Publish log |
| **YouTube** | Session URL ingestion · Transcript retrieval · Summary generation |

---

## 🔐 Security Design

- Strict tier isolation: Internet → App Service → Azure SQL (never direct DB exposure)
- No public endpoint on Azure SQL — accessed via private connection string through Key Vault
- Microsoft Entra External ID for authentication — supports Microsoft and Google federation
- JWT access tokens held in memory only — never written to `localStorage` or `sessionStorage`
- Refresh tokens delivered as `HttpOnly` cookies with rotation on every use
- All secrets stored exclusively in Azure Key Vault — never in source code or `appsettings`
- Managed Identity used between Azure services wherever supported
- Human approval gate required before any AI-generated content is published or sent
- Audit log records every send, publish, approve, and export action

---

## ⚖️ Dev vs Prod Strategy

| Concern | Dev | Prod |
|---|---|---|
| App Service SKU | B1 | P2v3 |
| SQL SKU | Basic (5 DTU) | S2+ (50 DTU) |
| Availability | Single region | Multi-AZ capable |
| Scaling | None | App Service Auto Scale |
| Key Vault access | Developers (Secrets Officer) | Pipeline only |
| Deployment slot | None | `staging` → swap for zero-downtime |
| Approval | Auto deploy on merge | Manual approval gate |
| EF Migrations | Auto on deploy | Auto on deploy with dry-run step |

---

## 🚀 CI/CD Pipeline

Three GitHub Actions pipelines cover the full delivery lifecycle:

**PR Check** — triggers on every pull request to `develop`
- .NET build + unit tests
- React build + lint
- Docker image build validation (no push)

**Dev Deploy** — triggers on merge to `develop`
- Build and push Docker images (local dev only — App Service uses source deploy)
- Terraform plan + apply to dev environment
- Deploy backend to Azure App Service (dev)
- Deploy frontend to Azure Static Web Apps (dev)
- Run EF Core migrations against dev Azure SQL

**Prod Deploy** — triggers on merge to `main`
- Requires manual approval via GitHub environment protection
- Terraform apply to prod environment
- EF Core migration dry-run before apply
- Deploy to staging slot → smoke test → swap to production

---

## 🤖 AI Agent Workflows

The portal treats AI as a first-class orchestration layer, not a side feature.

| Agent | Input | Output |
|---|---|---|
| **Session Summary Agent** | Transcript · Session title · Speaker · Event context | Executive summary · Key takeaways · Themes · Action points · Quotes (JSON schema) |
| **Marketing Agent** | Event metadata · Post type · Platform · Context notes | Caption · Hashtags · CTA · Alternative captions (JSON schema) |
| **PDF Narrative Agent** | All approved summaries · Event metadata | Cover page · Event overview narrative · Section introductions (JSON schema) |

**All agents enforce:**
- Structured JSON schema output — schema validation failure = job failure
- Raw AI output stored separately from admin-edited content
- Zero auto-publishing — human approval required before every external action
- Prompt templates version-controlled in source code with version suffix logging

---

## 🛠 Terraform Best Practices Demonstrated

- Remote state in Azure Storage with environment-isolated state files (dev vs prod)
- Reusable, composable modules with clear input/output boundaries
- Clean separation of networking, compute, database, AI, and observability concerns
- Environment-specific `.tfvars` files with `.example` templates committed to source

---

## 📂 Repository Structure

```
event-management-portal/
│
├── docs/
│   ├── specs/                     # GitHub Spec Kit specification files
│   │   ├── constitution.md        # Non-negotiable project principles
│   │   ├── 01-product-spec.md     # Business capabilities and user stories
│   │   ├── 02-domain-modules.md   # Backend module definitions and ownership
│   │   ├── 03-data-model.md       # Core entities, schema, and conventions
│   │   ├── 04-api-spec.md         # API endpoint contracts and shapes
│   │   ├── 05-ai-workflows.md     # Agent flows, pipeline steps, guardrails
│   │   ├── 06-integration-spec.md # External integration contracts
│   │   ├── 07-frontend-spec.md    # React structure, routing, component rules
│   │   └── 08-infra-devops.md     # Azure infrastructure and CI/CD
│   ├── design/                    # Architecture diagrams, ERD, user flows
│   ├── adr/                       # Architecture Decision Records
│   └── sprints/                   # Sprint Spec Kit specify + plan prompts
│       ├── sprint0-specify-prompt.md
│       └── sprint1-specify-prompt.md
│
├── frontend/                      # React application
│   ├── src/
│   │   ├── app/                   # Router, providers, Zustand store
│   │   ├── features/              # auth · dashboard · events · registrations
│   │   │                          # communications · social · content · reports
│   │   ├── components/            # layout · charts · forms · tables · feedback
│   │   └── services/              # apiClient · authService · eventService · etc.
│   ├── staticwebapp.config.json
│   └── vite.config.js
│
├── backend/                       # ASP.NET Core Web API
│   ├── src/EventPortal.Api/
│   │   ├── Modules/
│   │   │   ├── Auth/              # JWT · Entra · session management
│   │   │   ├── Events/            # Eventbrite sync · event entities
│   │   │   ├── Registrations/     # Attendee sync · snapshots · analytics
│   │   │   ├── Campaigns/         # SMS campaigns · Mailchimp integration
│   │   │   ├── SocialPosts/       # AI drafts · approval · Meta publish
│   │   │   ├── Sessions/          # YouTube ingestion · transcript · summary
│   │   │   ├── Reports/           # PDF compilation · Blob export
│   │   │   ├── AuditLogs/         # Append-only audit trail
│   │   │   └── Shared/            # Infrastructure · persistence · security · jobs
│   │   ├── Program.cs
│   │   └── appsettings.*.json
│   └── tests/
│       └── EventPortal.Tests/
│
├── infrastructure/
│   ├── modules/                   # Reusable Terraform modules
│   │   ├── app_service/
│   │   ├── static_web_app/
│   │   ├── sql_database/
│   │   ├── key_vault/
│   │   ├── storage/
│   │   └── monitoring/
│   └── environments/
│       ├── dev/                   # Dev environment config
│       └── prod/                  # Prod environment config
│
├── .github/
│   └── workflows/
│       ├── pr-check.yml           # Build + test on every PR
│       ├── dev-deploy.yml         # Deploy to dev on merge to develop
│       └── prod-deploy.yml        # Deploy to prod with manual approval
│
├── docker-compose.yml             # Local full-stack dev environment
└── README.md
```

---

## 🎯 What This Demonstrates

| Skill | Detail |
|---|---|
| **Spec-Driven Development** | GitHub Spec Kit workflow — `constitution.md`, `/specify`, `/plan`, `/tasks` per sprint |
| **AI Agent Orchestration** | Three production-grade agents with JSON schema outputs, approval guardrails, and observability |
| **Requirements Engineering** | Business capabilities → user stories → acceptance criteria → API contracts |
| **System Design** | Modular monolith architecture, ERD, ADRs documenting key decisions |
| **Full-Stack Development** | React SPA + ASP.NET Core REST API with Hangfire background jobs |
| **Cloud Engineering** | Azure App Service · Static Web Apps · Azure SQL · Blob Storage · Key Vault |
| **Infrastructure as Code** | Modular Terraform with remote state and environment separation |
| **Security Engineering** | Entra External ID · JWT in-memory + HttpOnly refresh · Key Vault · audit trail |
| **External Integrations** | Eventbrite · Mailchimp SMS · Meta Graph API · YouTube Data API |
| **CI/CD** | GitHub Actions with PR validation, dev auto-deploy, and prod manual approval gate |
| **Observability** | Structured Serilog logs · App Insights telemetry · correlation IDs · job monitoring |
| **Testing** | Unit tests, integration tests, and test cases mapped to acceptance criteria |

---

## 🎬 Spec and Walkthrough Index

| # | Topic | Format |
|---|---|---|
| 01 | Requirements and Business Capabilities | `01-product-spec.md` |
| 02 | Domain Module Design | `02-domain-modules.md` |
| 03 | Data Model and ERD | `03-data-model.md` |
| 04 | API Contract Design | `04-api-spec.md` |
| 05 | AI Agent Workflows | `05-ai-workflows.md` |
| 06 | Integration Contracts | `06-integration-spec.md` |
| 07 | Frontend Architecture | `07-frontend-spec.md` |
| 08 | Infrastructure and DevOps | `08-infra-devops.md` |
| 09 | Sprint 0 — Foundation | `sprints/sprint0-specify-prompt.md` |
| 10 | Sprint 1 — Authentication | `sprints/sprint1-specify-prompt.md` |

---

## 🚀 Getting Started (Local)

**Prerequisites:** Docker Desktop · Node.js 20+ · .NET 8 SDK · Azure CLI (optional)

```bash
# Clone the repository
git clone git@github.com:YOUR_USERNAME/event-management-portal.git
cd event-management-portal

# Copy environment config templates
cp frontend/.env.example frontend/.env.local
cp backend/src/EventPortal.Api/appsettings.Development.json.example \
   backend/src/EventPortal.Api/appsettings.Development.json

# Start all services locally
docker compose up --build
```

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:5001/api/v1 |
| Swagger UI | http://localhost:5001/swagger |
| Hangfire Dashboard | http://localhost:5001/hangfire |
| Database | localhost:1433 |

---

## 📖 Specification Documents

| Document | Purpose |
|---|---|
| [`constitution.md`](docs/specs/constitution.md) | Non-negotiable architectural principles and conventions |
| [`01-product-spec.md`](docs/specs/01-product-spec.md) | Business capabilities, user stories, success criteria |
| [`02-domain-modules.md`](docs/specs/02-domain-modules.md) | Backend module ownership and folder contracts |
| [`03-data-model.md`](docs/specs/03-data-model.md) | Entity definitions, schema conventions, index strategy |
| [`04-api-spec.md`](docs/specs/04-api-spec.md) | Full endpoint contracts, request/response shapes |
| [`05-ai-workflows.md`](docs/specs/05-ai-workflows.md) | Agent definitions, pipeline steps, guardrails |
| [`06-integration-spec.md`](docs/specs/06-integration-spec.md) | External API contracts and error handling matrices |
| [`07-frontend-spec.md`](docs/specs/07-frontend-spec.md) | React structure, routing, component and service rules |
| [`08-infra-devops.md`](docs/specs/08-infra-devops.md) | Azure services, Terraform layout, CI/CD pipelines |

---

## 📌 Project Status

| Phase | Status |
|---|---|
| Spec-Driven Design (Spec Kit) | ✅ Complete |
| Constitution and Architecture | ✅ Complete |
| Sprint 0 — Foundation Scaffold | ✅ Complete |
| Sprint 1 — Authentication | ✅ Complete |
| Sprint 2 — Eventbrite + Registration Dashboard | ✅ Complete |
| Sprint 3 — SMS Communication Module | ⬜ Not Started |
| Sprint 4 — AI Social Media and Publishing | ⬜ Not Started |
| Sprint 5 — YouTube Ingestion and Summaries | ⬜ Not Started |
| Sprint 6 — PDF Reports and Production Hardening | ⬜ Not Started |
| Terraform Infrastructure | ⬜ Not Started |
| CI/CD Pipelines | ⬜ Not Started |
| Testing | ⬜ Not Started |

---

*Built following the same modular design philosophy as the Geotech Lab project — clean architecture, spec-driven delivery, and production-aligned cloud deployment from day one.*
