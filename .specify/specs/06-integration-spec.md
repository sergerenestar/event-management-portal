# 06 — Integration Specification
# Event Management Portal — External Integration Contracts

> Version: 1.0
> Status: Approved
> Audience: Backend contributors, AI coding agents

---

## 1. Integration Design Principles

- Every external API client is behind an interface
- Business logic services call the interface — never the concrete client directly
- Integration clients handle HTTP calls, error mapping, and retry only
- No business logic lives inside an integration client
- All credentials sourced from Azure Key Vault via `IConfiguration`
- All integration clients registered in DI as singletons with typed `HttpClient`
- Retry policy: 3 attempts, exponential backoff (1s, 2s, 4s) on 429 and 503
- Failures propagated as domain exceptions — not raw `HttpRequestException`

---

## 2. Eventbrite Integration

**Module owner:** Events, Registrations

**Base URL:** `https://www.eventbriteapi.com/v3/`

**Authentication:** Bearer token in `Authorization` header, sourced from Key Vault key `Eventbrite:ApiToken`

---

### Interface

```csharp
public interface IEventbriteClient
{
    Task<IEnumerable<EventbriteEvent>> GetOrganizationEventsAsync(string organizationId, CancellationToken ct);
    Task<IEnumerable<EventbriteTicketClass>> GetTicketClassesAsync(string eventId, CancellationToken ct);
    Task<IEnumerable<EventbriteAttendee>> GetAttendeesAsync(string eventId, CancellationToken ct);
    Task<IEnumerable<EventbriteOrder>> GetOrdersAsync(string eventId, CancellationToken ct);
}
```

---

### Data Mapping

| Eventbrite Field | Local Entity Field |
|---|---|
| `event.id` | `Events.ExternalEventbriteId` |
| `event.name.text` | `Events.Name` |
| `event.start.utc` | `Events.StartDate` |
| `event.end.utc` | `Events.EndDate` |
| `event.venue.name` | `Events.Venue` |
| `event.status` | `Events.Status` |
| `event.logo.url` | `Events.ThumbnailUrl` |
| `ticket_class.id` | `TicketTypes.ExternalTicketClassId` |
| `ticket_class.name` | `TicketTypes.Name` |
| `ticket_class.cost.value` | `TicketTypes.Price` |
| `ticket_class.quantity_sold` | `TicketTypes.QuantitySold` |
| `attendee.id` | `Registrations.ExternalAttendeeId` |
| `attendee.order_id` | `Registrations.ExternalOrderId` |
| `attendee.profile.name` | `Registrations.AttendeeName` |
| `attendee.profile.email` | `Registrations.AttendeeEmail` |
| `attendee.created` | `Registrations.RegisteredAt` |
| `attendee.checked_in` | `Registrations.CheckInStatus` |

---

### Sync Behavior

- Eventbrite sync runs as a Hangfire recurring job (configurable schedule, default: every 6 hours)
- Sync is upsert-based: match on `ExternalEventbriteId` / `ExternalTicketClassId` / `ExternalAttendeeId`
- Deletions in Eventbrite are not automatically removed locally — status field updated only
- Manual sync can be triggered via `POST /api/v1/events/sync`
- Pagination handled internally by the client — caller receives full collection

---

### Error Handling

| HTTP Status | Action |
|---|---|
| 401 | Throw `IntegrationAuthException` — alert in job log |
| 429 | Retry with backoff — log warning |
| 404 | Skip record — log warning |
| 5xx | Retry with backoff — fail job after 3 attempts |

---

## 3. Mailchimp SMS Integration

**Module owner:** Campaigns

**Base URL:** `https://api.mailchimp.com/3.0/` (Marketing API) and `https://transactional.mailchimp.com/api/1.0/` (Transactional)

**Authentication:** API key in `Authorization: apikey <key>` header, sourced from Key Vault key `Mailchimp:ApiKey`

**Important:** Mailchimp SMS availability depends on account configuration,
purchased SMS credits, approved sending programs, and regional support.
The client must validate segment eligibility before queuing a send.

---

### Interface

```csharp
public interface IMailchimpClient
{
    Task<IEnumerable<MailchimpSegment>> GetSmsEligibleSegmentsAsync(string listId, CancellationToken ct);
    Task<MailchimpSendResult> SendSmsCampaignAsync(MailchimpSmsRequest request, CancellationToken ct);
    Task<MailchimpCampaignStatus> GetCampaignStatusAsync(string providerMessageId, CancellationToken ct);
}
```

---

### Request/Response Models

```csharp
public record MailchimpSmsRequest(
    string ListId,
    string SegmentId,
    string MessageBody,
    string CampaignName
);

public record MailchimpSendResult(
    bool Success,
    string ProviderMessageId,
    string Status,
    string? ErrorMessage
);
```

---

### Send Workflow

1. `CampaignService` validates the campaign is in `draft` status
2. `CampaignService` calls `IMailchimpClient.GetSmsEligibleSegmentsAsync()` to confirm the segment is still valid
3. If valid, `SmsSendJob` is queued with the campaign ID
4. `SmsSendJob` calls `IMailchimpClient.SendSmsCampaignAsync()`
5. On success: stores `ProviderMessageId`, sets status to `sent`, sets `SentAt`
6. On failure: sets status to `failed`, stores error message, records in `BackgroundJobStatuses`

---

### Error Handling

| Condition | Action |
|---|---|
| Segment not SMS-eligible | Fail validation before queuing — surface error to admin UI |
| 401 Unauthorized | Throw `IntegrationAuthException` — surface in job log |
| 400 Bad Request | Fail campaign with Mailchimp error detail stored |
| 429 Rate Limited | Retry with backoff |
| Send confirmed | Store `ProviderMessageId` in `SmsCampaigns` |

