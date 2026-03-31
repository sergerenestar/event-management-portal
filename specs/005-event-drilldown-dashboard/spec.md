# Feature Specification: Event Registration Drill-Down Dashboard

**Feature Branch**: `005-event-drilldown-dashboard`
**Created**: 2026-03-25
**Status**: Draft
**Input**: User description: "New feature: Event Registration Drill-Down Dashboard. When an admin clicks on an event in the Eventbrite Dashboard, it opens a dedicated dashboard page showing the registration progress for that event. The dashboard includes: (1) a time-series chart of registrations over time with adjustable daily granularity (daily view), (2) a breakdown of registrations by location shown as charts (bar or pie), (3) a breakdown of registrations by attendee type — adults vs children — shown as charts. All data comes from the locally synced Eventbrite registration data (no live API call on page load). The page should be reachable from the existing events list."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — View Registration Progress Over Time (Priority: P1)

An admin clicks on an event from the events list and arrives at the drill-down dashboard. They see a time-series chart showing the daily count of registrations over time. This gives the admin an at-a-glance picture of registration momentum — whether sign-ups are accelerating, plateauing, or spiking around specific promotion dates.

**Why this priority**: This is the primary analytical value of the page. Without a time-based view, the dashboard has no meaningful drill-down advantage over the existing event detail summary cards. It is the anchor chart from which all other breakdowns derive value.

**Independent Test**: Can be fully tested by navigating to `/events/{id}/analytics` and verifying the chart renders with daily-granularity data points pulled from the already-synced `DailyRegistrationSnapshots` table, without any new data collection required.

**Acceptance Scenarios**:

1. **Given** an event has synced registrations spanning multiple days, **When** an admin opens the drill-down dashboard, **Then** a chart displays one data point per day showing the registration count for that day.
2. **Given** the drill-down dashboard is open, **When** the admin views the chart, **Then** each data point on the X-axis is a calendar date and the Y-axis shows the registration count, making the trend readable without additional interaction.
3. **Given** an event has no registrations yet, **When** an admin opens the drill-down dashboard, **Then** the chart area shows an empty-state message ("No registration data available yet") rather than a blank or broken chart.
4. **Given** the drill-down dashboard is open, **When** the admin hovers over a data point, **Then** a tooltip shows the exact date and registration count for that day.

---

### User Story 2 — View Registrations by Location (Priority: P2)

On the same drill-down dashboard page, the admin sees a chart breaking down total registrations by attendee location — which branch, city, or region each registrant is coming from. This helps the admin understand geographic reach and identify which locations have the strongest or weakest turnout for this event.

**Why this priority**: Location breakdown is a key planning metric for CMFI Miracle Centre — knowing which branches or cities are registering helps organisers allocate resources. It comes second because it requires a data enrichment step (capturing location per attendee from Eventbrite) that the time-series chart does not.

**Independent Test**: Can be fully tested by verifying the location chart renders with correct groupings and counts for a synced event that has attendees with populated location data. Attendees without location data must be grouped under "Unknown."

**Acceptance Scenarios**:

1. **Given** synced registrations include location data for attendees, **When** the admin views the drill-down dashboard, **Then** a chart shows each distinct location as a labelled segment or bar with its registration count.
2. **Given** some attendees have no location recorded, **When** the admin views the location chart, **Then** those attendees are grouped under an "Unknown" label rather than being silently excluded.
3. **Given** a single location accounts for the majority of registrations, **When** the admin views the chart, **Then** the dominant location is visually prominent (largest bar or pie slice).
4. **Given** the location chart is displayed, **When** the admin reads it, **Then** the total of all location counts equals the total registration count shown in the summary cards.

---

### User Story 3 — View Registrations by Attendee Type: Adults vs Children (Priority: P3)

The drill-down dashboard shows a breakdown of registrations split between adults and children, displayed as a chart alongside raw counts. The admin can see at a glance what proportion of attendees are adults vs children — critical for event logistics such as children's programme capacity, safeguarding ratios, and catering.

