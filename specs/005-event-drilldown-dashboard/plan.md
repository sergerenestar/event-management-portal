# Implementation Plan: Event Registration Drill-Down Dashboard

**Branch**: `005-event-drilldown-dashboard` | **Date**: 2026-03-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-event-drilldown-dashboard/spec.md`

## Summary

Adds a dedicated analytics drill-down page at `/events/{id}/analytics` reachable from the existing events list. The page presents three charts — daily registration trend, location breakdown, and adult/children breakdown — all served from locally synced data with no live Eventbrite API calls on page load.

Location and attendee-type classification are both derived at query time from the ticket type name, using a structured naming convention: `"Location — AttendeeType"` (e.g., `"London Branch — Adult"`). No new database entities or sync jobs are required. The feature extends the existing `Registrations` module with two new query endpoints and extends the frontend with a new analytics page and three chart components.

## Technical Context

**Language/Version**: C# .NET 8 (backend) · JavaScript ES2022 / JSX (frontend)
**Primary Dependencies**: ASP.NET Core Web API · EF Core 8 · Recharts · Material UI · Zustand · React Router v6
**Storage**: Azure SQL via EF Core — existing `TicketTypes`, `Registrations`, `DailyRegistrationSnapshots` tables (Sprint 2). No new tables or migrations required.
**Testing**: xUnit + Moq (backend unit) · xUnit + WebApplicationFactory (backend integration) · Vitest (frontend)
**Target Platform**: Azure App Service (.NET 8) · Azure Static Web Apps (React)
**Project Type**: Web application (modular monolith backend + SPA frontend)
**Performance Goals**: SC-001 — drill-down page loads all three charts within 3 seconds for events with up to 5,000 registrations · SC-006 — each data endpoint responds within 2 seconds
**Constraints**: No live Eventbrite API call on page load · Admin JWT required on all endpoints · Location + type derived at query time (no new DB columns)
**Scale/Scope**: Up to 5,000 registrations per event · Up to 10 locations shown (remainder → "Other")

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I — Human-Gated AI Actions | ✅ N/A | No AI content generation in this feature |
| II — Security-First | ✅ Pass | All 2 new endpoints require `[Authorize(Policy = "AdminOnly")]`; no secrets introduced |
| III — Modular Monolith | ✅ Pass | New service methods added to `Registrations` module; frontend chart data fetched via `registrationService.js` (no direct axios in feature components) |
| IV — Spec-Driven | ✅ Pass | Spec complete, checklist fully green before planning |
| V — Observability | ✅ Pass | Correlation ID propagated via existing middleware; Serilog structured logs added for new service calls |
| VI — Infrastructure as Code | ✅ N/A | No new Azure resources; no Terraform changes required |
| VII — Test Coverage | ✅ Pass | Each acceptance scenario maps to a test case; unit tests for name-parsing logic; integration tests for new endpoints |

**Constitution Check Result: PASS — no violations. Proceed to Phase 0.**

### Post-Design Re-Check (after Phase 1)

| Principle | Status | Design Decision |
|-----------|--------|----------------|
| II — Security-First | ✅ Pass | Both new endpoints decorated `[Authorize(Policy = "AdminOnly")]`; no new secrets; no new Azure resources |
| III — Modular Monolith | ✅ Pass | `TicketTypeNameParser` lives inside `Registrations` module; `TicketType` entity accessed via EF Core join (not by importing Events module services) |
| V — Observability | ✅ Pass | Correlation ID propagated via existing `CorrelationIdMiddleware`; `RegistrationService` logs location/type breakdown query runs with Serilog structured events |
| VII — Test Coverage | ✅ Pass | `TicketTypeNameParser` has dedicated unit tests; new service methods have unit tests; new endpoints have integration tests; all acceptance scenarios mapped |

**Post-Design Result: PASS — design is constitution-compliant.**

## Project Structure

### Documentation (this feature)

```text
specs/005-event-drilldown-dashboard/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/           ← Phase 1 output
│   ├── location-breakdown.md
│   └── attendee-type-breakdown.md
└── tasks.md             ← Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/src/EventPortal.Api/
├── Modules/
│   ├── Shared/
│   │   └── Utilities/
│   │       └── TicketTypeNameParser.cs         # NEW: static helper for name parsing (Shared for reuse)
│   └── Registrations/
│       ├── Services/
│       │   ├── IRegistrationService.cs         # extend: +2 new methods
│       │   └── RegistrationService.cs          # extend: implement new methods
│       ├── Dtos/
│       │   ├── LocationBreakdownDto.cs         # NEW
│       │   └── AttendeeTypeBreakdownDto.cs     # NEW
│       └── Controllers/
│           └── RegistrationsController.cs      # extend: +2 new endpoints

backend/tests/EventPortal.Tests/
├── Shared/
│   └── TicketTypeNameParserTests.cs            # NEW
└── Registrations/
    ├── RegistrationServiceTests.cs             # extend: +new method tests
    └── RegistrationControllerIntegrationTests.cs  # extend: +new endpoint tests

frontend/src/
├── features/
│   └── events/
│       ├── EventAnalyticsPage.jsx              # NEW: route /events/{id}/analytics
│       ├── RegistrationTrendChart.jsx          # NEW
│       ├── LocationBreakdownChart.jsx          # NEW
│       └── AttendeeTypeChart.jsx               # NEW
└── services/
    └── registrationService.js                  # extend: +2 new service calls
```

**Structure Decision**: Web application (Option 2). All backend changes are additive to the existing `Registrations` module. No new modules. A `Helpers/` subfolder is introduced within `Registrations` to house the `TicketTypeNameParser` static class, keeping it co-located with the only consumers.

## Complexity Tracking

> No constitution violations — table not required.
