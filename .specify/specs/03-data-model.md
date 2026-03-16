# 03 — Data Model
# Event Management Portal — Core Entities and Schema

> Version: 1.0
> Status: Approved
> Audience: Backend contributors, AI coding agents, database reviewers

---

## 1. Conventions

All entities follow these conventions without exception:

- Primary key: `int Id` — identity, auto-increment
- All timestamps: `datetime2`, UTC, non-nullable unless specified
- `CreatedAt` is present on every entity
- `UpdatedAt` is present on all mutable entities
- External system IDs stored as separate `nvarchar` fields with `External` prefix
- JSON columns use `*Json` suffix (e.g., `KeyTakeawaysJson`, `MetadataJson`)
- Soft delete not used by default — records are hard-deleted unless auditing requires retention
- No entity is returned directly from a controller — DTOs only
- Foreign keys use `int` type and are named `<Entity>Id`

---

## 2. Entity Definitions

---

### AdminUser

Represents an authenticated admin user of the portal.

```
AdminUsers
├── Id                  int           PK, identity
├── Email               nvarchar(256) NOT NULL, unique index
├── DisplayName         nvarchar(256) NOT NULL
├── IdentityProvider    nvarchar(64)  NOT NULL  -- 'microsoft' | 'google'
├── ExternalObjectId    nvarchar(256) NOT NULL  -- Entra object ID
├── IsActive            bit           NOT NULL, default 1
├── CreatedAt           datetime2     NOT NULL
└── LastLoginAt         datetime2     NULL
```

---

### RefreshToken

Stores hashed refresh tokens for JWT rotation.

```
RefreshTokens
├── Id              int           PK, identity
├── AdminUserId     int           NOT NULL, FK → AdminUsers.Id
├── TokenHash       nvarchar(512) NOT NULL  -- bcrypt or SHA-256 hash, never raw
├── ExpiresAt       datetime2     NOT NULL
├── IsRevoked       bit           NOT NULL, default 0
└── CreatedAt       datetime2     NOT NULL
```

---

### Event

A synced event from Eventbrite, stored locally.

```
Events
├── Id                      int           PK, identity
├── ExternalEventbriteId    nvarchar(128) NOT NULL, unique index
├── Name                    nvarchar(512) NOT NULL
├── Slug                    nvarchar(256) NULL
├── StartDate               datetime2     NOT NULL
├── EndDate                 datetime2     NULL
├── Venue                   nvarchar(512) NULL
├── Status                  nvarchar(64)  NOT NULL  -- 'live' | 'draft' | 'ended' | 'canceled'
├── ThumbnailUrl            nvarchar(1024) NULL
├── CreatedAt               datetime2     NOT NULL
└── UpdatedAt               datetime2     NOT NULL
```

---

### TicketType

A ticket class belonging to an event, synced from Eventbrite.

```
TicketTypes
├── Id                      int           PK, identity
├── EventId                 int           NOT NULL, FK → Events.Id
├── ExternalTicketClassId   nvarchar(128) NOT NULL
├── Name                    nvarchar(256) NOT NULL
├── Price                   decimal(10,2) NOT NULL, default 0
├── Currency                nvarchar(8)   NOT NULL, default 'USD'
├── Capacity                int           NULL
├── QuantitySold            int           NOT NULL, default 0
├── CreatedAt               datetime2     NOT NULL
└── UpdatedAt               datetime2     NOT NULL
```

---

### Registration

An individual attendee registration synced from Eventbrite.

```
Registrations
├── Id                  int           PK, identity
├── EventId             int           NOT NULL, FK → Events.Id
├── TicketTypeId        int           NOT NULL, FK → TicketTypes.Id
├── ExternalOrderId     nvarchar(128) NOT NULL
├── ExternalAttendeeId  nvarchar(128) NOT NULL
├── AttendeeName        nvarchar(256) NULL
├── AttendeeEmail       nvarchar(256) NULL
├── RegisteredAt        datetime2     NOT NULL
├── CheckInStatus       nvarchar(32)  NOT NULL, default 'not_checked_in'
├── SourceSystem        nvarchar(64)  NOT NULL, default 'eventbrite'
└── CreatedAt           datetime2     NOT NULL
```

---

### DailyRegistrationSnapshot

Aggregated daily counts per event and ticket type. Drives dashboard charts.

```
DailyRegistrationSnapshots
├── Id                  int       PK, identity
├── EventId             int       NOT NULL, FK → Events.Id
├── TicketTypeId        int       NOT NULL, FK → TicketTypes.Id
├── SnapshotDate        date      NOT NULL
├── RegistrationCount   int       NOT NULL, default 0
└── CreatedAt           datetime2 NOT NULL

Unique index: (EventId, TicketTypeId, SnapshotDate)
```

---

### SmsCampaign

An SMS campaign drafted and sent through Mailchimp.

