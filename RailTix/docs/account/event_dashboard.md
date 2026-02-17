## Account — Event Dashboard (Hi.Events Parity Target)

Status: Draft v0.1  
Audience: Admin, Event Manager (event ownership/policy constrained)  
Primary route: `/account/events/:id` (or `/account/events/:id/dashboard`)

### Purpose
- Provide a dedicated per-event operations area with its own left navigation.
- Support fast workflow handoff from Account Dashboard and My Events into event-specific management.
- Mirror the structure and mental model of `hi.events.full` event management.

### Entry Points
- Account Dashboard active event quick links.
- My Events list row actions (“Open Event Dashboard”).
- Deep links from notifications (future).

### Core Navigation (Per-Event)
Order reflects expected primary workflows:
1. Dashboard — `/account/events/:id` (or `/account/events/:id/dashboard`)
2. Tickets — `/account/events/:id/tickets`
3. Attendees — `/account/events/:id/attendees`
4. Orders — `/account/events/:id/orders`
5. Questions — `/account/events/:id/questions`
6. Messages — `/account/events/:id/messages`
7. Capacity — `/account/events/:id/capacity`
8. Check-in Lists — `/account/events/:id/check-in-lists`
9. Homepage Design — `/account/events/:id/homepage-design`
10. Widget Embed — `/account/events/:id/widget-embed`

Optional/adjacent modules (phase-dependent):
- Promo Codes — `/account/events/:id/promos`
- Reports — `/account/events/:id/reports`
- Event Settings — `/account/events/:id/settings`

### Module Responsibilities (High-Level)
- Dashboard: event KPI summary (views, sales, orders, attendees, refunds), quick tasks.
- Tickets: products/pricing/inventory/sale windows/taxes and fees.
- Attendees: attendee records, search/filter/export, check-in state.
- Orders: order list, status, payment state, refunds and resend flows.
- Questions: custom checkout questions and answer visibility.
- Messages: event comms/bulk sending by segment.
- Capacity: event and shared capacity controls and warnings.
- Check-in Lists: list/device/staff level check-in operations.
- Homepage Design: event landing page composition/theme controls.
- Widget Embed: embeddable snippets/config for external sites.

### Routing + UX Rules
- `/account/events/:id` should resolve to the Dashboard module by default.
- Per-event nav is visible only in event context pages.
- Breadcrumb/back link to My Events is always available.
- Unauthorized event access (non-owner Event Manager) returns friendly 403 and safe navigation fallback.

### Implementation Note
- This spec defines required parity and information architecture.
- Implementation can be phased, but route and navigation contracts should be reserved now.

