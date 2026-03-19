# Tasks: Sprint 2 — Eventbrite Integration and Registration Dashboard

**Input**: `README_event_management_portal.md`, Sprint 1 complete
**Prerequisites**: Sprint 1 ✅ · Auth working ✅ · `AdminUsers` + `RefreshTokens` tables ✅
**Sprint Goal**: Admin can sync events from Eventbrite, view registration totals by ticket type, and see daily registration trend charts on the dashboard.

---

## User Story Map

| Story | Deliverable | Done When |
|---|---|---|
| US1 | Eventbrite connector — events, ticket types, orders, attendees | `POST /api/v1/events/sync` returns synced event list |
| US2 | Event sync background job | Hangfire job syncs events on schedule |
| US3 | Registration snapshot aggregator | Daily snapshot job populates `DailyRegistrationSnapshots` |
| US4 | Dashboard query endpoints | Summary, by-ticket-type, and daily-trends endpoints return data |
| US5 | React events list + event detail page | Events list shows all synced events |
| US6 | Ticket type totals widgets | Event detail shows registration count per ticket type |
| US7 | Daily registration trend chart | Recharts line chart shows registrations per day per ticket type |

---

## Phase 1: Backend — Entities and Migration

**Purpose**: Create all new EF Core entities for Sprint 2 and generate a single migration before any service code is written.

- [x] T001 Create `Modules/Events/Entities/Event.cs`:
  ```
  Id                     int, PK
  ExternalEventbriteId   nvarchar(128), unique index
  Name                   nvarchar(512)
  Slug                   nvarchar(256)
  StartDate              datetime2
  EndDate                datetime2
  Venue                  nvarchar(512)
  Status                 nvarchar(64)
  ThumbnailUrl           nvarchar(1024)
  CreatedAt              datetime2
  UpdatedAt              datetime2
  TicketTypes            ICollection navigation
  Registrations          ICollection navigation
  ```
- [x] T002 [P] Create `Modules/Events/Entities/TicketType.cs`:
  ```
  Id                     int, PK
  EventId                int, FK → Events.Id
  ExternalTicketClassId  nvarchar(128)
  Name                   nvarchar(256)
  Price                  decimal(18,2)
  Currency               nvarchar(8)
  Capacity               int
  QuantitySold           int
  CreatedAt              datetime2
  UpdatedAt              datetime2
  Event                  navigation property
  Registrations          ICollection navigation
  ```
- [x] T003 [P] Create `Modules/Registrations/Entities/Registration.cs`:
  ```
  Id                     int, PK
  EventId                int, FK → Events.Id
  TicketTypeId           int, FK → TicketTypes.Id
  ExternalOrderId        nvarchar(128)
  ExternalAttendeeId     nvarchar(128)
  AttendeeName           nvarchar(256)
  AttendeeEmail          nvarchar(256)
  RegisteredAt           datetime2
  CheckInStatus          nvarchar(64)
  SourceSystem           nvarchar(64)
  Event                  navigation property
  TicketType             navigation property
  ```
- [x] T004 [P] Create `Modules/Registrations/Entities/DailyRegistrationSnapshot.cs`:
  ```
  Id                     int, PK
  EventId                int, FK → Events.Id
  TicketTypeId           int, FK → TicketTypes.Id
  SnapshotDate           date
  RegistrationCount      int
  Event                  navigation property
  TicketType             navigation property
  ```
- [x] T005 Register all 4 entities in `AppDbContext`:
  - `DbSet<Event> Events`
  - `DbSet<TicketType> TicketTypes`
  - `DbSet<Registration> Registrations`
  - `DbSet<DailyRegistrationSnapshot> DailyRegistrationSnapshots`
  - `OnModelCreating`: unique index on `Events.ExternalEventbriteId`; index on `Registrations.EventId`; index on `DailyRegistrationSnapshots.EventId + SnapshotDate`; cascade deletes from Event to TicketTypes, Registrations, Snapshots
