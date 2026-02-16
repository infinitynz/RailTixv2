## RailTix - Stripe Payments Specification

Status: Draft v0.2  
Applies to: Stripe Connect, card payments, refunds, payouts, webhooks

Reference implementation in this repo (Hi.Events):
- `hi.events.full/backend/app/Services/Infrastructure/Stripe/*`
- `hi.events.full/backend/app/Services/Domain/Payment/Stripe/*`
- `hi.events.full/backend/app/Services/Application/Handlers/Account/Payment/Stripe/*`
- `hi.events.full/backend/app/Services/Application/Handlers/Order/Payment/Stripe/*`
- `hi.events.full/backend/app/Http/Actions/Common/Webhooks/StripeIncomingWebhookAction.php`
- `hi.events.full/backend/routes/api.php`
- `hi.events.full/backend/database/migrations/*stripe*`

Authoritative Stripe documentation (keep synced):
- Connect onboarding: https://docs.stripe.com/connect/connect-onboarding
- Express accounts: https://docs.stripe.com/connect/express-accounts
- Destination charges + application fees: https://docs.stripe.com/connect/destination-charges
- Connect account capabilities (`charges_enabled`, `payouts_enabled`): https://docs.stripe.com/connect/account-capabilities
- Webhooks: https://docs.stripe.com/webhooks

### 1) Goals
- Accept card payments for tickets with Stripe.
- Route funds to Event Managers via Stripe Connect.
- Allow platform fees (fixed and percentage) with optional pass-through to buyer.
- Keep payment processing idempotent and resilient.
- Use Stripe as the only card processor in v1.

### 2) Stripe Connect Model
- **Admin** is the platform owner and holds the master Stripe account.
- **Event Manager** connects a Stripe account to receive payouts.
- Use **Stripe Connect Express** (Hi.Events parity) for Event Manager onboarding.
- Payment intent is created with Connect account context and an application fee for platform revenue share.
- RailTix account/payment UX should mirror Hi.Events open-source flow:
  - no account linked: "Connect with Stripe"
  - account linked but incomplete: "Finish Stripe Setup"
  - completed: connected status + link to Stripe Dashboard

### 3) Configuration and Secrets
- Store keys in Azure Key Vault; load via app settings at runtime.
- Required keys:
  - `STRIPE_SECRET_KEY`
  - `STRIPE_PUBLIC_KEY`
  - `STRIPE_WEBHOOK_SECRET`
- Optional multi-platform support (future):
  - `STRIPE_CA_*`, `STRIPE_IE_*`, `STRIPE_PRIMARY_PLATFORM`
- Separate keys for test and live environments.

### 4) Data Model (RailTix)
Use these tables or equivalent entities in .NET:

- **StripeAccount** (Connect)
  - `event_manager_id`
  - `stripe_account_id`
  - `stripe_connect_account_type`
  - `stripe_platform` (optional)
  - `stripe_setup_completed_at`
  - `stripe_account_details` (json)
- **StripeCustomer**
  - `email`, `name`
  - `stripe_customer_id`
  - `stripe_account_id` (connected account)
- **StripePayment**
  - `order_id`
  - `payment_intent_id`
  - `charge_id`
  - `payment_method_id`
  - `amount_received`
  - `connected_account_id`
  - `stripe_platform`
  - `application_fee_gross`
  - `application_fee_net`
  - `application_fee_vat`
  - `application_fee_vat_rate`
  - `currency`
  - `payout_id`
  - `balance_transaction_id`
  - `payout_stripe_fee`
  - `payout_net_amount`
  - `payout_currency`
  - `payout_exchange_rate`
  - `last_error` (json)
- **StripePayout**
  - `payout_id`
  - `stripe_platform`
  - `amount_minor`
  - `currency`
  - `payout_date`
  - `payout_status`
  - `total_application_fee_vat_minor`
  - `total_application_fee_net_minor`
  - `metadata` (json)
  - `reconciled`