**Why this priority**: The adult/children split is derivable from ticket type data already captured in Sprint 2. It is lower priority than time-series and location because it requires no new data capture — only a classification rule applied to existing data.

**Independent Test**: Can be fully tested independently by verifying the adult/children chart renders correctly for an event where ticket types include both adult-type and child-type named ticket types.

**Acceptance Scenarios**:

1. **Given** an event has ticket types including both adult-type and child-type tickets, **When** the admin views the drill-down dashboard, **Then** a chart shows the total registrations split between adults and children.
2. **Given** an event has only adult-type ticket types, **When** the admin views the chart, **Then** the chart shows 100% adults and zero children, with appropriate labelling.
3. **Given** a ticket type name does not clearly map to either adult or child, **When** the classification rule is applied, **Then** that ticket type is shown as "Other" and not silently dropped.
4. **Given** the adult/children chart is visible, **When** the admin reads it, **Then** the chart also displays the raw counts (e.g., "Adults: 245 · Children: 87") not just percentages.

---

### User Story 4 — Navigate to Drill-Down Dashboard from Events List (Priority: P1)

An admin on the events list page clicks on an event row and is taken directly to the drill-down analytics dashboard for that event. The page loads using only locally-synced data — no live Eventbrite API call is made on page load.

**Why this priority**: Navigation is the entry point for the entire feature. Without a working navigation path from the events list, none of the other stories are reachable. Tied at P1 with US1 because they are co-dependent.

**Independent Test**: Can be tested by clicking any event row in the events list and verifying the browser navigates to `/events/{id}/analytics` and the page renders without triggering a live Eventbrite call.

**Acceptance Scenarios**:

1. **Given** the admin is on the events list page, **When** they click on an event row, **Then** they are navigated to the drill-down dashboard for that event at `/events/{id}/analytics`.
2. **Given** the admin is on the drill-down dashboard, **When** the page loads, **Then** all chart data is served from locally synced registration records — no live external API call is made.
3. **Given** the admin is on the drill-down dashboard, **When** they want to return to the events list, **Then** a visible breadcrumb or back-navigation link is available.

---

### Edge Cases

- What happens when an event has registrations but none have location data? → Location chart shows all under "Unknown" with an informational note suggesting a registration sync.
- What happens when an event has zero registrations? → All three charts display an appropriate empty state; no broken or empty chart containers are shown.
- What happens when `DailyRegistrationSnapshots` contains gaps (missing days)? → The time-series chart renders only the days with data; gaps do not cause errors.
- What happens when a ticket type name contains both "adult" and "child" (e.g., "Adult + Child Family Pass")? → That ticket type is classified as "Other" to avoid double-counting.
- What happens when an event has more than 10 distinct locations? → The chart shows the top 10 by count and groups the remainder as "Other (N more)".

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a dedicated drill-down analytics page for each event at the route `/events/{id}/analytics`, accessible by clicking the event row on the events list.
- **FR-002**: The drill-down page MUST display a daily time-series chart of registration counts using data from `DailyRegistrationSnapshots`, requiring no live Eventbrite API call on page load.
- **FR-003**: The drill-down page MUST display a location breakdown chart showing registration counts grouped by attendee location.
- **FR-004**: Attendees without a recorded location MUST be grouped under an "Unknown" label in the location chart — they MUST NOT be silently excluded from the total count.
- **FR-005**: The location chart MUST cap visible segments at 10 distinct locations. Locations beyond the top 10 by count MUST be aggregated into an "Other" group.
- **FR-006**: The drill-down page MUST display an adult/children breakdown chart. Classification is determined by ticket type name: names containing "child" or "children" (case-insensitive) are classified as children; all other names are classified as adults unless they contain both "adult" and "child" simultaneously, in which case they are classified as "Other".
- **FR-007**: The system MUST expose a backend query endpoint returning location-grouped registration counts for a given event, sourced exclusively from locally synced data.
- **FR-008**: The system MUST expose a backend query endpoint returning adult/children-grouped registration counts for a given event, derived from ticket type name classification at query time.
- **FR-009**: The drill-down page MUST display a "Last synced" timestamp so the admin knows how current the data is.
- **FR-010**: The drill-down page MUST display a breadcrumb or back-navigation element allowing the admin to return to the events list without using the browser back button.
- **FR-011**: All drill-down data endpoints MUST require admin authentication. Unauthenticated requests MUST return 401.
- **FR-012**: Attendee location MUST be derived from the ticket type name at query time by parsing the location segment from a structured ticket name convention (e.g., "London Branch — Adult" → location: "London Branch"). No new database field is required on `Registration` or `TicketType`.
- **FR-013**: The location parsing rule MUST extract the portion of the ticket type name that precedes a known separator (dash, em-dash, or hyphen followed by an attendee type keyword such as "Adult" or "Child"). If no separator is found, the full ticket type name is used as the location label.

