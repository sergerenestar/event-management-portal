# Sprint 3 — SMS Communication Module
## Task Breakdown

**Feature Branch**: `004-sms-campaigns`
**Spec**: `spec.md`
**Constitution Check**: All tasks comply with Principles I (Human-Gated AI Actions), II (Security-First), III (Modular Monolith), V (Observability), and VII (Test Coverage).

---

## Phase 0 — Backend Foundation

### TASK-001 — Create Campaign and CampaignRecipient entities

**Module**: `Modules/Campaigns/Entities/`
**Spec refs**: FR-001, FR-006, FR-011
**Depends on**: Sprint 2 Registration entity (read-only reference)

**What to build**:
- `Campaign` entity with fields:
  - `Id` (int, identity)
  - `Name` (string)
  - `MessageBody` (string)
  - `AudienceCriteriaJson` (string — stores filter snapshot: event, ticket type, registration status)
  - `Status` (enum: `Draft`, `Sending`, `Sent`, `Failed`)
  - `TotalRecipients` (int)
  - `DeliveredCount` (int)
  - `FailedCount` (int)
  - `CreatedByAdminId` (int, FK → AdminUsers)
  - `CreatedAt` (datetime UTC)
  - `SentAt` (datetime UTC, nullable)
- `CampaignRecipient` entity with fields:
  - `Id` (int, identity)
  - `CampaignId` (int, FK → Campaigns)
  - `RegistrationId` (int, FK → Registrations)
  - `PhoneNumberAtSend` (string — **write-once, never updated after creation**)
  - `DeliveryStatus` (enum: `Pending`, `Delivered`, `Failed`, `Unresolved`)
  - `StatusUpdatedAt` (datetime UTC, nullable)
  - `ProviderMessageId` (string, nullable)
  - `CreatedAt` (datetime UTC)
- EF Core configuration: cascade delete `CampaignRecipient` on `Campaign` delete
- EF Core migration

**Acceptance check**: Migration applies cleanly. Entities are queryable via EF Core. `PhoneNumberAtSend` has no update path in any service method.

---

### TASK-002 — Audience resolution service

**Module**: `Modules/Campaigns/Services/AudienceResolverService.cs`
**Spec refs**: FR-002, FR-003, FR-010, SC-002

**What to build**:
- `IAudienceResolverService` interface with method:
  `ResolveAsync(AudienceFilterDto filter) → AudienceResolutionResult`
- `AudienceFilterDto`: eventId (nullable), ticketTypeId (nullable), registrationStatus (nullable)
- `AudienceResolutionResult`: list of eligible `(RegistrationId, PhoneNumber)` pairs + `UnreachableCount`
- Eligibility rules:
  - Registration must match all provided filter criteria
  - Attendee must have a non-null, non-empty phone number
  - Exclusion rule: phone numbers that are null or empty are counted as unreachable, not eligible
- Query must hit `Registrations` and `TicketTypes` — no live Eventbrite call
- Target: resolves in under 5 seconds for 10,000 registrations (SC-002)

**Acceptance check**: Unit tests cover — all-filter match, partial filter match, zero reachable result, missing phone number exclusion. No integration call made during resolution.

---

### TASK-003 — Campaign service (core CRUD + send gate)

**Module**: `Modules/Campaigns/Services/CampaignService.cs`
**Spec refs**: FR-001, FR-004, FR-005, FR-006, FR-011, FR-012

**What to build**:
- `ICampaignService` interface with:
  - `CreateDraftAsync(CreateCampaignDto dto, int adminId) → CampaignDto`
  - `GetCampaignAsync(int id) → CampaignDto`
  - `GetCampaignHistoryAsync() → IEnumerable<CampaignSummaryDto>`
  - `ConfirmAndSendAsync(int campaignId, int adminId) → void`
- `ConfirmAndSendAsync` rules:
  - If `Campaign.Status != Draft` → throw `InvalidOperationException("Campaign has already been dispatched.")`
  - Set status to `Sending` in a single atomic update before handing off to background job (prevents double-send — SC-006)
  - Enqueue Hangfire job: `SmsSendJob`
  - Write audit log entry: actor, campaignId, action `CampaignSent`, timestamp
- `CreateDraftAsync` must persist `AudienceCriteriaJson` as a snapshot of the filter at creation time
- DTOs: `CreateCampaignDto`, `CampaignDto`, `CampaignSummaryDto`