```
SmsCampaigns
├── Id                  int            PK, identity
├── EventId             int            NOT NULL, FK → Events.Id
├── Name                nvarchar(256)  NOT NULL
├── AudienceSegmentId   int            NOT NULL, FK → SmsRecipientSegments.Id
├── MessageBody         nvarchar(1600) NOT NULL  -- 160-char SMS units
├── Status              nvarchar(32)   NOT NULL  -- 'draft' | 'sent' | 'failed'
├── SentAt              datetime2      NULL
├── ProviderMessageId   nvarchar(256)  NULL
├── CreatedByAdminId    int            NOT NULL, FK → AdminUsers.Id
├── CreatedAt           datetime2      NOT NULL
└── UpdatedAt           datetime2      NOT NULL
```

---

### SmsRecipientSegment

A Mailchimp audience segment eligible for SMS, synced and stored locally.

```
SmsRecipientSegments
├── Id                      int           PK, identity
├── ExternalSegmentId       nvarchar(128) NOT NULL
├── Name                    nvarchar(256) NOT NULL
├── EstimatedMemberCount    int           NULL
├── LastSyncedAt            datetime2     NULL
└── CreatedAt               datetime2     NOT NULL
```

---

### SocialPostDraft

An AI-generated or manually created social post awaiting review and approval.

```
SocialPostDrafts
├── Id                  int            PK, identity
├── EventId             int            NOT NULL, FK → Events.Id
├── SessionId           int            NULL, FK → SessionIngestions.Id
├── Platform            nvarchar(32)   NOT NULL  -- 'facebook' | 'instagram' | 'both'
├── PostType            nvarchar(32)   NOT NULL  -- 'promotion' | 'recap' | 'quote'
├── Caption             nvarchar(2200) NOT NULL
├── Hashtags            nvarchar(512)  NULL
├── MediaUrl            nvarchar(1024) NULL
├── Status              nvarchar(32)   NOT NULL  -- 'draft' | 'reviewed' | 'approved' | 'published' | 'rejected'
├── AiGenerated         bit            NOT NULL, default 0
├── RawAiOutputJson     nvarchar(max)  NULL  -- raw agent output before admin edits
├── CreatedByAdminId    int            NOT NULL, FK → AdminUsers.Id
├── CreatedAt           datetime2      NOT NULL
└── UpdatedAt           datetime2      NOT NULL
```

---

### SocialPostApproval

Immutable audit record of an admin approval action on a social post draft.

```
SocialPostApprovals
├── Id              int          PK, identity
├── PostDraftId     int          NOT NULL, FK → SocialPostDrafts.Id
├── Action          nvarchar(32) NOT NULL  -- 'approved' | 'rejected'
├── ApprovedByAdminId int        NOT NULL, FK → AdminUsers.Id
├── Notes           nvarchar(512) NULL
└── CreatedAt       datetime2    NOT NULL
```

---

### PublishedPost

Record of a successfully published social post with Meta's response data.

```
PublishedPosts
├── Id                  int            PK, identity
├── PostDraftId         int            NOT NULL, FK → SocialPostDrafts.Id
├── Platform            nvarchar(32)   NOT NULL
├── ExternalPostId      nvarchar(256)  NULL  -- Meta post ID
├── PublishedAt         datetime2      NULL
├── PublishStatus       nvarchar(32)   NOT NULL  -- 'success' | 'failed'
├── FailureReason       nvarchar(1024) NULL
├── PublishedByAdminId  int            NOT NULL, FK → AdminUsers.Id
└── CreatedAt           datetime2      NOT NULL
```

---

### SessionIngestion

Tracks the lifecycle of a YouTube session submitted for ingestion.

```
SessionIngestions
├── Id                  int            PK, identity
├── EventId             int            NOT NULL, FK → Events.Id
├── Title               nvarchar(512)  NOT NULL
├── Speaker             nvarchar(256)  NULL
├── YouTubeUrl          nvarchar(1024) NOT NULL
├── TranscriptStatus    nvarchar(32)   NOT NULL  -- 'pending' | 'processing' | 'complete' | 'failed'
├── SummaryStatus       nvarchar(32)   NOT NULL  -- 'pending' | 'processing' | 'complete' | 'failed'
├── SessionDate         datetime2      NULL
├── DurationSeconds     int            NULL
├── CreatedByAdminId    int            NOT NULL, FK → AdminUsers.Id
├── CreatedAt           datetime2      NOT NULL
└── UpdatedAt           datetime2      NOT NULL
```

---

### SessionTranscript

Stores the raw or processed transcript text from YouTube.

```
SessionTranscripts
├── Id              int           PK, identity
├── SessionId       int           NOT NULL, FK → SessionIngestions.Id, unique
├── TranscriptText  nvarchar(max) NOT NULL
├── SourceMethod    nvarchar(64)  NOT NULL  -- 'youtube_captions' | 'audio_transcription'
└── CreatedAt       datetime2     NOT NULL
```

---

### SessionSummary

AI-generated structured summary of a session.

