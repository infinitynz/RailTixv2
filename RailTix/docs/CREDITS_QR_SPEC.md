## RailTix — Credits Wallet & QR/POS (High Level)

### Goals
- Let users top up Credits and spend them on tickets and at-event POS purchases.
- Provide a fast, secure QR-based flow for staff to deduct Credits at the venue.

### Domain Concepts (Draft)
- CreditWallet
  - UserId
  - BalanceInCents
  - Currency
  - UpdatedAt
- CreditTransaction
  - Id, UserId
  - Type: Purchase | Deduct | Refund | Adjustment
  - AmountInCents (positive numbers; sign implied by Type)
  - Currency
  - Reference (payment provider id, POS reference, order id)
  - CorrelationId / IdempotencyKey
  - CreatedAt, CreatedBy (system/user/staff/device)
  - ReversalOfTransactionId (for refunds/voids)
- QRCodeToken
  - Encodes: user identifier, short-lived nonce, issued-at, expiry, jti
  - Signed (JWS) to prevent tampering

### User Flows
1) Top-up Credits
  - User selects amount ➜ pay via card ➜ payment webhook confirms ➜ CreditTransaction(Purchase) ➜ wallet balance increases ➜ receipt emailed.
2) Spend Credits on Tickets
  - At checkout, choose Credits as payment source (optionally split with card) ➜ CreditTransaction(Deduct) tied to order ➜ issue tickets.
3) POS Deduction at Event
  - Staff logs into POS UI (event-scoped).
  - Attendee opens wallet QR (short-lived).
  - Staff scans QR ➜ POS retrieves user (server verifies signature/expiry).
  - Staff selects/enters amount ➜ POST deduct with idempotency key.
  - Server creates CreditTransaction(Deduct) if sufficient funds; returns new balance.
  - Receipt printed/emailed (optional).

### Security & Integrity
- Signed QR (JWS/HMAC or asymmetric) with 60–120s expiry; include jti (unique id) and nonce.
- Server-side anti-replay: track used jti within expiry window.
- Idempotency: all POS and checkout deductions must accept an Idempotency-Key header to avoid double charges on retries.
- Staff & Device Auth: POS endpoints require staff role and event/device authorization; audit each action.
- Least privilege: staff POS cannot view full wallet history, only balances and last receipt.

### Error Handling (POS)
- Insufficient Funds ➜ show remaining balance; allow top-up via link/QR to user.
- Token Expired/Invalid ➜ request attendee to refresh QR.
- Network/Timeout ➜ rely on idempotent retry; never re-deduct on retry.

### Reporting & Audit
- Wallet balance derived from transaction ledger.
- Export transactions by date/range/type; tie to events (for POS).
- Immutable audit log for POS deductions and refunds.

### Open Questions (to refine)
- Refund policy rules for POS purchases (time window, approval).
- Offline mode needs? (If yes: buffered transactions with risk controls.)
- Multi-currency wallets (future).


