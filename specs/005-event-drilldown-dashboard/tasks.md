# Tasks: Sprint 4 — Event Registration Drill-Down Dashboard

**Input**: `specs/005-event-drilldown-dashboard/plan.md`, `spec.md`, `data-model.md`, `contracts/`, `research.md`, `quickstart.md`
**Prerequisites**: Sprint 2 complete ✅ · `Events`, `TicketTypes`, `Registrations`, `DailyRegistrationSnapshots` tables ✅ · `GET /api/v1/events/{id}/registrations/daily-trends` endpoint ✅
**Constitution Check**: All tasks comply with Principles II (Security-First), III (Modular Monolith), V (Observability), and VII (Test Coverage).

---

## User Story Map

| Story | Deliverable | Done When |
|-------|-------------|-----------|
| US4 (P1) | Analytics page shell — route, navigation, breadcrumb | `/events/{id}/analytics` renders with skeleton states; back nav works |
| US1 (P1) | Daily registration trend chart | LineChart renders with daily data from `DailyRegistrationSnapshots` |
| US2 (P2) | Location breakdown chart | Horizontal BarChart renders grouped by parsed ticket type location |
| US3 (P3) | Adults vs children breakdown chart | Donut PieChart renders with Adult/Children/Other classification |

---

## Phase 1: Setup — New DTOs

**Purpose**: Define all new response shapes before any service or controller code is written.

- [X] T001 [P] Create `backend/src/EventPortal.Api/Modules/Registrations/Dtos/LocationBreakdownDto.cs` — class with `string Location`, `int Count`, `decimal Percentage`; and `LocationBreakdownResultDto.cs` — class with `int EventId`, `int TotalRegistrations`, `DateTime? LastSyncedAt`, `List<LocationBreakdownDto> Locations`
- [X] T002 [P] Create `backend/src/EventPortal.Api/Modules/Registrations/Dtos/AttendeeTypeBreakdownDto.cs` — class with `string AttendeeType`, `int Count`, `decimal Percentage`; and `AttendeeTypeBreakdownResultDto.cs` — class with `int EventId`, `int TotalRegistrations`, `DateTime? LastSyncedAt`, `List<AttendeeTypeBreakdownDto> Breakdown`

**Checkpoint**: `dotnet build backend/src/EventPortal.Api` — 0 errors.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared name-parsing helper and frontend route wiring that ALL user stories depend on.

⚠️ **CRITICAL**: No user story frontend work can begin until T005 (route) is complete. No US2/US3 backend work can begin until T003 (parser) is complete.

- [X] T003 Create `backend/src/EventPortal.Api/Modules/Shared/Utilities/TicketTypeNameParser.cs` — static class with two public methods:
  - `ParseLocation(string ticketTypeName) → string`: split on last occurrence of ` — `, ` – `, or ` - `; return text before separator (trimmed); if no separator found return full name trimmed
  - `ParseAttendeeType(string ticketTypeName) → string`: case-insensitive check — if contains both "adult" and "child"/"children" → `"Other"`; if contains "child" or "children" (without "adult") → `"Children"`; all other names → `"Adult"` (per FR-006)
- [X] T004 [P] Create `backend/tests/EventPortal.Tests/Shared/TicketTypeNameParserTests.cs` — xUnit test class covering:
  - `ParseLocation("London Branch — Adult")` → `"London Branch"`
  - `ParseLocation("Manchester — Children")` → `"Manchester"`
  - `ParseLocation("General Admission")` → `"General Admission"` (no separator, full name returned)
  - `ParseLocation("London — North Branch — Adult")` → `"London — North Branch"` (last separator used)
  - `ParseAttendeeType("London Branch — Adult")` → `"Adult"`
  - `ParseAttendeeType("Manchester — Children")` → `"Children"`
  - `ParseAttendeeType("Birmingham — Child")` → `"Children"`
  - `ParseAttendeeType("Adult + Child Family Pass")` → `"Other"` (contains both keywords)
  - `ParseAttendeeType("General Admission")` → `"Adult"` (fallback — no keyword)
  - `ParseAttendeeType("VIP")` → `"Adult"` (fallback — no keyword)
