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

Open `appsettings.Development.json` and fill in the values below.
**Get these values from the project lead via a secure channel (password manager — not email/chat):**

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
    "TenantId": "← ask project lead",
    "ClientId": "← ask project lead",
    "Audience": "← same as ClientId"
  },
  "Jwt": {
    "SigningKey": "← ask project lead",
    "Issuer": "EventPortal",
    "Audience": "EventPortalClient",
    "ExpiryMinutes": 15
  },
  "Eventbrite": {
    "ApiToken": "← ask project lead",
    "OrganizationId": "← ask project lead",
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

> **Note:** If you are not using SQL Express, update `DefaultConnection` to match your local SQL Server instance name.

---

## Step 3 — Frontend config

Copy the example env file:

```bash
cp frontend/.env.example frontend/.env.local
```

Open `frontend/.env.local` and fill in:

```env
VITE_API_BASE_URL=http://localhost:5001
VITE_ENTRA_CLIENT_ID=← ask project lead
VITE_ENTRA_TENANT_ID=← ask project lead
VITE_REDIRECT_URI=http://localhost:5173
```

---

## Step 4 — Create the database

Run EF Core migrations to create and seed the local database:

```bash
cd backend && dotnet ef database update --project src/EventPortal.Api
```

---

## Step 5 — Run the backend

```bash
cd backend && dotnet run --project src/EventPortal.Api
```

Backend will be available at:
- API: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- Health check: http://localhost:5001/health

---

## Step 6 — Run the frontend

```bash
cd frontend && npm ci && npm run dev
```

Frontend will be available at: http://localhost:5173

---

## Step 7 — Log in

You must be added to the Entra External ID tenant by the project lead before you can log in.
Contact the project lead to have your Microsoft or Google account invited.

---

## Security reminders

- **Never commit** `appsettings.Development.json` or `frontend/.env.local` — both are gitignored
- **Never share secrets** over email, Teams, or Slack — use a password manager
- **Never hardcode** secrets in source files
