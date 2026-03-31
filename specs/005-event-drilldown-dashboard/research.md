# Research: Event Registration Drill-Down Dashboard

**Feature**: `005-event-drilldown-dashboard`
**Date**: 2026-03-25

---

## Topic 1: EF Core Aggregation — Server-Side vs Client-Side GroupBy

### Decision
Use **two-step query**: server-side query to get registration counts grouped by `TicketTypeId`, followed by client-side join to ticket type names for parsing and label grouping.

### Rationale
EF Core 8 can translate simple `GroupBy` → `Count()` projections to SQL (`SELECT TicketTypeId, COUNT(*) FROM Registrations GROUP BY TicketTypeId WHERE EventId = @id`). However, it cannot translate string-manipulation logic (split on separator, substring extraction) to SQL. The correct pattern is:

1. **Server-side** (SQL): `GROUP BY TicketTypeId` to get `{ TicketTypeId, Count }` pairs — this is the expensive operation that benefits from SQL indexes.
2. **Client-side** (C# in-memory): Load the event's ticket types (always a small set, typically ≤ 20), parse names using `TicketTypeNameParser`, and join the counts to the parsed labels.

This avoids the EF Core `client-side evaluation` warning in .NET 6+ (which throws by default if you accidentally trigger full table loads). With ≤ 5,000 registrations and ≤ 20 ticket types, the in-memory join is negligible.

### Alternatives Considered
- **Store location/type as columns on TicketType**: Rejected — the spec explicitly requires no new DB columns; naming convention is the source of truth.
- **SQL `CASE WHEN Name LIKE '%Child%'`**: Could work for attendee-type, but string-based SQL conditions are fragile and can't be reused in unit-testable C# logic. Parser helper is cleaner.

---

## Topic 2: Ticket Type Name Parsing Strategy

### Decision
**Static helper class** `TicketTypeNameParser` placed in `Modules/Shared/Utilities/` with two public methods: `ParseLocation(string)` and `ParseAttendeeType(string)`. Located in Shared (not Registrations) for potential reuse by Campaigns and Reports modules.

### Rationale
The parsing logic is pure string manipulation with no side effects and no dependencies — a static class is the simplest appropriate choice. Compared to a value object or extension method:
- A value object would be justified if the parsed result needed to carry behaviour or validation — unnecessary here.
- Extension methods would work but obscure intent; a named helper class is more discoverable.

**Parsing algorithm**:

```
ParseLocation(name):
  separators = [" — ", " – ", " - "]   // em-dash, en-dash, hyphen with spaces
  Find the LAST occurrence of any separator in name
  If found: return name.Substring(0, separatorIndex).Trim()
  Else: return name.Trim()   // full name = location label

ParseAttendeeType(name):
  lower = name.ToLowerInvariant()
  hasChild  = lower.Contains("child") OR lower.Contains("children")
  hasAdult  = lower.Contains("adult")
  If hasChild AND hasAdult: return "Other"
  If hasChild:              return "Children"
  Else:                     return "Adult"
```

**Why last separator**: Handles names like `"London — North Branch — Adult"` correctly — the location is `"London — North Branch"` and the type is `"Adult"`.

### Alternatives Considered
- **Regex**: More flexible but harder to read and maintain for simple separator logic. Regex compilation overhead is unnecessary for this use case.
- **Convention enforcement at sync time**: Normalising ticket type names on import was considered but rejected — it would mutate source data and add risk to the existing sync job.

---

## Topic 3: Recharts Component Selection

### Time-Series Chart — Daily Registration Count

**Decision**: `LineChart` — consistent with the existing `DailyTrendChart.jsx` already implemented in Sprint 2.

**Rationale**: The Sprint 2 `DailyTrendChart.jsx` already uses `LineChart` with multiple series (one `Line` per ticket type), `ResponsiveContainer`, `Tooltip`, `Legend`, `XAxis`, and `YAxis`. Reusing this component directly or adapting it is the right call — it shows trend direction clearly, supports multiple overlapping series without confusion, and is already proven in the project. An `AreaChart` would have been appropriate for proportion/stacking but is redundant here and inconsistent with existing patterns.

### Location Breakdown Chart — Up to 10+ Locations

**Decision**: `BarChart` (horizontal orientation — `layout="vertical"`).

**Rationale**: With up to 10 distinct locations plus "Other", a horizontal bar chart is far more readable than a pie chart:
- Bar labels (location names) fit naturally on the Y-axis without truncation or legend clutter.
- Relative magnitudes are easier to compare across bars than across pie slices.
- The Recharts `BarChart` with `layout="vertical"` handles long label text on the Y-axis cleanly.

A pie chart is appropriate for 2–5 segments; beyond 5 segments it becomes difficult to read. Given the cap of 10 + "Other" = up to 11 segments, a bar chart is the clear winner.

### Adults vs Children Chart — 2–3 Categories

**Decision**: `PieChart` with inner radius (donut style).

**Rationale**: With only 2–3 categories (Adult, Children, Other), a pie/donut chart is the ideal representation — proportional comparison between a small number of categories is exactly the pie chart's strength. A donut chart (non-zero `innerRadius`) allows the raw counts to be displayed in the centre or via a custom label, satisfying FR requirement to show both counts and percentages. Recharts `PieChart` with `Cell` components for MUI-consistent colours.

---

## Topic 4: Frontend Route and Page Structure

### Decision
New route `/events/:eventId/analytics` renders `EventAnalyticsPage.jsx`. The existing `/events/:eventId` (event detail) and the new analytics page coexist; the analytics page is the deeper drill-down.

### Navigation Entry Points
- Events list (`/events`) → click row → `/events/{id}/analytics` *(this feature)*
- The existing event detail page (`/events/{id}`) can optionally link to the analytics page via an "Analytics" tab or button (implementation detail for tasks phase).

### Data Fetching
`Promise.all` on page mount to fire all three requests in parallel:
1. `GET /api/v1/events/{id}/registrations/daily-trends` (existing)
2. `GET /api/v1/events/{id}/registrations/location-breakdown` (new)
3. `GET /api/v1/events/{id}/registrations/attendee-type-breakdown` (new)
4. `GET /api/v1/events/{id}` (existing — for event name in breadcrumb)

This keeps page load to a single round-trip latency window.

---

## Topic 5: Empty State and Error Handling

### Decision
Use a shared `EmptyState` component (already exists from Sprint 0) per chart area. Show a global `ErrorAlert` (also Sprint 0) if any data fetch fails.

### Chart-Level Empty States
- Zero data points: render `EmptyState` inside the chart's `ResponsiveContainer`
- Loading: render MUI `Skeleton` in the chart container height (same pattern as EventDetailPage)
- Error on individual endpoint: show inline `ErrorAlert` for that chart only; other charts remain functional

---

## Summary of Decisions

| Topic | Decision |
|-------|----------|
| EF Core aggregation | Server-side GROUP BY TicketTypeId; client-side label parsing |
| Name parsing | Static `TicketTypeNameParser` helper; last-separator split |
| Time-series chart | Recharts `LineChart` — reuses existing `DailyTrendChart.jsx` pattern |
| Location chart | Recharts `BarChart` (vertical layout, horizontal bars) |
| Adult/Children chart | Recharts `PieChart` (donut style) with centre count label |
| Data fetching | `Promise.all` on page mount; 4 parallel requests |
| Empty/error states | Per-chart `EmptyState` and `ErrorAlert` from Sprint 0 shared components |