- [X] T005 [P] Add route `/events/:eventId/analytics` to `frontend/src/app/router/AppRouter.jsx` — import `EventAnalyticsPage` (file created in T007); wrap in `AuthGuard`; place after the existing `/events/:id` route
- [X] T006 [P] Update `frontend/src/features/events/EventsListPage.jsx` row click handler — change `navigate('/events/${id}')` to `navigate('/events/${id}/analytics')` so clicking an event row opens the drill-down analytics page

**Checkpoint**: `dotnet build backend/` → 0 errors. `dotnet test` → all parser tests pass. `npm run build` → 0 errors (EventAnalyticsPage stub imported in router).

---

## Phase 3: US4 + US1 (Priority: P1) 🎯 MVP

**Goal**: Admin can navigate from the events list to a working analytics page that shows the daily trend chart, with back navigation and a "Last synced" timestamp.

**Independent Test**: Navigate to `/events/{id}/analytics` → page renders with event name in header, breadcrumb link to `/events`, skeleton loading states resolve to a LineChart with daily data points, "Last synced" timestamp visible. `GET /events/{id}/analytics` requires authentication (redirects to login if unauthenticated).

### Implementation

- [X] T007 [US4] Create `frontend/src/features/events/EventAnalyticsPage.jsx`:
  - Fetch on mount with `Promise.all`: `registrationService.getDailyTrends(id)`, `registrationService.getLocationBreakdown(id)` (stub, returns `[]` until US2), `registrationService.getAttendeeTypeBreakdown(id)` (stub, returns `[]` until US3), `eventService.getEventById(id)`
  - Page header: event name (`EventDetailDto.name`) + event date range
  - Breadcrumb: MUI `Breadcrumbs` → "Events" links to `/events` → event name (current page)
  - "Last synced" display: derived from event's most recent registration sync timestamp; shown as e.g. `Data as of 24 Mar 2026, 18:00`; displayed below the header
  - Three named chart sections rendered in a responsive MUI `Grid`: "Registration Trend", "Registrations by Location", "Registrations by Attendee Type"
  - Each section: MUI `Skeleton` height 320 while loading, replaced by chart component when data arrives, replaced by `EmptyState` if data is empty array
  - Per-chart `ErrorAlert` if that fetch fails (other charts continue to render)
- [X] T008 [P] [US1] Create `frontend/src/features/events/RegistrationTrendChart.jsx`:
  - Props: `data` (array of `{ date, count, ticketTypeName }` from existing `DailySnapshotDto`), `loading` (bool)
  - Recharts `LineChart`: one `<Line>` per unique `ticketTypeName` value in `data`, each with a distinct MUI-palette colour
  - `ResponsiveContainer width="100%" height={320}`, `XAxis dataKey="date"` (formatted `DD MMM` using `dateUtils.formatDate`), `YAxis allowDecimals={false}`, `Tooltip`, `Legend`, `CartesianGrid strokeDasharray="3 3"`
  - When `data` is empty array and `loading` is false: render shared `EmptyState` component with message "No registration data available yet"
  - Follows pattern established in `frontend/src/features/events/DailyTrendChart.jsx` (Sprint 2)
- [X] T009 [US1] Integrate `RegistrationTrendChart` into `EventAnalyticsPage.jsx` — replace the "Registration Trend" chart section skeleton with `<RegistrationTrendChart data={dailyTrends} loading={loading} />`; ensure tooltip data includes exact date and count per acceptance scenario 4

**Checkpoint**: `/events/{id}/analytics` renders. Trend chart shows data points or empty state. Breadcrumb links back to `/events`. "Last synced" timestamp visible. All routes require auth.

---

## Phase 4: US2 — Registrations by Location (Priority: P2)

**Goal**: Admin can see a horizontal bar chart on the analytics page showing registration counts grouped by location, derived from ticket type names.

