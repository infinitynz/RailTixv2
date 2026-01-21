RailTix — New hi.events (Project Rules)

These rules are always-on. All generated code and edits must conform.

Architecture and Technology
- Runtime: .NET 8, ASP.NET Core MVC (Razor views only; NO Razor Pages), Entity Framework Core (Code‑First).
- Build a new version (not a 1:1 migration). Favor clear, domain‑appropriate names over legacy names.
- Layering:
  - Web: Controllers, Razor Views/Layouts/Partials, ViewModels.
  - Application/Services: business logic orchestration.
  - Domain: entities, value objects, domain services.
  - Data: EF Core DbContext, configurations, repositories (if used).
- Dependency Injection: constructor injection for all services and data access.
- Async: use async/await for I/O and EF operations.
- Validation: use Data Annotations and/or FluentValidation; keep controllers thin.
- Logging: use built-in ILogger; log errors and key operations.

Entity Framework Core (Code‑First)
- Single DbContext per bounded context; place in `Data/`.
- Entities use `Id` primary key (or `<EntityName>Id` when required). Use GUIDs or long as appropriate.
- Use Fluent API configurations (split per-entity) for relationships, keys, indexes, precision.
- Use explicit migrations, never automatic schema updates in production.
- Query guidance: project to DTO/ViewModel with `Select(...)`; use `AsNoTracking()` where appropriate.

Controllers, Views, and ViewModels
- Controllers are thin: validate input, call services, return views or JSON.
- Strongly‑typed ViewModels only (no business logic or heavy branching in views).
- Use Layouts and Partials for shared UI (header, footer, nav, form fragments).
- Keep Razor minimal: presentation logic only.
- Prohibited: Razor Pages and `.cshtml.cs` code-behind files.

Front-End JavaScript (No bundlers)
- Modular JavaScript: one module per feature/page under `wwwroot/js/modules/<feature>/...`.
- Load scripts only on pages that need them using `<script type="module" defer src="..."></script>`.
- Prefer modern, framework‑free JS (ES modules). Avoid global variables; expose minimal entry points per page.
- No Gulp/Bower/Grunt/Webpack/Vite. Download libraries and place under `wwwroot/lib/<library>/<version>/`.
- Keep payload small: include only required CSS/JS on each page; defer scripts where possible.

UI Framework and Toolkit
- Base framework: Bootstrap 5.x with Tabler CSS (free, Bootstrap‑based) for cohesive styling and rich components.
- Icons: Tabler Icons (MIT).
- Complementary, page‑scoped libraries (free):
  - Date/Time: Flatpickr (Bootstrap theme).
  - Charts: Chart.js.
  - File upload: Dropzone.js.
  - Carousel: Swiper (or Bootstrap Carousel if sufficient).
  - Advanced tables (when needed): DataTables (Bootstrap 5 integration).
- Only include the libraries on views that use them.

Naming and Structure
- Namespaces mirror folders. File names match public classes.
- Types and members: PascalCase; locals/parameters: camelCase.
- Folders:
  - `Controllers/`, `Views/`, `Models/Domain/`, `Models/Dto/`, `Models/ViewModels/`,
    `Services/`, `Data/` (DbContext, Migrations, Configurations),
    `wwwroot/css/`, `wwwroot/js/modules/`, `wwwroot/lib/`.

Performance and UX
- Defer and minimize JS; use `asp-append-version="true"` for cache busting.
- Use server-rendered HTML; hydrate with light JS where necessary.
- Accessibility: follow WCAG 2.1 AA; use semantic HTML and ARIA where needed.

Security
- Enforce antiforgery on form posts.
- Validate and sanitize inputs; never trust client data.
- Keep secrets out of source; use appsettings with user secrets or environment config.

Documentation and Tests
- Keep high-level specs in `docs/`.
- Unit tests for Services and key Data logic; integration tests for critical flows.


