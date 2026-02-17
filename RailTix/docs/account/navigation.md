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
- Payment Settings — `/account/payment` — icon: `outline/credit-card.svg`
- My Events — `/account/events` — icon: `outline/calendar.svg`
- Reports (future) — `/account/reports` — icon: `outline/chart-bar.svg`
- Check-in (future) — `/account/checkin` — icon: `outline/qr-code.svg`

3) Administration (Admin)
- Admin Center (future) — `/account/admin` — icon: `outline/shield-check.svg`

Notes:
- Do not include “Edit Profile” in the left menu. Access to edit is via a cog icon on the profile view (see `profile.md`).
- If an item is not yet implemented, it should either be hidden or shown as disabled per product guidance.
- See `icons.md` for the inline SVG guidance and outline/solid directories.
- If Stripe setup is incomplete for an Event Manager/Admin seller, show a visual warning state on "Payment Settings" and use this route as the primary CTA target from event creation/publish guards.

### Contextual Items (Event-Specific / Hi.Events Parity Target)
When in an event context (e.g., `/account/events/:id/...`), the sidebar should display a dedicated event operations sub-section beneath “My Events”:
- Dashboard — `/account/events/:id` (or `/account/events/:id/dashboard`)
- Tickets — `/account/events/:id/tickets`
- Attendees — `/account/events/:id/attendees`
- Orders — `/account/events/:id/orders`
- Questions — `/account/events/:id/questions`
- Messages — `/account/events/:id/messages`
- Capacity — `/account/events/:id/capacity`
- Check-in Lists — `/account/events/:id/check-in-lists`
- Homepage Design — `/account/events/:id/homepage-design`
- Widget Embed — `/account/events/:id/widget-embed`

Optional/adjacent items (phase-dependent):
- Promo Codes — `/account/events/:id/promos`
- Reports — `/account/events/:id/reports`
- Event Settings — `/account/events/:id/settings`

This contextual block is shown only while viewing a specific event and must honor ownership/role checks.

### Visual/Interaction Details
- Icon + Label Pairing: Icon at 20–24px; label baseline-aligned; adequate spacing for readability.
- Active State: High-contrast background or left bar accent; icon and label color shift.
- Hover/Focus: Distinct state with accessible contrast ratios (WCAG AA minimum).
- Collapsed Mode: Width reduces to show icons only; tooltip on hover for labels.

### Global Header Account Context Menu
For authenticated users, the top-right navbar account control is an icon-triggered popout menu.

- Trigger: avatar/user icon + display name + chevron.
- Interaction: click opens/closes; click-outside and `Esc` close; smooth opacity/translate transitions.
- Visual style: dark RailTix surface with subtle custom warm/violet accenting.
- `Logout` is rendered as the final row, separated by a divider.

Menu item order (top → bottom), role-aware:
1) All authenticated users
- Profile & Settings — `/Account/Manage` (temporary profile/settings entry point)
2) Event Manager and Admin
- Payment Settings — `/account/payment`
- My Events — `/account/events`
- Create Event — `/account/events/create`
3) Admin
- Admin Center — `/account/admin`
- Content Management — `/account/admin/content/pages`
4) Final action
- Logout (POST) — `/Account/Logout`

Implementation note:
- When dedicated `/account/profile*` routes are shipped, rename "Profile & Settings" and repoint accordingly.

### Empty/Edge States
- If a role has access to no items (unlikely), show a friendly message and a link to Profile.
- If the user loses a role mid-session, remove items on the next render and handle current-page access gracefully (redirect if necessary).
- If user can access account pages but not a target event, show a friendly 403 and keep global account navigation visible.


