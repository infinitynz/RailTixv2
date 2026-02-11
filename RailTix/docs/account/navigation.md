## Account — Left Sidebar Navigation

Status: Draft v0.1  
Applies to: All authenticated users on `/account/*`

### Requirements
- A persistent left sidebar is displayed on every page under `/account/*`.
- Entries include an inline SVG icon and a label. Do not use `<img>` for icons; inline SVG enables CSS theming/animation.
- Items are role-aware (see `../ROLES_AND_PERMISSIONS.md`) and only show when the user has access.
- The active item is visually highlighted; focus states are keyboard-accessible.
- Mobile: the sidebar collapses under a menu button; it overlays or pushes content based on viewport.

### Information Architecture
Order of sections and items (top → bottom). Only items meeting the user’s minimum role are rendered.

1) Common (All roles)
- Profile — `/account/profile` — icon: `outline/user.svg`

2) Event Management (Event Manager, Admin)
- Dashboard — `/account/` — icon: `outline/home.svg`
- My Events — `/account/events` — icon: `outline/calendar.svg`
- Reports (future) — `/account/reports` — icon: `outline/chart-bar.svg`
- Check-in (future) — `/account/checkin` — icon: `outline/qr-code.svg`

3) Administration (Admin)
- Admin Center (future) — `/account/admin` — icon: `outline/shield-check.svg`

Notes:
- Do not include “Edit Profile” in the left menu. Access to edit is via a cog icon on the profile view (see `profile.md`).
- If an item is not yet implemented, it should either be hidden or shown as disabled per product guidance.
- See `icons.md` for the inline SVG guidance and outline/solid directories.

### Contextual Items (Event-Specific)
When in an event context (e.g., `/account/events/:id/...`), the sidebar may display a contextual sub-section beneath “My Events”:
- Overview — `/account/events/:id`
- Tickets — `/account/events/:id/tickets`
- Promo Codes — `/account/events/:id/promos`
- Attendees — `/account/events/:id/attendees`
- Reports — `/account/events/:id/reports`

This contextual block is only shown while viewing a specific event and honors ownership/role checks.

### Visual/Interaction Details
- Icon + Label Pairing: Icon at 20–24px; label baseline-aligned; adequate spacing for readability.
- Active State: High-contrast background or left bar accent; icon and label color shift.
- Hover/Focus: Distinct state with accessible contrast ratios (WCAG AA minimum).
- Collapsed Mode: Width reduces to show icons only; tooltip on hover for labels.

### Empty/Edge States
- If a role has access to no items (unlikely), show a friendly message and a link to Profile.
- If the user loses a role mid-session, remove items on the next render and handle current-page access gracefully (redirect if necessary).


