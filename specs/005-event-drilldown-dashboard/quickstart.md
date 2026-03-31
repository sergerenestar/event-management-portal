# Quickstart: Event Registration Drill-Down Dashboard

**Feature**: `005-event-drilldown-dashboard`
**Prerequisites**: Sprint 2 complete · events synced · registrations synced · local stack running

---

## What This Feature Adds

A dedicated analytics page at `/events/{id}/analytics` that shows three charts for any synced event:
1. **Daily trend** — registrations per day (existing `DailyRegistrationSnapshots` data)
2. **Location breakdown** — registrations per branch/location (parsed from ticket type names)
3. **Adults vs Children** — attendee type split (classified from ticket type names)

---

## Prerequisites Checklist

Before running or testing this feature, verify:

- [ ] Sprint 2 is merged and running locally (`docker compose up --build`)
- [ ] At least one event has been synced: `POST /api/v1/events/sync`
- [ ] Registrations have been synced for that event: `POST /api/v1/events/{id}/registrations/sync`
- [ ] Daily snapshots have been generated: trigger `SnapshotAggregatorJob` in Hangfire dashboard or wait for scheduled run
- [ ] Your test event has ticket types named using the `"Location — AttendeeType"` convention (e.g., `"London Branch — Adult"`, `"Manchester — Children"`)
- [ ] You have a valid admin JWT (log in at `/login` or `POST /api/v1/auth/login`)

---

## Local Development Setup

No additional environment variables, secrets, or migrations are needed. This feature is additive-only.

### Verify the backend builds

```bash
cd backend
dotnet build src/EventPortal.Api
```

Expected: 0 errors.

### Verify new endpoints are visible in Swagger

```
GET http://localhost:5001/swagger
```

Look for:
- `GET /api/v1/events/{eventId}/registrations/location-breakdown`
- `GET /api/v1/events/{eventId}/registrations/attendee-type-breakdown`

Both should have a padlock icon (auth required).

### Run backend tests

```bash
cd backend
dotnet test tests/EventPortal.Tests
```

Expected: all tests pass, including new `TicketTypeNameParserTests` and extended `RegistrationServiceTests`.

---

## Verify the Feature End-to-End

### Step 1 — Navigate to the drill-down dashboard

1. Open `http://localhost:5173/events`
2. Click any event row
3. Confirm you land on `http://localhost:5173/events/{id}/analytics`

### Step 2 — Verify all three charts render

| Chart | Expected |
|-------|----------|
| Daily Trend | Line/area chart with one point per day; X-axis = date, Y-axis = count |
| Location Breakdown | Bar chart with one bar per location; up to 10 locations + "Other" |
| Adults vs Children | Pie/donut chart with Adult, Children, Other segments |

### Step 3 — Verify "Last synced" timestamp

The page header should show a timestamp like: `Data as of 24 Mar 2026, 18:00`.

### Step 4 — Verify back navigation

A breadcrumb or "Back to Events" link at the top of the page should navigate back to `/events` without using the browser back button.

### Step 5 — Verify unauthenticated access is blocked

```bash
curl -i http://localhost:5001/api/v1/events/1/registrations/location-breakdown
# Expected: HTTP 401 Unauthorized
```

---

## Testing Ticket Type Name Parsing

To exercise the full range of parsing behaviour, create or mock ticket types with these names:

| Ticket Type Name | Expected Location | Expected Type |
|-----------------|-------------------|---------------|
| `"London Branch — Adult"` | London Branch | Adult |
| `"Manchester — Children"` | Manchester | Children |
| `"Birmingham — Child"` | Birmingham | Children |
| `"Adult + Child Family Pass"` | Adult + Child Family Pass | Other |
| `"General Admission"` | General Admission | Adult |

---

## Empty State Verification

To verify the empty state:
1. Navigate to an event that has been synced but has zero registrations
2. All three chart areas should display a user-friendly "No data available yet" message
3. The page should not show broken chart containers or JavaScript errors

---

## Sprint Validation Checklist

- [ ] `GET /api/v1/events/{id}/registrations/location-breakdown` → 200 with correct location groups
- [ ] `GET /api/v1/events/{id}/registrations/attendee-type-breakdown` → 200 with Adult/Children/Other counts
- [ ] Unauthenticated requests to both endpoints → 401
- [ ] `/events/{id}/analytics` renders daily trend chart
- [ ] `/events/{id}/analytics` renders location breakdown chart
- [ ] `/events/{id}/analytics` renders adults vs children chart
- [ ] Clicking event row on `/events` navigates to `/events/{id}/analytics`
- [ ] Breadcrumb on analytics page navigates back to `/events`
- [ ] "Last synced" timestamp visible on analytics page
- [ ] Empty-state message shown when event has zero registrations
- [ ] `dotnet test` → 0 failures
- [ ] `npm run build` → 0 errors
- [ ] `npm run lint` → 0 errors
