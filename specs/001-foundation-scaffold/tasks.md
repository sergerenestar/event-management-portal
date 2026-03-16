# Tasks: Sprint 0 — Foundation Scaffold

**Input**: `specs/001-foundation-scaffold/plan.md`, `data-model.md`, `contracts/health.md`, `research.md`, `quickstart.md`
**Prerequisites**: plan.md ✅ · data-model.md ✅ · contracts/ ✅ · research.md ✅ · quickstart.md ✅
**Note**: No spec.md — Sprint 0 is a scaffold sprint. User stories map to the 5 scaffold deliverables.

## User Story Map (Sprint 0 Scaffold Deliverables)

| Story | Deliverable | Done When |
|---|---|---|
| US1 | Backend — ASP.NET Core modular monolith scaffold | `dotnet build` passes, 9 module folders present |
| US2 | Frontend — React + Vite + MUI feature scaffold | `npm run build` passes, folder structure complete |
| US3 | Database — EF Core + AdminUsers migration + Health check | `GET /health` returns 200 with DB probe |
| US4 | Infrastructure — Terraform module stubs | `terraform validate` passes for dev and prod |
| US5 | CI/CD — GitHub Actions pipelines + Docker Compose | `docker compose up --build` starts all services |

---

## Phase 1: Setup

**Purpose**: Create all top-level directories, solution files, and project scaffolds from scratch.

- [ ] T001 Create `.gitignore` at repo root covering .NET, Node, Terraform, and IDE artefacts
- [ ] T002 Create `backend/EventPortal.sln` .NET solution file with `dotnet new sln`
- [ ] T003 [P] Create `backend/src/EventPortal.Api/EventPortal.Api.csproj` using `dotnet new webapi --no-openapi false` (or equivalent), target `net8.0`
- [ ] T004 [P] Create `backend/tests/EventPortal.Tests/EventPortal.Tests.csproj` using `dotnet new xunit`, reference `EventPortal.Api` project
- [ ] T005 Add both projects to `backend/EventPortal.sln` with `dotnet sln add`
- [ ] T006 Scaffold `frontend/` using `npm create vite@latest . -- --template react` inside `frontend/` directory
- [ ] T007 [P] Create `infra/`, `infra/modules/`, `infra/env/dev/`, `infra/env/prod/` directory structure
- [ ] T008 [P] Create `.github/workflows/` directory structure

**Checkpoint**: `dotnet build backend/src/EventPortal.Api` and `npm run build` inside `frontend/` both succeed (pre-wiring baseline).

---

## Phase 2: Foundational — Shared Module & Program.cs Wiring

**Purpose**: Wire the backbone that all 9 modules depend on. MUST complete before module scaffolding.

⚠️ **CRITICAL**: No user story work begins until this phase is complete.