**Independent Test**: `GET /api/v1/events/{id}/registrations/location-breakdown` returns a JSON response where the sum of all `locations[].count` equals the total registration count. Chart renders in `/events/{id}/analytics` with one bar per location (top 10 + "Other"). Attendees with no parseable location grouped under "Unknown".

### Backend

- [X] T010 [US2] Add `GetLocationBreakdownAsync(int eventId) → Task<LocationBreakdownResultDto>` to `IRegistrationService.cs` interface and implement in `RegistrationService.cs`:
  - Server-side EF Core query: load `TicketTypes` for the event (`Id`, `Name`); load registration counts grouped by `TicketTypeId` — `Registrations.Where(r => r.EventId == eventId).GroupBy(r => r.TicketTypeId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync()`
  - Client-side: for each `(TicketTypeId, Count)` pair, look up `TicketType.Name` from loaded types and call `TicketTypeNameParser.ParseLocation(name)`; if `TicketTypeId` has no matching TicketType record, label as `"Unknown"`
  - Group by parsed location label; sum counts per label
  - Sort descending by count; take top 10; sum remainder into `{ Location = "Other", Count = N }`; if no "Unknown" entries exist, omit the "Unknown" group
  - Calculate `Percentage = Math.Round(count * 100m / totalRegistrations, 1)` per entry
  - Set `LastSyncedAt` = `Registrations.Where(r => r.EventId == eventId).MaxAsync(r => (DateTime?)r.CreatedAt)`
  - Return `LocationBreakdownResultDto`
- [X] T011 [P] [US2] Add `GetLocationBreakdown` action to `RegistrationsController.cs`:
  - `[HttpGet("{eventId}/registrations/location-breakdown")]`
  - `[Authorize(Policy = "AdminOnly")]`
  - Call `_registrationService.GetLocationBreakdownAsync(eventId)`
  - Return `200 OK` with `LocationBreakdownResultDto`
  - Return `404 Not Found` (RFC 7807 problem+json) if event does not exist
  - Log structured Serilog entry: `"LocationBreakdown queried"` with `eventId`, `locationCount`, `totalRegistrations`, `correlationId`

### Frontend

- [X] T012 [P] [US2] Add `getLocationBreakdown(eventId)` to `frontend/src/services/registrationService.js` — `GET /api/v1/events/{eventId}/registrations/location-breakdown` via `apiClient`; returns `LocationBreakdownResultDto`
- [X] T013 [P] [US2] Create `frontend/src/features/events/LocationBreakdownChart.jsx`:
  - Props: `data` (array of `{ location, count, percentage }` from `LocationBreakdownResultDto.locations`), `loading` (bool)
  - Recharts `BarChart` with `layout="vertical"` (horizontal bars): `XAxis type="number" allowDecimals={false}`, `YAxis type="category" dataKey="location" width={140}`, `Bar dataKey="count"` with MUI primary colour, `Tooltip` showing count + percentage, `CartesianGrid strokeDasharray="3 3"`
  - `ResponsiveContainer width="100%" height={Math.max(data.length * 40, 280)}`
  - When `data` is empty and `loading` is false: render `EmptyState` with message "No location data available"
- [X] T014 [US2] Integrate `LocationBreakdownChart` into `EventAnalyticsPage.jsx` — replace "Registrations by Location" skeleton with `<LocationBreakdownChart data={locationBreakdown.locations} loading={loading} />`; update the `Promise.all` mount fetch to call `registrationService.getLocationBreakdown(id)` (replace the stub from T007)

### Tests

- [X] T015 [P] [US2] Add unit tests for `GetLocationBreakdownAsync` to `backend/tests/EventPortal.Tests/Registrations/RegistrationServiceTests.cs`:
  - Two ticket types with different locations → two location groups returned correctly
  - More than 10 locations → top 10 returned + "Other" aggregation
  - TicketType name has no separator → full name used as location label
  - All registrations from one location → single group, 100% percentage
  - Event with zero registrations → empty `Locations` list, `TotalRegistrations = 0`
  - Sum of all `Count` values equals `TotalRegistrations`
