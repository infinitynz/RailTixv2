## RailTix - Credits Payouts (Idempotency + Reconciliation)

Status: Draft v0.1  
Applies to: Credit payout batches to Event Managers

### 1) Goals
- Ensure payout batches are **idempotent** and never double-pay.
- Provide a **reconciliation format** for audit and support.
- Keep payout logic deterministic and repeatable.

### 2) Core Entities
- **CreditPayoutBatch**
  - `Id`
  - `EventManagerId`
  - `WindowStartUtc`, `WindowEndUtc`
  - `TotalCredits`, `TotalNZD`
  - `StripeTransferId` (nullable until paid)
  - `Status`: Pending | Paid | Failed
  - `IdempotencyKey`
  - `CreatedAt`, `ProcessedAt`

- **CreditTransaction** (existing)
  - `PayoutBatchId` (nullable)
  - `PayoutStatus`: Pending | Paid | Offset

### 3) Idempotency Strategy
Idempotency must be enforced at three levels:

**A) Batch Creation**
- IdempotencyKey is derived from a deterministic tuple:
  - `eventManagerId + windowStartUtc + windowEndUtc + version`
- Store key on `CreditPayoutBatch` with a unique index.
- If the job re-runs, it loads the existing batch by key.

**B) Stripe Transfer**
- Use Stripe transfer idempotency header:
  - Key = `credits_payout_{batchId}`
- If the job re-runs, it reuses the same key.

**C) Transaction Selection**
- Only include transactions where:
  - `PayoutBatchId IS NULL`
  - `PayoutStatus = Pending`
  - `CreatedAt` within the batch window
- After successful transfer, update all included rows with `PayoutBatchId` and `PayoutStatus = Paid`.
- If transfer fails, do not update transactions.

### 4) Batch Windowing
- Batch window duration: **12 hours**.
- Window boundaries are fixed UTC intervals (e.g., 00:00–12:00, 12:00–24:00).
- Each Event Manager gets one batch per window.
- Late transactions (arriving after window end) fall into the next window.

### 5) Refund Offsets
- Refunds create `CreditTransaction(Type=Refund)` referencing the original spend.
- Refund transactions reduce the payout total.
- If payout already happened, refunds appear as **Offset** and reduce the **next** batch.
- `PayoutStatus = Offset` for refund-related entries that adjust future payouts.

### 6) Reconciliation Output
Each payout batch must produce a reconciliation record, stored as JSON and visible in admin UI.

Suggested format:
```
{
  "batch_id": "CPB-2026-02-03-EM-123",
  "event_manager_id": 123,
  "window_start_utc": "2026-02-03T00:00:00Z",
  "window_end_utc": "2026-02-03T12:00:00Z",
  "currency": "NZD",
  "totals": {
    "credits": 5400,
    "nzd": 2700.00,
    "refunds_nzd": 120.00,
    "net_nzd": 2580.00
  },
  "stripe_transfer_id": "tr_123",
  "transactions": [
    {
      "credit_transaction_id": "CT-001",
      "type": "Deduct",
      "event_id": 555,
      "order_id": 888,
      "amount_credits": 200,
      "amount_nzd": 100.00,
      "created_at": "2026-02-03T03:42:00Z"
    }
  ]
}
```

### 7) Failure Handling
- If Stripe transfer fails:
  - Mark batch as Failed.
  - Leave transactions Pending.
  - Retry on next run with the same IdempotencyKey.
- If reconciliation generation fails:
  - Transfer status still determines Paid.
  - Reconciliation can be regenerated from batch + transactions.

### 8) Admin Visibility
Event Manager view:
- List of batches with totals and status.
- Drill-down to transaction list per batch.

Admin view:
- Cross-manager batch list.
- Retry failed batches.
- Export reconciliation JSON or CSV.

### 9) Open Items
- Exact IdempotencyKey format (string length limit).
- Whether to allow manual batch closure or forced re-run.


