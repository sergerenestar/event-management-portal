# 05 — AI Workflows
# Event Management Portal — Agent Flows and Guardrails

> Version: 1.0
> Status: Approved
> Audience: Backend contributors, AI coding agents, reviewers

---

## 1. AI Design Principles

- AI is a first-class orchestration layer, not a side feature
- No AI agent may publish, send, or export without admin approval
- All AI agents are behind interfaces — prompts can evolve without touching services
- Raw AI output is always stored separately from approved/final output
- Every prompt and response is logged via Application Insights
- All agent outputs that feed downstream actions use structured JSON schema
- AI failures are caught, logged, and surfaced as job failures — never silently dropped
- Prompt templates are version-controlled in source code, not in a database

---

## 2. Azure OpenAI Configuration

| Setting | Value |
|---|---|
| Service | Azure OpenAI |
| Default reasoning model | GPT-4.1 |
| Multimodal model | GPT-4o |
| API pattern | Azure OpenAI REST via typed client |
| Response format | JSON schema enforced on all structured outputs |
| Credentials | Azure Key Vault — never hardcoded |
| Retry policy | 3 attempts with exponential backoff on 429 and 503 |
| Logging | Prompt + response logged as structured events via Serilog |

---

## 3. Agent Definitions

---

### 3.1 Session Summary Agent

**Purpose:** Given a session transcript and metadata, generate a
structured executive summary with key takeaways, quotes, themes, and
action points.

**Trigger:** Admin calls `POST /api/v1/sessions/{id}/generate-summary`
after transcript ingestion is complete.

**Model:** GPT-4.1

**Interface:**
```csharp
public interface ISessionSummaryAgent
{
    Task<SessionSummaryOutput> GenerateAsync(SessionSummaryInput input, CancellationToken ct);
}
```

**Input:**
```json
{
  "sessionTitle": "Keynote: AI in 2025",
  "speakerName": "Jane Smith",
  "eventName": "Tech Summit 2025",
  "transcriptText": "Full transcript text...",
  "eventContext": "A technology conference focused on enterprise AI adoption"
}
```

**System prompt (template — version controlled):**
```
You are an expert content analyst for professional events.
Given a session transcript, produce a structured JSON summary.
Be concise but thorough. Use professional tone.
Return ONLY valid JSON matching the schema below.
Do not include markdown, preamble, or explanation.
```

**Output JSON schema:**
```json
{
  "executiveSummary": "string (2-4 paragraphs in markdown)",
  "keyTakeaways": ["string", "string"],
  "themes": ["string", "string"],
  "actionPoints": ["string", "string"],
  "quotes": [
    {
      "quoteText": "string",
      "attributedTo": "string",
      "contextNote": "string or null"
    }
  ],
  "modelUsed": "string",
  "promptVersion": "string"
}
```

**Storage:**
- Raw JSON output stored in `SessionSummaries.RawAiOutputJson` (if added) or logged separately
- Parsed fields stored in `SessionSummaries` and `SessionQuotes` tables
- Status updated to `complete` on success, `failed` on error

**Guardrails:**
- Output must parse against the JSON schema — if it fails, job fails with error
- Summary not visible to admin until job completes successfully
- Admin must approve before summary appears in PDF

---

### 3.2 Marketing Agent (Social Post Generator)

**Purpose:** Given event or session metadata, generate social post
drafts for Facebook and/or Instagram including caption, hashtags, and CTA.

**Trigger:** Admin calls `POST /api/v1/social-posts/generate`

**Model:** GPT-4.1 (or GPT-4o if image context is added in future)

**Interface:**
```csharp
public interface IMarketingAgent
{
    Task<MarketingAgentOutput> GeneratePostAsync(MarketingAgentInput input, CancellationToken ct);
}
```

**Input:**
```json
{
  "eventName": "Tech Summit 2025",
  "eventDate": "2025-06-01",
  "venue": "Convention Center",
  "postType": "promotion",
  "platform": "instagram",
  "sessionTitle": null,
  "sessionSpeaker": null,
  "quotes": [],
  "audienceDescription": "Tech professionals and enterprise decision-makers",
  "contextNotes": "Focus on AI track launch"
}
```

**System prompt (template — version controlled):**
```
You are a professional social media content writer for technology events.
Generate a social media post for the specified platform and post type.
Use concise, engaging language suited to the platform's tone.
Include 3-5 relevant hashtags. Include a clear call-to-action.
Return ONLY valid JSON matching the schema below.
```

**Output JSON schema:**
```json
{
  "platform": "facebook | instagram",
  "postType": "promotion | recap | quote",
  "caption": "string",
  "hashtags": ["string"],
  "callToAction": "string",
  "alternativeCaptions": ["string", "string"]
}
```

**Storage:**
- Raw output stored in `SocialPostDrafts.RawAiOutputJson`
- Parsed caption and hashtags stored as editable draft fields
- Status set to `draft` — admin must edit and approve before publishing

**Guardrails:**
- Agent output is a draft only — never published automatically
- `alternativeCaptions` field gives admin options without extra API calls
- Admin can edit any field before approval

---

### 3.3 PDF Narrative Agent

**Purpose:** Given all approved session summaries for an event, generate
the cover page text, event overview narrative, and section introductions
for the PDF report.

**Trigger:** Called internally by `PdfCompilationJob` when report generation starts

**Model:** GPT-4.1

**Interface:**
```csharp
public interface IPdfNarrativeAgent
{
    Task<PdfNarrativeOutput> GenerateAsync(PdfNarrativeInput input, CancellationToken ct);
}
```

