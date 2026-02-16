## RailTix — Ticketing Core Specification (Hi.Events-Derived)

Status: Draft v0.2  
Applies to: Event creation, ticketing, checkout, orders, payments, attendees, check-in

Reference implementation in this repo (Hi.Events):
- `hi.events.full/backend/docs/database-schema.md`
- `hi.events.full/backend/docs/architecture-overview.md`
- `hi.events.full/backend/docs/events-and-jobs.md`
- `hi.events.full/frontend/src/types.ts`
- `hi.events.full/frontend/src/router.tsx`
- `hi.events.full/frontend/src/components/routes/event/HomepageDesigner`
- `hi.events.full/frontend/src/components/routes/event/TicketDesigner`
- `hi.events.full/frontend/src/components/routes/organizer/OrganizerHomepageDesigner`

### 1) Scope and Goals
- Build the core ticketing system inside RailTix (ASP.NET Core MVC, .NET 8) using Hi.Events feature parity where appropriate.
- Integrate with RailTix roles and business logic (Admin is platform owner; Event Managers sell on the platform).
- Provide a clean, secure checkout flow with inventory locking, Stripe Connect payouts, QR tickets, and check-in.
- English-only UI; timezone-aware but no multi-language content.
- No embeddable widgets.
- CMS is separate and should not drive event landing pages.

### 2) Roles, Tenancy, and Ownership
- **Admin**: Platform owner with master Stripe account. Full system access.
- **Event Manager**: Can create/manage their events, connect Stripe account, sell tickets.
- **Site User**: Can buy tickets, manage profile, view orders/tickets.
- **Attendee**: A Site User or guest. Guest attendees can later claim tickets to their account.
- No multi-tenant "account" system; the platform is a single tenant with per-event ownership and access checks.
- If new roles are needed, they must be explicit and map cleanly to the existing role inheritance model.

### 3) Core Domain Entities (RailTix Model)
Mirror the Hi.Events structure but align with RailTix naming and roles.

- **Organizer**: Branding container for a set of events (name, slug, socials, SEO).
- **OrganizerSettings**: Homepage theme, visibility (public/private/password), SEO, social links.
- **Event**: Title, description, dates, status, organizer, owner user, timezone, currency, location, slug.
- **EventSettings**: Event page theme, cover/logo images, SEO, checkout messages, payment settings, and rules content (`rules_enabled`, `rules_rich_text`).
- **Product**: Ticket or add-on (non-ticket product), including a `is_kids_ticket` flag.
- **ProductPrice**: Tiered pricing (early bird, VIP), donation/free/paid.
- **ProductCategory**: Group ticket types (e.g., General, VIP).
- **CapacityAssignment**: Shared capacity across products or event-wide capacity.
- **TaxAndFee**: Configurable taxes/fees, fixed or percentage, inclusive/exclusive display.
- **PromoCode**: Discount codes with usage limits and product restrictions.
- **Order**: Customer purchase, status, totals, payment info, reserved_until, and guardian details when kid tickets are included.
- **OrderItem**: Product line items with price snapshots.
- **Attendee**: One per ticket; check-in status, QR, personal details, and guardian detail snapshot fields used at check-in.
- **Question / QuestionAnswer**: Custom checkout questions (order-level or attendee-level).
- **RulesAcceptanceAudit**: Records acceptance of event rules during checkout (timestamp, IP, user agent).
- **CheckInList**: Multiple entry points or time windows.
- **AttendeeCheckIn**: Scan logs with device/staff metadata.
- **StripeAccount / StripePayment**: Connect account and payment intent tracking.
- **Invoice**: Optional invoice generation with numbering and tax details.
- **EventStatistics / EventDailyStatistics**: Aggregated metrics for dashboards.
- **Message**: Bulk messaging by event or ticket type.

### 4) Event Lifecycle and CRUD
- **Statuses**: DRAFT, LIVE, PAUSED, ARCHIVED.
- **Publish rules**:
  - At least one active ticket product with inventory.
  - Stripe Connect complete if card payments are enabled.
  - Offline payments allowed if instructions are set.
- **Event end rule**: Once the event has ended, ticket sales are disabled.
- **Event creation**: Title, start/end, timezone, currency, organizer, location.
  - Mandatory precondition for Event Managers: Stripe Connect setup complete.
  - If not connected, user is redirected to account payment setup and create is blocked server-side.
