# Data Model: Event Registration Drill-Down Dashboard

**Feature**: `005-event-drilldown-dashboard`
**Date**: 2026-03-25

## Summary

No new database tables or EF Core migrations are required. This feature is entirely read-only: it adds query methods on top of three existing Sprint 2 tables (`TicketTypes`, `Registrations`, `DailyRegistrationSnapshots`). All new data shapes are expressed as query-result DTOs only.

---

## Existing Entities Used (read-only, no changes)

### TicketType *(Modules/Events/Entities/TicketType.cs)*

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int | PK |
| `EventId` | int | FK → Events.Id |
| `Name` | nvarchar(256) | **Key field** — location and attendee type are parsed from this at query time |
| `QuantitySold` | int | Used for adult/children totals |
| `Capacity` | int | Not used by this feature |

**Name parsing convention**: Ticket type names follow the pattern `"Location — AttendeeType"` using an em-dash, en-dash, or hyphen-space separator. Examples:
- `"London Branch — Adult"` → location: `"London Branch"`, type: `Adult`
- `"Manchester — Children"` → location: `"Manchester"`, type: `Children`
- `"General Admission"` → location: `"General Admission"`, type: `Adult` (fallback when no separator found)

---

### Registration *(Modules/Registrations/Entities/Registration.cs)*

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int | PK |
| `EventId` | int | FK → Events.Id |
| `TicketTypeId` | int | FK → TicketTypes.Id — join to get Name for parsing |
| `RegisteredAt` | datetime2 | Not directly used by this feature |

No changes. Location and attendee type are resolved via the joined `TicketType.Name`.

---

### DailyRegistrationSnapshot *(Modules/Registrations/Entities/DailyRegistrationSnapshot.cs)*

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int | PK |
| `EventId` | int | FK → Events.Id |
| `TicketTypeId` | int | FK → TicketTypes.Id |
| `SnapshotDate` | date | X-axis value for the time-series chart |
| `RegistrationCount` | int | Y-axis value for the time-series chart |

No changes. Already populated by the Sprint 2 `SnapshotAggregatorJob`.

---

## New Query-Result DTOs

### LocationBreakdownDto *(Modules/Registrations/Dtos/LocationBreakdownDto.cs)*

Represents one location group in the location breakdown chart response.

| Field | Type | Notes |
|-------|------|-------|
| `Location` | string | Parsed location label (e.g., "London Branch") |
| `Count` | int | Total registrations from this location |
| `Percentage` | decimal | Rounded to 1 decimal place; calculated server-side |

**Ordering**: Descending by `Count`. The top 10 are returned individually; the remainder are collapsed into a single entry with `Location = "Other"` by the service layer.

---

### AttendeeTypeBreakdownDto *(Modules/Registrations/Dtos/AttendeeTypeBreakdownDto.cs)*

Represents one attendee type group in the adult/children breakdown chart response.

| Field | Type | Notes |
|-------|------|-------|
| `AttendeeType` | string | One of: `"Adult"`, `"Children"`, `"Other"` |
| `Count` | int | Total registrations of this type |
| `Percentage` | decimal | Rounded to 1 decimal place; calculated server-side |

**Classification rules** (applied to `TicketType.Name`, case-insensitive):
- Contains `"child"` or `"children"` → `"Children"`
- Contains both `"adult"` and `"child"` simultaneously → `"Other"`
- All other names → `"Adult"`

---

## Name Parsing Helper

### TicketTypeNameParser *(Modules/Shared/Utilities/TicketTypeNameParser.cs)*

A static helper class in the Shared module. Placed in Shared (not Registrations) so it can be reused by Campaigns and Reports modules if they need to classify ticket types. Used by `RegistrationService` for both breakdown queries.

**Methods**:

| Method | Signature | Returns |
|--------|-----------|---------|
| `ParseLocation` | `string ParseLocation(string ticketTypeName)` | Location prefix before separator, or full name if no separator |
| `ParseAttendeeType` | `string ParseAttendeeType(string ticketTypeName)` | One of `"Adult"`, `"Children"`, `"Other"` |

**Separator detection**: Splits on ` — `, ` – `, or ` - ` (em-dash, en-dash, hyphen with spaces). Takes the last match to handle names like `"London — North — Adult"` correctly (location: `"London — North"`, type: `"Adult"`).

---

## Query Patterns

### Location Breakdown Query

```
1. Load all TicketTypes for the event (small set, fits in memory)
2. For each TicketType, parse the location label using TicketTypeNameParser
3. Load Registration counts grouped by TicketTypeId for the event
4. Join in memory: sum counts per parsed location label
5. Sort descending; take top 10; aggregate remainder → "Other"
6. Calculate percentages
```

**Why client-side grouping**: EF Core cannot translate string-parsing logic (split on separator) to SQL. With a maximum of ~20 ticket types per event, loading them into memory is trivially cheap.

### Attendee Type Breakdown Query

```
1. Load all TicketTypes for the event (same fetch as above, can reuse)
2. For each TicketType, classify attendee type using TicketTypeNameParser
3. Load Registration counts grouped by TicketTypeId for the event
4. Join in memory: sum counts per attendee type classification
5. Calculate percentages
```

### Daily Trend Query

Already implemented in Sprint 2 via `GetDailyTrendsAsync`. No changes needed. The existing `DailySnapshotDto` response is used as-is by the time-series chart.

---

## EF Core Migration

**No migration required.** All queries operate on existing tables. No schema changes.
