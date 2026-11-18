# RoyalDesk — full local demonstration data

## Important

The full seeder is **opt-in** and only runs in ASP.NET Core `Development`. It requires a **fresh, dedicated test SQLite database** and refuses to run if the database already contains employees, assets, requests, store items or write-offs. It does not erase or modify ordinary records. Data is fabricated; no genuine employee information or financial data is included.

Running the seeder creates:
- Two staff credentials (administrator and technician) from environment-provided passwords.
- Six fictional employees.
- Eight invented asset records (available, assigned/accepted and damaged/write-off cases).
- Five Store Room stock items and opening stock movements.
- Two accepted assignments.
- Onboarding/offboarding checklist examples.
- One pending write-off.
- Fifteen IT tickets spanning New, Assigned, In Progress, Waiting, Resolved, Closed and Escalated.
- An external-email scenario, an already merged pair, two unmerged duplicate incidents ready to test the merge UI, and a keyboard delivery awaiting driver pickup.
- Ticket status histories and asset history records.

**Not seeded:** real email messages, real file attachments, live procurement/SAP transactions, signed delivery evidence or live courier pickups. These require a separate integration test environment. No fake data should be sent to real recipients.

## Quick setup with known test passwords

From the repository root, run:

```powershell
git switch feature/royaldesk-asset-lifecycle
git pull
powershell -ExecutionPolicy Bypass -File ./scripts/seed-local-accounts.ps1
```

The local-only script starts the API against a separate SQLite database and seeds two staff accounts, plus the full demo fixtures. It does not change the normal application database.

| Role | Username | Local-only test password |
| --- | --- | --- |
| Admin | `royaldesk.admin` | `RoyalDesk-Admin-Demo-2026!` |
| Technician | `royaldesk.technician` | `RoyalDesk-Tech-Demo-2026!` |

These are public test passwords for an isolated local machine only. Never deploy them as real accounts. The seeder skips existing usernames and does not reset their passwords.

## Run with PowerShell (Windows)

From the repository root:

```powershell
git switch feature/royaldesk-asset-lifecycle
git pull

$env:ASPNETCORE_ENVIRONMENT = "Development"
# Explicitly use a NEW test DB. Never point at your existing royaldesk.db.
$env:ConnectionStrings__DefaultConnection = "Data Source=royaldesk-full-demo.db"

$env:ROYALDESK_SEED_DEMO_USERS = "true"
$env:ROYALDESK_SEED_FULL_DEMO = "true"
# Pick your own unique strong local-only passwords; do NOT use these examples in shared environments.
$env:ROYALDESK_DEMO_ADMIN_PASSWORD = "Replace-This-Admin-Password-2026!"
$env:ROYALDESK_DEMO_TECH_PASSWORD = "Replace-This-Tech-Password-2026!"

dotnet run --project RoyalDesk.Api
```

The server applies existing FluentMigrator migrations and seeds the database once. Stop/restart is safe: the seed marker prevents duplicate fixtures. If an earlier existing database contains operational records, the full seed refuses to proceed.

Run Angular separately:

```powershell
cd royaldesk-client
npm install
npm start
```

## Local login

| User | Username | Password | Permission |
| --- | --- | --- | --- |
| Development admin | `royaldesk.admin` | `ROYALDESK_DEMO_ADMIN_PASSWORD` environment value | All staff modules |
| Development technician | `royaldesk.technician` | `ROYALDESK_DEMO_TECH_PASSWORD` environment value | Tickets, dashboard, assets |
| Employee requester | No staff username | None | Public employee portal |

Open `http://localhost:4200/portal` for staff and `http://localhost:4200/` for public request submission.

**Note:** `example.test` recipients are intentionally non-deliverable. Use your own controlled inbox for live email delivery tests. `demo.user` in the original README is not the staff portal account.

## Verification scenarios

- Dashboard: asset statuses and employee metrics should be populated.
- Asset Register: assets `SIM-LAP-001` to `SIM-LAP-004` and other categories.
- Assignments: two employees have accepted laptops.
- Store Room: five items with nonzero opening balances and movements, including one reserved keyboard.
- Onboarding and Offboarding: sample incomplete and completed tasks.
- Write-Offs: a damaged Lenovo laptop with pending approval.
- Tickets: status/category filters produce varied results and history.\n- Deliveries: one demo keyboard reserved and awaiting driver pickup with a delivery-event timeline.
- Merge: select the two Anele email incidents, choose a primary, provide a reason and confirm; the historical external pair already shows merged status.
- Public request: submit a **new** incident with your own inbox and any domain, independent of staff login.
- Access Management: administrator can see staff permissions.

The suite is intended for hands-on testing. It is **not** a claim that automated tests, migrations or browser checks have passed. Run:

```powershell
dotnet build RoyalDesk.sln
dotnet test RoyalDesk.sln
cd royaldesk-client
npm run build
```

## Reset safely

Use only a dedicated demo database. Stop the API, back up the data if needed and delete the **demo-specific** SQLite file (`royaldesk-full-demo.db`, in the API working directory) to regenerate fresh fixtures. Never delete `royaldesk.db` or any shared database to reset demo data.

Unset the environment variables when finished, especially `ROYALDESK_SEED_DEMO_USERS`, `ROYALDESK_SEED_FULL_DEMO` and the demo passwords. Never enable the seeder in production.
