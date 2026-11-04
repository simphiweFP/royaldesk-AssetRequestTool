# RoyalDesk — Unified UI, Staff Access and Request Evidence Implementation

**Working branch:** `feature/royaldesk-asset-lifecycle` (do not rename or create a replacement branch)

## Current scope and constraints

Preserve the RoyalDesk application, its existing employee request flows, asset lifecycle features and "Set Up RoyalDesk Administrator" page. Use the existing Royal Tyres colours, logo, typography, form controls and card system. Do not introduce a new visual design or duplicate app shell.

## Phase 1 — Staff access and initial setup

- [x] Normal `/portal` uses employee username + password rather than a public profile selector.
- [x] Create `POST /api/staff-access/login`, reusing existing password hashing, lockout and cookie session implementation.
- [x] Keep initial setup at dedicated `/portal/setup` (not linked from public navigation).
- [x] Keep the original "Set Up RoyalDesk Administrator" title and form.
- [x] Preserve server-side `ROYALDESK_BOOTSTRAP_KEY` verification and one-time setup check; never bundle the configured value into Angular.
- [x] Improve staff login/setup visuals using existing RoyalDesk styling.
- [ ] Automated backend/frontend build, login/logout and first-admin tests.
- [ ] Review username uniqueness and prevent ambiguity before enabling additional account creation.
- [ ] Review rate limits, bootstrap race conditions, audit logs and error handling before production deployment.
- [ ] Review whether the legacy `/unlock` and `profiles/public` endpoints remain needed and retire them if unused.

**Important:** the dedicated URL is not access control; the bootstrap key must be provisioned to authorised IT administrators out of band. A deployment should never reveal its value in client JavaScript or API responses. Initial setup should be disabled after first-use and ideally restricted by network or operator workflow.

## Phase 2 — Unified styling

- [x] Replace landing-page introductory copy with a welcoming RoyalDesk message.
- [ ] Audit all duplicate headers across landing, problem, equipment, service, tracking, staff and lifecycle pages.
- [ ] Reuse existing layout/styling primitives rather than building a new design system.
- [ ] Align form heights, labels, validation, buttons, spacing and mobile layout.
- [ ] Do not redesign the functional IT request form.

## Phase 3 — Request attachments

- [ ] Audit current ticket APIs, storage and form model for existing attachment support.
- [ ] Require a supporting document on the stock-code-creation service subtype (PDF/Excel/Word, as accepted by business rules).
- [ ] Allow optional incident evidence: existing files, photos, and live device-camera capture.
- [ ] Show selected files, previews, remove controls, validation and upload progress.
- [ ] Link documents to the right ticket and provide permission-checked retrieval for IT.
- [ ] Enforce allowed file types, actual content validation, size/count limits, safe storage and access control on the API.
- [ ] Prevent orphan files on failed requests; add tests for unauthorised download.

## Phase 4 — Regression and release

- [ ] Run `dotnet test RoyalDesk.sln`.
- [ ] Run `cd royaldesk-client && npm ci && npm run build`.
- [ ] Test initial admin creation, repeated bootstrap rejection, username login, invalid password lockout, logout, session restore and permissions.
- [ ] Test problem/equipment/service submission, stock-code document upload, camera capture, request tracking and attachment authorisation.
- [ ] Test mobile and desktop UI against the original screenshots.
- [ ] Record changed files, tests and known limitations before PR.

## Implementation guidance

Prioritise backend enforcement over hiding navigation. Keep changes reviewable and avoid unrelated refactors. Preserve existing frontend route structure and existing data. Never commit real secrets.