- [ ] T009 Create `backend/src/EventPortal.Api/Modules/Shared/Persistence/BaseEntity.cs` — abstract class with `int Id`, `DateTime CreatedAt`, `DateTime UpdatedAt`
- [ ] T010 Create `backend/src/EventPortal.Api/Modules/Shared/Persistence/AppDbContext.cs` — EF Core `DbContext`, constructor accepts `DbContextOptions<AppDbContext>`, empty `OnModelCreating` override
- [ ] T011 [P] Create `backend/src/EventPortal.Api/Modules/Shared/Observability/SerilogConfiguration.cs` — static `Configure(WebApplicationBuilder builder)` method; adds Console sink (always) and ApplicationInsights sink (when connection string present)
- [ ] T012 [P] Create `backend/src/EventPortal.Api/Modules/Shared/Observability/CorrelationIdMiddleware.cs` — reads `X-Correlation-Id` header (or generates a GUID), stores in `HttpContext.Items["CorrelationId"]`, adds to response headers and Serilog `LogContext`
- [ ] T013 [P] Create `backend/src/EventPortal.Api/Modules/Shared/Observability/GlobalExceptionMiddleware.cs` — catches unhandled exceptions, logs via Serilog with correlation ID, returns RFC 7807 `application/problem+json` 500 response
- [ ] T014 [P] Create `backend/src/EventPortal.Api/Modules/Shared/Security/JwtConfiguration.cs` — static `Configure(WebApplicationBuilder builder)` method; reads `Jwt:SigningKey`, `Jwt:Issuer`, `Jwt:Audience` from `IConfiguration`; registers `AddAuthentication().AddJwtBearer()`
- [ ] T015 [P] Create `backend/src/EventPortal.Api/Modules/Shared/Security/AuthorizationPolicies.cs` — defines `AdminOnly` policy requiring `authenticated` claim; registered via `builder.Services.AddAuthorization()`
- [ ] T016 [P] Create `backend/src/EventPortal.Api/Modules/Shared/BackgroundJobs/HangfireConfiguration.cs` — static `Configure(WebApplicationBuilder builder)` registering Hangfire with SQL Server storage; dashboard secured behind admin auth
- [ ] T017 [P] Create `backend/src/EventPortal.Api/Modules/Shared/BackgroundJobs/JobRegistry.cs` — empty static class with `RegisterJobs(IApplicationBuilder app)` stub; all future Hangfire recurring jobs declared here
- [ ] T018 [P] Create `backend/src/EventPortal.Api/Modules/Shared/Infrastructure/IBlobStorageClient.cs` — interface with `UploadAsync`, `GetPresignedDownloadUrlAsync`, `DeleteAsync` method signatures (no implementation in Sprint 0)
- [ ] T019 [P] Create `backend/src/EventPortal.Api/Modules/Shared/Infrastructure/KeyVaultConfiguration.cs` — static `Configure(WebApplicationBuilder builder)` method; reads `AZURE_KEY_VAULT_ENDPOINT` from env; adds `AddAzureKeyVault` to config pipeline when endpoint present
- [ ] T020 Install NuGet packages into `EventPortal.Api.csproj`:
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `Microsoft.EntityFrameworkCore.Design`
  - `Hangfire.AspNetCore`
  - `Hangfire.SqlServer`
  - `Serilog.AspNetCore`
  - `Serilog.Sinks.ApplicationInsights`
  - `Microsoft.ApplicationInsights.AspNetCore`
  - `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
  - `FluentValidation.AspNetCore`
  - `Azure.Extensions.AspNetCore.Configuration.Secrets`
  - `Azure.Identity`
- [ ] T021 Write `backend/src/EventPortal.Api/Program.cs` wiring all Shared services:
  - `KeyVaultConfiguration.Configure(builder)`
  - `SerilogConfiguration.Configure(builder)`
  - `JwtConfiguration.Configure(builder)`
  - `AuthorizationPolicies.Configure(builder)`
  - `HangfireConfiguration.Configure(builder)`
  - `builder.Services.AddDbContext<AppDbContext>(...)`
  - `builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database")`
  - `builder.Services.AddControllers()`
  - `builder.Services.AddEndpointsApiExplorer()`
  - `builder.Services.AddSwaggerGen()`
  - Middleware pipeline: `UseCorrelationId` → `UseGlobalExceptionHandler` → `UseHttpsRedirection` → `UseAuthentication` → `UseAuthorization` → `MapControllers` → `MapHealthChecks("/health")` → `UseHangfireDashboard`
- [ ] T022 Create `backend/src/EventPortal.Api/appsettings.json` — skeleton with `Serilog`, `Jwt`, `ApplicationInsights`, `ConnectionStrings:DefaultConnection` (empty), `AZURE_KEY_VAULT_ENDPOINT` (empty)
- [ ] T023 [P] Create `backend/src/EventPortal.Api/appsettings.Development.json.example` — populated with local SQL Server connection string, dev JWT signing key (placeholder), empty App Insights connection string
- [ ] T024 [P] Create `backend/tests/EventPortal.Tests/PlaceholderTest.cs` — single `[Fact]` test that asserts `true == true`; keeps test project green in Sprint 0

**Checkpoint**: `dotnet build backend/` passes. `dotnet test backend/` passes (1 test).

---

## Phase 3: US1 — Backend Modular Monolith Scaffold

**Goal**: All 9 module folders created with correct subfolder structure and at minimum one `.cs` file per
module to ensure .NET includes them in the build. No business logic — stubs only.

