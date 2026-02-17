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
- `/account/payment`
  - GET → Stripe Connect/payment setup and status (Admins and Event Managers only).

Future (stubs; role-gated as appropriate)
- `/account/events` — list of owned events
- `/account/events/new` — create event
- `/account/events/:id` — event dashboard landing (redirect/default to dashboard tab)
- `/account/events/:id/dashboard` — event dashboard overview
- `/account/events/:id/tickets` — ticket/product management
- `/account/events/:id/attendees` — attendee management
- `/account/events/:id/orders` — order management
- `/account/events/:id/questions` — checkout questions
- `/account/events/:id/messages` — messaging center
- `/account/events/:id/capacity` — capacity controls
- `/account/events/:id/check-in-lists` — check-in lists and operations
- `/account/events/:id/homepage-design` — event homepage designer
- `/account/events/:id/widget-embed` — widget embed tools/config
- `/account/reports` — aggregated reports
- `/account/checkin` — check-in tools
- `/account/admin` — admin center

### Permissions (Minimum Role)
- `/account/profile*` → Site User
- `/account/` (Dashboard) → Event Manager
- `/account/payment*` → Event Manager
- `/account/events*` → Event Manager
- `/account/events/:id*` → Event Manager + ownership/resource policy
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
- If a user has the right role but does not own the event resource, return friendly 403 and keep account navigation visible.
- Stripe gate:
  - Event Managers (and Admins acting as event sellers) must complete Stripe Connect before event creation.
  - Accessing `/account/events/create` without Stripe setup should redirect to `/account/payment` with return URL context.
  - Server-side create endpoints must enforce the same rule (`403` or policy failure) even if UI checks are bypassed.

### Audit & Security Considerations
- Sensitive actions (password change) must be logged/audited as per platform policy.
- Use policy checks on resources (e.g., event ownership) beyond role checks.


