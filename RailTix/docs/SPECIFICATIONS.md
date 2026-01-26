RailTix — New hi.events (High‑Level Specifications)

Scope and Goals
- Build a new, maintainable version of hi.events within RailTix (ASP.NET Core MVC, .NET 8).
- Not a 1:1 migration: use domain‑appropriate naming and modern conventions.
- Lightweight, efficient, modular front end with concerns separated.
- EF Core Code‑First for database; explicit migrations.

Architecture Overview
- Layers
  - Web (MVC): Controllers, Razor Views/Layouts/Partials, ViewModels.
  - Application/Services: business logic orchestration and policies.
  - Domain: entities and domain logic.
  - Data: EF Core DbContext, configurations, repositories (optional).
- Cross‑cutting
  - Dependency Injection via built‑in container.
  - Logging with ILogger.
  - Validation via Data Annotations and/or FluentValidation.
  - Authentication/Authorization via ASP.NET Core Identity or external providers (future decision).

Entity Framework Core (Code‑First)
- Define domain entities with clear relationships and invariants.
- Central DbContext with per‑entity configuration classes.
- Use migrations for schema changes; version and review changes.
- Performance: project to DTOs, `AsNoTracking()` where appropriate, indexes via Fluent API.

UI Framework and Styling
- No third‑party CSS framework. Custom SCSS only.
- Structure:
  - `wwwroot/css/_tokens.scss`: shared variables/mixins (colors, radii, shadows, transitions).
  - `wwwroot/css/site.scss`: global base (typography, layout, navbar, buttons).
  - `wwwroot/css/components/forms.scss`: form controls, dialog cards, validation.
- Loading:
  - Layout includes `site.css` globally and exposes a `Styles` section.
  - Views include component styles only when needed (e.g., forms.css once per page).

UI/UX Style & Theming
- The app uses a custom dark theme layered on top of Bootstrap.
- Primary highlight is green (#5fff00); negative/cancel accent is pink (#ff00d8).
- Forms can be rendered as dialog-like white cards using the `.form-dialog` helper.
- See `docs/STYLE_GUIDE.md` for tokens, utilities, and usage patterns.

Front‑End Strategy (Modular JS, No Bundlers)
- Place feature modules under `wwwroot/js/modules/<feature>/...`.
- Each view includes only the scripts it needs with `<script type="module" defer src="..."></script>`.
- Keep a small set of shared utilities in `wwwroot/js/common/` (e.g., fetch wrappers, DOM helpers).
- Avoid global variables; export minimal entry points from modules.
- Vendor scripts live in `wwwroot/lib/<library>/<version>/` (downloaded files or CDN fallback).
- Client inputs shown to the user (e.g., country/city lists) MUST come from the server (AJAX/JSON) and are validated server-side on submit. No hardcoded choice lists in client JS.
- Use OO jQuery modules for page-specific behaviors (e.g., `wwwroot/js/modules/account/register.js`) and keep inline scripts out of views.

Views and ViewModels
- Strongly‑typed ViewModels per page; no business logic in views.
- Use `_Layout.cshtml` for global CSS, header/footer, and minimal global JS.
- Use partials for reusable UI fragments (e.g., forms, list items, nav).

Routing and Controllers
- REST‑like routes where appropriate (`/events`, `/tickets`, `/orders`).
- Controllers delegate to services; handle validation results and return views or JSON.
- Keep actions small, async, and explicit about inputs/outputs.

Security, Accessibility, and Performance
- CSRF protection (`[ValidateAntiForgeryToken]`) for form posts.
- Input validation/sanitization; never trust client data.
- Accessibility: WCAG 2.1 AA; semantic HTML; ARIA where needed; focus management in modals.
- Performance: defer scripts; minimize CSS/JS per page; cache static assets; responsive images.

Testing and Quality
- Unit tests for Services and critical Data logic.
- Integration tests for key flows (e.g., purchase, ticket issuance).
- Linting and analyzers via `.editorconfig` and Roslyn analyzers.

Initial Folder Structure (target)
```
RailTix/
  Controllers/
  Data/
    Configurations/
    Migrations/
    RailTixDbContext.cs
  Models/
    Domain/
    Dto/
    ViewModels/
  Services/
  Views/
    Shared/
  wwwroot/
    css/
    js/
      common/
      modules/
    lib/
  .cursor/
    rules/
  docs/
```

Authentication & Identity
- ASP.NET Core Identity with roles: `SiteUser`, `EventManager`, `Admin` (inheritance by inclusion).
- MVC controllers & views ONLY (no Razor Pages anywhere).
- Email verification required for sign‑in.
- Features: Register, Login (Remember Me), Logout, Forgot/Reset Password, lockout after failures.
- Dev mail: smtp4dev on localhost; emails for confirmation/reset.
- reCAPTCHA enforced on Register/Login/Forgot Password (dev keys in config).

Currency, Locale & Timezone
- Multi‑currency: user profile stores preferred currency (ISO 4217). Events have their own currency.
- Locale: user profile stores locale (e.g., `en-NZ`). 
- Timezone: user profile stores IANA TZ (`Pacific/Auckland`). Event has explicit timezone; defaults to the creator’s preference.
- If browser detection unavailable at signup, default to NZD and Pacific/Auckland in dev.

Location & “Near Me”
- Events store latitude/longitude plus city/region metadata.
- v1 (unauthenticated): layered fallback with caching
  1) Cookie (`rtx_loc`) if present (HttpOnly, signed, 30‑day TTL)
  2) Browser Geolocation API (with consent) → server maps to nearest supported city and sets cookie
  3) IP geolocation (free provider) → server maps to nearest supported city and sets cookie
  4) Sane default (NZ/AU mapping)
- Authenticated users: use profile Country/City and set/refresh the cookie on login; no geolocation calls needed.
- Server is the source of truth: all Country/City/currency/timezone mapping is computed server‑side via `ILocationService`; client never hardcodes lists.
- Endpoints:
  - `GET /Location/cities?country=…` → server-sourced cities
  - `POST /Location/update { lat,lng }` → set cookie from browser geolocation
  - `GET /Location/guess` → set cookie from IP geolocation
  - `GET /Location/current` → returns current cookie state (for diagnostics)

