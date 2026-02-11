## RailTix — Roles & Permissions (High Level)

### Roles (Inheritance)
- Admin ⟹ inherits Event Manager and Site User
- Event Manager ⟹ inherits Site User
- Site User

### Permission Groups
- Account & Profile
  - View/Edit own profile
  - View security activity (recent logins, device sessions)
- Discovery & Purchase
  - Browse & search events
  - Purchase credits
  - Buy tickets (using credits and/or card)
  - View orders & tickets
- Event Management
  - Request Event Manager upgrade
  - Create/Edit/Publish own events
  - Define ticket types, pricing, inventory
  - Mark ticket types as kids tickets
  - Issue/Manage promo codes and comp tickets
  - View sales & attendance reports (own events)
  - Export attendees/orders
  - View guardian details for attendees on own events (for check-in verification)
  - Manage check-in devices & staff for own events
  - Process refunds within policy (own events)
- POS & Check-in
  - Check-in tickets for own events
  - Deduct credits via POS (authorized staff)
- Administration
  - Approve Event Manager upgrades
  - Manage users & roles
  - Moderate events/content
  - Manage CMS pages & components
  - Manage tax/fees, categories/tags, system settings
  - View system logs/audit trail

### Matrix (Summary)
- Site User
  - Account & Profile (own)
  - Discovery & Purchase
  - Orders & Tickets (own)
  - POS: present wallet QR only
- Event Manager = Site User plus:
  - Event Management (own events)
  - POS & Check-in (own events; staff-control)
- Admin = Event Manager plus:
  - Administration (global)

### Role Upgrade Flow
1. Site User submits an upgrade request with justification and business info.
2. Admin reviews (KYC or basic verification as needed).
3. Admin approves/denies; on approval, user gains Event Manager role.
4. Audit log records request, review, and decision.

### Authorization Principles
- Policy-based checks wrap resource ownership (e.g., event owner).
- Inheritance ensures higher roles can perform lower-level actions.
- Sensitive actions require explicit policies (refunds, POS).
- Admin retains global powers by role, but dedicated global admin UI for cross-event guardian-detail management is deferred in v1.


