# API Contract: Attendee Type Breakdown

**Endpoint**: `GET /api/v1/events/{eventId}/registrations/attendee-type-breakdown`
**Module**: Registrations
**Sprint**: 005-event-drilldown-dashboard

## Purpose

Returns registration counts split by attendee type (Adult / Children / Other) for a given event. Classification is determined at query time by inspecting the ticket type name (case-insensitive keyword matching). Used to populate the adults vs children chart on the drill-down analytics page.

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
  "breakdown": [
    {
      "attendeeType": "Adult",
      "count": 245,
      "percentage": 73.8
    },
    {
      "attendeeType": "Children",
      "count": 82,
      "percentage": 24.7
    },
    {
      "attendeeType": "Other",
      "count": 5,
      "percentage": 1.5
    }
  ]
}
```

**Response fields**:

| Field | Type | Notes |
|-------|------|-------|
| `eventId` | int | Echoes the requested event ID |
| `totalRegistrations` | int | Sum of all `count` values |
| `lastSyncedAt` | ISO 8601 datetime | Timestamp of the most recent registration sync; `null` if never synced |
| `breakdown` | array | Always 1–3 entries: Adult, Children, Other (omitted if count is 0) |
| `breakdown[].attendeeType` | string | One of `"Adult"`, `"Children"`, `"Other"` |
| `breakdown[].count` | int | Total registrations of this type |
| `breakdown[].percentage` | decimal | Percentage of `totalRegistrations`, rounded to 1 decimal place |

**Ordering**: Fixed — Adult first, then Children, then Other.

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
  "breakdown": []
}
```

---

## Classification Rules

Applied to `TicketType.Name` (case-insensitive):

| Condition | Assigned Type |
|-----------|--------------|
| Name contains `"child"` OR `"children"` (but NOT also "adult") | `"Children"` |
| Name contains both `"adult"` AND `"child"`/`"children"` simultaneously | `"Other"` |
| All other names | `"Adult"` |

**Examples**:

| Ticket Type Name | Classified As |
|-----------------|---------------|
| `"London Branch — Adult"` | Adult |
| `"Manchester — Children"` | Children |
| `"Birmingham — Child"` | Children |
| `"Adult + Child Family Pass"` | Other |
| `"General Admission"` | Adult |
| `"VIP"` | Adult |

---

## Behaviour Rules

1. Entries with a `count` of 0 are omitted from the `breakdown` array entirely.
2. The classification rule is applied per ticket type; registrations are then summed per classification.
3. All `percentage` values in the response sum to 100.0 (within rounding tolerance of ±0.1).
4. No live Eventbrite API call is made. All data comes from the local database.