- **Event duplication**: Optional (copy settings, products, questions, capacities).
- **Event homepage**: Separate from CMS; driven by EventSettings and organizer branding.

### 5) Event URLs and Routing
- Event URL pattern: `/event/{event-slug}`.
- Slug is generated at creation and can be overridden if available.
- Slug uniqueness across all events and reserved routes.
- URL normalization to lowercase kebab-case; 301 redirect for non-canonical paths.
- If the event is ended, the page remains viewable but "Buy" actions are disabled.

### 6) Organizer Homepage and Branding
- Organizer has a public page listing its events.
- Organizer branding includes:
  - Cover image, logo, accent colors.
  - SEO fields: title, description, keywords, allow indexing.
  - Social links (Facebook, Instagram, etc.).
- Visibility options: public, private, or password protected.

### 7) Ticketing Model (Products)
- **Product types**: Ticket (entry) and General (add-ons).
- **Price types**: Paid, Free, Donation, Tiered.
- **Sale windows**: Start/end per product or price tier.
- **Visibility**: Hidden/locked tickets behind promo codes.
- **Per-order limits**: min/max per order.
- **Kids ticket flag**: Ticket products can be marked `is_kids_ticket = true`; this drives guardian detail requirements at checkout.
- **Capacity assignments**: Shared capacity across multiple products.
- **Price display**: Inclusive or exclusive tax/fees (event setting).
- **Platform fees**: Option to pass platform fees to buyer or absorb.

### 8) Taxes, Fees, and Currency
- Taxes/fees are configurable and can be attached to products.
- Support fixed or percentage amounts.
- Currency stored per event; all totals use event currency.
- Order stores a point-in-time snapshot for taxes/fees, products, and prices.

### 9) Checkout Flow (Public)
1. User selects products on event page.
2. System reserves inventory and creates an order in `RESERVED`.
3. If event `rules_enabled` is true:
   - Show a "View rules" link that opens rules content in a popup/modal.
   - Require a checkbox acknowledgement before continuing.
   - Write a `RulesAcceptanceAudit` record (accepted_at, ip_address, user_agent, order_id/event_id/user_id).
4. Collect attendee details (per order or per ticket).
5. If one or more selected ticket products have `is_kids_ticket = true`, collect guardian details once per order (required fields, client + server validation).
6. Apply promo code and compute totals.
7. Payment step (card, credits, or offline).
8. On success: order `COMPLETED`, tickets issued, emails sent.
9. On failure or timeout: reservation expires and inventory is released.

Guest checkout is allowed. If the buyer is not logged in, they can later claim tickets.

### 10) Order Lifecycle and Statuses
- `RESERVED`: Inventory locked; awaiting payment.
- `AWAITING_OFFLINE_PAYMENT`: Offline payment chosen; tickets issued but marked unpaid.
- `COMPLETED`: Payment confirmed.
- `CANCELLED`: Expired or cancelled by event manager/admin.
- `REFUNDED` / `PARTIALLY_REFUNDED`: Refund completed.

### 11) Inventory Locking and Concurrency (Azure/.NET)
The database is the source of truth; keep it simple and safe.

Reservation model (mirrors Hi.Events):
- **Do not increment `quantity_sold` at reservation time.**
- A reservation is represented by an `Order` in status `RESERVED` with `reserved_until` in the future.
- **Availability is computed** as: `initial_quantity_available - quantity_sold - reserved_quantity`.
  - `reserved_quantity` = sum of quantities for `RESERVED` orders where `reserved_until > now`.
- Inventory is only finalized (increment `quantity_sold`) after payment succeeds (or for free orders).

Reservation algorithm (SQL Server / Azure SQL):
- Start a transaction.
- Validate requested quantities against computed availability (including shared capacities).
- Create `Order` with `status = RESERVED` and `reserved_until = now + OrderTimeoutMinutes`.
- Create `OrderItems` with requested quantities.
- Commit.

Completion:
- On payment success (webhook confirmation) or free order completion, **increment `quantity_sold`** for each `OrderItem`.
- Mark order `COMPLETED` and attendees `ACTIVE`.