**Independent Test**: `dotnet build backend/` compiles with 0 errors. All 9 `Modules/<Name>/` directories exist.

- [ ] T025 [P] [US1] Scaffold `Modules/Auth/` folder structure:
  - `Controllers/AuthController.cs` — empty controller class, `[Route("api/v1/auth")]`, no action methods
  - `Services/IAuthService.cs` — empty interface
  - `Services/AuthService.cs` — class implementing `IAuthService`
  - `Dtos/` — empty directory (`.gitkeep`)
  - `Entities/` — empty directory (`.gitkeep`; `AdminUser.cs` added in Phase 5)
- [ ] T026 [P] [US1] Scaffold `Modules/Events/` folder structure:
  - `Controllers/EventsController.cs` — empty controller, `[Route("api/v1/events")]`
  - `Services/IEventService.cs` — empty interface
  - `Services/EventService.cs` — class implementing `IEventService`
  - `Integrations/IEventbriteClient.cs` — empty interface
  - `Integrations/EventbriteClient.cs` — empty implementation
  - `Jobs/EventSyncJob.cs` — empty class
  - `Dtos/` — `.gitkeep`
  - `Entities/` — `.gitkeep`
- [ ] T027 [P] [US1] Scaffold `Modules/Registrations/` folder structure:
  - `Controllers/RegistrationsController.cs` — empty controller, `[Route("api/v1/events")]`
  - `Services/IRegistrationService.cs` — empty interface
  - `Services/RegistrationService.cs` — empty implementation
  - `Queries/` — `.gitkeep`
  - `Jobs/RegistrationSyncJob.cs` — empty class
  - `Jobs/SnapshotAggregatorJob.cs` — empty class
  - `Dtos/` — `.gitkeep`
  - `Entities/` — `.gitkeep`
- [ ] T028 [P] [US1] Scaffold `Modules/Campaigns/` folder structure:
  - `Controllers/CampaignsController.cs` — empty controller, `[Route("api/v1/campaigns")]`
  - `Services/ICampaignService.cs` — empty interface
  - `Services/CampaignService.cs` — empty implementation
  - `Integrations/IMailchimpClient.cs` — empty interface
  - `Integrations/MailchimpClient.cs` — empty implementation
  - `Jobs/SmsSendJob.cs` — empty class
  - `Dtos/` — `.gitkeep`
  - `Entities/` — `.gitkeep`
- [ ] T029 [P] [US1] Scaffold `Modules/SocialPosts/` folder structure:
  - `Controllers/SocialPostsController.cs` — empty controller, `[Route("api/v1/social-posts")]`
  - `Services/ISocialPostService.cs` — empty interface
  - `Services/SocialPostService.cs` — empty implementation
  - `Agents/IMarketingAgent.cs` — empty interface
  - `Agents/MarketingAgent.cs` — empty implementation
  - `Integrations/IMetaClient.cs` — empty interface
  - `Integrations/MetaClient.cs` — empty implementation
  - `Jobs/SocialPublishJob.cs` — empty class
  - `Dtos/` — `.gitkeep`
  - `Entities/` — `.gitkeep`
- [ ] T030 [P] [US1] Scaffold `Modules/Sessions/` folder structure:
  - `Controllers/SessionsController.cs` — empty controller, `[Route("api/v1/sessions")]`
  - `Services/ISessionService.cs` — empty interface
  - `Services/SessionService.cs` — empty implementation
  - `Agents/ISessionSummaryAgent.cs` — empty interface
  - `Agents/SessionSummaryAgent.cs` — empty implementation
  - `Integrations/IYouTubeClient.cs` — empty interface
  - `Integrations/YouTubeClient.cs` — empty implementation
  - `Jobs/TranscriptIngestionJob.cs` — empty class
  - `Jobs/SummaryGenerationJob.cs` — empty class
  - `Dtos/` — `.gitkeep`
  - `Entities/` — `.gitkeep`
