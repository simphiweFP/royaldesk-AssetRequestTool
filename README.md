# RoyalDesk Asset Request Tool

A small full-stack IT asset request tool prepared for the Royal Tyres technical assessment. Employees can submit equipment requests for a Royal Tyres branch and department, while the API validates, stores and audits each request.

## Requirements covered

- Angular request form with client-side validation
- ASP.NET Core POST API
- SQLite persistence with Dapper
- FluentMigrator database migrations
- Basic Authentication with a simulated user
- Server-side allow-list and length validation
- Parameterised SQL to prevent SQL injection
- Angular interpolation to avoid unsafe HTML rendering
- File-based JSON audit logging
- Focused service, validation and repository tests

## Project structure

- RoyalDesk.Api — ASP.NET Core API, authentication, validation, persistence and logging
- RoyalDesk.Api.Tests — focused backend tests
- royaldesk-client — standalone Angular client

## Prerequisites

- .NET 10 SDK
- Node.js 20 or later
- npm

SQLite does not need a separate database server. The API creates royaldesk.db and applies migrations at startup.

## Run locally

From the repository root, start the API:

    dotnet restore
    dotnet run --project RoyalDesk.Api

The API runs at https://localhost:7080.

In a second terminal, start Angular:

    cd royaldesk-client
    npm install
    npm start

Open http://localhost:4200.

## Demo authentication

This assessment uses Basic Auth to simulate a logged-in employee without adding registration or roles that were not requested.

- Username: demo.user
- Password: RoyalDesk123!

The Angular service sends these demonstration credentials. In production, credentials would come from a secure identity provider and secrets store, and Basic Auth would only be used over HTTPS.

## API

### Create an asset request

POST /api/asset-requests

Example body:

    {
      "branch": "Phoenix",
      "department": "IT",
      "itemType": "Laptop",
      "quantity": 1,
      "reason": "Required for a new employee starting next week."
    }

Successful requests return 201 Created. Invalid input returns 400 Bad Request, and missing or incorrect credentials return 401 Unauthorized.

## Validation and security choices

The client gives immediate feedback, but the API remains the source of truth. Branches, departments and item types use allow-lists; quantity is restricted to 1–10; and the reason is restricted to 10–500 characters. Dapper parameters keep user input separate from SQL. No user-provided HTML is rendered.

Basic Auth is intentionally small because the requirement asks for simulated authentication, not account registration or role management.

## Logging

After a request is stored, a JSON audit entry is appended to RoyalDesk.Api/logs/asset-requests.log.

Each entry includes the request ID, authenticated username, timestamp, branch, department, item and quantity. The reason is excluded to keep the audit log focused.

## Tests

Run the backend test suite with:

    dotnet test RoyalDesk.sln

The tests cover validation rules, repository persistence, the authenticated requestor and file audit logging.

## 15-minute presentation flow

1. **Problem and scope — 2 min:** Explain the employee request journey and why registration, roles and extra endpoints were excluded.
2. **Frontend demo — 3 min:** Show required fields, invalid input feedback and a successful request.
3. **API and database — 4 min:** Walk through controller, validator, service, parameterised Dapper insert and FluentMigrator migration.
4. **Security and logging — 3 min:** Demonstrate 401, Basic Auth identity, validation and the audit log.
5. **Tests and trade-offs — 2 min:** Show focused tests and explain what would change for production.
6. **Close — 1 min:** Confirm every assessment requirement and invite questions.

## Key trade-offs

- SQLite keeps the assessment easy to run; the connection factory allows another SQL provider later.
- One POST endpoint matches the requested workflow; listing and approval endpoints can be added when required.
- Configuration-based Basic Auth avoids unnecessary user tables and roles for a simulated login.
- Audit logging occurs after persistence so only successfully created requests are recorded.
