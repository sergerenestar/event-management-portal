# Local Development Setup

Follow these steps to run the Event Management Portal on your local machine.

---

## Prerequisites

Install the following before you begin:

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.x | https://dotnet.microsoft.com/download |
| Node.js | 20.x | https://nodejs.org |
| SQL Server Express | any | https://www.microsoft.com/en-us/sql-server/sql-server-downloads |
| Git | latest | https://git-scm.com |

---

## Secrets — Request from project lead

Before starting, request the following values from the project lead via a **secure channel (password manager — not email, Teams, or Slack).**

| # | Secret | Used in | Description |
|---|--------|---------|-------------|
| 1 | `Entra__TenantId` | Backend + Frontend | Azure Entra External ID tenant ID |
| 2 | `Entra__ClientId` | Backend + Frontend | Azure Entra app registration client ID |
| 3 | `Jwt__SigningKey` | Backend | HMAC-SHA256 key used to sign access tokens |
| 4 | `Eventbrite__ApiToken` | Backend | Eventbrite private API token |
| 5 | `Eventbrite__OrganizationId` | Backend | Eventbrite organization ID |
| 6 | Entra tenant access | Login | Your Microsoft or Google account must be invited to the Entra tenant |

> All values are shared as a single secure note. Do not store them anywhere outside your local config files.

---

## Step 1 — Clone the repo

```bash
git clone https://github.com/sergerenestar/event-management-portal.git
cd event-management-portal
```

---

## Step 2 — Backend config

Copy the example config file:

```bash
cp backend/src/EventPortal.Api/appsettings.Development.json.example \
   backend/src/EventPortal.Api/appsettings.Development.json
```

Open `appsettings.Development.json` and replace the placeholder values using the secrets table above:

```json
{
  "AllowedHosts": "*",
  "AZURE_KEY_VAULT_ENDPOINT": "",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EventPortal;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:5173"
  },
  "Entra": {
    "TenantId": "SECRET #1",
    "ClientId": "SECRET #2",
    "Audience": "SECRET #2"
  },
  "Jwt": {
    "SigningKey": "SECRET #3",
    "Issuer": "EventPortal",
    "Audience": "EventPortalClient",
    "ExpiryMinutes": 15
  },
  "Eventbrite": {
    "ApiToken": "SECRET #4",
    "OrganizationId": "SECRET #5",
    "BaseUrl": "https://www.eventbriteapi.com/v3"
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

> **Note:** If your SQL Server instance name is different from `SQLEXPRESS`, update `DefaultConnection` accordingly. You can find your instance name in SQL Server Configuration Manager.

---

## Step 3 — Frontend config

Copy the example env file:

```bash
cp frontend/.env.example frontend/.env.local
```

Open `frontend/.env.local` and fill in:

```env
VITE_API_BASE_URL=http://localhost:5001
VITE_ENTRA_CLIENT_ID=SECRET #2
VITE_ENTRA_TENANT_ID=SECRET #1
VITE_REDIRECT_URI=http://localhost:5173
```

---

## Step 4 — Create the database

Run EF Core migrations to create all tables in your local SQL Server:

```bash
cd backend && dotnet ef database update --project src/EventPortal.Api
```

---

## Step 5 — Run the backend

```bash
cd backend && dotnet run --project src/EventPortal.Api
```

Backend endpoints once running:

| URL | Description |
|-----|-------------|
| http://localhost:5001 | REST API |
| http://localhost:5001/swagger | Swagger UI (API explorer) |
| http://localhost:5001/health | Health check |
| http://localhost:5001/hangfire | Background jobs dashboard |

---

## Step 6 — Run the frontend

```bash
cd frontend && npm ci && npm run dev
```

Frontend will be available at: **http://localhost:5173**

---

## Step 7 — Log in

Use the Microsoft or Google account that the project lead has invited to the Entra tenant (Secret #6).
Navigate to http://localhost:5173 and click **Sign in with Microsoft** or **Sign in with Google**.

---

## Security reminders

- **Never commit** `appsettings.Development.json` or `frontend/.env.local` — both are gitignored
- **Never share secrets** over email, Teams, or Slack — use a password manager
- **Never hardcode** secrets in any source file
- If you suspect a secret has been exposed, notify the project lead immediately to rotate it
