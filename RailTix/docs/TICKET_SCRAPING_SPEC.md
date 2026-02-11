## RailTix - Ticket Scraping & External Events Spec

Status: Draft v0.1  
Applies to: Background ingestion, admin approval, CMS/discovery display

### 1) Goals
- Ingest third-party events from configurable sources.
- Normalize and store external events for Admin approval.
- Approved external events appear in discovery and CMS listings.
- Events remain read-only and link out to the original ticket site.

### 2) Scope
- **In scope**: source configuration, scheduled ingestion, admin review, updates, expiration handling, discovery inclusion.
- **Out of scope**: automated ticket sales, seat maps, or payment processing for external events.

### 3) Source Management (Admin)
Admins manage sources in the admin panel:
- Source name, base URL, auth keys (if required), rate limits.
- Endpoint configuration is open-ended and supports multiple schemas.
- Source-specific parsing rules are stored per source.
- Enable/disable sources without code changes.

### 4) Data Requirements (Minimum)
Required for approval:
- Event title
- Start date/time + timezone
- Location (city/region/venue)
- Image/flyer URL
- External ticket URL

Optional:
- End date/time
- Price info
- Organizer info
- Tags/categories
- Description/summary

### 5) Data Model (Core Entities)
Suggested entities (names can be adjusted):
- **ExternalEventSource**
  - Name, base URL, auth config, rate limits, enabled flag.
  - Parsing/transform configuration (JSON).
- **ExternalEventRaw**
  - Source ID, source event ID, raw payload (JSON), fetched timestamp.
- **ExternalEvent**
  - Normalized fields (title, dates, timezone, location, image, url, status).
  - Status: `PENDING_APPROVAL`, `APPROVED`, `DECLINED`, `EXPIRED`.
  - Source ID + source event ID (unique per source).
  - Last synced timestamp and sync hash (optional).
- **ExternalEventMedia**
  - Image URLs and local cached file refs.
- **ExternalEventApprovalLog**
  - Admin, action, timestamp, notes.

### 6) Ingestion Pipeline (Background Job)
Schedule:
- Runs every 30 minutes (configurable).

Flow:
1. **Fetch** from each enabled source (respect rate limits).
2. **Store Raw** payload for auditing and debugging.
3. **Normalize** into ExternalEvent structure.
4. **Upsert**:
   - If source event already exists: update it.
   - If it was **DECLINED**, do not re-add or re-approve.
5. **Expire**:
   - Mark as `EXPIRED` if end date (or start date) is in the past.
6. **Log** ingestion metrics (counts, errors, duration).

### 7) Update Rules
- Always update existing external events when the source changes.
- If already **APPROVED**, keep approved and update fields.
- If **DECLINED**, keep declined and do not resurface.
- If event date passes, mark as **EXPIRED** (still visible in admin only).

### 8) Admin Approval Workflow
Admin queue:
- View event, raw payload, and normalized fields.
- Approve or decline with optional notes.
- Approved events appear in discovery and CMS listings.
- Declined events never resurface unless manually reactivated.

### 9) Front-End Visibility Rules
- External events display alongside internal events in discovery.
- External events look the same as internal events for now.
- External events link out to the ticket URL (no internal checkout).
- Internal events have a setting: **Hide from discovery**.
- Expired events are hidden from the public site.

### 10) CMS Integration
CMS components that list events should support:
- Include internal events, external events, or both.
- Filters by location/date/category.
- Approved external events should appear wherever events are shown.

### 11) Performance and Safety
- Throttle requests per source.
- Support incremental fetching if the source provides cursors.
- Avoid long-running jobs; process in batches.
- Log failures with source + endpoint context.
- Use retries with backoff for transient errors.

### 12) Observability
- Ingestion logs: events fetched, inserted, updated, expired, declined skipped.
- Source health status (OK/Degraded/Down).
- Admin view for last run time and error summary.

### 13) Open Questions (if needed later)
- Do we need a manual "re-enable declined event" action?
- Should external events be editable by admin (title/description overrides)?
- Do we cache external images locally or hotlink?