- [X] T016 [P] [US2] Add integration tests for new endpoint to `backend/tests/EventPortal.Tests/Registrations/RegistrationControllerIntegrationTests.cs`:
  - Unauthenticated `GET /api/v1/events/{id}/registrations/location-breakdown` → 401
  - Unknown `eventId` → 404 with problem+json body
  - Valid event with registrations → 200 with correct `locations` array structure
  - Sum of `count` fields in response equals total registrations for event

**Checkpoint**: `dotnet test` passes. `GET /api/v1/events/{id}/registrations/location-breakdown` returns correct data. Location chart renders in `/events/{id}/analytics`.

---

## Phase 5: US3 — Registrations by Attendee Type (Priority: P3)

**Goal**: Admin can see a donut pie chart showing the split between Adults, Children, and Other registrations.

**Independent Test**: `GET /api/v1/events/{id}/registrations/attendee-type-breakdown` returns a response where `breakdown` contains Adult, Children, and/or Other groups, and their `count` values sum to `totalRegistrations`. Chart renders in `/events/{id}/analytics` showing proportions with raw counts per acceptance scenario 4.

### Backend

- [X] T017 [US3] Add `GetAttendeeTypeBreakdownAsync(int eventId) → Task<AttendeeTypeBreakdownResultDto>` to `IRegistrationService.cs` interface and implement in `RegistrationService.cs`:
  - Server-side EF Core query: load registration counts grouped by `TicketTypeId` (same pattern as `GetLocationBreakdownAsync` — reuse or extract shared method)
  - Client-side: for each `(TicketTypeId, Count)` pair, look up `TicketType.Name` and call `TicketTypeNameParser.ParseAttendeeType(name)`
  - Group by `AttendeeType` label; sum counts per label; omit groups with `Count = 0` from result
  - Fixed sort order: Adult first, Children second, Other last
  - Calculate `Percentage = Math.Round(count * 100m / totalRegistrations, 1)` per entry
  - Set `LastSyncedAt` using same pattern as `GetLocationBreakdownAsync`
  - Return `AttendeeTypeBreakdownResultDto`
- [X] T018 [P] [US3] Add `GetAttendeeTypeBreakdown` action to `RegistrationsController.cs`:
  - `[HttpGet("{eventId}/registrations/attendee-type-breakdown")]`
  - `[Authorize(Policy = "AdminOnly")]`
  - Call `_registrationService.GetAttendeeTypeBreakdownAsync(eventId)`
  - Return `200 OK` with `AttendeeTypeBreakdownResultDto`
  - Return `404 Not Found` if event does not exist
  - Log structured Serilog entry: `"AttendeeTypeBreakdown queried"` with `eventId`, `adultCount`, `childrenCount`, `otherCount`, `correlationId`

### Frontend

- [X] T019 [P] [US3] Add `getAttendeeTypeBreakdown(eventId)` to `frontend/src/services/registrationService.js` — `GET /api/v1/events/{eventId}/registrations/attendee-type-breakdown` via `apiClient`; returns `AttendeeTypeBreakdownResultDto`
- [X] T020 [P] [US3] Create `frontend/src/features/events/AttendeeTypeChart.jsx`:
  - Props: `data` (array of `{ attendeeType, count, percentage }` from `AttendeeTypeBreakdownResultDto.breakdown`), `loading` (bool)
  - Recharts `PieChart` with donut style: `<Pie dataKey="count" innerRadius="55%" outerRadius="80%" label={({ name, value, percent }) => \`${name}: ${value} (${(percent * 100).toFixed(1)}%)\`}`
  - Three `Cell` components with distinct colours: Adult (MUI primary), Children (MUI secondary), Other (MUI grey)
  - `Tooltip` formatter showing count with comma separators
  - `ResponsiveContainer width="100%" height={300}`
  - When `data` is empty and `loading` is false: render `EmptyState` with message "No attendee type data available"