- [x] T006 Generate EF Core migration:
  ```bash
  cd backend
  dotnet ef migrations add AddEventbriteEntities \
    --project src/EventPortal.Api \
    --startup-project src/EventPortal.Api \
    --output-dir Modules/Events/Migrations
  ```
- [x] T007 Run migration locally:
  ```bash
  dotnet ef database update \
    --project src/EventPortal.Api \
    --startup-project src/EventPortal.Api
  ```

**Checkpoint**: `dotnet build backend/` — 0 errors. All 4 tables visible in SSMS.

---

## Phase 2: Backend — Eventbrite API Client

**Purpose**: HTTP client for Eventbrite REST API, isolated behind an interface for testability.

- [x] T008 Add to `appsettings.json`:
  ```json
  "Eventbrite": {
    "ApiToken": "",
    "OrganizationId": "",
    "BaseUrl": "https://www.eventbriteapi.com/v3"
  }
  ```
- [x] T009 [P] Add same keys to `appsettings.Development.json.example` with placeholder values
- [x] T010 Create `Modules/Events/Integrations/EventbriteModels.cs` — response DTOs:
  - `EventbriteEvent`: `id`, `name.text`, `start.utc`, `end.utc`, `venue`, `status`, `logo.url`
  - `EventbriteTicketClass`: `id`, `name`, `cost.value`, `currency`, `capacity`, `quantity_sold`
  - `EventbriteOrder`: `id`, attendees list
  - `EventbriteAttendee`: `id`, `profile.name`, `profile.email`, `checked_in`, `created`
  - `EventbritePagedResponse<T>`: `pagination.page_count`, `pagination.page_number`, data list
- [x] T011 Create `Modules/Events/Integrations/IEventbriteClient.cs`:
  ```csharp
  Task<List<EventbriteEvent>> GetEventsAsync(string organizationId);
  Task<List<EventbriteTicketClass>> GetTicketClassesAsync(string eventId);
  Task<List<EventbriteOrder>> GetOrdersAsync(string eventId);
  Task<List<EventbriteAttendee>> GetAttendeesAsync(string eventId);
  ```
- [x] T012 Implement `Modules/Events/Integrations/EventbriteClient.cs`:
  - Bearer token auth from `Eventbrite:ApiToken`
  - `GetEventsAsync` — `GET /organizations/{orgId}/events/?expand=venue&page_size=50` with pagination loop
  - `GetTicketClassesAsync` — `GET /events/{eventId}/ticket_classes/`
  - `GetOrdersAsync` — `GET /events/{eventId}/orders/?expand=attendees&page_size=100` with pagination loop
  - `GetAttendeesAsync` — `GET /events/{eventId}/attendees/` with pagination loop
  - Retry once on 429 (rate limit) with 1s delay; log errors with correlation ID
- [x] T013 Register in `Program.cs`: `builder.Services.AddHttpClient<IEventbriteClient, EventbriteClient>()`

**Checkpoint**: `dotnet build backend/` — 0 errors.

---

## Phase 3: Backend — Event Service and Sync Logic

- [x] T014 Create `Modules/Events/Repositories/IEventRepository.cs`:
  ```csharp
  Task<List<Event>> GetAllAsync();
  Task<Event?> GetByIdAsync(int id);
  Task<Event?> GetByExternalIdAsync(string externalId);
  Task UpsertAsync(Event ev);
  Task UpsertTicketTypeAsync(TicketType ticketType);
  ```
- [x] T015 [P] Create `Modules/Events/Repositories/EventRepository.cs`:
  - `UpsertAsync` — find by `ExternalEventbriteId`; update if exists, insert if not
  - `UpsertTicketTypeAsync` — find by `ExternalTicketClassId + EventId`; same upsert pattern
- [x] T016 Update `Modules/Events/Services/IEventService.cs`:
  ```csharp
  Task<List<Event>> GetEventsAsync();
  Task<Event?> GetEventByIdAsync(int id);
  Task SyncEventsAsync();
  Task SyncEventDetailAsync(int eventId);
  ```