---

## 4. Meta (Facebook / Instagram) Integration

**Module owner:** SocialPosts

**Base URL:** `https://graph.facebook.com/v19.0/`

**Authentication:** Page Access Token sourced from Key Vault key `Meta:PageAccessToken`

**Scope:** Publishes to connected Facebook Pages and Instagram Business/Creator accounts only.

---

### Interface

```csharp
public interface IMetaClient
{
    Task<MetaPublishResult> PublishFacebookPostAsync(MetaPostRequest request, CancellationToken ct);
    Task<MetaPublishResult> PublishInstagramPostAsync(MetaPostRequest request, CancellationToken ct);
    Task<MetaPostStatus> GetPostStatusAsync(string externalPostId, string platform, CancellationToken ct);
}
```

---

### Request/Response Models

```csharp
public record MetaPostRequest(
    string Caption,
    string? MediaUrl,
    string PageOrAccountId
);

public record MetaPublishResult(
    bool Success,
    string? ExternalPostId,
    string? FailureReason
);
```

---

### Publish Workflow

All publishing goes through the approval workflow before the client is called:

1. Post must have status `approved` in `SocialPostDrafts`
2. Admin triggers publish via `POST /api/v1/social-posts/{id}/publish`
3. `SocialPublishJob` queued
4. Job calls the appropriate Meta client method based on `Platform` field
5. On success:
   - Create `PublishedPost` record with `ExternalPostId`
   - Update `SocialPostDrafts.Status = published`
   - Write audit log entry
6. On failure:
   - Create `PublishedPost` record with `PublishStatus = failed` and `FailureReason`
   - Keep `SocialPostDrafts.Status = approved` (eligible for retry)
   - Surface failure in admin UI

---

### Instagram-Specific Notes

Instagram publishing via Graph API requires a two-step process:
1. Create a media container: `POST /{ig-user-id}/media`
2. Publish the container: `POST /{ig-user-id}/media_publish`

The `MetaClient` handles both steps internally. The caller only calls `PublishInstagramPostAsync()`.

---

### Error Handling

| Condition | Action |
|---|---|
| 190 Invalid token | Throw `IntegrationAuthException` — surface in admin UI as integration error |
| 100 Invalid parameter | Fail job with error detail stored |
| 368 Blocked content | Fail job with reason stored — do not retry |
| 500 / transient | Retry with backoff up to 3 times |

---

## 5. YouTube Integration

**Module owner:** Sessions

**API:** YouTube Data API v3

**Authentication:** API key sourced from Key Vault key `YouTube:ApiKey`

**Scope:** Read-only. Fetches video metadata and caption tracks. No write operations.

---

### Interface

```csharp
public interface IYouTubeClient
{
    Task<YouTubeVideoMetadata> GetVideoMetadataAsync(string videoId, CancellationToken ct);
    Task<string?> GetTranscriptTextAsync(string videoId, CancellationToken ct);
}
```

---

### Request/Response Models

```csharp
public record YouTubeVideoMetadata(
    string VideoId,
    string Title,
    string ChannelName,
    int? DurationSeconds,
    bool HasCaptions
);
```

---

### Transcript Retrieval Strategy

1. Parse video ID from the submitted YouTube URL
2. Call `GetVideoMetadataAsync()` to confirm video exists and check `HasCaptions`
3. If captions exist: call `GetTranscriptTextAsync()` to fetch and concatenate caption segments
4. If captions do not exist: set `TranscriptStatus = failed` with message "No captions available"
5. Store cleaned text in `SessionTranscripts.TranscriptText`
6. Set `SourceMethod = youtube_captions`

**Note:** Azure AI Speech for audio-based transcription is a planned future enhancement
for videos without captions. The `SourceMethod` field reserves space for this.

---

### Error Handling

| Condition | Action |
|---|---|
| Invalid YouTube URL | Validation error at API layer — do not create session record |
| Video not found (404) | Fail ingestion job with clear message |
| Video is private | Fail ingestion job with "Video is not publicly accessible" |
| No captions available | Fail ingestion job with "No captions available for this video" |
| 403 Quota exceeded | Retry after backoff — log alert |

---

## 6. Azure Blob Storage Integration

**Module owner:** Reports (primary), Shared (interface)

**Authentication:** Managed Identity (preferred in Azure) or connection string from Key Vault

**Container:** `reports` for PDF exports

---

### Interface (in Shared module)

```csharp
public interface IBlobStorageClient
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken ct);
    Task<string> GetPresignedDownloadUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken ct);
    Task DeleteAsync(string containerName, string blobName, CancellationToken ct);
}
```

---

### Blob Naming Convention

PDF reports are stored with the path:
```
reports/{eventId}/{reportId}_{timestamp}.pdf
```

Example: `reports/1/42_20250315T140000Z.pdf`

The full blob path is stored in `PdfReports.BlobPath`.

---

## 7. Integration Connection Registry

The `IntegrationConnections` table tracks the connection state of all integrations.

```
IntegrationConnections
├── Id              int           PK, identity
├── Provider        nvarchar(64)  NOT NULL  -- 'eventbrite' | 'mailchimp' | 'meta' | 'youtube'
├── DisplayName     nvarchar(256) NOT NULL
├── IsEnabled       bit           NOT NULL, default 0
├── LastSyncAt      datetime2     NULL
├── MetadataJson    nvarchar(max) NULL  -- non-secret config like org IDs, page IDs
└── CreatedAt       datetime2     NOT NULL
```

The Settings / Integrations page reads from this table. Credentials
themselves are never stored here — only in Azure Key Vault.