**Acceptance check**: Unit tests cover — double-send prevention (concurrent calls return error on second), draft creation, send gate audit log entry. Service never calls Mailchimp directly.

---

### TASK-004 — Mailchimp SMS integration client

**Module**: `Modules/Campaigns/Integrations/MailchimpSmsClient.cs`
**Spec refs**: FR-005, FR-007, FR-008, constitution Section 5 (integration rules)

**What to build**:
- `IMailchimpSmsClient` interface with:
  - `SendMessagesAsync(IEnumerable<SmsMessageRequest> messages) → MailchimpSendResult`
  - `GetDeliveryStatusAsync(IEnumerable<string> providerMessageIds) → IEnumerable<MailchimpDeliveryStatus>`
- `SmsMessageRequest`: phone number, message body, campaignRecipientId (for correlation)
- `MailchimpSendResult`: per-recipient provider message ID + initial send status
- Client handles: HTTP, retry on transient failure (3 attempts, exponential backoff), maps Mailchimp errors to domain exceptions
- Credentials fetched from `IConfiguration` → Key Vault reference (never hardcoded)
- No business logic inside this client

**Acceptance check**: Integration tests (against Mailchimp test environment or mock) verify send dispatch, error mapping, and retry behaviour. Business logic tests mock this interface — never call it directly.

---

### TASK-005 — SmsSendJob (Hangfire background job)

**Module**: `Modules/Campaigns/Jobs/SmsSendJob.cs`
**Spec refs**: FR-005, FR-006, FR-010, constitution Section 2.4

**What to build**:
- Hangfire background job `SmsSendJob(int campaignId)`
- Steps:
  1. Load campaign and eligible recipients (status `Pending`) from DB
  2. Exclude any recipient with null/empty `PhoneNumberAtSend` (count as unreachable)
  3. Call `IMailchimpSmsClient.SendMessagesAsync`
  4. Persist `ProviderMessageId` per recipient
  5. On full batch success: update `Campaign.Status = Sent`, `SentAt = UTC now`
  6. On full batch failure: update `Campaign.Status = Failed`, log error via Serilog, surface via Application Insights
  7. On partial failure: mark individual recipients as `Failed`, update counts, set campaign status to `Sent` with failed count recorded
- Correlation ID must be propagated through the job context
- Job failure must be visible in Hangfire dashboard

**Acceptance check**: Unit tests mock `IMailchimpSmsClient`. Tests cover full success, full failure, partial failure, and unreachable recipient exclusion paths.

---

### TASK-006 — DeliveryStatusPollingJob (Hangfire recurring job)

**Module**: `Modules/Campaigns/Jobs/DeliveryStatusPollingJob.cs`
**Spec refs**: FR-007, FR-008, SC-004

**What to build**:
- Recurring Hangfire job with a **two-stage cadence** (FR-008):
  - Every 5 minutes for the first hour after `Campaign.SentAt`
  - Every 30 minutes from 1 hour to 24 hours after `Campaign.SentAt`
  - After 24 hours: mark all remaining `Pending` recipients as `Unresolved` and stop polling that campaign
- Logic per run:
  1. Query all `Campaign` records with status `Sent` and at least one `CampaignRecipient` with `DeliveryStatus = Pending`
  2. For each qualifying campaign, determine polling stage based on elapsed time since `SentAt`
  3. Skip campaigns not yet due for their next poll based on stage interval
  4. Collect `ProviderMessageId` values from `Pending` recipients
  5. Call `IMailchimpSmsClient.GetDeliveryStatusAsync`
  6. Update each recipient's `DeliveryStatus` and `StatusUpdatedAt`
  7. Recalculate and update `Campaign.DeliveredCount` and `Campaign.FailedCount`
  8. If 24 hours have elapsed since `SentAt`: set remaining `Pending` recipients to `Unresolved`, stop polling
- Stop polling a campaign once all recipients are in a terminal state (`Delivered`, `Failed`, or `Unresolved`)
- Log polling run with campaign count, update count, and unresolved count via Serilog

**Acceptance check**: Unit tests cover — polling stops when all recipients are terminal; recipients still Pending after 24 hours are marked Unresolved; correct stage interval is applied based on elapsed time.

---

### TASK-007 — Campaign API controller

**Module**: `Modules/Campaigns/Controllers/CampaignController.cs`
**Spec refs**: FR-001 to FR-012, FR-012 (auth), SC-003, SC-005

