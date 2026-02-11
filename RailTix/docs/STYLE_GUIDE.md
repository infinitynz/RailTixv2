RailTix UI Style Guide

Overview
- The visual identity is dark, sleek, and a little rebellious while remaining professional and clean.
- Primary highlight is green (#5fff00). Negative/secondary accent is pink (#ff00d8). 
- Interactions use subtle brightness shifts, soft shadows, and smooth transitions.

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
- Smooth transitions for interactive elements (color, background, border, brightness).

Forms
- Wrap complex forms in `.form-dialog` for a white card UI.
- Controls: use `.form-control`, `.form-select`, `.form-check-input`; buttons `.btn` + variants.
- Validation: `.text-danger`/`.field-validation-error` styled in `components/forms.scss`.

Buttons
- `.btn` base with hover/active; `.btn-primary` (green), `.btn-outline-primary`, `.btn-danger` (pink).

Navbar and Layout
- Custom `nav` with `.nav-brand`, `.nav-menu`, `.nav-item`; responsive toggle adds `.is-open` to menu.
- `.container` centers content with max-width and horizontal padding.

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


