## Account — Icons

Status: Draft v0.1  
Source directory: `/images/icons` (served path)  
Note: In Rails, assets typically live under `app/assets/images/icons` and are served under `/images/icons`.

### Inline Usage (Required)
- Always inline SVG markup directly in views/partials instead of using `<img>`.
- Rationale: allows CSS theming (color via `currentColor`) and simple animations/transitions.
- Paths follow a style family:
  - Outline: `/images/icons/outline/*.svg` (default for sidebar)
  - Solid: `/images/icons/solid/*.svg`

Example (inline):

```html
<svg class="account-nav__icon" aria-hidden="true" focusable="false" viewBox="0 0 24 24">
  <!-- svg paths here -->
</svg>
```

### Purpose
- Define a consistent set of icons for Account navigation and related UI elements.
- Provide contribution guidelines for adding new icons.

### Initial Mapping (MVP)
- Dashboard — `outline/home.svg`
- Profile — `outline/user.svg`
- Cog (Profile Edit entry point) — `outline/cog.svg` (or `solid/cog.svg` if preferred)
- My Events — `outline/calendar.svg`
- Reports — `outline/chart-bar.svg`
- Check-in — `outline/qr-code.svg`
- Admin Center — `outline/shield-check.svg`

If a listed icon is not present yet, it may be added (see guidelines below) and this document should be updated accordingly.

### Guidelines for Adding Icons
- Format: SVG preferred (single-color, scalable, inline-able).
- Size: Designed on a 24×24 grid; ensure legible at 20–24px.
- Style: Consistent stroke widths; rounded joins/caps where applicable.
- Naming: Lowercase, hyphen-separated if multiple words (e.g., `user-circle.svg`).
- Accessibility: Provide descriptive `title`/`aria-label` in markup where icons convey meaning.
- Theming: Icons should be monochrome by default; color applied via CSS to respect themes/states.

### Usage Notes
- Always inline SVGs (no `<img>` for icons).
- Pair icons with text labels in navigation for clarity and accessibility.
- Ensure hover/focus/active states meet WCAG AA contrast requirements.
- In collapsed sidebars, show icons only; provide tooltips with text labels.


