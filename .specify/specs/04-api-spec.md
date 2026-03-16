# 04 — API Specification
# Event Management Portal — Endpoint Contracts

> Version: 1.0
> Status: Approved
> Audience: Backend contributors, frontend contributors, AI coding agents

---

## 1. API Design Principles

- Controllers handle routing and input validation only — no business logic
- Services contain all business logic — controllers call services
- Entities are never returned from controllers — DTOs only
- All endpoints require authentication (Bearer JWT) unless marked `[Public]`
- All write endpoints validate input using FluentValidation
- All error responses follow RFC 7807 Problem Details format
- API versioned via URL prefix: `/api/v1/`
- Pagination uses `?page=1&pageSize=20` on all list endpoints
- All datetime values in request/response are ISO 8601 UTC

---

## 2. Standard Response Shapes

### Success (single resource)
```json
{
  "data": { ... }
}
```

### Success (list)
```json
{
  "data": [ ... ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 142,
    "totalPages": 8
  }
}
```

### Error (RFC 7807)
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation failed",
  "status": 422,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "messageBody": ["Message body cannot exceed 1600 characters"]
  }
}
```

---

## 3. Auth Endpoints

### POST /api/v1/auth/login `[Public]`
Exchange an Entra External ID token for a portal JWT.

**Request:**
```json
{
  "entraIdToken": "string"
}
```

**Response 200:**
```json
{
  "accessToken": "string",
  "expiresIn": 900,
  "admin": {
    "id": 1,
    "email": "admin@example.com",
    "displayName": "Jane Smith"
  }
}
```
Refresh token returned as `HttpOnly` cookie.

**Response 401:** Invalid or expired Entra token.
**Response 403:** Admin account is inactive.

---

### GET /api/v1/auth/me
Returns the currently authenticated admin's profile.

**Response 200:**
```json
{
  "id": 1,
  "email": "admin@example.com",
  "displayName": "Jane Smith",
  "isActive": true,
  "lastLoginAt": "2025-03-15T10:00:00Z"
}
```

---

### POST /api/v1/auth/refresh `[Public]`
Issue a new access token from a valid refresh token cookie.

**Response 200:** Same shape as /login response.
**Response 401:** Refresh token missing, expired, or revoked.

---

### POST /api/v1/auth/logout
Revoke the current refresh token.

**Response 204:** No content.

---

## 4. Events Endpoints

### GET /api/v1/events
Returns all synced events.

**Query params:** `?status=live&page=1&pageSize=20`

**Response 200:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Tech Summit 2025",
      "startDate": "2025-06-01T09:00:00Z",
      "endDate": "2025-06-01T18:00:00Z",
      "venue": "Convention Center",
      "status": "live",
      "thumbnailUrl": "https://...",
      "ticketTypeCount": 3
    }
  ],
  "pagination": { ... }
}
```

---

### GET /api/v1/events/{id}
Returns a single event with its ticket types.

**Response 200:**
```json
{
  "id": 1,
  "name": "Tech Summit 2025",
  "startDate": "2025-06-01T09:00:00Z",
  "endDate": "2025-06-01T18:00:00Z",
  "venue": "Convention Center",
  "status": "live",
  "thumbnailUrl": "https://...",
  "ticketTypes": [
    {
      "id": 10,
      "name": "General Admission",
      "price": 0.00,
      "currency": "USD",
      "capacity": 500,
      "quantitySold": 312
    }
  ],
  "lastSyncedAt": "2025-03-15T08:00:00Z"
}
```

---

### POST /api/v1/events/sync
Triggers a manual Eventbrite sync for all events.

**Response 202:** Accepted — sync job queued.
```json
{
  "jobId": "abc123",
  "message": "Event sync job queued"
}
```

---

## 5. Registration Analytics Endpoints

### GET /api/v1/events/{id}/registrations/summary
Returns total registrations for an event, grouped by ticket type.

**Response 200:**
```json
{
  "eventId": 1,
  "totalRegistrations": 450,
  "byTicketType": [
    {
      "ticketTypeId": 10,
      "ticketTypeName": "General Admission",
      "registrationCount": 312
    },
    {
      "ticketTypeId": 11,
      "ticketTypeName": "VIP",
      "registrationCount": 138
    }
  ],
  "lastSyncedAt": "2025-03-15T08:00:00Z"
}
```

---

### GET /api/v1/events/{id}/registrations/daily-trends
Returns daily registration counts over time, by ticket type.

**Query params:** `?from=2025-01-01&to=2025-03-15`

**Response 200:**
```json
{
  "eventId": 1,
  "trends": [
    {
      "date": "2025-03-01",
      "byTicketType": [
        { "ticketTypeId": 10, "ticketTypeName": "General Admission", "count": 14 },
        { "ticketTypeId": 11, "ticketTypeName": "VIP", "count": 3 }
      ]
    }
  ]
}
```

---

### POST /api/v1/events/{id}/registrations/sync
Triggers a manual registration sync for a specific event.

**Response 202:** Job queued — same shape as event sync.

---

## 6. SMS Campaign Endpoints

### GET /api/v1/campaigns/segments
Returns Mailchimp audience segments available for SMS.

**Response 200:**
```json
{
  "data": [
    {
      "id": 1,
      "externalSegmentId": "seg_123",
      "name": "Tech Summit Registrants",
      "estimatedMemberCount": 450
    }
  ]
}
```

---

### POST /api/v1/campaigns
Create a new SMS campaign draft.

**Request:**
```json
{
  "eventId": 1,
  "name": "Day 1 Reminder",
  "audienceSegmentId": 1,
  "messageBody": "Tech Summit starts tomorrow at 9am. See you there!"
}
```