- [ ] T031 [P] [US1] Scaffold `Modules/Reports/` folder structure:
  - `Controllers/ReportsController.cs` — empty controller, `[Route("api/v1/reports")]`
  - `Services/IReportService.cs` — empty interface
  - `Services/ReportService.cs` — empty implementation
  - `Agents/IPdfNarrativeAgent.cs` — empty interface
  - `Agents/PdfNarrativeAgent.cs` — empty implementation
  - `Pdf/IPdfBuilder.cs` — empty interface
  - `Pdf/PdfBuilder.cs` — empty implementation
  - `Jobs/PdfCompilationJob.cs` — empty class
  - `Dtos/` — `.gitkeep`
  - `Entities/` — `.gitkeep`
- [ ] T032 [P] [US1] Scaffold `Modules/AuditLogs/` folder structure:
  - `Controllers/AuditLogsController.cs` — empty controller, `[Route("api/v1/audit-logs")]`
  - `Services/IAuditLogger.cs` — empty interface
  - `Services/AuditLogger.cs` — empty implementation
  - `Dtos/` — `.gitkeep`
  - `Entities/` — `.gitkeep`
- [ ] T033 [US1] Register all module services in `Program.cs` (add after Shared registrations):
  - `builder.Services.AddScoped<IAuthService, AuthService>()`
  - `builder.Services.AddScoped<IEventService, EventService>()`
  - `builder.Services.AddScoped<IRegistrationService, RegistrationService>()`
  - `builder.Services.AddScoped<ICampaignService, CampaignService>()`
  - `builder.Services.AddScoped<ISocialPostService, SocialPostService>()`
  - `builder.Services.AddScoped<ISessionService, SessionService>()`
  - `builder.Services.AddScoped<IReportService, ReportService>()`
  - `builder.Services.AddScoped<IAuditLogger, AuditLogger>()`

**Checkpoint**: `dotnet build backend/` — 0 errors, 0 warnings about missing registrations. All 9 module controller classes present.

---

## Phase 4: US2 — Frontend React Feature Scaffold

**Goal**: Complete `frontend/src/` folder structure matching spec 07 exactly. Dev server starts and renders a blank shell with routing wired.

**Independent Test**: `npm run build` inside `frontend/` exits 0. `npm run dev` serves `http://localhost:5173` with a rendered page (even if blank).

- [ ] T034 [P] [US2] Install frontend dependencies inside `frontend/`:
  ```bash
  npm install @mui/material @emotion/react @emotion/styled @mui/icons-material
  npm install react-router-dom@6 zustand axios recharts
  npm install -D vitest
  ```
- [ ] T035 [US2] Create `frontend/src/app/store/useAppStore.js` — Zustand store with shape: `accessToken: null`, `admin: null`, `setSession(token, admin)`, `clearSession()`, `refreshSession()` (stub — returns `Promise.resolve()`)
- [ ] T036 [US2] Create `frontend/src/services/apiClient.js` — Axios instance with `baseURL: import.meta.env.VITE_API_BASE_URL`, `withCredentials: true`; request interceptor injects `Authorization: Bearer <accessToken>` from store; response interceptor stubs 401 handler
- [ ] T037 [US2] Create `frontend/src/app/providers/AppProviders.jsx` — wraps children in MUI `ThemeProvider` (default theme), `BrowserRouter`
- [ ] T038 [US2] Create `frontend/src/app/router/AppRouter.jsx` — defines all routes from spec 07 Section 3 using React Router v6 `Routes`/`Route`; all protected routes wrapped in `AuthGuard`; each route renders a `<div>Placeholder — {PageName}</div>` component
- [ ] T039 [US2] Create `frontend/src/features/auth/AuthGuard.jsx` — checks `accessToken` in store; calls `refreshSession()` if missing; redirects to `/login` on failure; shows `LoadingSpinner` while checking (stub: always renders children in Sprint 0 since no real auth)
- [ ] T040 [US2] Create placeholder page files (one per route in spec 07):
  - `features/auth/LoginPage.jsx`
  - `features/dashboard/DashboardPage.jsx`
  - `features/events/EventsListPage.jsx`
  - `features/events/EventDetailPage.jsx`
  - `features/communications/CampaignsPage.jsx`
  - `features/communications/CampaignComposerPage.jsx`
  - `features/social/SocialPostsPage.jsx`
  - `features/social/PostGeneratorPage.jsx`
  - `features/content/SessionsPage.jsx`
  - `features/content/SessionIngestionPage.jsx`
  - `features/content/SessionSummaryPage.jsx`
  - `features/reports/ReportsPage.jsx`
  Each file exports a default functional component returning `<div>PageName — Placeholder</div>`
