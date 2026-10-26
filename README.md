# RoyalDesk — Employee & Asset Lifecycle Portal

RoyalDesk started as a small full-stack IT asset request assessment and now includes an end-to-end employee/equipment lifecycle workflow while preserving the original request feature.

## What it does

- Equipment requests with API validation and audit logging
- Central asset register with branch, department, serial, value, condition and lifecycle status
- Employee register
- Asset assignment history
- Digital employee acceptance and generated acceptance PDF
- Employee account/setup profile for Email, SAP, CisTech, VPN, phone, groups, print/scan, folders and printers
- Onboarding and offboarding checklists
- Asset returns, transfers, repair/status changes and audit history
- Write-off approval and e-waste disposal
- Legacy e-waste workbook import endpoint and local import utility
- E-waste CSV export
- Management dashboard with asset value, availability, utilisation inputs and reuse opportunity

See `docs/ROYALDESK-LIFECYCLE.md` for the workflow and business mapping.

## Stack

- Angular 20 standalone client
- ASP.NET Core / .NET 10 API
- Dapper
- SQLite for the local assessment/demo
- FluentMigrator

## Run locally

```bash
dotnet restore
dotnet run --project RoyalDesk.Api
```

The API runs at `https://localhost:7080`.

Then:

```bash
cd royaldesk-client
npm install
npm start
```

Open `http://localhost:4200`.

## Demo authentication

The public assessment continues to use the original simulated credentials:

- Username: `demo.user`
- Password: `RoyalDesk123!`

Production should replace this with the company identity provider and secrets management; do not commit live RT-Auth credentials to this repository.

## Import the existing E-Waste workbook

Keep the company workbook outside source control, then run:

```bash
pip install openpyxl
python tools/import-ewaste.py "E-Waste Asset Write-Off Register.xlsx"
```

The importer validates the workbook headers and posts rows to `/api/lifecycle/imports/e-waste`. Duplicate serial numbers are skipped with warnings.

## Tests

```bash
dotnet test RoyalDesk.sln
```

The original request validation/repository/service/audit tests remain in place. Lifecycle code is structured behind `ILifecycleRepository` so workflow tests can be expanded independently.

## Architecture note

RoyalDesk owns the employee/equipment lifecycle. SAP should remain the financial/procurement system of record. When procurement integration is added, RoyalDesk should request purchasing only after available internal inventory has been checked.
