## Account — Dashboard (Admins & Event Managers)

Status: Draft v0.1  
Audience: Admin, Event Manager  
Route: `/account/`

### Purpose
- Provide a role-appropriate landing page for Event Managers and Admins.
- Summarize account activity and surface quick actions for managing events.

### Content (MVP)
- Header: “Dashboard”
- Account Summary (placeholder for future metrics)
  - Number of events created
  - Upcoming events (next 30–60 days)
  - Recent sales (future)
  - Approvals & flags (Admin only; future)
- Active Events Quick Access
  - If user has events, show a prioritized list of active events at the top of the dashboard.
  - Active means event is `LIVE` and not ended; if there are no active events, show the most recently updated owned events instead.
  - Each row/card includes: title, status, next start date, timezone, and a primary action: “Open Event Dashboard”.
  - “Open Event Dashboard” navigates to `/account/events/:id` (or canonical event dashboard route).
  - Default ordering:
    1) Active events sorted by nearest upcoming start.
    2) Fallback events sorted by latest update.
- My Events List (secondary)
  - Table/cards of owned events remains available for full browsing and management.
  - Key details: title, status, start date, sales (future).
  - Row actions: Open Event Dashboard, View Public Event, (future: Duplicate, Archive).
- Quick Actions
  - Create New Event — navigates to `/account/events/new` (future)
  - View All Events — navigates to `/account/events`

### Event Dashboard Handoff
- Dashboard quick links must open the per-event dashboard, not the generic event list.
- Per-event management modules are specified in `event_dashboard.md` and mirrored in `navigation.md`.

### Empty State
- If the user has no events:
  - Show illustration + message: “You haven’t created any events yet.”
  - Primary action: “Create New Event”

### Permissions
- Only Admins and Event Managers can land on and view the Dashboard.
- Site Users who reach `/account/` should be redirected to `/account/profile`.

### Performance & Loading
- Use lightweight summaries; defer heavy analytics to “Reports”.
- Paginate events if the user owns many; show skeleton loaders during fetch.

### Future Enhancements
- Sales/attendance snapshots
- Task/alert center (pending approvals, expiring inventories)
- Shortcuts to promo codes and check-in configuration