- [ ] T041 [P] [US2] Create shared component stubs (one file each):
  - `components/layout/AppShell.jsx` — renders `{children}` with MUI `Box`
  - `components/layout/Sidebar.jsx` — empty MUI `Drawer` stub
  - `components/layout/TopBar.jsx` — empty MUI `AppBar` stub
  - `components/feedback/LoadingSpinner.jsx` — MUI `CircularProgress` centered
  - `components/feedback/ErrorAlert.jsx` — MUI `Alert` severity="error"
  - `components/feedback/EmptyState.jsx` — MUI `Typography` "No data"
  - `components/feedback/SuccessBanner.jsx` — MUI `Alert` severity="success"
- [ ] T042 [P] [US2] Create service stub files (empty named exports):
  - `services/authService.js`
  - `services/eventService.js`
  - `services/registrationService.js`
  - `services/campaignService.js`
  - `services/socialService.js`
  - `services/sessionService.js`
  - `services/reportService.js`
- [ ] T043 [P] [US2] Create utility stubs:
  - `utils/dateUtils.js` — exports `formatDate(date)` returning `date.toISOString()`
  - `utils/statusColors.js` — exports `getStatusColor(status)` returning `'default'`
  - `utils/validators.js` — empty module
- [ ] T044 [US2] Update `frontend/src/main.jsx` to render `<AppProviders><AppRouter /></AppProviders>`
- [ ] T045 [US2] Create `frontend/staticwebapp.config.json` — SPA fallback config per spec 08 Section 5
- [ ] T046 [US2] Create `frontend/.env.example` — all `VITE_` vars with placeholder values per spec 07 Section 9
- [ ] T047 [US2] Configure `frontend/vite.config.js` — set `server.port: 5173`, `server.proxy` routing `/api` to `http://localhost:5001`

**Checkpoint**: `npm run build` in `frontend/` exits 0. All 12 route paths resolve to a rendered placeholder page.

---

## Phase 5: US3 — EF Core + AdminUsers Migration + Health Check

**Goal**: `AdminUsers` table exists in the local database after `dotnet ef database update`. `GET /health` returns 200 with a passing DB probe.

**Independent Test**: `GET http://localhost:5001/health` returns `{"status":"Healthy"}`. `AdminUsers` table visible in SQL Server.

- [ ] T048 [US3] Create `backend/src/EventPortal.Api/Modules/Auth/Entities/AdminUser.cs` — full entity class per `data-model.md`
- [ ] T049 [US3] Register `AdminUser` in `AppDbContext`:
  - Add `public DbSet<AdminUser> AdminUsers => Set<AdminUser>();`
  - Add `OnModelCreating` configuration: unique index on `Email`, column types, max lengths — per `data-model.md` EF config block
- [ ] T050 [US3] Generate EF Core migration:
  ```bash
  cd backend
  dotnet ef migrations add InitialCreate \
    --project src/EventPortal.Api \
    --startup-project src/EventPortal.Api \
    --output-dir Modules/Auth/Migrations
  ```
  Verify generated migration creates only `AdminUsers` table and its unique index on `Email`
- [ ] T051 [US3] Wire health check endpoint in `Program.cs` (already registered in T021 — verify):
  - `app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = ... })` present
  - `[AllowAnonymous]` or public route confirmed for `/health`
  - Verify `GET /health` responds 200 after DB migration applied
- [ ] T052 [US3] Run `dotnet ef database update` locally against Docker SQL Server and confirm `AdminUsers` table created

**Checkpoint**: `GET http://localhost:5001/health` → `200 {"status":"Healthy","checks":[{"name":"database","status":"Healthy"}]}`. `dotnet ef migrations list` shows `InitialCreate` applied.

---

## Phase 6: US4 — Terraform Module Stubs

**Goal**: All 6 Terraform module directories exist with valid stub `main.tf`, `variables.tf`, `outputs.tf`. `terraform validate` passes for both dev and prod environments.

