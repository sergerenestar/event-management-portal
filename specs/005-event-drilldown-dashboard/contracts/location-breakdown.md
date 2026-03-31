# API Contract: Location Breakdown

**Endpoint**: `GET /api/v1/events/{eventId}/registrations/location-breakdown`
**Module**: Registrations
**Sprint**: 005-event-drilldown-dashboard

## Purpose

Returns registration counts grouped by parsed location label for a given event. Location is derived at query time from the ticket type name convention `"Location — AttendeeType"`. Used to populate the location breakdown chart on the drill-down analytics page.

---

## Request

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `eventId` | int | Yes | The internal ID of the event |

### Headers

| Header | Value | Required |
|--------|-------|----------|
| `Authorization` | `Bearer <access_token>` | Yes — admin JWT |
| `X-Correlation-Id` | GUID string | No — generated if absent |

### Query Parameters

None.

---

## Response

### 200 OK — Success

```json
{
  "eventId": 42,
  "totalRegistrations": 332,
  "lastSyncedAt": "2026-03-24T18:00:00Z",
  "locations": [
    {
      "location": "London Branch",
      "count": 145,
      "percentage": 43.7
    },
    {
      "location": "Manchester",
      "count": 87,
      "percentage": 26.2
    },
    {
      "location": "Birmingham",
      "count": 61,
      "percentage": 18.4
    },
    {
      "location": "Other",
      "count": 39,
      "percentage": 11.7
    }
  ]
}
```

**Response fields**:

| Field | Type | Notes |
|-------|------|-------|
| `eventId` | int | Echoes the requested event ID |
| `totalRegistrations` | int | Sum of all `count` values (including "Other" and "Unknown") |
| `lastSyncedAt` | ISO 8601 datetime | Timestamp of the most recent registration sync for this event |
| `locations` | array | Up to 11 entries: top 10 by count + 1 "Other" aggregation |
| `locations[].location` | string | Parsed location label, or `"Unknown"` if no location could be derived |
| `locations[].count` | int | Total registrations from this location |
| `locations[].percentage` | decimal | Percentage of `totalRegistrations`, rounded to 1 decimal place |

**Ordering**: Descending by `count`. The `"Other"` and `"Unknown"` entries are always placed last regardless of count.

---

### 401 Unauthorized

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Unauthorized",
  "status": 401
}
```

### 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Event not found",
  "status": 404,
  "detail": "No event with ID 42 exists."
}
```

### 200 OK — Event exists but has no registrations

```json
{
  "eventId": 42,
  "totalRegistrations": 0,
  "lastSyncedAt": null,
  "locations": []
}
```

---

## Behaviour Rules

1. Registrations are grouped by the location parsed from `TicketType.Name`. If the name contains a separator (` — `, ` – `, ` - `), the portion before the last separator is the location label.
2. If no separator is found in the ticket type name, the full ticket type name is used as the location label.
3. The top 10 locations by count are returned individually. All remaining locations are summed and returned as a single entry with `location = "Other"`.
4. If any registrations cannot be linked to a ticket type with a parseable location, they are grouped as `"Unknown"`.
5. The `lastSyncedAt` value is derived from the most recent `Registration.CreatedAt` (or equivalent sync timestamp) for the event. It is `null` if the event has never been synced.
6. No live Eventbrite API call is made. All data comes from the local database.
