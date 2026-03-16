# 02 — Domain Modules
# Event Management Portal — Backend Module Definitions

> Version: 1.0
> Status: Approved
> Audience: Backend contributors, AI coding agents

---

## 1. Modular Monolith Principle

The backend is organized as a **modular monolith**. Every feature domain
owns its own internal structure. Modules do not directly reference each
other's internal types. Cross-module communication happens through shared
interfaces or DTOs in the `Shared` module only.

This structure is designed to scale into microservices later if needed,
without requiring architectural rewrites.

---

## 2. Module Inventory

| Module | Responsibility |
|---|---|
| Auth | Authentication, JWT issuance, session management |
| Events | Eventbrite sync, event entity management |
| Registrations | Attendee sync, ticket type tracking, analytics queries |
| Campaigns | SMS campaign drafting, Mailchimp send, delivery tracking |
| SocialPosts | AI draft generation, approval workflow, Meta publishing |
| Sessions | YouTube ingestion, transcript retrieval, summary generation |
| Reports | PDF compilation, Blob Storage export, report history |
| AuditLogs | System-wide audit trail for all significant admin actions |
| Shared | Infrastructure, persistence, security, background jobs, observability |

---

## 3. Module Definitions

---

### 3.1 Auth

**Purpose:** Manages the full authentication lifecycle — validating Entra
External ID tokens, issuing portal JWTs, refresh token rotation, and
admin session management.

**Owns:**
- `AdminUsers` entity and repository
- `RefreshTokens` entity and repository
- JWT generation and validation logic
- Entra External ID token validation
- Login, logout, refresh, and /me endpoints

**Does not own:**
- Any business domain data
- Any integration with Eventbrite, Mailchimp, Meta, or YouTube
- Authorization policy definitions (those live in Shared/Security)

**Folder layout:**
```
Modules/Auth/
├── Controllers/
│   └── AuthController.cs
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── ITokenService.cs
│   └── TokenService.cs
├── Dtos/
│   ├── LoginRequestDto.cs
│   ├── LoginResponseDto.cs
│   ├── RefreshRequestDto.cs
│   └── AdminProfileDto.cs
└── Entities/
    ├── AdminUser.cs
    └── RefreshToken.cs
```

---

### 3.2 Events

**Purpose:** Manages the Eventbrite sync lifecycle — pulling events,
ticket classes, and keeping the local events table current.

**Owns:**
- `Events` entity and repository
- `TicketTypes` entity and repository
- Eventbrite API integration client (behind interface)
- Event sync background job
- Event list and detail query endpoints

**Does not own:**
- Attendee or registration data (owned by Registrations)
- Snapshot aggregation (owned by Registrations)

**Folder layout:**
```
Modules/Events/
├── Controllers/
│   └── EventsController.cs
├── Services/
│   ├── IEventService.cs
│   └── EventService.cs
├── Integrations/
│   ├── IEventbriteClient.cs
│   └── EventbriteClient.cs
├── Jobs/
│   └── EventSyncJob.cs
├── Dtos/
│   ├── EventDto.cs
│   ├── EventDetailDto.cs
│   └── TicketTypeDto.cs
└── Entities/
    ├── Event.cs
    └── TicketType.cs
```

---

### 3.3 Registrations

**Purpose:** Handles attendee and order sync from Eventbrite, computes
registration metrics, and serves dashboard analytics queries.

**Owns:**
- `Registrations` entity and repository
- `DailyRegistrationSnapshots` entity and repository
- Registration sync background job
- Snapshot aggregator job (daily)
- Analytics query endpoints (summary, by-ticket-type, daily-trends)