- **OrderPaymentPlatformFee**
  - `order_id`
  - `payment_platform_fee_amount_minor`
  - `application_fee_gross_amount_minor`
  - `application_fee_net_amount_minor`
  - `application_fee_vat_amount_minor`
  - `application_fee_vat_rate`
  - `transaction_id`
  - `charge_id`
  - `currency`
  - `fee_rollup` (json)
- **OrderRefund**
  - `order_id`
  - `refund_id`
  - `amount`
  - `currency`
  - `status`
  - `payment_provider` = Stripe
  - `metadata` (json)

### 5) Connect Onboarding Flow
1. Event Manager clicks "Connect Stripe".
2. Create or retrieve Stripe Connect account.
3. Generate Stripe account link (refresh and return URLs).
4. Store `stripe_account_id` and account type.
5. Mark setup complete when `charges_enabled` and `payouts_enabled` are true.
6. Block event creation and card payments if setup is incomplete.
7. Update status on `account.updated` webhook.

Hi.Events API parity target:
- `POST /accounts/{account_id}/stripe/connect` (create or get connect details; may return `connect_url`)
- `GET /accounts/{account_id}/stripe/connect_accounts` (status, setup complete, primary account)

### 5.1) Account UI Requirements (Event Manager + Admin)
Event Manager account/payment page must include:
- Stripe status panel with three explicit states:
  - `NotConnected`: CTA "Connect with Stripe"
  - `Incomplete`: CTA "Finish Stripe Setup"
  - `Connected`: badge + "Open Stripe Dashboard"
- Helper copy describing that Stripe Connect is required to receive ticket payout funds.
- If onboarding returns from Stripe (`is_return`, `is_refresh` query flags), refresh status immediately.
- Optional docs links: Stripe Connect overview and integration docs.

Admin account/payment page differs from Event Manager:
- Admin sees global platform Stripe connection status and environment (test/live).
- Admin manages default platform fee policy (global 2% default).
- Admin can view reconciliation health, webhook health, and payout summaries.
- Admin can override fee behavior for admin-owned events if needed (policy toggle).

### 5.2) Event Creation Gate (Mandatory)
Event Managers must not be able to create events before Stripe Connect setup is complete.

Enforcement requirements:
- UI: "Create Event" button is disabled/hidden if `stripe_connect_setup_complete != true`, with CTA to `/account/payment`.
- Route guard: direct access to `/account/events/create` redirects to `/account/payment` with return URL.
- Server-side guard: event create endpoint returns `403` if Stripe setup is incomplete, regardless of UI state.

### 6) Payment Intent Creation
Pre-conditions:
- Order is `RESERVED` and not expired.
- Session ownership verified.
- Event Manager has completed Stripe Connect setup.

Process:
- Create or update a Stripe Customer per email per connected account.
- Create Payment Intent with:
  - `amount`, `currency`
  - `automatic_payment_methods.enabled = true`
  - `application_fee_amount` (platform fee)
  - metadata: `order_id`, `order_short_id`, `event_id`, `event_manager_id`
  - Stripe-Account header set to connected account id
- Store StripePayment record with intent id and fee breakdown.

### 7) Payment Confirmation
Only webhooks finalize payment.

- `payment_intent.succeeded`
  - Verify order still valid; if expired, refund immediately.
  - Mark order paid and completed.
  - Activate attendees.
  - Update inventory and event stats.
  - Store application fee and charge info.
- `payment_intent.payment_failed`
  - Mark order payment failed.

### 8) Platform Fees and Pass-Through
- Platform fee configured per platform (fixed + percent).
- Optional "pass fee to buyer" setting on Event:
  - Compute fee so that Stripe application fee is covered by buyer.
  - Formula: `P = (fixed + total * r) / (1 - r)`
- Store fee values in StripePayment and OrderPaymentPlatformFee.
- On `charge.succeeded`, retrieve balance transaction and store:
  - Stripe fee
  - Application fee
  - Net and gross amounts

