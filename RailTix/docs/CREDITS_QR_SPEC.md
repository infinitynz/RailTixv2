## RailTix — Rail Credits (Wallet + QR/POS) Specification

Status: Draft v0.2  
Applies to: Credits purchase, ticket checkout, POS deductions, receipts

### 1) Goals
- Let users purchase Rail Credits and spend them on tickets or on-site POS.
- Provide QR-based scanning for fast credit deductions at events.
- Keep credits scoped to the Event Manager who issued them.
- Provide receipts, audit logs, and balance tracking.

### 2) Credit System Scope (Updated Model)
- **Global Rail Credits**: Users can spend credits at any event.
- **Site Admin** owns the master Stripe account and processes all credit purchases.
- Event Managers are connected to the Site Admin via Stripe Connect.
- Credits are a platform-wide balance, not per Event Manager.
- Admin can manually adjust user balances (credit/debit).

### 3) Exchange Rate and Pricing
- Fixed rate: **2 Credits = $1 NZD**.
- Rate is not dynamic and does not change per event.
- Event Managers set **two prices** for tickets:
  - Price in dollars
  - Price in credits
- No extra percentage discount logic; pricing is explicit.
 - If an event uses a different currency, convert to NZD for credit equivalence.

### 4) Minimum/Maximum Top-Up
- Minimum top-up: **$10 NZD**
- Maximum top-up: **$500 NZD**
- Top-ups use Stripe only (no alternate payment methods).
- Top-up purchases generate a receipt/invoice like a ticket purchase.
 - Platform fee: **2%** on all purchases (tickets and credits), configured like Hi.Events.

### 5) Wallet and Ledger Model
Core entities:
- **CreditProgram**
  - Platform-wide: Name, IsEnabled, RateCreditsPerNZD (fixed = 2)
- **CreditWallet**
  - UserId, CreditProgramId, BalanceCredits, UpdatedAt
- **CreditTransaction**
  - Id, UserId, CreditProgramId
  - Type: Purchase | Deduct | Refund | Adjustment
  - AmountCredits (positive; sign implied by Type)
  - AmountNZD (optional, for reconciliation)
  - Reference (payment intent id, order id, POS ref)
  - EventManagerId (for attribution/payouts)
  - EventId (optional, for POS or ticket linkage)
  - PayoutBatchId (nullable; set when paid out)
  - PayoutStatus: Pending | Paid | Offset
  - IdempotencyKey
  - CreatedAt, CreatedBy (user/admin/system)
  - ReversalOfTransactionId (for refunds/voids)

- **CreditPayoutBatch**
  - Id, EventManagerId
  - ScheduledAt, ProcessedAt
  - TotalCredits, TotalNZD
  - StripeTransferId
  - Status: Pending | Paid | Failed
  - IdempotencyKey

### 6) User Flows
1) **Buy Credits**
   - Choose amount ($10–$500).
   - Pay via Stripe → webhook confirms.
   - CreditTransaction(Purchase) created → wallet balance updated.
   - Receipt emailed.

2) **Buy Ticket with Credits**
   - If Event Manager has credits enabled, show credit price option.
   - If user has enough credits → deduct and complete order.
   - If not enough credits → user must top up (no split with Stripe).
   - Refunds return credits to the same wallet.

3) **POS Deduction at Event**
   - Event Manager logs into POS view.
   - User displays wallet QR token.
   - Scan → confirm screen (amount, balance, event details).
   - Confirm → CreditTransaction(Deduct) and balance update.
   - Email receipt sent to user.
   - Log entry visible in Event Manager account area.

### 7) QR Token Rules
- Token validity: **120 seconds**.
- Token contains: user id, credit program id, issued-at, expiry, nonce, jti.
- Signed token (HMAC/JWS).
- UI shows countdown and auto-refresh when expired.

### 8) Security & Integrity
- Signed QR with anti-replay (track jti for expiry window).
- Idempotency for POS deductions and top-ups.
- Online-only POS flow (assume internet availability).
- POS access: Event Manager only (global role).
- Audit log for all adjustments and deductions.

### 9) Refunds and Adjustments
- Ticket refunds return credits to the same program.
- Admin can manually credit or debit users.
- Declined/failed Stripe payments do not create credits.
- Manual credit/debit always logged with reason.
 - Refunds are offset from the next payout for the Event Manager.

### 10) Receipts and Logs
- Email receipt for:
  - Credit purchase
  - POS deduction
- Receipt includes:
  - Event name, venue, date/time
  - Amount in credits and NZD equivalent
  - Staff/user identifier
  - Transaction id
- Event Managers can view transaction logs for their program.
 - Transaction log shows payout status (Pending/Paid/Offset).

### 11) UI Requirements
- Show balance per credit program in account area.
- POS confirm screen with final amount and remaining balance.
- Admin tools to enable/disable program and adjust balances.
 - Event Manager account view shows owed/paid totals and payout history.

### 12) Reporting
- Ledger export by date/type.
- Summaries per event manager (credits sold, spent, refunded).
- Top-up totals via Stripe reconciliation.

### 13) Payouts to Event Managers (Batch Transfers)
Schedule:
- Run every **12 hours**.
- Payout everything; no minimum threshold.

Calculation:
- Sum all pending CreditTransaction(Deduct) for each Event Manager.
- Subtract offsets for refunds and adjustments.
- Create a Stripe **transfer** from Site Admin to Event Manager.
- Mark included transactions as Paid with `PayoutBatchId`.
- Idempotency: batch id + event manager + schedule window.

Failure handling:
- If transfer fails, batch remains Pending and retried.
- Transactions are never paid twice.

### 14) Open Items
- Idempotency key format for payout batches.
- Whether admin can manually re-run or void a payout batch.