**Endpoints**:
```
POST   /api/campaigns               → CreateDraft
GET    /api/campaigns               → GetHistory
GET    /api/campaigns/{id}          → GetById (includes recipient count + delivery summary)
POST   /api/campaigns/{id}/preview  → ResolveAudience (returns count, no DB write)
POST   /api/campaigns/{id}/send     → ConfirmAndSend
```

**Rules**:
- All endpoints require `[Authorize]` (admin JWT)
- `POST /send` returns `409 Conflict` if campaign is not in `Draft` status
- `POST /preview` is a read-only resolution call — it does not create recipients or modify state
- FluentValidation on `CreateCampaignDto`: message body required, max 160 characters, audience filter must have at least one criterion

**Acceptance check**: Integration tests verify auth guard (401 without token), 409 on duplicate send attempt, validation rejection on empty message, preview returns count without side effects.

---

## Phase 1 — Frontend

### TASK-008 — Campaign compose page

**Feature**: `src/features/campaigns/CampaignComposePage.jsx`
**Spec refs**: US1 scenarios 1–4, US2 scenarios 1–3, FR-003, FR-004

**What to build**:
- Route: `/campaigns/new`
- Form fields:
  - Campaign name (text input)
  - Message body (textarea, character counter, 160-char limit with visual warning at 140)
  - Audience selector (event dropdown → ticket type dropdown → registration status dropdown)
- Resolved recipient count displayed live after audience selection (calls `POST /api/campaigns/{id}/preview`)
- Zero-recipient warning state: block proceed button, show explanatory message
- Preview modal: shows exact message text + recipient count + Confirm / Cancel actions
- On Confirm: calls `POST /api/campaigns/{id}/send`, shows success toast, redirects to history
- On Cancel: closes modal, returns to compose form with state preserved
- No send happens without the preview modal confirmation step

**Acceptance check**: Manual test — compose → preview → cancel returns to compose with data intact. Compose → preview → confirm calls send endpoint once only.

---

### TASK-009 — Campaign history page

**Feature**: `src/features/campaigns/CampaignHistoryPage.jsx`
**Spec refs**: US1 scenario 5, US3 scenarios 1–3, SC-005

**What to build**:
- Route: `/campaigns`
- Table columns: Name, Status badge, Total Recipients, Delivered, Failed, Sent At
- Clickable row → campaign detail drawer or page showing per-recipient status (where available)
- Status badge colours: Draft (grey), Sending (blue), Sent (green), Failed (red)
- Auto-refresh every 60 seconds while any campaign has `Sending` or `Pending` delivery counts
- Empty state when no campaigns exist

**Acceptance check**: History page reachable within 2 clicks from main nav (SC-005). Table updates without full page reload.

---

### TASK-010 — Campaign service module

**Frontend**: `src/services/campaignService.js`
**Spec refs**: constitution Section 8 (no direct axios calls in features)

**What to build**:
- `createCampaignDraft(payload)` → POST `/api/campaigns`
- `previewAudience(campaignId)` → POST `/api/campaigns/{id}/preview`
- `sendCampaign(campaignId)` → POST `/api/campaigns/{id}/send`
- `getCampaigns()` → GET `/api/campaigns`
- `getCampaign(id)` → GET `/api/campaigns/{id}`
- All calls via shared `apiClient.js` (Axios instance with auth header injection)
- Error responses mapped to user-readable messages in a central error handler

**Acceptance check**: No feature component imports axios directly. All API calls go through this module.

---

## Phase 2 — Tests

### TASK-011 — Unit tests: AudienceResolverService

**File**: `Tests/Campaigns/AudienceResolverServiceTests.cs`
**Spec refs**: US2 acceptance scenarios, FR-002, FR-010

**Scenarios to cover**:
- Filter by event only → correct attendees returned
- Filter by event + ticket type → correct subset returned
- Filter by registration status → correct subset returned
- Attendees with no phone number excluded, counted as unreachable
- Zero reachable result returned (not an error, valid state)
- All three filters applied simultaneously

---

### TASK-012 — Unit tests: CampaignService

**File**: `Tests/Campaigns/CampaignServiceTests.cs`
**Spec refs**: FR-005, FR-011, SC-006

**Scenarios to cover**:
- Draft created, status is `Draft`
- `ConfirmAndSendAsync` on Draft campaign transitions to `Sending` and enqueues job
- `ConfirmAndSendAsync` on non-Draft campaign throws `InvalidOperationException`
- Audit log entry written on send confirmation
- Concurrent send: second call sees `Sending` status and is rejected

---

