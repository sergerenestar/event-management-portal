# event-management-portal Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-16

## Active Technologies

- C# .NET 8 (backend) · JavaScript ES2022 (frontend) (001-foundation-scaffold)

## Project Structure

```text
backend/src/EventPortal.Api/Modules/   # 9 domain modules (Auth, Events, Registrations,
                                       #   Campaigns, SocialPosts, Sessions, Reports,
                                       #   AuditLogs, Shared)
backend/tests/EventPortal.Tests/       # xUnit test project
frontend/src/                          # React + Vite (features/, components/, services/)
infra/modules/                         # Terraform reusable modules (6 Azure services)
infra/env/dev|prod/                    # Terraform environment roots
.github/workflows/                     # pr-check.yml, dev-deploy.yml, prod-deploy.yml
specs/001-foundation-scaffold/         # Sprint 0 plan, research, data-model, contracts
.specify/memory/constitution.md        # Project constitution (authoritative)
docs/specs/                            # Human-readable spec copies
```

## Commands

```bash
# Backend
cd backend && dotnet restore src/EventPortal.Api
cd backend && dotnet build src/EventPortal.Api
cd backend && dotnet test tests/EventPortal.Tests
cd backend && dotnet ef database update --project src/EventPortal.Api

# Frontend
cd frontend && npm ci && npm run dev
cd frontend && npm run build && npm run lint

# Full stack (local)
docker compose up --build

# Terraform validate
cd infra/env/dev && terraform init && terraform validate
```

## Code Style

- Backend: C# .NET 8, PascalCase for classes/methods, camelCase for locals
- Frontend: JavaScript ES2022, JSX, camelCase, functional components only
- No business logic in controllers (backend) or feature components calling fetch/axios directly
- All secrets via IConfiguration + Key Vault — never hardcoded

## Recent Changes

- 001-foundation-scaffold: Full scaffold plan, research, data-model, contracts, quickstart authored

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