- [X] T021 [US3] Integrate `AttendeeTypeChart` into `EventAnalyticsPage.jsx` — replace "Registrations by Attendee Type" skeleton with `<AttendeeTypeChart data={attendeeTypeBreakdown.breakdown} loading={loading} />`; update the `Promise.all` mount fetch to call `registrationService.getAttendeeTypeBreakdown(id)` (replace the stub from T007)

### Tests

- [X] T022 [P] [US3] Add unit tests for `GetAttendeeTypeBreakdownAsync` to `backend/tests/EventPortal.Tests/Registrations/RegistrationServiceTests.cs`:
  - Mixed ticket types (adult + children) → both groups returned with correct counts
  - Only adult ticket types → only "Adult" in response, no "Children" entry
  - Ambiguous ticket type name ("Adult + Child Family Pass") → classified as "Other"
  - "General Admission" ticket → classified as "Adult" (default fallback)
  - Zero-count types omitted from response
  - Sum of all `Count` values equals `TotalRegistrations`
  - `Percentage` values sum to 100.0 (±0.1 rounding tolerance)
- [X] T023 [P] [US3] Add integration tests for new endpoint to `backend/tests/EventPortal.Tests/Registrations/RegistrationControllerIntegrationTests.cs`:
  - Unauthenticated `GET /api/v1/events/{id}/registrations/attendee-type-breakdown` → 401
  - Unknown `eventId` → 404 with problem+json body
  - Valid event → 200 with `breakdown` array containing Adult and/or Children entries
  - Fixed response order: Adult before Children before Other

**Checkpoint**: `dotnet test` passes for all new tests. Attendee type chart renders in `/events/{id}/analytics`. Donut shows raw counts + percentages per acceptance scenario 4.

---

## Phase 6: Polish & Sprint Validation

**Purpose**: Observability hardening, edge-case verification, and final sprint sign-off.

- [X] T024 Verify Serilog log entries in `RegistrationService.cs` for both new methods include `eventId`, result counts, and correlation ID — confirm log entries appear in Application Insights format by checking Serilog sink configuration in `SerilogConfiguration.cs`
- [X] T025 [P] Verify edge cases in `EventAnalyticsPage.jsx` and all three chart components:
  - Navigate to an event with zero registrations → all three chart sections show `EmptyState` (not blank containers or JS errors)
  - Simulate a fetch failure for one endpoint → that chart section shows `ErrorAlert`; other two charts render normally
- [X] T026 [P] Verify back navigation: confirm MUI `Breadcrumbs` "Events" link in `EventAnalyticsPage.jsx` navigates to `/events` without using browser history; confirm the link is visible on mobile viewport (MUI `xs` breakpoint)
- [X] T027 [P] Update `CLAUDE.md` Recent Changes section: add entry `005-event-drilldown-dashboard: Drill-down analytics page — location + attendee type + daily trend charts, TicketTypeNameParser in Shared/Utilities`
- [X] T028 Sprint validation — run quickstart.md checklist:
  - [X] `dotnet build backend/EventPortal.sln` → 0 errors, 0 warnings
  - [X] `dotnet test backend/EventPortal.sln` → all tests pass (including new parser, service, and integration tests)
  - [X] `npm run build` → 0 errors
  - [X] `npm run lint` → 0 errors
  - [X] `GET /api/v1/events/{id}/registrations/location-breakdown` (with auth) → 200 with correct location groups
  - [X] `GET /api/v1/events/{id}/registrations/attendee-type-breakdown` (with auth) → 200 with Adult/Children/Other breakdown
  - [X] Unauthenticated requests to both new endpoints → 401
  - [X] `/events/{id}/analytics` renders all three charts with real synced data
  - [X] Clicking an event row on `/events` navigates to `/events/{id}/analytics`
  - [X] Breadcrumb on analytics page navigates back to `/events`
  - [X] "Last synced" timestamp visible on analytics page
  - [X] Empty-state message shown for event with zero registrations

