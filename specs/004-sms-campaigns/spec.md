# Feature Specification: SMS Communication Module

**Feature Branch**: `004-sms-campaigns`
**Created**: 2026-03-20
**Status**: Draft
**Input**: Sprint 3 — Campaign and CampaignRecipient entities, Mailchimp SMS integration, campaign compose page with audience selection, human approval gate, and campaign history.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Compose and Send an SMS Campaign (Priority: P1)

An admin wants to notify a specific group of event attendees by SMS. They open the campaign compose page, select an audience segment built from registration data (e.g. all attendees of a specific event or ticket type), write a message, preview exactly what will be sent, and explicitly approve it before dispatch.

**Why this priority**: Core business value — the entire module exists to send targeted communications. All other stories depend on this flow working end-to-end.

**Independent Test**: Can be fully tested by creating a campaign, selecting an audience, writing a message, previewing it, approving it, and verifying that recipients receive the SMS. Delivers immediate communication value.

**Acceptance Scenarios**:

1. **Given** an admin is on the campaign compose page, **When** they select an audience segment, **Then** the page shows the number of recipients that will receive the message.
2. **Given** an admin has written a message and selected an audience, **When** they click Preview, **Then** they see the exact message text and recipient count before any action is taken.
3. **Given** an admin has previewed a campaign, **When** they click Confirm & Send, **Then** the campaign is dispatched and a success notification is shown.
4. **Given** an admin has previewed a campaign, **When** they click Cancel, **Then** nothing is sent and they return to the compose screen.
5. **Given** a campaign has been sent, **When** the admin views campaign history, **Then** the campaign appears with status Sent and a timestamp.

---

### User Story 2 — Select Audience from Registration Data (Priority: P2)

An admin wants to target a specific subset of attendees rather than sending a blanket message. They can filter the audience by event, ticket type, or registration status so the message reaches only the relevant people.

**Why this priority**: Untargeted mass SMS reduces engagement and increases opt-out rates. Audience segmentation is essential for effective communications.

**Independent Test**: Can be tested by verifying that selecting different segment filters updates the displayed recipient count and that only matching registrations are included when the campaign is sent.

**Acceptance Scenarios**:

1. **Given** an admin is composing a campaign, **When** they open the audience selector, **Then** they can filter by event name, ticket type, and registration status.
2. **Given** an admin selects a specific event, **When** the audience is resolved, **Then** only attendees registered for that event with a valid phone number are included.
3. **Given** no attendees in the selected segment have a phone number on record, **When** the admin tries to proceed, **Then** they are shown a warning that the segment has zero reachable recipients and cannot proceed.

---

### User Story 3 — Track Campaign Delivery Status (Priority: P3)

An admin wants to know how many messages were delivered, failed, or are still pending after a campaign is sent. They can view a history list showing all past campaigns and drill into each one for delivery statistics.

**Why this priority**: Delivery visibility is needed to assess communication effectiveness and identify issues, but it does not block the core send flow.

**Independent Test**: Can be tested independently by reviewing the campaign history page and verifying that delivery counts (sent, delivered, failed) update as the external SMS provider reports back status.

**Acceptance Scenarios**:

1. **Given** a campaign has been sent, **When** the admin views campaign history, **Then** each campaign shows total recipients, delivered count, failed count, and current status.
2. **Given** the SMS provider has reported delivery updates, **When** the admin refreshes the campaign history, **Then** delivery counts reflect the latest reported status.
3. **Given** an admin clicks on a campaign in history, **Then** they see per-recipient delivery status where available.

---

### Edge Cases

- What happens when a recipient's phone number is invalid or missing? They are excluded from the send and counted as unreachable.
- What happens if the SMS provider returns an error for the entire batch? The campaign status is set to Failed, the admin is notified, and no partial send is recorded as successful.
- What happens if the admin closes the browser during the approval step? The campaign remains in Draft status and nothing is sent.
- What happens if two admins attempt to send the same campaign simultaneously? Only the first confirmation is processed; the second receives an error that the campaign has already been sent.
- What happens when a campaign has been sent but delivery status has not yet been received? Status shows as Pending until the background polling job updates it.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow an admin to create a new SMS campaign by providing a message body and selecting an audience segment.
- **FR-002**: System MUST build audience segments from existing registration data, filterable by event, ticket type, and registration status.
- **FR-003**: System MUST display the resolved recipient count to the admin before any message is sent.
- **FR-004**: System MUST present a preview screen showing the exact message and recipient count, requiring explicit admin confirmation before dispatch.
- **FR-005**: System MUST NOT send any SMS without explicit admin approval — no automated or scheduled sends without a human confirmation step.
- **FR-006**: System MUST record each campaign with its message body, audience criteria, recipient list, send timestamp, and current status.
- **FR-007**: System MUST track delivery status per campaign (total sent, delivered, failed) and update it as the SMS provider reports back.
- **FR-008**: System MUST run a background job to poll the SMS provider for delivery status updates on campaigns in Pending status.
- **FR-009**: System MUST display a campaign history list showing all past campaigns with their status and delivery summary.
- **FR-010**: System MUST exclude recipients without a valid phone number from the send and count them as unreachable.
- **FR-011**: System MUST prevent a campaign from being sent more than once.
- **FR-012**: System MUST restrict all campaign operations to authenticated admin users.

### Key Entities

- **Campaign**: Represents a single SMS communication event. Attributes: message body, audience filter criteria, status (Draft, Sending, Sent, Failed), created timestamp, sent timestamp, total recipients, delivered count, failed count.
- **CampaignRecipient**: Represents one attendee targeted by a campaign. Attributes: link to Campaign, link to Registration/attendee, phone number at time of send, delivery status (Pending, Delivered, Failed), status updated timestamp.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can compose, preview, and send an SMS campaign in under 3 minutes from start to confirmation.
- **SC-002**: Audience segment resolution completes and displays recipient count in under 5 seconds for any event with up to 10,000 registrations.
- **SC-003**: 100% of campaigns require explicit admin confirmation — zero messages are sent without a deliberate approval action.
- **SC-004**: Delivery status for all recipients is reflected in campaign history within 15 minutes of the SMS provider reporting it.
- **SC-005**: Admins can identify the send status of any past campaign within 2 clicks from the main navigation.
- **SC-006**: Zero duplicate sends — each campaign can be dispatched exactly once regardless of concurrent admin actions.

---

## Assumptions

- Attendee phone numbers are stored in the Registration data already synced from Eventbrite (Sprint 2). If Eventbrite does not provide phone numbers, a data gap exists that must be addressed separately.
- Mailchimp SMS API supports audience creation, message dispatch, and delivery status polling.
- All recipients are assumed to have opted in to communications via their event registration — opt-out management is out of scope for this sprint.
- Campaign scheduling (send at a future time) is out of scope — only immediate send is supported in Sprint 3.
- Only admins interact with this module — there is no attendee-facing UI.