- [x] T017 Implement `Modules/Events/Services/EventService.cs`:
  - `GetEventsAsync` — return all events from DB ordered by `StartDate` desc
  - `GetEventByIdAsync` — return event with ticket types included
  - `SyncEventsAsync` — call Eventbrite, upsert all events and their ticket classes, audit log `"EventSync"`
  - `SyncEventDetailAsync` — sync orders and attendees for one event, update `QuantitySold`
- [x] T018 Register `EventRepository` and `EventService` in `Program.cs`

**Checkpoint**: `dotnet build backend/` — 0 errors.

---

## Phase 4: Backend — Registration Service and Jobs

- [x] T019 Update `Modules/Registrations/Services/IRegistrationService.cs`:
  ```csharp
  Task<RegistrationSummaryDto> GetSummaryAsync(int eventId);
  Task<List<TicketTypeSummaryDto>> GetByTicketTypeAsync(int eventId);
  Task<List<DailySnapshotDto>> GetDailyTrendsAsync(int eventId);
  Task SyncRegistrationsAsync(int eventId);
  Task AggregateSnapshotsAsync(int eventId);
  ```
- [x] T020 [P] Create DTOs:
  - `RegistrationSummaryDto` — `TotalRegistrations`, `TotalCapacity`, `FillRate`, `LastSyncAt`
  - `TicketTypeSummaryDto` — `TicketTypeId`, `Name`, `QuantitySold`, `Capacity`, `Price`, `Currency`
  - `DailySnapshotDto` — `Date`, `Count`, `TicketTypeName`, `TicketTypeId`
- [x] T021 Implement `Modules/Registrations/Services/RegistrationService.cs`:
  - `GetSummaryAsync` — aggregate sold/capacity across all ticket types for event
  - `GetByTicketTypeAsync` — per-ticket breakdown with fill percentage
  - `GetDailyTrendsAsync` — query snapshots ordered by date asc, grouped by ticket type
  - `AggregateSnapshotsAsync` — count registrations per day per ticket type, upsert snapshots (idempotent)
- [x] T022 [P] Create `Modules/Events/Jobs/EventSyncJob.cs`: calls `IEventService.SyncEventsAsync()`
- [x] T023 [P] Create `Modules/Registrations/Jobs/SnapshotAggregatorJob.cs`: loops all events, calls `AggregateSnapshotsAsync` for each
- [x] T024 Register recurring jobs in `JobRegistry.cs`:
  - `event-sync` — hourly
  - `snapshot-aggregator` — daily

**Checkpoint**: `dotnet build backend/` — 0 errors.

---

## Phase 5: Backend — Controllers

- [x] T025 Implement `Modules/Events/Controllers/EventsController.cs`:
  - `[Authorize(Policy = "AdminOnly")]`
  - `GET /api/v1/events` → list all events → `200 List<EventSummaryDto>`
  - `GET /api/v1/events/{id}` → event detail → `200 EventDetailDto` or `404`
  - `POST /api/v1/events/sync` → enqueue `EventSyncJob` → `202`
- [x] T026 [P] Create `EventSummaryDto` and `EventDetailDto` in `Modules/Events/Dtos/`
- [x] T027 Implement `Modules/Registrations/Controllers/RegistrationsController.cs`:
  - `[Authorize]`
  - `GET /api/v1/events/{eventId}/registrations/summary` → `200 RegistrationSummaryDto`
  - `GET /api/v1/events/{eventId}/registrations/by-ticket-type` → `200 List<TicketTypeSummaryDto>`
  - `GET /api/v1/events/{eventId}/registrations/daily-trends` → `200 List<DailySnapshotDto>`
  - `POST /api/v1/events/{eventId}/registrations/sync` → enqueue sync → `202`

**Checkpoint**: `dotnet build backend/` — 0 errors. All 6 endpoints visible in Swagger with padlock.

---

## Phase 6: Backend — Unit Tests

- [x] T028 [P] Create `tests/EventPortal.Tests/Events/EventServiceTests.cs`:
  - Sync returns 2 events → both upserted
  - Existing event → updated not duplicated
  - `GetEventsAsync` → ordered by StartDate desc
