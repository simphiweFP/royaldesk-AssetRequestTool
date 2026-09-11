# RoyalDesk Asset Request Tool

A small full-stack IT asset request tool prepared for the Royal Tyres technical assessment.

## Problem and value

Employees often request laptops, monitors and other IT equipment through informal channels such as email, phone calls or chat messages. These requests can be incomplete, difficult to trace and disconnected from the employee's branch or department.

RoyalDesk provides one consistent request process. It captures the branch, department, item, quantity and business reason; validates the request on the API; stores it in a searchable database; links it to an authenticated employee; and creates an audit record for support follow-up.

For Royal Tyres, this means fewer incomplete requests, clearer accountability across branches, less manual administration for IT support and a reliable foundation for future approval or helpdesk workflows.

## Requirements covered

- Angular request form for capturing the required request details
- ASP.NET Core POST API
- Server-side request validation in the API
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

Validation belongs to the API so the same rules are enforced regardless of which client calls the endpoint. The Angular form guides the user through the expected fields, but the backend remains responsible for deciding whether a request is valid.

The API checks branch, department and item type against allow-lists, restricts quantity to 1–10 and restricts the reason to 10–500 characters. Invalid requests return 400 Bad Request. Dapper parameters keep user input separate from SQL, and the Angular client does not render user-provided HTML.

Basic Auth is intentionally small because the requirement asks for simulated authentication, not account registration or role management.

## Logging

After a request is stored, a JSON audit entry is appended to RoyalDesk.Api/logs/asset-requests.log.

Each entry includes the request ID, authenticated username, timestamp, branch, department, item and quantity. The reason is excluded to keep the audit log focused.

## Tests

Run the backend test suite with:

    dotnet test RoyalDesk.sln

The tests cover validation rules, repository persistence, the authenticated requestor and file audit logging.

## Key trade-offs

- SQLite keeps the assessment easy to run; the connection factory allows another SQL provider later.
- One POST endpoint matches the requested workflow; listing and approval endpoints can be added when required.
- Configuration-based Basic Auth avoids unnecessary user tables and roles for a simulated login.
- Audit logging occurs after persistence so only successfully created requests are recorded.
