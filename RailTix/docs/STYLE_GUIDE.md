RailTix UI Style Guide

Overview
- The visual identity is dark, sleek, and a little rebellious while remaining professional and clean.
- Primary highlight is green (#5fff00). Negative/secondary accent is pink (#ff00d8). 
- Interactions favor flatter surfaces with subtle depth (desktop OS-style) and smooth transitions.

Typography
- Headings: Bebas Neue (display). Body: Inter (300–800). Linked in `_Layout.cshtml`.

SCSS Structure
- Global tokens/mixins: `wwwroot/css/_tokens.scss`
- Global/base: `wwwroot/css/site.scss`
- Components: one file per component under `wwwroot/css/components/` (e.g., `forms.scss`)
- Views load component CSS via Razor `@section Styles` to avoid duplicates.

Hard Rules (CSS/SCSS)
- DO NOT edit any compiled `.css` files in `wwwroot/css` (including `site.css`).
- Always edit the `.scss` source (`site.scss` or `components/*.scss`) and compile.
- Do NOT use CSS Grid for layout; use Flexbox instead.

Loading Pattern
- Layout:
  - Always: `site.css`
  - Optional per-view: `@section Styles { <link rel="stylesheet" href="~/css/components/forms.css" /> }`

Color and Interaction
- Backgrounds: very dark gray canvas; white cards for form dialogs.
- Highlights: green (#5fff00) for positive/primary; pink (#ff00d8) for errors/cancel.
- Keep gradients minimal; prefer solid fills plus light border/shadow depth.
- Smooth transitions for interactive elements (color, background, border, brightness, shadow).
- Avoid `transform`-based hover motion (no `translate/scale/rotate` on `:hover`) so controls do not shift position.

Forms
- Use reusable form wrappers from `components/forms.scss`:
  - `.form-shell` for dark in-app forms (default for account/admin/event forms)
  - `.form-dialog` for focused auth-style forms where a light card is appropriate
- Controls: use `.form-control`, `.form-select`, `.form-check-input`; buttons `.btn` + variants.
- Validation: `.text-danger`/`.field-validation-error` styled in `components/forms.scss`.
- For larger forms, use `.form-shell__header`, `.form-section`, and `.form-actions` to keep structure consistent site-wide.
- In `.form-actions`, treat cancel/back as a negative action using `.btn-outline-danger` (not `.btn-outline-primary`).

Buttons
- `.btn` base with hover/active; `.btn-primary` (green), `.btn-outline-primary`, `.btn-danger` (pink).
- Keep corners subtle (`$rtx-radius-sm`/`$rtx-radius-md`) rather than pill-shaped for primary actions.
- Use a flatter 3D look (border contrast + soft inset/outset shadow) instead of heavy gradients.

Tables
- Use shared table styles from `site.scss` for all tabular data.
- Recommended structure:
  - `.table-shell` wrapper for surface/border/shadow.
  - `.table-shell__header` for title + actions.
  - `.table-wrap` for horizontal overflow safety.
  - `.table` for row/cell styling.
- For row actions, use `.table-actions`, `.table-actions__link`, and `.table-actions__link--danger` for destructive actions.
- Keep table headers compact, uppercase, and muted; keep body rows readable with subtle hover and zebra states.

Navbar and Layout
- Custom `nav` with `.nav-brand`, `.nav-menu`, `.nav-item`; responsive toggle adds `.is-open` to menu.
- Authenticated users use `.nav-account` popout menu (`.nav-account__trigger`, `.nav-account__menu`, `.nav-account__item`).
- Account menu styling stays dark and neon-consistent, with subtle custom warm/violet accents for the avatar trigger and menu borders.
- Keep account menu interactions animated with subtle transitions (open/close translate+fade, chevron rotation, icon/row hover feedback).
- `.container` centers content with max-width `1340px`, width `100%`, and `20px` side padding.
- Use `.layout-full` + `.layout-full__inner` for full-width components that should not be limited by `.container`.
- Keep layout composition flex-based (`display: flex`; optional wrapping) rather than grid.

Implementation Notes
- All SCSS files compile via `compilerconfig.json`. Edit and Save in Visual Studio to compile.
- Keep specificity low; use nesting sparingly and rely on tokens/mixins.

Local Workflow Rule
- Do not run `dotnet build` by default after edits. This project is often already running in the developer's environment.
- Run full build/start commands only when explicitly requested.

Usage Tips
- Include component CSS only on pages that use it; don’t include the same CSS file multiple times.
- Use `.form-dialog` for auth screens and settings forms.

Accessibility
- Maintain sufficient contrast on dark surfaces (WCAG 2.1 AA).
- Focus states remain visible by design; do not remove them.

Where to Edit
- Tokens/Mixins: `wwwroot/css/_tokens.scss`
- Global: `wwwroot/css/site.scss`
- Components: `wwwroot/css/components/*.scss`
- Fonts: `Views/Shared/_Layout.cshtml`
- Build/compile: `compilerconfig.json`