Default fee rules (Hi.Events parity):
- **Admin-owned events** (platform is the seller):
  - Charge runs on the platform Stripe account.
  - `application_fee_amount = 0` (no platform fee applied).
  - No destination transfer required.
- **Event Manager-owned events** (connected account is the seller):
  - Destination charge with `transfer_data.destination = connected_account_id`.
  - `application_fee_amount = 2%` of the order total (global default).
  - If pass-through is enabled, buyer covers the fee; otherwise it is deducted from payout.

Global 2% clarification:
- The "global 2% cut" is the platform application fee policy for connected-account sales.
- This is configured centrally by Admin and applied consistently unless explicitly overridden by policy.

### 9) Refunds
- Refunds can be partial or full.
- Refund uses connected account of original payment.
- Mark order refund status as pending, then update on `refund.updated`.
- Update event statistics and order totals.
- Optionally email buyer on refund creation.

### 10) Payout Reconciliation
- Listen to `payout.paid` and `payout.updated`.
- Fetch balance transactions for the payout and reconcile:
  - Map application fee transactions to charge ids.
  - Update StripePayment with payout details.
  - Store StripePayout summary record.

### 11) Webhooks
Endpoint:
- `POST /webhooks/stripe`

Supported events:
- `payment_intent.succeeded`
- `payment_intent.payment_failed`
- `charge.succeeded`
- `charge.updated`
- `refund.updated`
- `account.updated`
- `payout.paid`
- `payout.updated`

Rules:
- Verify signature using webhook secret(s).
- Idempotency: cache or persist event id for 60 minutes.
- Process asynchronously using background jobs.

### 12) Admin and Event Manager UI
Event Manager:
- Connect Stripe button and status indicator.
- Show "setup complete" or outstanding requirements.
- Control payment providers (card, offline).
- "Create Event" is blocked until Stripe setup is complete.

Admin:
- Connect/verify the global platform Stripe account.
- Platform fee configuration.
- Stripe key management (config only, no UI editing in prod).
- Payout reconciliation logs and errors.

### 12.1) Minimum Event Manager UX (Hi.Events style)
- In dashboard/checklist areas, show "Connect payment processing" if incomplete.
- CTA goes to account payment page.
- Until connected, block:
  - create event
  - publish event
  - card payment activation

### 13) Security and Compliance
- Do not store card data; use Stripe.js on the client.
- Webhook payloads are verified, logged, and retried.
- Idempotency keys for payment intent creation and refunds.
- Audit log all payment status transitions.

### 14) Azure Implementation Notes
- Store keys in Azure Key Vault.
- Use background jobs for webhook processing:
  - Hangfire, Azure WebJobs, or Service Bus + worker.
- Use Azure Redis Cache for webhook idempotency cache.
- Ensure webhook endpoint is public and has TLS.

### 15) Admin Platform Account Runbook (Global 2% Cut)
Use this when linking the main platform account and enabling the default 2% platform fee.

1. Create or verify the Stripe platform account (test + live).
2. Enable Stripe Connect for the platform account.
3. Confirm Connect type is Express for organizer onboarding.
4. Configure platform keys and webhook secret in environment/Key Vault.
5. Configure global fee policy:
   - `default_application_fee_percent = 2.0`
   - apply to connected-account event sales by default.
6. Verify onboarding path:
   - Event Manager opens `/account/payment`
   - connects Stripe
   - status becomes complete only when Stripe returns `charges_enabled && payouts_enabled`.
7. Verify fee behavior with test payment:
   - Event Manager-owned event: application fee = 2%
   - Admin-owned event (platform seller mode): application fee default = 0 (policy overrideable).
8. Verify webhook processing:
   - `payment_intent.succeeded`, `charge.succeeded`, `account.updated`, `payout.*`
   - reconciliation records update correctly.
9. Verify creation guard:
   - disconnected Event Manager cannot create event.

### 16) Open Decisions
- Multi-platform Stripe support: single or region-specific.
- Dispute and chargeback handling policy.
- Stripe Tax vs internal tax calculations.


