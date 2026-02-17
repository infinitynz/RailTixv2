## RailTix — Functional Specification (High Level)

### 1) Overview
- RailTix is a ticketing platform that expands on hi.events.
- Public event discovery similar to Eventfinda (homepage listings, search, location-based results).
- Wallet-based Credits: users top up credits and can use them to buy tickets and at-event POS purchases via QR codes.
- Self-service event creation and sales (hi.events-style dashboard) with check-in system.
- Lightweight CMS: admins create content pages and assemble components (Umbraco-like).

### 2) Personas & Roles
- Site User (base role): register/login, manage profile, browse/search events, purchase credits, buy tickets, view wallet/tickets, present QR for POS.
- Event Manager (inherits Site User): create/manage events, ticket types, promo codes, order management, check-in, reporting.
- Admin (inherits Event Manager): user/role management, content pages & components, moderation & approvals, system settings, analytics.
- Role upgrade path: Site User requests Event Manager; Admin reviews and approves.

### 3) Core Feature Areas
3.1 Event Discovery (Public)
- Homepage: featured, trending, upcoming near me, curated categories.
- Search & Filters: keyword, date range, location (city/region/near me via geolocation), price, category/tags.
- Event Detail Page: description, schedule, venue map, organizer info, ticket options, FAQs, images/media, SEO metadata.
- SEO: clean URLs (/events/{slug}), sitemaps, structured data (Event schema).

3.2 Ticketing & Checkout
- Ticket Types: general admission, VIP, child/student, bundles, time-slots; inventory, pricing, fees/tax.
- Checkout: sign up / login, contact details, payment via Credits and/or card (provider TBD), order confirmation, e-receipts.
- Event Rules: optional event-level rich text rules shown via popup at checkout; when enabled, buyer must explicitly agree before payment.
- Kids Ticket Guardian Data: ticket types can be marked as kids tickets; if selected, one guardian record is required for the order and shown at check-in.
- Order Management (user): view orders, tickets, invoices; refund/transfer policies per event; email ticket PDFs/QRs.
- Promotions: promo codes, early-bird, group discounts.

3.3 Credits Wallet
- Top-up: choose amount, pay via card; funds added to wallet as Credits.
- Balance & History: transactions list (purchases, deductions, refunds/adjustments).
- Spend Credits: at checkout for tickets, and at-event POS via QR.
- Currency: base currency configured per tenant/site; support multi-currency later (out of scope v1).

3.4 On-site POS & Check-in
- Check-in (tickets): scan ticket QR, validate, mark as used, handle duplicates, offline grace (optional).
- POS Credits: staff scans user wallet QR; select or enter amount; deduct Credits; print/email receipt.
- Staff/Device Access: staff login, device registration; role-based permissions.
- Security: signed QR tokens, short expiration, anti-replay, idempotent deduction.

3.5 Event Manager Dashboard (hi.events-style)
- Account dashboard should show active events first and provide quick links into each event’s dashboard.
- Event Setup: event details, images, schedule, venue, capacity, categories/tags.
- Payment setup prerequisite: Stripe Connect must be completed before event creation is allowed.
- Ticketing: ticket types, inventory, pricing rules, fees/taxes, promo codes, holds/comp tickets.
- Orders & Attendees: order list, exports (CSV/Excel), attendees list, resend tickets, refunds (policy-based).
- Check-in App: device codes, staff roles, live stats.
- Reports: sales by ticket type/date/channel, attendance metrics.
- Per-event dashboard navigation should mirror Hi.Events structure and include:
  - Dashboard, Tickets, Attendees, Orders, Questions, Messages, Capacity, Check-in Lists, Homepage Design, Widget Embed.

3.6 CMS (Lightweight)
- Pages: create content pages with URL, SEO settings, publish schedule.
- Components: reusable blocks (Hero, RichText, ImageGallery, CTA, EventList, etc.) assembled per page.
- Navigation: menus and ordering; footer links.
- Workflow (v1 simple): draft/publish; optional schedule; audit log.

### 4) Authentication & Accounts
- Phase 1: Email/password signup & login; email verification; password reset.
- Phase 2: Social login (Google, Facebook).
- Profile: name, contact info, preferences (locale/timezone), marketing opt-in.

### 5) Authorization (High-level)
- Role-based plus policy checks:
  - Site User: access to own orders, wallet.
  - Event Manager: manage own events and related data.
  - Admin: global management and content.
- Inheritance: Admin ⊃ Event Manager ⊃ Site User.

### 6) Non-Functional Requirements
- Performance: server-rendered pages; minimal JS per page; CDN-ready static assets.
- Accessibility: WCAG 2.1 AA; semantic HTML; keyboard nav; ARIA for components.
- Security: CSRF protection, input validation, audit logs, least-privilege access, secure QR token scheme.
- Observability: structured logs, error tracking, basic metrics (requests, latency, conversion funnels).
- Internationalization: i18n-ready; timezone-aware dates; locale formatting.
- Compliance: GDPR-aligned data handling; privacy policy; cookie consent.

### 7) Integrations (Required + Pluggable)
- Payments: Stripe Connect (required) for card payments, payouts, platform fees, and webhooks.
- Platform fee baseline: global 2% application fee on connected-account event sales (admin-managed policy).
- Email: SMTP or transactional provider (e.g., SendGrid).
- Maps: basic map embed or provider SDK for venue display.

### 8) Milestones (Draft)
- M1: MVC scaffolding, roles, basic event listing/detail, simple checkout (non-credits).
- M2: Credits wallet purchase and ticket checkout using credits.
- M3: Event Manager dashboard (CRUD events/tickets), basic reports.
- M4: Check-in and POS credits deduction.
- M5: CMS pages and components; homepage powered by CMS.


