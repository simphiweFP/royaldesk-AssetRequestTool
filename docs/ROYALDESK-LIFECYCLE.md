# RoyalDesk Employee & Asset Lifecycle

RoyalDesk has been expanded from a single asset-request assessment into a lifecycle platform.

## Implemented flow

1. Employee requests equipment.
2. Request can move through manager and IT workflow states.
3. IT checks the asset register before purchasing.
4. Available equipment is assigned to an employee.
5. Employee digitally acknowledges the assignment and RoyalDesk can generate an acceptance PDF.
6. Onboarding checklist tracks email, SAP, CisTech, VPN, groups, print/scan, folders, printers, assets and acceptance.
7. Assets can be returned, transferred, placed in repair, marked lost/stolen, or routed to write-off.
8. Write-offs are approved/rejected, then disposed through the e-waste flow.
9. Offboarding cannot close while assets or checklist items remain unresolved.
10. Dashboard metrics expose utilisation, pending work and available inventory value for reuse.

## Existing I.T. Employee Acceptance Form mapping

The employee access profile captures the same operational fields used by the current form: email, SAP B1 username, CisTech username, VPN username, phone extension, relevant user groups, relevant email groups, print/scan access, shared folders and printers. Asset assignments capture equipment identity, condition and employee acceptance.

## Existing E-Waste Register mapping

The import endpoint accepts Serial Number, Make, Model, Category, Condition, Value, Fully Depreciated, Disposal Method, Disposal Date, Approved By and Notes. Use `tools/import-ewaste.py` to map an existing workbook without committing company data into Git.

## API areas

- `/api/asset-requests` — existing request creation.
- `/api/lifecycle/*` — employees, assets, assignments, acceptance, returns, onboarding/offboarding tasks, write-offs, disposal, dashboard and e-waste import.
- `/api/operations/*` — request list, employee account profile, transfers, repair/operational status, asset history, offboarding completion and e-waste CSV export.

## Production boundaries

The repository still uses the original demo Basic Auth so the assessment can run locally. Do not place RT-Auth secrets or company credentials in this public repository. Production deployment should swap the authentication handler for the company identity provider through configuration/secrets. Procurement/SAP should remain an integration boundary: RoyalDesk owns employee/equipment lifecycle state while SAP remains the financial purchasing source.

## Business value metrics

The dashboard currently calculates total asset value, available stock value, assigned/available counts, pending requests, pending write-offs and active employees. `estimatedReuseValue` represents available inventory value and is the first measurable input for a future cost-avoidance KPI when RoyalDesk reuses stock instead of triggering a purchase.