Release and expiry (mirrors Hi.Events):
- Availability calculations already exclude expired reservations (`reserved_until <= now`), so no stock decrement is required.
- A background job **marks expired `RESERVED` orders as `CANCELLED`** and logs the release.
- Abandoned orders can be explicitly marked `ABANDONED` by the buyer; these are also excluded from availability.

Idempotency:
- Use `Idempotency-Key` header for order creation and payment confirmation.
- Deduplicate retries on server to prevent duplicate orders or charges.

### 12) Stripe Connect and Payments
Roles:
- **Admin** owns the platform Stripe account.
- **Event Managers** connect their own Stripe accounts.

Flows:
- Event Manager completes Connect onboarding.
- Store `stripe_account_id` and `stripe_connect_setup_complete`.
- If card payments enabled, event cannot go LIVE until Stripe setup is complete.
- Event creation itself is blocked for Event Managers until Stripe setup is complete.
- UI parity target is Hi.Events account/payment flow: Connect -> Finish Setup -> Connected.

Card payments (recommended approach):
- Destination charge from platform account with `transfer_data.destination` to connected account.
- `application_fee_amount` for platform fees.
- Use Stripe Payment Intents and webhooks for confirmation.
- Default platform fee for connected-account sales is 2% (global policy).

Offline payments:
- Event-level setting: enable offline payments + instructions.
- Orders are marked `AWAITING_OFFLINE_PAYMENT`.
- Check-in can be allowed or blocked based on event setting.

Refunds:
- Partial or full refunds via Stripe.
- Order totals, event stats, and invoice records updated.

Additional Stripe details:
- See `docs/STRIPE_SPEC.md` for onboarding, payment intent flow, webhooks, and payouts.

### 13) Attendee Management
- An attendee record is created per ticket item.
- Attendee may be linked to a Site User or remain a guest.
- Event Managers can search/filter attendees and export lists.
- Attendee self-edit is optional and controlled by event settings.
- Custom questions can be order-level or attendee-level.
- If order contains kid tickets, one guardian record is required for the order and copied to attendee snapshot fields for operational check-in.

### 14) QR Tickets and Check-In
- QR codes are generated per attendee ticket (signed, short ID).
- Multiple check-in lists per event (door/session).
- Scan results:
  - First scan marks as checked in and logs time/device/user.
  - Duplicate scan shows prior check-in info.
- For kid tickets, scan view shows guardian details so staff can manually match guardian ID at entry.
- Offline scanning:
  - Allowed with local list cache.
  - Sync when back online; conflicts resolved by first check-in timestamp.

### 15) Email and PDF Tickets
- Order confirmation email includes ticket PDF and QR codes.
- Ticket design is configurable (logo, colors, footer text).
- Unpaid offline orders show "Awaiting Payment" on ticket.
- Resend tickets available to Event Manager and Admin.
- If event rules are enabled:
  - Purchase confirmation email includes event rules content.
  - Ticket PDF includes event rules content.
  - Rules rendered are always the latest event rules (no version snapshotting in v1).
- Resend ticket behavior:
  - Resend remains available.
  - Regenerated ticket PDF should include latest saved guardian details for account holders.

### 16) Dashboard and Reporting
- Event dashboards show:
  - Views, unique views
  - Sales totals (gross, tax, fee, net)
  - Orders, attendees, refunds
- Daily stats chart for the event.
- Admin dashboards aggregate across all events.
- Exports: orders, attendees, question answers, promo codes (CSV/XLSX).

### 17) Exclusions (Not in v1)
- Embeddable widgets.
- Multi-language UI.
- Affiliates.
- External webhooks (non-Stripe).

### 18) Open Decisions / Questions
- Stripe Connect type (Standard vs Express vs Custom).
- Offline payment handling: allowed to check in or not.
- Invoice numbering scope (global vs per organizer).
- Ticket transfer rules (allow name transfer or not).

### 19) Reservation Expiry Job (Hi.Events-Style Detail)
Purpose: keep orders tidy and ensure expired reservations are finalized consistently.

- Frequency: run every 1–5 minutes (configurable).
- Selection: `orders` where `status = RESERVED` and `reserved_until <= now`.
- Processing:
  - Mark order `CANCELLED` with reason `EXPIRED`.
  - Do **not** modify `quantity_sold` (reservation never incremented it).
  - Optionally notify buyer if an email was collected before payment.