```
SessionSummaries
├── Id                  int            PK, identity
├── SessionId           int            NOT NULL, FK → SessionIngestions.Id, unique
├── SummaryMarkdown     nvarchar(max)  NOT NULL
├── KeyTakeawaysJson    nvarchar(max)  NOT NULL  -- JSON array of strings
├── ActionPointsJson    nvarchar(max)  NULL       -- JSON array of strings
├── ThemesJson          nvarchar(max)  NULL       -- JSON array of strings
├── ModelUsed           nvarchar(128)  NOT NULL
├── PromptVersion       nvarchar(64)   NULL
├── GeneratedAt         datetime2      NOT NULL
├── ApprovedAt          datetime2      NULL
├── ApprovedByAdminId   int            NULL, FK → AdminUsers.Id
└── CreatedAt           datetime2      NOT NULL
```

---

### SessionQuote

An individual quote extracted from a session by the AI summary agent.

```
SessionQuotes
├── Id              int            PK, identity
├── SessionId       int            NOT NULL, FK → SessionIngestions.Id
├── QuoteText       nvarchar(1024) NOT NULL
├── AttributedTo    nvarchar(256)  NULL  -- speaker name or 'Session'
├── ContextNote     nvarchar(512)  NULL
├── IsApproved      bit            NOT NULL, default 0
└── CreatedAt       datetime2      NOT NULL
```

---

### PdfReport

Tracks generated PDF reports per event.

```
PdfReports
├── Id                  int            PK, identity
├── EventId             int            NOT NULL, FK → Events.Id
├── Name                nvarchar(512)  NOT NULL
├── BlobPath            nvarchar(1024) NOT NULL  -- Azure Blob Storage path
├── ReportType          nvarchar(64)   NOT NULL, default 'session_summary'
├── GeneratedAt         datetime2      NOT NULL
├── GeneratedByAdminId  int            NOT NULL, FK → AdminUsers.Id
└── CreatedAt           datetime2      NOT NULL
```

---

### AuditLog

Append-only system-wide audit trail. Never updated or deleted.

```
AuditLogs
├── Id              int            PK, identity
├── AdminUserId     int            NOT NULL, FK → AdminUsers.Id
├── Action          nvarchar(128)  NOT NULL  -- e.g. 'sms.sent' | 'post.approved' | 'report.generated'
├── EntityType      nvarchar(64)   NULL       -- e.g. 'SmsCampaign'
├── EntityId        int            NULL
├── MetadataJson    nvarchar(max)  NULL       -- JSON blob with action context
├── IpAddress       nvarchar(64)   NULL
├── CorrelationId   nvarchar(128)  NULL
└── CreatedAt       datetime2      NOT NULL
```

---

### BackgroundJobStatus

Tracks status of long-running background jobs for admin visibility.

```
BackgroundJobStatuses
├── Id              int            PK, identity
├── HangfireJobId   nvarchar(128)  NULL  -- Hangfire's internal job ID
├── JobType         nvarchar(128)  NOT NULL  -- e.g. 'RegistrationSync' | 'SummaryGeneration'
├── Status          nvarchar(32)   NOT NULL  -- 'queued' | 'running' | 'complete' | 'failed'
├── RelatedEntityType nvarchar(64) NULL
├── RelatedEntityId int            NULL
├── StartedAt       datetime2      NULL
├── CompletedAt     datetime2      NULL
├── ErrorMessage    nvarchar(max)  NULL
└── CreatedAt       datetime2      NOT NULL
```

---

## 3. Relationships Summary

```
AdminUsers
  └── RefreshTokens (1:many)
  └── SmsCampaigns (1:many, CreatedBy)
  └── SocialPostDrafts (1:many, CreatedBy)
  └── SocialPostApprovals (1:many, ApprovedBy)
  └── PublishedPosts (1:many, PublishedBy)
  └── SessionIngestions (1:many, CreatedBy)
  └── SessionSummaries (1:many, ApprovedBy)
  └── PdfReports (1:many, GeneratedBy)
  └── AuditLogs (1:many)

Events
  └── TicketTypes (1:many)
  └── Registrations (1:many)
  └── DailyRegistrationSnapshots (1:many)
  └── SmsCampaigns (1:many)
  └── SocialPostDrafts (1:many)
  └── SessionIngestions (1:many)
  └── PdfReports (1:many)

TicketTypes
  └── Registrations (1:many)
  └── DailyRegistrationSnapshots (1:many)

SessionIngestions
  └── SessionTranscript (1:1)
  └── SessionSummary (1:1)
  └── SessionQuotes (1:many)
  └── SocialPostDrafts (1:many, optional)

SocialPostDrafts
  └── SocialPostApprovals (1:many)
  └── PublishedPosts (1:many)
```

---

## 4. Index Strategy

| Table | Index |
|---|---|
| AdminUsers | Unique on Email |
| Events | Unique on ExternalEventbriteId |
| TicketTypes | Index on EventId |
| Registrations | Index on EventId, TicketTypeId |
| DailyRegistrationSnapshots | Unique on (EventId, TicketTypeId, SnapshotDate) |
| SmsCampaigns | Index on EventId, Status |
| SocialPostDrafts | Index on EventId, Status, Platform |
| SessionIngestions | Index on EventId, TranscriptStatus, SummaryStatus |
| AuditLogs | Index on AdminUserId, CreatedAt; Index on Action |
| BackgroundJobStatuses | Index on JobType, Status |
