## RailTix — Account Section (Specs)

Status: Draft v0.1  
Owners: Product, Design, Engineering  
Scope: All pages under `/account/*` for authenticated users

### Goals
- Provide a consistent, role-aware Account area with a persistent left sidebar.
- Ensure clear default landing pages per role.
- Establish routes, basic behaviors, and acceptance criteria for initial MVP.

### Roles and Default Landing
- Admin and Event Manager: default to `/account/` (Dashboard).
- Site User: default to `/account/profile`.

Role inheritance is defined in `../ROLES_AND_PERMISSIONS.md`.

### Global UX Conventions
- All `/account/*` pages include a left sidebar with icon + label entries.
- Icons are served from `/images/icons` (see `icons.md` for mapping and guidelines).
- Active menu item is visually highlighted; keyboard navigation is supported.
- Mobile: sidebar collapses behind a menu button; content remains accessible.
- All `/account/*` routes require authentication.

### Out of Scope (for this iteration)
- Detailed event management flows (beyond listing a user’s own events).
- Admin center content beyond basic navigation.

### Sub-specs
- `navigation.md` — Left sidebar navigation and role-specific items
- `dashboard.md` — Account dashboard (Admins & Event Managers)
- `profile.md` — Profile view/edit and password change (all users)
- `routes_and_permissions.md` — Routing, default redirects, auth guards
- `icons.md` — Icon inventory and contribution guidelines

### Acceptance Criteria (MVP)
1. Visiting `/account` routes the user by role:
   - Admin/Event Manager → `/account/` (Dashboard).
   - Site User → `/account/profile`.
2. All `/account/*` pages render the left sidebar with iconized entries appropriate to the user’s role.
3. `/account/profile` shows the user’s information; a cog icon provides access to `/account/profile/edit`.
4. On save from `/account/profile/edit`, the user is redirected back to `/account/profile`.
5. Authorization gates prevent unauthorized access outside the user’s role/ownership scope (see `../ROLES_AND_PERMISSIONS.md`).

### References
- `../ROLES_AND_PERMISSIONS.md`


