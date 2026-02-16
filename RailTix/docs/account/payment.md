## Account — Payment Settings (Stripe Connect)

Status: Draft v0.1  
Audience: Event Managers and Admins  
Route: `/account/payment`

### Purpose
- Provide a single place for payment processing setup and health.
- Make Stripe Connect onboarding explicit and recoverable.
- Surface platform fee and payout context to organizers.

### Hi.Events Parity Requirements
The Event Manager experience should mirror `hi.events.full` account/payment behavior:
- State 1 (`NotConnected`): no `stripe_account_id` yet.
  - Primary CTA: **Connect with Stripe**
- State 2 (`Incomplete`): `stripe_account_id` exists, setup incomplete.
  - Primary CTA: **Finish Stripe Setup**
- State 3 (`Connected`): setup complete (`charges_enabled && payouts_enabled`).
  - Show connected badge/status and **Open Stripe Dashboard** link.

Backend parity target:
- `POST /accounts/{account_id}/stripe/connect` (create-or-get + possible `connect_url`)
- `GET /accounts/{account_id}/stripe/connect_accounts` (current state and account(s))

### Admin Variant
Admin payment settings differ from Event Manager view:
- Shows global platform Stripe connection status (test/live environment awareness).
- Shows and manages default platform fee policy (global default 2% for connected-account sales).
- Shows webhook and payout/reconciliation health indicators.
- Can define policy for admin-owned events (whether platform fee applies or is zero).

### Event Creation Gate
- Event Managers must complete Stripe setup before creating events.
- If incomplete:
  - "Create Event" controls should route users to `/account/payment`.
  - Direct route access to create pages should redirect to `/account/payment` with return URL.
  - Server-side event creation API must reject attempts when setup is incomplete.

### UX Notes
- If returning from Stripe onboarding (`is_return`, `is_refresh`), refresh connect state immediately.
- Always provide explanatory copy:
  - Why Stripe is required (secure payments + payouts).
  - What remains to complete setup.
- Include links to Stripe Dashboard and Connect documentation once connected.

### Acceptance Criteria
1. Event Manager sees exactly one of three Stripe states (NotConnected, Incomplete, Connected).
2. CTA text and behavior change by state (`Connect` -> `Finish Setup` -> `Connected` state actions).
3. Event creation is blocked when setup is incomplete (UI + server-side).
4. Admin sees global platform account controls and global fee policy controls.