---

## Dependencies & Execution Order

### Phase Dependencies

| Phase | Depends On |
|-------|-----------|
| Phase 1 (DTOs) | Sprint 2 complete |
| Phase 2 (Foundational) | Phase 1 |
| Phase 3 (US4 + US1) | Phase 2 complete (T003 parser not required for trend chart; T005 route required) |
| Phase 4 (US2 Backend) | Phase 2 (T003 parser required) |
| Phase 4 (US2 Frontend) | Phase 3 (EventAnalyticsPage shell) + Phase 4 backend |
| Phase 5 (US3 Backend) | Phase 2 (T003 parser required); can run in parallel with Phase 4 backend |
| Phase 5 (US3 Frontend) | Phase 3 (EventAnalyticsPage shell) + Phase 5 backend |
| Phase 6 (Polish) | All phases complete |

### User Story Dependencies

- **US4 + US1 (P1)**: Depends only on Phase 2. No dependency on US2 or US3.
- **US2 (P2)**: Depends on Phase 2 (parser). Backend can start in parallel with US1 frontend work.
- **US3 (P3)**: Depends on Phase 2 (parser). Backend can start in parallel with US1 and US2 frontend work.

### Within Each Phase

- T001 and T002 are fully parallel (different files)
- T003 (parser) must complete before T010 (US2 backend) and T017 (US3 backend)
- T007 (EventAnalyticsPage shell) must complete before T014 (US2 integration) and T021 (US3 integration)
- T011 (US2 endpoint) must complete before T016 (US2 integration tests)
- T018 (US3 endpoint) must complete before T023 (US3 integration tests)

---

## Parallel Opportunities

### Phase 2: Foundational

```
Parallel: T003 (parser impl) + T004 (parser tests) + T005 (route) + T006 (events list nav)
```

### Phase 3: US4 + US1

```
Parallel: T008 (RegistrationTrendChart) can be developed while T007 (page shell) is in progress
Sequential: T009 (integrate chart) depends on T007 and T008 both complete
```

### Phase 4: US2 — Backend + Frontend can be parallelised

```
Parallel backend: T010 (service) + T011 (controller) — after T010 interface is added
Parallel frontend: T012 (service.js) + T013 (LocationBreakdownChart) — after T010 is done
Parallel tests: T015 (unit tests) + T016 (integration tests) — after T010 and T011 complete
```

### Phase 5: US3 — Parallel with Phase 4 (if two developers available)

```
Developer A: Phase 4 (US2) complete set
Developer B: Phase 5 (US3) complete set — start T017/T018 while Developer A does T010/T011
```

---

## Implementation Strategy

### MVP First (US4 + US1 Only)

1. Complete Phase 1 (DTOs)
2. Complete Phase 2 (Foundational — parser, route, nav)
3. Complete Phase 3 (EventAnalyticsPage + trend chart)
4. **STOP and VALIDATE**: `/events/{id}/analytics` renders with trend chart, breadcrumb, timestamp
5. Demo: admin can drill into any event and see registration momentum

### Full Sprint Delivery

1. MVP (above)
2. Phase 4: Location breakdown → chart visible in analytics page
3. Phase 5: Attendee type breakdown → chart visible in analytics page
4. Phase 6: Polish and sprint sign-off

---

## Notes

- `[P]` = safe to parallelise — different files, no shared dependencies
- No EF Core migrations required — all queries operate on existing Sprint 2 tables
- `TicketTypeNameParser` is pure static C# — test it exhaustively before wiring into service methods
- Frontend empty states use existing `EmptyState` component from `frontend/src/components/feedback/EmptyState.jsx` (Sprint 0 scaffold)
- `EventAnalyticsPage` stubs out `getLocationBreakdown` and `getAttendeeTypeBreakdown` in T007 so the page works as MVP (US1) without waiting for US2/US3 backend
- The existing `GET /api/v1/events/{eventId}/registrations/daily-trends` endpoint (Sprint 2) is used as-is — no backend changes needed for US1
