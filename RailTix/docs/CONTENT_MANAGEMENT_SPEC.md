## Content Management & Routing Specification

Status: Draft v0.1  
Applies to: Admin role; public site routing

### 1) Goals
- Provide an admin-managed content tree rooted at Homepage.
- Support CRUD for pages and page components.
- Add a new routing system that resolves CMS pages without breaking MVC routes.
- Enforce a reserved-routes list (`/events`, `/account`) that is updatable over time.
- Normalize all URLs to lowercase kebab-case site-wide.

### 2) Content Tree Model
- Root node: Homepage.
- Pages are organized as a tree with ordered siblings.
- Example:
  - Home (`/`)
    - TermsAndConditions (`/terms-and-conditions`)
    - About (`/about`)
      - ContactUs (`/about/contact-us`)

#### 2.1 Page Entity (v1)
- `Id` (Guid)
- `Title` (string, required)
- `Slug` (string, required, lowercase kebab-case)
- `Path` (string, required, computed unless custom URL set)
- `ParentId` (Guid?, null for Homepage)
- `Position` (int, sibling ordering)
- `IsHomepage` (bool, only one true)
- `IsPublished` (bool)
- `CustomUrl` (string?, absolute path override)
- `CreatedAt`, `UpdatedAt`

#### 2.2 Slug + Path Rules
- `Slug` defaults to a lowercase kebab-case version of `Title`.
- If `Slug` conflicts with a sibling, append an incrementing numeric suffix:
  - `terms-and-conditions`, `terms-and-conditions-2`, `terms-and-conditions-3`, ...
- `Path` is computed by walking parents and joining slugs:
  - `/about/contact-us`
- `CustomUrl` overrides the computed path (see 2.3).

#### 2.3 Custom URL Override
- If set, `CustomUrl` is treated as the canonical absolute path for that page.
- Input can be with or without leading `/`; it is normalized and stored as absolute.
- `CustomUrl` still must pass reserved-route checks and uniqueness validation.
- Note: The page remains in the tree under its parent even if `CustomUrl` is outside the parent path.

### 3) URL Normalization (Global)
- All public-facing URLs are normalized to lowercase kebab-case.
- Normalization applies to:
  - Auto-generated slugs
  - Custom URLs
  - Incoming requests (canonicalization)

#### 3.1 Normalization Rules
- Split on whitespace and punctuation.
- Lowercase all characters.
- Replace whitespace/punctuation with single hyphens.
- Strip non-alphanumeric characters (except hyphens).
- Example mappings:
  - `Terms & Conditions` → `terms-and-conditions`
  - `Contact Us` → `contact-us`

#### 3.2 Canonicalization
- If an incoming request path is not canonical (case, separators), 301 redirect to the normalized path.
- Canonicalization applies across all site routes (CMS + MVC endpoints).

### 4) Reserved Routes (Updatable)
- Reserved top-level segments block CMS paths from taking those URLs.
- Seeded defaults: `/events`, `/account`.
- Stored in a DB table so the list can be updated without code changes.

#### 4.1 ReservedRoute Entity (v1)
- `Id` (Guid)
- `Segment` (string, lowercase kebab-case, no slashes)
- `IsActive` (bool)
- `CreatedAt`, `UpdatedAt`

#### 4.2 Validation Rules
- CMS page `Path`/`CustomUrl` cannot use a reserved top-level segment.
- Admin UI exposes CRUD for reserved segments (Admin-only).
- Cache reserved list in memory with invalidation on change.

### 5) Components
Pages are composed from ordered components. v1 types are limited but extendable.

#### 5.1 PageComponent Entity (v1)
- `Id` (Guid)
- `PageId` (Guid)
- `Type` (enum/string)
- `SettingsJson` (JSON)
- `Position` (int)
- `IsEnabled` (bool)
- `CreatedAt`, `UpdatedAt`

#### 5.2 Default Component Types (v1)
- `Image`
  - `Url`, `AltText`, `Caption`, `Alignment`, `LinkUrl?`
- `Banner`
  - `Heading`, `Subheading`, `BackgroundImageUrl`, `CtaLabel?`, `CtaUrl?`
- `EventList`
  - `Source` (manual/query), `EventIds?`, `Category?`, `Location?`, `Limit`

### 6) Admin UI (Content Section)
- New “Content” area in the admin panel.
- Screens:
  1) Pages Tree
     - Add page, edit page, delete page, reorder, add child.
     - Show computed URL and publish state.
  2) Page Editor
     - Title, slug (auto), custom URL override, publish toggle.
     - Component list with add/reorder/delete/enable/disable.
  3) Reserved Routes
     - CRUD list of reserved segments (Admin only).

### 7) Routing System (Public Site)
CMS routes are resolved after MVC routes to avoid conflicts.

#### 7.1 Route Resolution Order
1) Explicit MVC routes (controllers like `Events`, `Account`, etc.)
2) Admin routes (e.g., `/account/*` or `/admin/*`)
3) CMS catch-all route for non-reserved paths

#### 7.2 CMS Catch-all Behavior
- A single controller action resolves requests by path:
  - Normalize incoming path.
  - If reserved segment, 404 (or allow MVC to handle).
  - Look up page by `Path` (or `CustomUrl`).
  - If not found, return 404.
  - If found but not published, return 404 (unless admin preview is enabled).

### 8) Publishing & Preview
- `IsPublished` controls visibility.
- Optional preview for admins via query string token or header (future v2).

### 9) Validation & Constraints
- `Title` required.
- `Slug` required and unique among siblings.
- `Path` must be unique globally (computed or custom).
- Reserved routes cannot be used.
- `CustomUrl` must be valid:
  - Absolute path, lowercase kebab-case segments only.

### 10) Migration & Seed Data
- Seed Homepage with `IsHomepage = true`, `Path = /`.
- Seed ReservedRoutes: `events`, `account`.

### 11) Open Items (Assumptions)
- Custom URL is absolute and can escape the parent path.
- Sibling ordering uses integer position with manual drag/drop support.
- Admin-only access to Content and Reserved Routes screens.
- EventList `Category` filtering depends on the Event domain exposing categories/tags in v1 data model.