**Response 201:**
```json
{
  "id": 5,
  "status": "draft",
  "createdAt": "2025-03-15T10:00:00Z"
}
```

---

### GET /api/v1/campaigns
List all SMS campaigns, optionally filtered by event.

**Query params:** `?eventId=1&status=sent`

**Response 200:** Paginated list of campaign summaries.

---

### GET /api/v1/campaigns/{id}
Get full campaign details including send status.

**Response 200:** Full campaign DTO including messageBody, status, sentAt, providerMessageId.

---

### POST /api/v1/campaigns/{id}/send
Send a drafted campaign to Mailchimp.

**Response 202:** Send job queued.

**Business rules:**
- Campaign must be in `draft` status
- Segment eligibility validated before queuing
- Admin confirmation required — this endpoint is the confirmation action

---

## 7. Social Posts Endpoints

### POST /api/v1/social-posts/generate
Request AI-generated draft post(s) for an event or session.

**Request:**
```json
{
  "eventId": 1,
  "sessionId": null,
  "platform": "both",
  "postType": "promotion",
  "contextNotes": "Focus on AI track announcement"
}
```

**Response 202:** Generation job queued, returns draft IDs when ready.

---

### GET /api/v1/social-posts
List all post drafts, filtered by event or status.

**Query params:** `?eventId=1&status=approved&platform=instagram`

**Response 200:** Paginated list of post draft summaries.

---

### GET /api/v1/social-posts/{id}
Get full draft detail including raw AI output and edit history.

**Response 200:**
```json
{
  "id": 12,
  "eventId": 1,
  "platform": "instagram",
  "postType": "promotion",
  "caption": "...",
  "hashtags": "#TechSummit2025 #AI",
  "status": "approved",
  "aiGenerated": true,
  "createdAt": "...",
  "updatedAt": "..."
}
```

---

### PATCH /api/v1/social-posts/{id}
Edit a draft post caption, hashtags, or media URL.

**Request:**
```json
{
  "caption": "Updated caption text",
  "hashtags": "#TechSummit2025"
}
```

**Business rules:** Only allowed on drafts with status `draft` or `reviewed`.

---

### POST /api/v1/social-posts/{id}/approve
Approve a post draft for publishing.

**Request:**
```json
{
  "notes": "Looks great"
}
```

**Response 200:** Draft status updated to `approved`. Approval record created.

---

### POST /api/v1/social-posts/{id}/publish
Queue the approved post for publishing to Meta.

**Response 202:** Publish job queued.

**Business rules:** Post must be in `approved` status.

---

## 8. Sessions Endpoints

### POST /api/v1/sessions
Submit a YouTube URL to create a session ingestion record.

**Request:**
```json
{
  "eventId": 1,
  "title": "Keynote: AI in 2025",
  "speaker": "Jane Smith",
  "youTubeUrl": "https://youtube.com/watch?v=abc123",
  "sessionDate": "2025-06-01T09:00:00Z"
}
```

**Response 201:** Session created. Transcript ingestion job queued automatically.

---

### GET /api/v1/sessions
List sessions for an event.

**Query params:** `?eventId=1&summaryStatus=complete`

**Response 200:** Paginated list of session records with status fields.

---

### GET /api/v1/sessions/{id}
Get full session detail including transcript and summary status.

---

### POST /api/v1/sessions/{id}/generate-summary
Trigger AI summary generation (only after transcript is complete).

**Response 202:** Summary generation job queued.

---

### GET /api/v1/sessions/{id}/summary
Get the full generated summary with quotes, takeaways, and themes.

**Response 200:**
```json
{
  "sessionId": 1,
  "summaryMarkdown": "...",
  "keyTakeaways": ["...", "..."],
  "actionPoints": ["...", "..."],
  "themes": ["AI Adoption", "Ethics"],
  "quotes": [
    {
      "id": 3,
      "quoteText": "...",
      "attributedTo": "Jane Smith",
      "isApproved": false
    }
  ],
  "generatedAt": "2025-03-15T12:00:00Z",
  "approvedAt": null
}
```

---

### POST /api/v1/sessions/{id}/summary/approve
Approve a session summary for inclusion in the PDF report.

**Response 200:** Summary marked approved with admin ID and timestamp.

---

## 9. Reports Endpoints

### POST /api/v1/reports/events/{eventId}/generate
Trigger PDF generation for all approved session summaries of an event.

**Response 202:** PDF compilation job queued.

**Business rules:** At least one approved session summary must exist.

---

### GET /api/v1/reports/events/{eventId}
List all generated reports for an event.

**Response 200:**
```json
{
  "data": [
    {
      "id": 2,
      "name": "Tech Summit 2025 — Session Summary Report",
      "reportType": "session_summary",
      "generatedAt": "2025-03-15T14:00:00Z",
      "generatedBy": "Jane Smith",
      "downloadUrl": "https://..."
    }
  ]
}
```

---

### GET /api/v1/reports/{id}/download
Returns a short-lived pre-signed Azure Blob Storage URL for the PDF.

**Response 200:**
```json
{
  "downloadUrl": "https://blob.azure.com/...?sig=...",
  "expiresAt": "2025-03-15T15:00:00Z"
}
```

---

## 10. Audit Log Endpoints

### GET /api/v1/audit-logs
Returns the audit log, paginated and filtered.

**Query params:** `?adminId=1&action=post.approved&from=2025-03-01`

**Response 200:** Paginated list of audit log entries.

---

## 11. Background Job Status Endpoints

### GET /api/v1/jobs
List all background jobs with their current status.

**Query params:** `?jobType=RegistrationSync&status=failed`

**Response 200:** Paginated list of job status records.

---

### GET /api/v1/jobs/{id}
Get full detail for a single background job, including error message if failed.
