# Quickstart: Local Development Setup

**Branch**: `001-foundation-scaffold`
**Prerequisites**: Docker Desktop · .NET 8 SDK · Node.js 20+ · Git

---

## 1. Clone and Configure

```bash
git clone <repo-url>
cd event-management-portal
git checkout 001-foundation-scaffold
```

Copy environment config templates:

```bash
# Backend local secrets
cp backend/src/EventPortal.Api/appsettings.Development.json.example \
   backend/src/EventPortal.Api/appsettings.Development.json

# Frontend env vars
cp frontend/.env.example frontend/.env.local

# Terraform tfvars (dev)
cp infra/env/dev/terraform.tfvars.example infra/env/dev/terraform.tfvars
```

---

## 2. Start with Docker Compose (Recommended)

```bash
docker compose up --build
```

This starts:

| Service | URL | Notes |
|---|---|---|
| Backend API | http://localhost:5001 | ASP.NET Core .NET 8 |
| Swagger UI | http://localhost:5001/swagger | API docs |
| Health Check | http://localhost:5001/health | DB connectivity probe |
| Frontend | http://localhost:5173 | Vite React dev server |
| SQL Server | localhost:1433 | `sa` password in docker-compose.yml |

---

## 3. Run Backend Without Docker

```bash
cd backend

# Restore packages
dotnet restore src/EventPortal.Api

# Run database migrations (requires SQL Server running)
dotnet ef database update \
  --project src/EventPortal.Api \
  --startup-project src/EventPortal.Api

# Start the API
dotnet run --project src/EventPortal.Api
```

---

## 4. Run Frontend Without Docker

```bash
cd frontend

npm install
npm run dev
```

Frontend available at http://localhost:5173.

---

## 5. Run Backend Tests

```bash
cd backend
dotnet test tests/EventPortal.Tests
```

Sprint 0 test project is a scaffold — `PlaceholderTest.cs` passes by default.

---

## 6. Verify the Scaffold

After startup, confirm:

- [ ] `GET http://localhost:5001/health` returns `200 Healthy`
- [ ] Database `AdminUsers` table exists (connect via `localhost:1433`, db `EventPortal`)
- [ ] `GET http://localhost:5001/swagger` loads the Swagger UI
- [ ] `http://localhost:5173` loads the React app shell (placeholder page)
- [ ] `dotnet test` exits with 0 failures

---

## 7. Local appsettings.Development.json Reference

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EventPortal;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SigningKey": "dev-local-signing-key-min-32-chars!!",
    "Issuer": "EventPortal.Api",
    "Audience": "EventPortal.Frontend",
    "AccessTokenExpiryMinutes": 15
  },
  "Serilog": {
    "MinimumLevel": "Debug"
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

---

## 8. Terraform Init (Local Validation Only)

Sprint 0 Terraform is stubs only — no real Azure resources are provisioned.
To validate syntax:

```bash
cd infra/env/dev
terraform init
terraform validate
```

No `terraform apply` in Sprint 0 — infrastructure is provisioned in Sprint 1+.

---

## 9. Frontend .env.local Reference

```
VITE_API_BASE_URL=http://localhost:5001
VITE_MSAL_CLIENT_ID=<placeholder-from-entra-setup>
VITE_MSAL_AUTHORITY=https://login.microsoftonline.com/<tenant-id>
VITE_MSAL_REDIRECT_URI=http://localhost:5173/login
```

Auth login flow will not work locally until Sprint 1 (Entra External ID setup).
The login page scaffold renders but does not make real MSAL calls in Sprint 0.
