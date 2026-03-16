# 01 — Product Specification
# Event Management Portal

> Version: 1.0
> Status: Approved
> Audience: All contributors, AI coding agents, product stakeholders

---

## 1. Product Purpose

The Event Management Portal is a centralized, internal admin platform
built for event operations teams. It replaces fragmented manual workflows
across multiple third-party dashboards with a single portal that connects
Eventbrite, Mailchimp, Meta, and YouTube into one operational surface.

The portal does not serve attendees. It serves the admin team that runs
events. There is one role: Admin.

---

## 2. Business Capabilities

### 2.1 Registration Analytics

**Problem:** Admins currently check Eventbrite directly to monitor
registrations, with no way to track trends over time or compare ticket
types without manual exports.

**Capability:** The portal syncs event and registration data from
Eventbrite on a scheduled basis and presents it as actionable analytics.

**Key features:**
- Sync events from Eventbrite and store them locally
- Sync ticket types and map them to events
- Sync attendee and order data
- Display total registration count by ticket type
- Display daily registration trends as a chart, segmented by ticket type
- Surface check-in status when available from Eventbrite data
- All dashboard data served from stored snapshots, not live Eventbrite queries

**User stories:**
- As an admin, I can view all synced events and their current registration totals
- As an admin, I can see how many registrations each ticket type has received
- As an admin, I can view a daily trend chart showing registration growth over time
- As an admin, I can trigger a manual sync to pull the latest Eventbrite data
- As an admin, I can see when the last sync occurred

---

### 2.2 SMS Communications

**Problem:** Admins need to send targeted SMS messages to event audiences
but currently rely on Mailchimp's own UI with no local record of
campaign history or send confirmation tied to the portal.

**Capability:** The portal allows admins to compose, review, and send
SMS campaigns through Mailchimp without leaving the portal. All
campaigns and send outcomes are recorded locally.

**Key features:**
- Browse Mailchimp audience segments eligible for SMS
- Compose an SMS campaign message with character count awareness
- Preview the message before sending
- Send the campaign to the selected segment via Mailchimp
- Store campaign draft, send confirmation, and provider message ID
- Display send status and delivery metrics

**User stories:**
- As an admin, I can compose an SMS campaign for a specific event
- As an admin, I can select a Mailchimp audience segment as the recipient group
- As an admin, I can review the message before sending
- As an admin, I can send the campaign and receive confirmation
- As an admin, I can view the history of sent SMS campaigns and their status

---

### 2.3 Social Media Marketing

**Problem:** Writing social media posts for every event session, in the
right tone, with hashtags and calls-to-action, is repetitive and
time-consuming. Admins have no central place to manage, approve, and
publish social content.

**Capability:** The portal uses Azure OpenAI to generate social post
drafts for Facebook and Instagram. Admins review, edit, and approve
drafts before they are published through the Meta API.

**Key features:**
- Generate post drafts using AI from event or session metadata
- Generate promotion posts (pre-event) and recap posts (post-session)
- Generate captions, quote card text, hashtags, and call-to-action suggestions
- Save all drafts with status tracking (Draft → Reviewed → Approved → Published)
- Allow admin to edit any draft before approval
- Publish approved posts to Facebook and/or Instagram via Meta API
- Store publish confirmation and external post ID
- Log all generated and published content per event

**User stories:**
- As an admin, I can request AI-generated social post drafts for an event
- As an admin, I can choose Facebook, Instagram, or both as target platforms
- As an admin, I can edit a draft before approving it
- As an admin, I can approve a draft to queue it for publishing
- As an admin, I can publish an approved post and confirm it was sent
- As an admin, I can view the full post history for any event

---

### 2.4 YouTube Session Ingestion and Summarization

**Problem:** Event sessions are recorded to YouTube but extracting key
takeaways, memorable quotes, and action points requires manually watching
or transcribing each video.

**Capability:** Admins submit a YouTube URL, and the portal automatically
retrieves the transcript, generates a structured session summary, extracts
quotes and themes, and stores the result for review and export.

**Key features:**
- Accept a YouTube URL and create a session record
- Retrieve session transcript via YouTube pipeline (transcript API or caption extraction)
- Generate structured session summary using Azure OpenAI
- Extract key quotes in structured JSON format
- Extract themes and action points
- Display summary and quotes for admin review
- Store approved summaries for PDF compilation
- Optionally schedule social snippet posts from extracted quotes

**User stories:**
- As an admin, I can submit a YouTube URL to start a session ingestion
- As an admin, I can see the transcript retrieval and summary generation status
- As an admin, I can view the generated summary, quotes, and action points
- As an admin, I can approve a session summary for inclusion in the PDF report
- As an admin, I can see all sessions for an event and their summary status

---

### 2.5 PDF Takeaway Report

**Problem:** After an event, producing a polished summary document that
compiles all session summaries is time-intensive and requires copying
content from multiple places.

**Capability:** Once session summaries are approved, the portal compiles
them into a branded, downloadable PDF report that covers the full event.

**Key features:**
- Compile all approved session summaries for a given event into a single PDF
- AI-assisted narrative introduction and event overview section
- Ordered sections per session: summary, quotes, key takeaways, action points
- Export to Azure Blob Storage
- Allow admin to download the PDF from the portal
- Maintain a version history of generated reports per event

**User stories:**
- As an admin, I can request a PDF report for an event after sessions are summarized
- As an admin, I can download the generated PDF from the portal
- As an admin, I can see when each report was generated and by whom

---

## 3. Out of Scope (v1)

The following capabilities are explicitly excluded from the initial build:

- Attendee-facing portal or registration UI
- Multi-tenant or organization-level account management
- WhatsApp or email campaign channels
- Automatic quote card image rendering
- Multilingual session summaries
- Attendee feedback collection or sentiment analysis
- Sponsor dashboards or custom reporting templates
- Role-based permissions beyond the single Admin role
- Real-time event data streaming from Eventbrite
- Live session monitoring or streaming ingestion

---

## 4. Success Criteria

| Capability | Measurable Outcome |
|---|---|
| Registration Analytics | Admin can see daily trend charts within 24h of a registration event |
| SMS Communications | Admin can send a campaign and confirm delivery via stored provider ID |
| Social Publishing | AI draft to published post in under 5 minutes with admin approval |
| YouTube Ingestion | Session summary generated from URL within 3 minutes of submission |
| PDF Report | Full event PDF generated and downloadable within 2 minutes of request |

---

## 5. Non-Goals Clarification

This product does **not** aim to replace Eventbrite, Mailchimp, or Meta
as primary operational tools. It provides an **operational layer** on top
of them that reduces the time admins spend switching between platforms
and manually compiling data.