**Input:**
```json
{
  "eventName": "Tech Summit 2025",
  "eventDate": "2025-06-01",
  "venue": "Convention Center",
  "totalSessions": 4,
  "sessionTitles": ["Keynote: AI in 2025", "Workshop: Prompt Engineering"],
  "speakers": ["Jane Smith", "Bob Lee"],
  "overallThemes": ["AI Adoption", "Ethics", "Productivity"]
}
```

**Output JSON schema:**
```json
{
  "coverPageTitle": "string",
  "coverPageSubtitle": "string",
  "eventOverviewNarrative": "string (markdown, 3-5 paragraphs)",
  "sectionIntroductions": [
    {
      "sessionTitle": "string",
      "introText": "string (1-2 sentences)"
    }
  ]
}
```

**Storage:**
- Output used inline during PDF compilation — stored in `PdfReports` metadata if needed
- Not directly surfaced in admin UI

**Guardrails:**
- PDF is not generated until all required approved summaries are confirmed present
- Admin triggers report generation manually — not automatic

---

## 4. YouTube Ingestion Pipeline

The YouTube ingestion workflow is not a single AI agent call — it is a
multi-step pipeline with distinct jobs and status checkpoints.

### Step 1 — Session Record Created

Admin submits YouTube URL via `POST /api/v1/sessions`.

System creates a `SessionIngestion` record with:
- `TranscriptStatus = pending`
- `SummaryStatus = pending`

System queues `TranscriptIngestionJob`.

---

### Step 2 — Transcript Ingestion Job

**Job:** `TranscriptIngestionJob`

**Process:**
1. Set `TranscriptStatus = processing`
2. Call YouTube Data API to fetch video captions (auto-generated or manual)
3. If captions available: extract and clean text
4. If captions unavailable: set status to `failed` with error message
   (Azure AI Speech for audio transcription is a future enhancement)
5. Store cleaned transcript text in `SessionTranscripts` table
6. Set `TranscriptStatus = complete`
7. Queue `SummaryGenerationJob` automatically if auto-summarize is enabled,
   or wait for admin to trigger manually

**Failure handling:**
- Set `TranscriptStatus = failed`
- Record error in `BackgroundJobStatuses`
- Surface failure in admin UI — do not auto-retry without admin action

---

### Step 3 — Summary Generation Job

**Job:** `SummaryGenerationJob`

**Process:**
1. Set `SummaryStatus = processing`
2. Load transcript from `SessionTranscripts`
3. Call `ISessionSummaryAgent.GenerateAsync()`
4. Parse and validate JSON output against schema
5. Persist structured summary to `SessionSummaries`
6. Persist individual quotes to `SessionQuotes`
7. Set `SummaryStatus = complete`

**Failure handling:**
- Set `SummaryStatus = failed`
- Store raw failed output for debugging
- Record error in `BackgroundJobStatuses`
- Surface failure in admin UI

---

### Step 4 — Admin Review and Approval

Admin reviews summary, quotes, and themes via the Sessions UI.
Admin approves selected quotes individually.
Admin approves the full summary via `POST /api/v1/sessions/{id}/summary/approve`.
Only approved summaries are eligible for PDF compilation.

---

## 5. Social Publishing Pipeline

### Step 1 — Draft Generation

Admin triggers via `POST /api/v1/social-posts/generate`.
`MarketingAgent` generates drafts for the requested platform and post type.
Drafts saved with status `draft`.

### Step 2 — Admin Review

Admin views draft in portal UI.
Admin may edit caption, hashtags, or media URL.
Admin updates status to `reviewed` (optional intermediate step).

### Step 3 — Approval

Admin calls `POST /api/v1/social-posts/{id}/approve`.
Status updated to `approved`.
`SocialPostApproval` audit record created.

### Step 4 — Publish

Admin calls `POST /api/v1/social-posts/{id}/publish`.
`SocialPublishJob` queued.
Job calls `IMetaClient.PublishAsync()`.
On success: `PublishedPost` record created, status updated to `published`.
On failure: failure logged in `PublishedPosts.FailureReason`, status remains `approved` for retry.

---

## 6. Prompt Template Versioning

All system prompts are stored as `.txt` or `.md` files in:

```
src/EventPortal.Api/Modules/<Module>/Agents/Prompts/
├── session-summary-system-v1.md
├── marketing-instagram-promotion-v1.md
├── marketing-facebook-promotion-v1.md
├── pdf-narrative-system-v1.md
```

Each prompt file includes:
- Version header comment
- System instruction text
- JSON schema the model must return

When a prompt is updated, the version suffix is incremented.
The agent logs `promptVersion` in every AI response record.

---

## 7. AI Observability

Every AI agent call logs the following as a structured Application Insights event:

| Field | Value |
|---|---|
| EventName | `AiAgentCall` |
| AgentName | e.g. `SessionSummaryAgent` |
| Model | e.g. `gpt-4.1` |
| PromptVersion | e.g. `v1` |
| InputTokenEstimate | int |
| OutputTokenEstimate | int |
| DurationMs | int |
| Success | bool |
| ErrorType | string or null |
| CorrelationId | string |

This enables cost tracking, latency monitoring, and failure rate alerting
without logging full prompt or response text by default (PII concern).

---

## 8. Guardrails Summary

| Guardrail | Enforcement |
|---|---|
| No auto-publish without approval | `SocialPublishJob` only processes `approved` status posts |
| No PDF without approved summaries | `PdfCompilationJob` validates approval count before starting |
| Raw output stored separately | `RawAiOutputJson` never overwritten by admin edits |
| Schema validation on all outputs | JSON parse failure = job failure, not silent partial save |
| Prompt templates version-controlled | Files in source, not DB — changes tracked in git |
| All AI calls logged | Structured Application Insights events on every call |
| Failures surfaced to admin | `BackgroundJobStatuses` table + admin UI job status view |
