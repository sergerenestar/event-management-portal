# 08 — Infrastructure and DevOps
# Event Management Portal — Azure Cloud and CI/CD

> Version: 1.0
> Status: Approved
> Audience: DevOps engineers, backend contributors, AI coding agents

---

## 1. Infrastructure Principles

- Azure is the only cloud provider for this project
- All infrastructure is defined as code — no manual Azure portal provisioning in production
- IaC tool: **Terraform** (preferred) or **Bicep** — pick one per project, do not mix
- Environments: `dev` and `prod` — add `test` when pipeline matures
- All secrets managed in Azure Key Vault — never in source code or environment variables
- Managed Identity used wherever supported — avoid static credentials between Azure services
- No HTTP traffic — HTTPS enforced at all layers

---

## 2. Azure Service Inventory

| Service | Purpose | SKU (dev) | SKU (prod) |
|---|---|---|---|
| Azure App Service | ASP.NET Core API hosting | B1 | P2v3 |
| Azure Static Web Apps | React frontend hosting | Free | Standard |
| Azure SQL Database | Primary relational database | Basic (5 DTU) | S2 (50 DTU) or GP vCore |
| Azure Blob Storage | PDF export storage | LRS | ZRS |
| Azure Key Vault | Secrets management | Standard | Standard |
| Application Insights | Observability and monitoring | Pay-per-use | Pay-per-use |
| Log Analytics Workspace | App Insights backend | Pay-per-use | Pay-per-use |
| Azure OpenAI | AI model API | Standard | Standard |
| Entra External ID | Authentication | Included | Included |

---

## 3. IaC Repository Structure

```
infra/
├── env/
│   ├── dev/
│   │   ├── main.tf          # Dev environment root module
│   │   ├── variables.tf
│   │   └── terraform.tfvars
│   └── prod/
│       ├── main.tf          # Prod environment root module
│       ├── variables.tf
│       └── terraform.tfvars
└── modules/
    ├── app_service/
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── static_web_app/
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── sql_database/
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── key_vault/
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── storage/
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    └── monitoring/
        ├── main.tf          # App Insights + Log Analytics Workspace
        ├── variables.tf
        └── outputs.tf
```

---

## 4. Azure App Service (Backend API)

**Resource:** `azurerm_app_service` / `azurerm_linux_web_app`

**Runtime:** .NET 8 on Linux

**Key configuration:**
- System-assigned Managed Identity enabled
- `ASPNETCORE_ENVIRONMENT` set to `Development` (dev) or `Production` (prod)
- `AZURE_KEY_VAULT_ENDPOINT` app setting pointing to the env-specific Key Vault URI
- All connection strings and secrets referenced via Key Vault references in app settings:
  ```
  @Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<name>/)
  ```
- Always-On enabled in prod
- Deployment slot: `staging` in prod for zero-downtime swap deploys
- HTTPS only enforced
- Minimum TLS 1.2

**Hangfire:**
- Hangfire Dashboard mounted at `/hangfire` — protected by JWT admin auth middleware
- Hangfire uses the same Azure SQL Database

**CORS:**
- Allowed origin: `VITE_API_BASE_URL` value per environment
- Only the React frontend origin is allowed

---

## 5. Azure Static Web Apps (Frontend)

**Resource:** `azurerm_static_web_app`

**Build:** Vite React app, output dir `dist/`