- [x] T029 [P] Create `tests/EventPortal.Tests/Registrations/RegistrationServiceTests.cs`:
  - `GetSummaryAsync` → correct fill rate calculation
  - `GetDailyTrendsAsync` → ordered by date asc
  - `AggregateSnapshotsAsync` → idempotent — running twice produces same result

**Checkpoint**: `dotnet test backend/` → all tests pass.

---

## Phase 7: Frontend — Services

- [x] T030 Implement `frontend/src/services/eventService.js`:
  - `getEvents()`, `getEventById(id)`, `syncEvents()`
- [x] T031 [P] Implement `frontend/src/services/registrationService.js`:
  - `getSummary(eventId)`, `getByTicketType(eventId)`, `getDailyTrends(eventId)`, `syncRegistrations(eventId)`

**Checkpoint**: `npm run build` — 0 errors.

---

## Phase 8: Frontend — Events List Page

- [x] T032 Implement `frontend/src/features/events/EventsListPage.jsx`:
  - MUI `Table`: thumbnail, name, date, venue, status chip, total registrations
  - "Sync from Eventbrite" button → `syncEvents()` → success snackbar
  - MUI `Skeleton` loading state; `EmptyState` when empty
  - Row click → navigate to `/events/{id}`
- [x] T033 [P] Create `frontend/src/features/events/EventStatusChip.jsx`:
- make  everything visualy appealing but not flashy
  - MUI `Chip` — `live` = green, `ended` = gray, `draft` = amber, `cancelled` = red

**Checkpoint**: `/events` renders after sync.

---

## Phase 9: Frontend — Event Detail Page

- [x] T034 Implement `frontend/src/features/events/EventDetailPage.jsx`:
  - `Promise.all` — fetch event, summary, ticket types, daily trends in parallel
  - Top section: event name, dates, venue, status
  - 3 stat cards: total registrations, capacity, fill rate %
  - Ticket types table + daily trend chart
  - "Sync Registrations" button
  - make  everything visualy appealing but not flashy
- [x] T035 [P] Create `frontend/src/features/events/TicketTypesTable.jsx`:
  - MUI `Table` with fill % as `LinearProgress` bar
- [x] T036 [P] Create `frontend/src/features/events/DailyTrendChart.jsx`:
  - Recharts `LineChart` — one `Line` per ticket type
  - `ResponsiveContainer width="100%" height={320}`
  - `Tooltip` + `Legend` + `XAxis` (date) + `YAxis` (count)

**Checkpoint**: `/events/{id}` renders with chart.

---

## Phase 10: Frontend — Dashboard Page

- [x] T037 Implement `frontend/src/features/dashboard/DashboardPage.jsx`:
  - Stat row: total events, total registrations, next upcoming event
  - Event grid: up to 6 `EventCard` components + "View all" link
- [x] T038 [P] Create `frontend/src/features/dashboard/EventCard.jsx`:
  - MUI `Card`: thumbnail, name, date, registration progress bar
  - make  everything visualy appealing but not flashy

**Checkpoint**: `/dashboard` shows real event data.

---

## Phase 11: Frontend — App Shell and Sidebar Navigation

- [x] T039 Update `frontend/src/components/layout/Sidebar.jsx`:
  - MUI permanent `Drawer` (240px)
  - Nav items: Dashboard, Events, Campaigns, Social Posts, Sessions, Reports
  - Active item highlighted via `useLocation()`
- [x] T040 [P] Update `frontend/src/components/layout/AppShell.jsx`:
  - Flex layout: Sidebar (240px) + content area
- [x] T041 Update `frontend/src/app/router/AppRouter.jsx`:
  - All protected routes wrapped in `<AppShell>`
  - make  everything visualy appealing but not flashy

**Checkpoint**: Sidebar visible on all protected pages. Navigation works.

---

## Phase 12: Docker and CI/CD Updates

- [x] T042 Update `docker-compose.yml`: add `Eventbrite__ApiToken` and `Eventbrite__OrganizationId` env vars
- [x] T043 [P] Update `.env.example`: `EVENTBRITE_API_TOKEN=` and `EVENTBRITE_ORGANIZATION_ID=`
- [x] T044 [P] Update `dev-deploy.yml` and `prod-deploy.yml`: add Eventbrite secrets to backend deploy env

