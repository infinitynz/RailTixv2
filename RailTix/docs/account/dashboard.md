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
- My Events List
  - Table or cards of events the user owns
  - Key details: title, status (draft/published), start date, sales (future)
  - Row actions: Manage, View, (future: Duplicate, Archive)
- Quick Actions
  - Create New Event — navigates to `/account/events/new` (future)
  - View All Events — navigates to `/account/events`

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