**Independent Test**: `cd infra/env/dev && terraform init && terraform validate` exits 0. Same for `infra/env/prod`.

- [ ] T053 [P] [US4] Create `infra/modules/app_service/variables.tf` — input variables: `resource_group_name`, `location`, `app_service_plan_name`, `app_name`, `environment`, `key_vault_uri`, `app_insights_connection_string`
- [ ] T054 [P] [US4] Create `infra/modules/app_service/main.tf` — stub `resource "azurerm_service_plan"` and `resource "azurerm_linux_web_app"` blocks with `var.*` references; no real config yet
- [ ] T055 [P] [US4] Create `infra/modules/app_service/outputs.tf` — output: `app_service_url`, `principal_id`
- [ ] T056 [P] [US4] Create `infra/modules/static_web_app/variables.tf` — `resource_group_name`, `location`, `app_name`, `environment`
- [ ] T057 [P] [US4] Create `infra/modules/static_web_app/main.tf` — stub `resource "azurerm_static_web_app"` block
- [ ] T058 [P] [US4] Create `infra/modules/static_web_app/outputs.tf` — output: `default_host_name`, `api_key`
- [ ] T059 [P] [US4] Create `infra/modules/sql_database/variables.tf` — `resource_group_name`, `location`, `server_name`, `database_name`, `environment`, `sku_name`
- [ ] T060 [P] [US4] Create `infra/modules/sql_database/main.tf` — stub `azurerm_mssql_server` and `azurerm_mssql_database` blocks
- [ ] T061 [P] [US4] Create `infra/modules/sql_database/outputs.tf` — output: `server_fqdn`, `database_id`
- [ ] T062 [P] [US4] Create `infra/modules/key_vault/variables.tf` — `resource_group_name`, `location`, `vault_name`, `environment`, `tenant_id`, `app_service_principal_id`
- [ ] T063 [P] [US4] Create `infra/modules/key_vault/main.tf` — stub `azurerm_key_vault` and `azurerm_role_assignment` blocks
- [ ] T064 [P] [US4] Create `infra/modules/key_vault/outputs.tf` — output: `vault_uri`
- [ ] T065 [P] [US4] Create `infra/modules/storage/variables.tf` — `resource_group_name`, `location`, `account_name`, `environment`
- [ ] T066 [P] [US4] Create `infra/modules/storage/main.tf` — stub `azurerm_storage_account` and `azurerm_storage_container` (name: `reports`) blocks
- [ ] T067 [P] [US4] Create `infra/modules/storage/outputs.tf` — output: `primary_blob_endpoint`, `reports_container_name`
- [ ] T068 [P] [US4] Create `infra/modules/monitoring/variables.tf` — `resource_group_name`, `location`, `workspace_name`, `app_insights_name`, `environment`
- [ ] T069 [P] [US4] Create `infra/modules/monitoring/main.tf` — stub `azurerm_log_analytics_workspace` and `azurerm_application_insights` blocks
- [ ] T070 [P] [US4] Create `infra/modules/monitoring/outputs.tf` — output: `app_insights_connection_string`, `app_insights_instrumentation_key`
- [ ] T071 [US4] Create `infra/env/dev/main.tf` — Terraform root for dev; `terraform` block with `required_providers { azurerm }`, `azurerm` provider block; module references to all 6 modules passing dev-specific `var.*` values
- [ ] T072 [P] [US4] Create `infra/env/dev/variables.tf` — all variables used in `dev/main.tf`: `resource_group_name`, `location`, `environment = "dev"`, etc.
- [ ] T073 [P] [US4] Create `infra/env/dev/terraform.tfvars.example` — example values matching dev naming convention from spec 08 Section 12
- [ ] T074 [US4] Create `infra/env/prod/main.tf` — same structure as dev; `environment = "prod"` and prod-specific SKU variables
- [ ] T075 [P] [US4] Create `infra/env/prod/variables.tf` — prod variables
- [ ] T076 [P] [US4] Create `infra/env/prod/terraform.tfvars.example` — prod example values

**Checkpoint**: `cd infra/env/dev && terraform init -backend=false && terraform validate` → "Success! The configuration is valid." Same for `infra/env/prod`.