**Routing:**
- `staticwebapp.config.json` in repository root configures:
  - SPA fallback: all unmatched routes return `index.html`
  - Cache headers for static assets

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "/favicon.ico"]
  },
  "globalHeaders": {
    "Cache-Control": "no-cache, no-store"
  }
}
```

**Environment variables:**
- `VITE_*` build-time vars injected via GitHub Actions secrets at build time
- No runtime server-side env injection — all values baked at build

---

## 6. Azure SQL Database

**Resource:** `azurerm_mssql_server` + `azurerm_mssql_database`

**Authentication strategy:**
- Azure AD authentication enabled
- App Service Managed Identity granted `db_datareader` + `db_datawriter` + `db_ddladmin` in prod
- Local dev uses SQL authentication via connection string in user secrets

**Key configuration:**
- Firewall: Allow Azure services enabled; public access restricted to known IPs in prod
- Auditing enabled in prod
- Automated backups: short-term retention 7 days (dev), 35 days (prod)
- Connection string stored in Key Vault as secret `SqlDatabase:ConnectionString`

**EF Core migrations:**
- Migrations run as part of the CD pipeline before app deployment
- Command: `dotnet ef database update --connection "<connection-string>"`
- Never run migrations manually in prod — always via pipeline

---

## 7. Azure Key Vault

**Resource:** `azurerm_key_vault`

**Access model:**
- App Service Managed Identity granted `Key Vault Secrets User` role (RBAC)
- Developers granted `Key Vault Secrets Officer` role for dev vault (via Entra group)
- Prod vault: developer access restricted — secrets managed via pipeline only

**Secret naming convention:**

| Secret Name | Purpose |
|---|---|
| `SqlDatabase--ConnectionString` | EF Core connection string (note: `--` maps to `:` in .NET config) |
| `Jwt--SigningKey` | JWT signing key |
| `Eventbrite--ApiToken` | Eventbrite API bearer token |
| `Mailchimp--ApiKey` | Mailchimp API key |
| `Meta--PageAccessToken` | Meta Graph API page access token |
| `YouTube--ApiKey` | YouTube Data API key |
| `AzureOpenAI--ApiKey` | Azure OpenAI API key |
| `AzureOpenAI--Endpoint` | Azure OpenAI service endpoint URL |
| `BlobStorage--ConnectionString` | Azure Blob Storage connection string |

**Rule:** Secret names use `--` as hierarchy separator (maps to `:` in ASP.NET Core `IConfiguration`).

---

## 8. Azure Blob Storage

**Resource:** `azurerm_storage_account` + `azurerm_storage_container`

**Container:** `reports` — private access only

**Access:** App Service Managed Identity granted `Storage Blob Data Contributor` role

**PDF download links:** Pre-signed SAS URLs with 1-hour expiry — generated on demand, never stored

**Lifecycle policy (prod):** Archive PDFs older than 90 days, delete after 365 days (configurable)

---

## 9. Application Insights and Monitoring

**Resources:** `azurerm_application_insights` + `azurerm_log_analytics_workspace`

**Backend instrumentation:**
- `Microsoft.ApplicationInsights.AspNetCore` package
- `Serilog.Sinks.ApplicationInsights` for structured log forwarding
- Correlation ID propagated through all requests and background jobs
- Custom telemetry events for: AI agent calls, sync job completions, publish actions

**Frontend instrumentation (optional, Sprint 6+):**
- `@microsoft/applicationinsights-web` SDK
- Page views and custom events for key admin actions

**Alerts configured in prod:**
- App Service 5xx rate > 5% over 5 minutes
- Background job failure rate > 10% over 1 hour
- SQL DTU > 80% sustained
- Key Vault access denied spike

---

## 10. GitHub Actions CI/CD

### Pipeline Overview

```
.github/workflows/
├── ci.yml          # Pull request validation
└── cd.yml          # Deploy on merge to main
```

---

### CI Pipeline (`ci.yml`)

**Trigger:** `pull_request` targeting `main`

**Jobs:**

```yaml
jobs:
  backend-build:
    runs-on: ubuntu-latest
    steps:
      - checkout
      - setup .NET 8
      - dotnet restore
      - dotnet build --no-restore
      - dotnet test --no-build

  frontend-build:
    runs-on: ubuntu-latest
    steps:
      - checkout
      - setup Node 20
      - npm ci
      - npm run build
      - npm run lint
```

---

### CD Pipeline (`cd.yml`)

**Trigger:** `push` to `main` (merge)

**Jobs:**

```yaml
jobs:
  deploy-backend:
    environment: dev   # or prod, based on branch/tag
    runs-on: ubuntu-latest
    steps:
      - checkout
      - setup .NET 8
      - dotnet publish -c Release -o ./publish
      - run EF Core migrations against target DB
      - deploy to Azure App Service via azure/webapps-deploy action

  deploy-frontend:
    runs-on: ubuntu-latest
    steps:
      - checkout
      - setup Node 20
      - npm ci
      - npm run build (with VITE_ vars injected from GitHub secrets)
      - deploy to Azure Static Web Apps via azure/static-web-apps-deploy action
```

**Secrets in GitHub:**
- `AZURE_CREDENTIALS` — service principal for Terraform and deployment
- `AZURE_APP_SERVICE_PUBLISH_PROFILE` — per environment
- `AZURE_STATIC_WEB_APPS_API_TOKEN` — per environment
- `VITE_API_BASE_URL`, `VITE_MSAL_CLIENT_ID`, etc. — per environment

---

## 11. Environment Separation Strategy

| Concern | Dev | Prod |
|---|---|---|
| App Service SKU | B1 | P2v3 |
| SQL SKU | Basic | S2+ |
| Key Vault | `kv-eventportal-dev` | `kv-eventportal-prod` |
| Storage account | `storeventportaldev` | `storeventportalprod` |
| App Insights | Separate workspace | Separate workspace |
| Developer Key Vault access | Yes (Secrets Officer) | No (pipeline only) |
| Deployment slot | None | `staging` for zero-downtime swap |
| EF migrations | Auto on deploy | Auto on deploy — with dry-run step |

---

## 12. Naming Conventions

| Resource | Pattern | Example |
|---|---|---|
| Resource Group | `rg-eventportal-{env}` | `rg-eventportal-dev` |
| App Service | `app-eventportal-{env}` | `app-eventportal-prod` |
| App Service Plan | `asp-eventportal-{env}` | `asp-eventportal-prod` |
| Static Web App | `swa-eventportal-{env}` | `swa-eventportal-dev` |
| SQL Server | `sql-eventportal-{env}` | `sql-eventportal-prod` |
| SQL Database | `sqldb-eventportal-{env}` | `sqldb-eventportal-prod` |
| Key Vault | `kv-eventportal-{env}` | `kv-eventportal-dev` |
| Storage Account | `steventportal{env}` | `steventportalprod` |
| App Insights | `appi-eventportal-{env}` | `appi-eventportal-prod` |
| Log Analytics | `law-eventportal-{env}` | `law-eventportal-prod` |

All names lowercase, hyphens allowed except storage account (alphanumeric only).