**Does not own:**
- Event or ticket type entities (references Events module entities by FK)
- Eventbrite client (calls Events module's IEventbriteClient via DI)

**Folder layout:**
```
Modules/Registrations/
├── Controllers/
│   └── RegistrationsController.cs
├── Services/
│   ├── IRegistrationService.cs
│   └── RegistrationService.cs
├── Queries/
│   ├── RegistrationSummaryQuery.cs
│   └── DailyTrendQuery.cs
├── Jobs/
│   ├── RegistrationSyncJob.cs
│   └── SnapshotAggregatorJob.cs
├── Dtos/
│   ├── RegistrationSummaryDto.cs
│   ├── TicketTypeSummaryDto.cs
│   └── DailyTrendDto.cs
└── Entities/
    ├── Registration.cs
    └── DailyRegistrationSnapshot.cs
```

---

### 3.4 Campaigns

**Purpose:** Manages the full SMS campaign lifecycle — drafting,
Mailchimp audience segment browsing, send dispatch, and delivery tracking.

**Owns:**
- `SmsCampaigns` entity and repository
- `SmsRecipientSegments` entity (synced from Mailchimp)
- Mailchimp SMS integration client (behind interface)
- Campaign draft, send, and status endpoints
- SMS dispatch background job

**Does not own:**
- Audience data within Mailchimp (synced only, not mastered here)
- Any social or AI content

**Folder layout:**
```
Modules/Campaigns/
├── Controllers/
│   └── CampaignsController.cs
├── Services/
│   ├── ICampaignService.cs
│   └── CampaignService.cs
├── Integrations/
│   ├── IMailchimpClient.cs
│   └── MailchimpClient.cs
├── Jobs/
│   └── SmsSendJob.cs
├── Dtos/
│   ├── CampaignDraftDto.cs
│   ├── CampaignSendRequestDto.cs
│   ├── CampaignStatusDto.cs
│   └── AudienceSegmentDto.cs
└── Entities/
    ├── SmsCampaign.cs
    └── SmsRecipientSegment.cs
```

---

### 3.5 SocialPosts

**Purpose:** Manages the full social post lifecycle — AI-assisted draft
generation, admin review and approval, and Meta API publishing for
Facebook and Instagram.

**Owns:**
- `SocialPostDrafts` entity and repository
- `SocialPostApprovals` entity (approval audit record)
- `PublishedPosts` entity and repository
- Marketing AI agent (caption, hashtag, CTA generation)
- Meta publishing integration client (behind interface)
- Post generation, approval, and publish endpoints
- Social publish background job

**Does not own:**
- Session or transcript data (references Sessions module by ID)
- PDF content

**Folder layout:**
```
Modules/SocialPosts/
├── Controllers/
│   └── SocialPostsController.cs
├── Services/
│   ├── ISocialPostService.cs
│   └── SocialPostService.cs
├── Agents/
│   ├── IMarketingAgent.cs
│   └── MarketingAgent.cs
├── Integrations/
│   ├── IMetaClient.cs
│   └── MetaClient.cs
├── Jobs/
│   └── SocialPublishJob.cs
├── Dtos/
│   ├── PostGenerateRequestDto.cs
│   ├── PostDraftDto.cs
│   ├── PostApproveRequestDto.cs
│   └── PublishedPostDto.cs
└── Entities/
    ├── SocialPostDraft.cs
    ├── SocialPostApproval.cs
    └── PublishedPost.cs
```

---

### 3.6 Sessions

**Purpose:** Manages the full YouTube ingestion and AI summarization
lifecycle — from URL submission through transcript retrieval, summary
generation, quote extraction, and summary storage.

**Owns:**
- `SessionIngestions` entity (tracks the ingestion job lifecycle)
- `SessionTranscripts` entity (raw or processed transcript text)
- `SessionSummaries` entity (AI-generated structured summary)
- `SessionQuotes` entity (extracted quotes in structured form)
- YouTube ingestion pipeline
- Session summary AI agent
- Ingestion trigger, status, and summary query endpoints
- Ingestion and summary generation background jobs

**Does not own:**
- PDF compilation (owned by Reports)
- Social post scheduling from quotes (owned by SocialPosts)

**Folder layout:**
```
Modules/Sessions/
├── Controllers/
│   └── SessionsController.cs
├── Services/
│   ├── ISessionService.cs
│   └── SessionService.cs
├── Agents/
│   ├── ISessionSummaryAgent.cs
│   └── SessionSummaryAgent.cs
├── Integrations/
│   ├── IYouTubeClient.cs
│   └── YouTubeClient.cs
├── Jobs/
│   ├── TranscriptIngestionJob.cs
│   └── SummaryGenerationJob.cs
├── Dtos/
│   ├── SessionCreateDto.cs
│   ├── SessionStatusDto.cs
│   ├── SessionSummaryDto.cs
│   └── SessionQuoteDto.cs
└── Entities/
    ├── SessionIngestion.cs
    ├── SessionTranscript.cs
    ├── SessionSummary.cs
    └── SessionQuote.cs
```

---

### 3.7 Reports

**Purpose:** Compiles approved session summaries into a branded PDF
report, exports to Azure Blob Storage, and manages report download history.

**Owns:**
- `PdfReports` entity and repository
- PDF generation logic (using a PDF library such as QuestPDF)
- PDF narrative AI agent (cover page, event overview)
- Azure Blob Storage upload client (behind interface)
- Report generation trigger and download endpoints
- PDF compilation background job

**Does not own:**
- Session summary content (reads from Sessions module entities)
- Blob Storage credentials (sourced from Key Vault via IConfiguration)

**Folder layout:**
```
Modules/Reports/
├── Controllers/
│   └── ReportsController.cs
├── Services/
│   ├── IReportService.cs
│   └── ReportService.cs
├── Agents/
│   ├── IPdfNarrativeAgent.cs
│   └── PdfNarrativeAgent.cs
├── Pdf/
│   ├── IPdfBuilder.cs
│   └── PdfBuilder.cs
├── Jobs/
│   └── PdfCompilationJob.cs
├── Dtos/
│   ├── ReportRequestDto.cs
│   └── ReportDto.cs
└── Entities/
    └── PdfReport.cs
```

---

### 3.8 AuditLogs

**Purpose:** Provides a centralized, append-only audit trail for all
significant admin actions across the portal. Other modules write audit
entries via a shared interface — they do not read or query audit data.

**Owns:**
- `AuditLogs` entity and repository
- Audit write service (interface consumed by all other modules)
- Audit query endpoint (admin-only, read-only)

**Does not own:**
- Any business logic
- Any integration client

**Integration pattern:** All modules receive `IAuditLogger` via dependency
injection and call it after significant actions (login, send, publish,
approve, generate, export).

**Folder layout:**
```
Modules/AuditLogs/
├── Controllers/
│   └── AuditLogsController.cs
├── Services/
│   ├── IAuditLogger.cs
│   └── AuditLogger.cs
├── Dtos/
│   └── AuditLogEntryDto.cs
└── Entities/
    └── AuditLog.cs
```

---

### 3.9 Shared

**Purpose:** Provides infrastructure, persistence, security, background
job registration, and observability utilities consumed by all modules.
Contains no business logic.

**Owns:**
- EF Core `AppDbContext` and migration history
- Base entity conventions (Id, CreatedAt, UpdatedAt)
- JWT authentication and authorization middleware configuration
- Azure Key Vault configuration bootstrapping
- Serilog pipeline setup and Application Insights sink
- Hangfire registration and dashboard configuration
- Global exception handling middleware
- Correlation ID middleware
- Health check registration
- CORS policy configuration
- Rate limiting policy configuration
- Shared interfaces used across modules (e.g., IBlobStorageClient)

**Does not own:**
- Any entity outside of base conventions
- Any controller or business service

**Folder layout:**
```
Modules/Shared/
├── Infrastructure/
│   ├── KeyVaultConfiguration.cs
│   ├── BlobStorageClient.cs
│   └── IBlobStorageClient.cs
├── Persistence/
│   ├── AppDbContext.cs
│   └── BaseEntity.cs
├── Security/
│   ├── JwtConfiguration.cs
│   └── AuthorizationPolicies.cs
├── BackgroundJobs/
│   ├── HangfireConfiguration.cs
│   └── JobRegistry.cs
└── Observability/
    ├── SerilogConfiguration.cs
    ├── CorrelationIdMiddleware.cs
    └── GlobalExceptionMiddleware.cs
```

---

## 4. Cross-Module Rules

| Rule | Detail |
|---|---|
| No direct module-to-module entity references | Use IDs or shared DTOs in Shared/ only |
| Integration clients always behind interfaces | Enables unit testing and future swap |
| AI agents always behind interfaces | Allows prompt iteration without touching service layer |
| Background jobs registered centrally | All Hangfire jobs declared in Shared/BackgroundJobs/JobRegistry.cs |
| Audit writes go through IAuditLogger | Never write directly to AuditLogs table |
| All secrets via IConfiguration + Key Vault | No module hardcodes credentials |
