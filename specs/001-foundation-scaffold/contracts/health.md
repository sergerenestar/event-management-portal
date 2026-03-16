# Contract: Health Check Endpoint

Sprint 0 exposes a single public endpoint — the health check. All business API endpoints
are scaffolded as empty controllers with no routes in this sprint.

---

## GET /health `[Public]`

Returns the application health status. Used by Azure App Service health probes and CI smoke tests.

**Authentication**: None required — public endpoint.

**Response 200 — Healthy:**
```json
{
  "status": "Healthy",
  "environment": "Development",
  "timestamp": "2026-03-16T10:00:00Z"
}
```

**Response 503 — Unhealthy:**
```json
{
  "status": "Unhealthy",
  "environment": "Production",
  "timestamp": "2026-03-16T10:00:00Z",
  "details": {
    "database": "Unhealthy: Connection refused"
  }
}
```

---

## Implementation Notes

- Registered via `app.MapHealthChecks("/health")` in `Program.cs`
- ASP.NET Core health checks package: `Microsoft.Extensions.Diagnostics.HealthChecks`
- In Sprint 0, the health check MUST include a database connectivity probe via EF Core
- DB health check package: `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

```csharp
// Program.cs registration
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// Endpoint mapping
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

- The health endpoint is the **only** route that Sprint 0 implements beyond scaffold structure.
- All other module controllers are empty stubs — no routes registered.