### TASK-013 — Unit tests: SmsSendJob + DeliveryStatusPollingJob

**File**: `Tests/Campaigns/SmsSendJobTests.cs`, `Tests/Campaigns/DeliveryStatusPollingJobTests.cs`
**Spec refs**: FR-005, FR-006, FR-008, FR-010

**SmsSendJob scenarios**:
- Full success: all recipients delivered, campaign set to `Sent`
- Full failure: campaign set to `Failed`, error logged
- Partial failure: individual recipients marked `Failed`, campaign still set to `Sent` with counts
- Recipients with no phone number excluded before send call

**DeliveryStatusPollingJob scenarios**:
- Polling stops once all recipients reach a terminal state (`Delivered` or `Failed`)
- Recipients still `Pending` after 24 hours are marked `Unresolved` and polling stops for that campaign
- Job applies 5-minute interval logic within first hour of `SentAt`
- Job applies 30-minute interval logic between 1 and 24 hours of `SentAt`
- `Campaign.DeliveredCount` and `Campaign.FailedCount` are recalculated correctly after each poll

---

### TASK-014 — Integration tests: Campaign API endpoints

**File**: `Tests/Campaigns/CampaignControllerIntegrationTests.cs`
**Spec refs**: FR-012, SC-003, SC-006

**Scenarios to cover**:
- Unauthenticated request returns 401
- Create draft → returns 201 with campaign ID
- Preview returns recipient count without creating DB records
- Send transitions campaign to `Sending`
- Second send on same campaign returns 409
- FluentValidation rejects empty message body
- FluentValidation rejects message over 160 characters

---

## Phase 3 — Observability and Hardening

### TASK-015 — Structured logging and audit log integration

**Spec refs**: constitution Principle I (audit log), Principle V (observability)

**What to build**:
- Serilog log entries for:
  - Campaign draft created (campaignId, adminId)
  - Send confirmed (campaignId, adminId, recipientCount)
  - Send job started / completed / failed (campaignId, recipientCount, failedCount)
  - Delivery polling run (campaignsPolled, recipientsUpdated)
- Application Insights custom events:
  - `SmsCampaignSent` (campaignId, recipientCount)
  - `SmsCampaignFailed` (campaignId, error)
  - `SmsDeliveryUpdated` (campaignId, deliveredCount, failedCount)
- Correlation ID present in all log entries for campaign-related operations

**Acceptance check**: A single send flow produces traceable log entries from controller → service → job. Campaign ID is the correlation anchor.

---

### TASK-016 — Hangfire dashboard security

**Spec refs**: constitution Principle II (security), Principle V (Hangfire visibility)

**What to build**:
- Hangfire dashboard accessible at `/hangfire` (dev) and behind admin auth check (prod)
- `SmsSendJob` and `DeliveryStatusPollingJob` visible in dashboard
- Failed job retries configured: 3 attempts, exponential backoff
- Alerts configured in Log Analytics for job failure rate above threshold

**Acceptance check**: Dashboard is not publicly accessible. Failed jobs surface with failure reason and retry count.

---

## Task Summary

| ID | Title | Phase | Effort |
|---|---|---|---|
| TASK-001 | Campaign + CampaignRecipient entities | 0 | S |
| TASK-002 | Audience resolution service | 0 | M |
| TASK-003 | Campaign service (CRUD + send gate) | 0 | M |
| TASK-004 | Mailchimp SMS integration client | 0 | M |
| TASK-005 | SmsSendJob | 0 | M |
| TASK-006 | DeliveryStatusPollingJob | 0 | S |
| TASK-007 | Campaign API controller | 0 | S |
| TASK-008 | Campaign compose page | 1 | M |
| TASK-009 | Campaign history page | 1 | S |
| TASK-010 | Campaign service module (frontend) | 1 | S |
| TASK-011 | Unit tests: AudienceResolverService | 2 | S |
| TASK-012 | Unit tests: CampaignService | 2 | S |
| TASK-013 | Unit tests: SmsSendJob | 2 | S |
| TASK-014 | Integration tests: Campaign API | 2 | M |
| TASK-015 | Structured logging + audit log | 3 | S |
| TASK-016 | Hangfire dashboard security | 3 | S |

**Effort key**: S = half day or less · M = 1–2 days

---

## Definition of Done

A task is complete when:
- Code is merged to `feature/004-sms-campaigns` with a passing `pr-check.yml` run
- All acceptance checks for that task pass
- No secrets appear in source code or appsettings files
- Constitution Check is present in the PR description