---

## Phase 7: US5 — CI/CD Pipelines + Docker Compose

**Goal**: Three GitHub Actions workflow files functional. `docker compose up --build` starts backend, frontend, and SQL Server locally.

**Independent Test**: `docker compose up --build` starts all services within 3 minutes; `GET http://localhost:5001/health` returns 200; frontend dev server visible at `http://localhost:5173`.

- [ ] T077 [US5] Create `backend/Dockerfile` — multi-stage: `sdk:8.0` build stage (`dotnet publish -c Release -o /app/publish`), `aspnet:8.0` runtime stage; `EXPOSE 8080`; `ENTRYPOINT ["dotnet", "EventPortal.Api.dll"]`
- [ ] T078 [US5] Create `frontend/Dockerfile` — Node 20 Alpine; `npm ci && npm run dev -- --host`; `EXPOSE 5173`; for local dev only (not used by Azure Static Web Apps)
- [ ] T079 [US5] Create `docker-compose.yml` at repo root:
  - `db`: `mcr.microsoft.com/mssql/server:2022-latest`; env: `SA_PASSWORD`, `ACCEPT_EULA=Y`; port `1433:1433`; volume `sqldata:/var/opt/mssql`
  - `api`: build `backend/`; depends_on: `db`; port `5001:8080`; env: `ASPNETCORE_ENVIRONMENT=Development`, `ConnectionStrings__DefaultConnection` pointing to `db` container
  - `frontend`: build `frontend/`; depends_on: `api`; port `5173:5173`
  - named volume: `sqldata`
- [ ] T080 [US5] Create `.github/workflows/pr-check.yml`:
  - Trigger: `pull_request` targeting `main` and `develop`
  - Job `backend-check`: `ubuntu-latest`; setup .NET 8; `dotnet restore`; `dotnet build --no-restore`; `dotnet test --no-build`
  - Job `frontend-check`: `ubuntu-latest`; setup Node 20; `npm ci`; `npm run build`; `npm run lint` (add `"lint": "eslint src"` to package.json)
  - Jobs run in parallel
- [ ] T081 [US5] Create `.github/workflows/dev-deploy.yml`:
  - Trigger: `push` to `develop`
  - Job `deploy-backend`: `ubuntu-latest`; environment: `dev`; `dotnet publish -c Release`; run EF migrations via `dotnet ef database update`; deploy via `azure/webapps-deploy@v3`
  - Job `deploy-frontend`: `ubuntu-latest`; `npm ci && npm run build`; deploy via `azure/static-web-apps-deploy@v1`
  - GitHub secrets referenced: `AZURE_APP_SERVICE_PUBLISH_PROFILE_DEV`, `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV`, all `VITE_*` vars
- [ ] T082 [US5] Create `.github/workflows/prod-deploy.yml`:
  - Trigger: `push` to `main`
  - Environment protection: `prod` (requires manual approval reviewer in GitHub settings)
  - Job `deploy-backend`: EF migration dry-run step before apply; deploy to staging slot; swap to production
  - Job `deploy-frontend`: same as dev but uses prod token and prod `VITE_*` vars
  - GitHub secrets referenced: `AZURE_APP_SERVICE_PUBLISH_PROFILE_PROD`, `AZURE_STATIC_WEB_APPS_API_TOKEN_PROD`
- [ ] T083 [US5] Add `"lint": "eslint src --ext .js,.jsx"` script to `frontend/package.json`; install `eslint` and `eslint-plugin-react` as dev dependencies

**Checkpoint**: `docker compose up --build` starts cleanly. `GET http://localhost:5001/health` returns `200 Healthy`. `GET http://localhost:5001/swagger` loads. Frontend at `http://localhost:5173` renders placeholder page.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, documentation, and cleanup to confirm Sprint 0 exit criteria are met.

