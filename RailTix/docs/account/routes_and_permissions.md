## Account — Routes & Permissions

Status: Draft v0.1  
All `/account/*` routes require authentication.

### Default Landing Logic
- If role is Admin or Event Manager → land on `/account/` (Dashboard).
- If role is Site User → land on `/account/profile`.
- If multiple roles are present, use the highest (Admin > Event Manager > Site User).

### Core Routes (MVP)
- `/account`  
  - GET → Resolves to Dashboard or redirects to `/account/profile` based on role (above).
- `/account/profile`  
  - GET → Profile view (all roles).
- `/account/profile/edit`  
  - GET → Profile edit form (all roles).
- `/account/profile`  
  - PATCH/PUT → Update profile and/or password; on success redirect to `/account/profile`.
- `/account/` (Dashboard)  
  - GET → Dashboard view (Admins and Event Managers only).

Future (stubs; role-gated as appropriate)
- `/account/events` — list of owned events
- `/account/events/new` — create event
- `/account/reports` — aggregated reports
- `/account/checkin` — check-in tools
- `/account/admin` — admin center

### Permissions (Minimum Role)
- `/account/profile*` → Site User
- `/account/` (Dashboard) → Event Manager
- `/account/events*` → Event Manager
- `/account/reports*` → Event Manager
- `/account/checkin*` → Event Manager
- `/account/admin*` → Admin

See `../ROLES_AND_PERMISSIONS.md` for inheritance and detailed capabilities.

### Layout & Sidebar
- All `/account/*` pages use a dedicated Account layout that renders the left sidebar (see `navigation.md`).
- The layout injects role-aware items and highlights the current route.

### Redirect & Guard Behavior
- Unauthenticated access to any `/account/*` route prompts login; after login, continue to intended route and then apply role-based landing if the route is `/account`.
- If a user without the minimum role accesses a restricted route:
  - Prefer a friendly 403 page with a link to an accessible destination (e.g., Profile).
  - If currently rendered inside `/account/*`, also render the sidebar so navigation remains consistent.

### Audit & Security Considerations
- Sensitive actions (password change) must be logged/audited as per platform policy.
- Use policy checks on resources (e.g., event ownership) beyond role checks.


