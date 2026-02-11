## Account — Profile (View & Edit)

Status: Draft v0.1  
Audience: All authenticated users  
Routes:
- View: `/account/profile`
- Edit: `/account/profile/edit`
- Update: `PATCH /account/profile` (or `PUT`, per framework conventions)

### Purpose
- Provide a central place for users to see and manage their account information.
- Enable users to update profile details and change their password.

### View (`/account/profile`)
- Content
  - Avatar (optional/future)
  - Full Name
  - Email
  - Phone (optional)
  - Organization/Company (optional)
  - Other profile fields as defined by Product
- Actions
  - Cog icon (icon: `cog.svg`) opens `/account/profile/edit`.
  - Non-destructive links (e.g., “View Security Activity” in future).

### Edit (`/account/profile/edit`)
- Editable Fields (initial)
  - First Name
  - Last Name
  - Display Name (optional)
  - Email
  - Phone (optional)
  - Organization/Company (optional)
  - Guardian Legal Name (optional; used to prefill kid-ticket checkout)
  - Guardian DOB (optional; used to prefill kid-ticket checkout)
  - Guardian Driver License Number (optional; encrypted at rest)
  - Guardian License Issuing Country (optional)
  - Guardian License Issuing Region/State (optional)
  - Guardian Contact Phone (optional)
  - Avatar upload (future)
- Password Change (inline section)
  - Current Password (required to change password)
  - New Password
  - Confirm New Password
  - If password fields are blank, only profile fields are updated.
  - On password update, validate current password and standard password rules.
- Actions
  - Save → updates profile and/or password, then redirects to `/account/profile`.
  - Cancel → returns to `/account/profile` with no changes.

### Validation & Security
- Email must be unique; show inline errors and preserve valid inputs.
- Strong password rules per platform policy; show real-time hints if possible.
- Re-authenticate-sensitive changes: require current password when changing password.
- Server-side validation mirrors client-side; never rely solely on client validation.
- Guardian driver license numbers are stored as encrypted ciphertext, not plaintext.

### UX Details
- Show success toast upon save: “Profile updated”.
- If password is changed: “Password updated” (separate toast or combined).
- Disable Save while submitting; show progress state.
- Keyboard-accessible forms; visible focus; field-level error messaging.

### Acceptance Criteria
1. Any authenticated user can view `/account/profile`.
2. Clicking the cog icon navigates to `/account/profile/edit`.
3. Saving profile changes without password fields updates the profile and redirects to `/account/profile`.
4. Saving with password fields validates current password and updates password, then redirects to `/account/profile`.
5. Validation errors are shown inline without losing other user-entered values.
6. If guardian fields are saved in profile, kid-ticket checkout pre-fills them for logged-in users.