- [ ] T084 Verify `backend/src/EventPortal.Api/appsettings.Development.json` is in `.gitignore` (contains local secrets); `.example` version is committed
- [ ] T085 [P] Verify `frontend/.env.local` is in `.gitignore`; `.env.example` is committed
- [ ] T086 [P] Verify `infra/env/*/terraform.tfvars` is in `.gitignore`; `.tfvars.example` files are committed
- [ ] T087 [P] Add `README` section update: confirm "Sprint 0 — Foundation Scaffold: ✅ Complete" in project status table
- [ ] T088 Run full Sprint 0 validation checklist from `quickstart.md`:
  - [ ] `GET http://localhost:5001/health` → 200 Healthy
  - [ ] `AdminUsers` table exists in local DB
  - [ ] `GET http://localhost:5001/swagger` loads Swagger UI
  - [ ] `http://localhost:5173` renders React placeholder page
  - [ ] `dotnet test` → 0 failures (1 placeholder test passes)
  - [ ] `cd infra/env/dev && terraform init -backend=false && terraform validate` → Success
  - [ ] `dotnet build backend/` → 0 errors
  - [ ] `npm run build` inside `frontend/` → 0 errors

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 completion — BLOCKS all US phases
- **Phase 3 (US1 Backend)**: Depends on Phase 2 — can start in parallel with US2, US4, US5
- **Phase 4 (US2 Frontend)**: Depends on Phase 1 only — can start in parallel with US1, US3, US4, US5
- **Phase 5 (US3 DB + Health)**: Depends on Phase 2 (AppDbContext must exist)
- **Phase 6 (US4 Terraform)**: Depends on Phase 1 (directory structure) — fully independent otherwise
- **Phase 7 (US5 CI/CD + Docker)**: Depends on US1 + US2 being compilable (needs Dockerfiles to build)
- **Phase 8 (Polish)**: Depends on all phases complete

### Parallel Opportunities After Phase 2

| Developer A | Developer B |
|---|---|
| Phase 3: US1 — Backend scaffold (T025–T033) | Phase 4: US2 — Frontend scaffold (T034–T047) |
| Phase 5: US3 — EF Core + Health (T048–T052) | Phase 6: US4 — Terraform stubs (T053–T076) |

Phase 7 (CI/CD) can follow once US1 and US2 compilers are green.

### Within Each Phase

- All tasks marked `[P]` within the same phase can run in parallel (different files, no shared dependencies)
- T033 (module DI registration) depends on T025–T032 (module stub files exist)
- T044 (main.jsx wiring) depends on T037 and T038 (AppProviders + AppRouter)
- T050 (migration generation) depends on T048 and T049 (entity + DbContext registered)
- T071 and T074 (env roots) depend on T053–T070 (all module files exist)

---

## Parallel Example: Phase 3 US1

```bash
# Launch all 8 module scaffolds in parallel (different folders, no shared state):
Task: "Scaffold Modules/Auth/"          → T025
Task: "Scaffold Modules/Events/"        → T026
Task: "Scaffold Modules/Registrations/" → T027
Task: "Scaffold Modules/Campaigns/"     → T028
Task: "Scaffold Modules/SocialPosts/"   → T029
Task: "Scaffold Modules/Sessions/"      → T030
Task: "Scaffold Modules/Reports/"       → T031
Task: "Scaffold Modules/AuditLogs/"     → T032

# After all complete:
Task: "Register all services in Program.cs" → T033
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US3)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational — CRITICAL)
3. Complete Phase 3 (US1 — backend builds)
4. Complete Phase 5 (US3 — DB + health check)
5. **STOP and VALIDATE**: `dotnet build` ✅ + `GET /health` ✅
6. Add Phase 4 (US2 — frontend builds)
7. **VALIDATE**: `npm run build` ✅

### Full Sprint Delivery

1. MVP (above)
2. Phase 6: Terraform stubs → `terraform validate` ✅
3. Phase 7: CI/CD + Docker → `docker compose up --build` ✅
4. Phase 8: Polish and checklist sign-off

---

## Notes

- `[P]` tasks = different files, no dependencies — safe to parallelize
- `[USn]` label maps task to scaffold deliverable for traceability
- No business logic in any task — stubs and empty implementations only
- Commit after each phase or logical group; do not commit `appsettings.Development.json` or `.env.local`
- `dotnet build` must be clean (0 errors, 0 warnings) before Phase 7
- Terraform stubs do not require real Azure credentials — `terraform validate` only needs the HCL to be syntactically correct