---

## Phase 13: Sprint 2 Validation

- [ ] T045 Full checklist:
  - [ ] `dotnet build backend/EventPortal.sln` → 0 errors, 0 warnings
  - [ ] `dotnet test backend/EventPortal.sln` → all tests pass
  - [ ] `npm run build` → 0 errors
  - [ ] `npm run lint` → 0 errors
  - [ ] `POST /api/v1/events/sync` → 202, Hangfire job enqueued
  - [ ] `GET /api/v1/events` → synced event list returned
  - [ ] `GET /api/v1/events/{id}/registrations/summary` → summary returned
  - [ ] `GET /api/v1/events/{id}/registrations/by-ticket-type` → ticket breakdown returned
  - [ ] `GET /api/v1/events/{id}/registrations/daily-trends` → snapshot data returned
  - [ ] `/events` → event list renders after sync
  - [ ] `/events/{id}` → detail page with Recharts trend chart renders
  - [ ] `/dashboard` → real event data and stat cards
  - [ ] Sidebar navigation works on all pages
  - [ ] `Events`, `TicketTypes`, `DailyRegistrationSnapshots` tables populated in SSMS
- [ ] T046 [P] Update `README.md`: `Sprint 2 — Eventbrite + Registration Dashboard: ✅ Complete`
- [ ] T047 [P] Commit all work on branch `003-eventbrite-dashboard`

---

## Dependencies and Execution Order

| Phase | Depends On |
|---|---|
| Phase 1 (Entities + Migration) | Sprint 1 |
| Phase 2 (Eventbrite Client) | Phase 1 |
| Phase 3 (Event Service) | Phase 1 + Phase 2 |
| Phase 4 (Registration Service + Jobs) | Phase 3 |
| Phase 5 (Controllers) | Phase 3 + Phase 4 |
| Phase 6 (Tests) | Phase 3 + Phase 4 |
| Phase 7 (Frontend Services) | Phase 5 |
| Phase 8 (Events List) | Phase 7 |
| Phase 9 (Event Detail) | Phase 7 |
| Phase 10 (Dashboard) | Phase 7 |
| Phase 11 (App Shell + Nav) | Phase 8 + Phase 9 + Phase 10 |
| Phase 12 (Docker + CI/CD) | All phases |
| Phase 13 (Validation) | All phases |

### Parallel Opportunities

| Developer A — Backend | Developer B — Frontend |
|---|---|
| Phase 1: Entities + migration | Phase 11: App shell + sidebar |
| Phase 2: Eventbrite client | Phase 7: Service stubs with mock data |
| Phase 3: Event service | Phase 8: Events list (mock data) |
| Phase 4: Registration service | Phase 9: Event detail + chart |
| Phase 5: Controllers | Phase 10: Dashboard |
| Phase 6: Tests | — |

---

## Getting Your Eventbrite API Token

1. Go to [eventbrite.com/account-settings/apps](https://www.eventbrite.com/account-settings/apps)
2. Click **Create a New App** → copy the **Private Token**
3. Get your **Organization ID**:
   ```bash
   curl -H "Authorization: Bearer YOUR_TOKEN" \
     https://www.eventbriteapi.com/v3/users/me/organizations/
   ```

Add both to `.env` and `appsettings.Development.json`:
```json
"Eventbrite": {
  "ApiToken": "your-private-token",
  "OrganizationId": "your-org-id"
}
```

---

## Notes

- `[P]` = safe to parallelize — different files, no shared dependencies
- Eventbrite API is paginated — all list endpoints must follow `pagination.page_count` and loop through all pages
- `DailyRegistrationSnapshots` are append-only — upsert by `date + ticketTypeId`, never delete
- `AggregateSnapshotsAsync` must be idempotent — running twice on the same day produces the same result
- Sprint 3 (SMS Communication) depends on event data being available for campaign targeting