- Concurrency safety:
  - Use `UPDATE ... WHERE status = RESERVED AND reserved_until <= now` to avoid races.
  - Job is idempotent: reprocessing an already-cancelled order is a no-op.

### 20) Idempotency Storage (DB-Backed)
Goal: ensure safe retries across API timeouts, webhook redelivery, and client resubmits.

Recommended table: `idempotency_keys`
- `id` (PK)
- `scope` (e.g., `order.create`, `payment.confirm`, `stripe.webhook`)
- `key` (client-supplied or webhook event id)
- `request_hash` (hash of request body to detect mismatches)
- `response_payload` (JSON, optional)
- `status` (IN_PROGRESS, COMPLETED, FAILED)
- `created_at`, `updated_at`
- `expires_at` (UTC)
- Unique constraint on (`scope`, `key`)

Flow:
- Begin transaction.
- Insert idempotency record with `IN_PROGRESS` if not exists; if exists:
  - If `request_hash` differs, return 409 conflict.
  - If `COMPLETED`, return stored `response_payload`.
  - If `IN_PROGRESS`, return 409/202 to indicate retry later.
- Execute handler logic and store `response_payload`, set `COMPLETED`.
- Commit.

TTL:
- Default `expires_at` = now + 24 hours (covers retries + webhook delays).
- Cleanup job runs daily to purge expired records.

### 21) Operational Resilience (High-Demand Readiness)
Keep this minimal but explicit for v1:
- **Rate limiting**: per IP + per session for checkout endpoints (configurable).
- **Webhook safety**: verify signatures, process asynchronously, and retry with backoff.
- **Queue health**: expose queue depth, failure count, and worker heartbeat metrics.
- **Reservation monitoring**: dashboard stats for `RESERVED` vs `COMPLETED` vs `CANCELLED`.
- **Load shedding**: optional maintenance/queue mode on event page when traffic spikes.

### 22) Event Rules + Kids Ticket Guardian Details (Decisioned v1)
This section captures confirmed product decisions and required fields.

Event settings fields:
- `rules_enabled` (bool, default false)
- `rules_rich_text` (rich text/html, nullable when disabled)

Rules behavior:
- Event managers can enable/disable rules per event.
- Checkout shows a popup/modal link to the rich text rules when enabled.
- Buyer must explicitly accept rules before checkout can continue.
- No publish-time block if rules are enabled but empty (business decision).
- Rules can contain arbitrary links (e.g., terms/FAQ) via rich text content.

Rules acceptance audit:
- New table/entity: `rules_acceptance_audits`
- Required fields:
  - `id`
  - `order_id`
  - `event_id`
  - `user_id` (nullable for guests)
  - `accepted_at_utc`
  - `ip_address`
  - `user_agent`
- v1 does not snapshot rules versions; operationally this tracks acceptance event only.

Kids ticket field:
- Product/ticket field: `is_kids_ticket` (bool, default false)

Guardian details (one guardian per order):
- Trigger: if any selected ticket has `is_kids_ticket = true`, guardian details are required once per order.
- Required fields:
  - Guardian legal name
  - Guardian DOB
  - Guardian driver license number
  - Guardian license issuing country
  - Guardian license issuing region/state
  - Guardian phone
- Validation:
  - Required + basic format validation only (no legal age rules in v1).
  - Validate on both client and server.

Storage model:
- Logged-in buyer:
  - Prefill guardian fields from profile when available.
  - Save/update guardian fields on profile on successful checkout.
  - Also persist guardian details to order and attendee snapshot fields used at check-in/ticket generation.
- Guest buyer:
  - Do not merge into a user profile later.
  - Persist guardian details in ticket/attendee data (and order if needed for reporting) for that purchase only.

Encryption:
- Driver license values must be encrypted at rest using reversible encryption (ciphertext), not password hashing.
- Decryption is allowed only in authorized server-side flows that require display (ticket/check-in/email generation).

Access and visibility:
- Event managers can view guardian details only for events they own/host.
- Admin role still has global powers by policy, but dedicated global admin UI for this data is deferred (not required in v1).

Retention:
- Profile guardian details persist until changed or deleted by future account/privacy workflows (no auto-expiry in v1).


