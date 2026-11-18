# RoyalDesk — local test accounts (development only)

The Employee Portal is **passwordless** for new IT incident, equipment and service requests. Staff Access at `/portal` intentionally requires a staff username and password.

## Create local staff accounts

The branch includes an **opt-in** seed for the local development database. It is disabled by default, runs only when the ASP.NET Core environment is `Development`, and never modifies existing staff accounts or passwords.

In PowerShell, from the repository root, run (choose your **own** strong passwords and do not commit or share them):

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ROYALDESK_SEED_DEMO_USERS = "true"
$env:ROYALDESK_DEMO_ADMIN_PASSWORD = "ChangeThis-Admin-Local2026!"
$env:ROYALDESK_DEMO_TECH_PASSWORD = "ChangeThis-Tech-Local2026!"
dotnet run --project RoyalDesk.Api
```

Once the API starts and migrations complete, open `http://localhost:4200/portal`.

| Username | Password | Access |
| --- | --- | --- |
| `royaldesk.admin` | value of `ROYALDESK_DEMO_ADMIN_PASSWORD` | All staff pages |
| `royaldesk.technician` | value of `ROYALDESK_DEMO_TECH_PASSWORD` | Tickets, dashboard, assets |

The password strings above are **examples for an isolated local development database only**; choose different values for your machine. They are not active credentials until the seed runs. If these users already exist, the seeder will not overwrite their passwords. Use existing authorised Access Management to reset them, or use a fresh local test database.

To disable seeding for later runs:

```powershell
Remove-Item Env:ROYALDESK_SEED_DEMO_USERS -ErrorAction SilentlyContinue
Remove-Item Env:ROYALDESK_DEMO_ADMIN_PASSWORD -ErrorAction SilentlyContinue
Remove-Item Env:ROYALDESK_DEMO_TECH_PASSWORD -ErrorAction SilentlyContinue
```

Do not enable demo seeding on shared, public or production environments. The demo `demo.user` Basic Auth values in the project README are for the original assessment endpoint; they are **not** staff portal credentials.

## Employee test

Visit `http://localhost:4200/` or `http://localhost:4200/login`, choose an IT request type, and submit using your own email address. Any syntactically valid email domain is accepted; no staff password is required for submission.

Tracking uses the existing request lookup and verification flow. A submitted email address by itself must not be treated as proof of ownership.

## Verify

- Log in as both test accounts using the environment passwords.
- Confirm the technician can access IT Ticket Queue and the administrator has Access Management.
- Confirm employee submission works without a staff login.
- Check the staff hero benefits strip on desktop and mobile.
- Reconfirm that the original employee workflow, ticket history, delivery and asset modules still work.

**Status:** This guide describes committed code, not an executed local seed or a passed runtime test.