### Key Entities

- **Event**: Existing entity (Sprint 2). No structural changes required.
- **Registration**: Existing entity (Sprint 2). No structural changes required. Location is derived at query time from the associated ticket type name.
- **DailyRegistrationSnapshot**: Existing entity (Sprint 2). Used as-is for the time-series chart — no structural changes required.
- **TicketType**: Existing entity (Sprint 2). Used as-is for adult/children classification — classification is a query-time rule, not a persisted field.
- **LocationBreakdownDto** *(new, response only)*: A location group result — location label, registration count, percentage of total.
- **AttendeeTypeBreakdownDto** *(new, response only)*: An attendee type group result — label (Adult / Children / Other), count, percentage of total.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The drill-down dashboard loads and renders all three charts within 3 seconds on an event with up to 5,000 registrations, using only locally synced data.
- **SC-002**: An admin can navigate from the events list to the drill-down dashboard and view all three charts within 2 clicks.
- **SC-003**: The location chart reflects 100% of synced registrations — the sum of all location group counts equals the event's total registration count with zero records silently omitted.
- **SC-004**: The adult/children breakdown correctly classifies registrations for ticket types named exactly "Adult", "Child", or "Children" with zero misclassification.
- **SC-005**: The time-series chart renders without errors for events with registration data spanning any date range from 1 day to 365 days.
- **SC-006**: All three drill-down data endpoints return a 401 response for unauthenticated requests and return data within 2 seconds for events with up to 5,000 registrations.

---

## Assumptions

- **A-001**: The `DailyRegistrationSnapshot` table (populated by the existing `SnapshotAggregatorJob` from Sprint 2) is the authoritative source for the time-series chart. No new aggregation job is needed.
- **A-002**: Adult/children classification is a query-time rule applied to ticket type names. No new database column is needed on `TicketType`.
- **A-003**: The drill-down page lives at `/events/{id}/analytics`, distinct from the existing event detail page at `/events/{id}`. Both pages coexist and complement each other.
- **A-004**: Location is encoded in the ticket type name using a structured naming convention (e.g., "London Branch — Adult"). No changes to the sync process or database schema are required. Location is extracted at query time by parsing the ticket type name.
- **A-005**: Location labels are the text segment of the ticket type name that precedes the attendee-type keyword separator. Complex multi-level location hierarchies are out of scope.
- **A-006**: The Recharts charting library already installed in Sprint 2 is used for all charts on this page.

---

## Out of Scope

- Date range filtering on the time-series chart (only full-history daily view is in scope).
- Export of chart data to CSV or PDF (covered by the future Reports module).
- Real-time live chart updates without a manual page refresh or sync trigger.
- Per-location or per-attendee-type drill-down (clicking a chart segment to filter further).
- Age-based adult/children classification (classification is by ticket type name only, not by attendee date of birth).
