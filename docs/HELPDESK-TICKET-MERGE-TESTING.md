# RoyalDesk Helpdesk additions — ticket merge and external-email testing

Branch: `feature/royaldesk-asset-lifecycle`

## Additive scope

These changes extend the existing RoyalDesk asset-request, helpdesk and lifecycle modules. They do not replace equipment requests, staff access, deliveries, store-room management, onboarding/offboarding or asset history.

## Existing external email support

`CreateAssetRequestDto.RequesterEmail` accepts any syntactically valid email address. The existing validator has **no Royal Tyres domain restriction**. The employee submission API is `POST /api/asset-requests` and accepts incident, equipment and service requests with the usual required branch/department/category fields.

Example request (replace branch/category values only with values supported by the existing validator):

```json
{
  "requestType": "Incident",
  "requesterName": "External Test Requester",
  "requesterEmail": "external@example.com",
  "branch": "Phoenix",
  "department": "IT",
  "itemType": "Email",
  "quantity": 1,
  "reason": "Test ticket from a non-company email address."
}
```

A submitted email address is **not** proof of identity. Never grant access to existing tickets solely by accepting an email string. Test outbound delivery only with inboxes controlled by the tester. Do not send real emails to example.com.

## Ticket merge endpoint (new)

Requires an active IT team member and the existing `tickets` staff permission.

`POST /api/helpdesk/tickets/merge`

```json
{
  "primaryTicketId": 101,
  "secondaryTicketIds": [102, 103],
  "reason": "Duplicate reports for the same issue",
  "confirmDifferentRequesters": false
}
```

The IDs are illustrative; use actual existing IDs in the test database.

`GET /api/helpdesk/tickets/101/merged` returns associated merged tickets. The merge transaction preserves secondary ticket records and all their existing history and attachments. Secondary ticket statuses become `Merged`, and their normal work status cannot be updated. Original history/attachments remain associated with their original IDs; presenting a combined conversation and attachment list in the UI is **future work**.

The merge rejects:
- Unknown IDs, duplicate IDs and attempts to merge a primary into itself.
- Already merged tickets or a secondary ticket that owns merged tickets.
- Any ticket with an equipment delivery record (to protect fulfilment).
- Different requesters unless explicitly confirmed.
- Users without the tickets permission and active IT technician/admin role.

Audit entries are written to the status history of primary and secondary tickets, and an additive `TicketMerges` table records the linkage. There is no data deletion.

## Test user setup

This GitHub change does **not** create runtime accounts, because account records live in the deployed database and are managed by the existing Staff Access system.

- Existing assessment/demo Basic Auth from the branch README: `demo.user` / `RoyalDesk123!`. **Development/demo only.**
- IT technician test identity: create an isolated Staff Access record with the `tickets` permission, then verify that the corresponding `ITTeamMembers` row is active.
- External requester: use a tester-controlled personal email address in the public employee ticket form; no IT staff account should be necessary merely to submit a ticket.
- An additional requester with another email domain can be used to test cross-requester merge confirmation.

Do not publish generated staff passwords in source code or use demo credentials in production.

## Verification checklist (not yet executed)

- Apply database migrations to a **copy** of the local test database.
- Run `dotnet test RoyalDesk.sln` and `dotnet build RoyalDesk.sln`.
- Create two incident tickets with the same email, merge them, verify both original tickets and history remain.
- Try merging three tickets into one.
- Verify cross-requester merge requires explicit confirmation.
- Verify delivery-linked tickets cannot be merged.
- Verify a non-IT user cannot invoke the merge endpoint.
- Verify no existing lifecycle, asset request or delivery screen regresses.
- Verify a personal email domain passes existing ticket validation.
- Verify staff users cannot update a merged secondary ticket.

## Outstanding integration

The backend merge API has been added, but the UI's multi-select/primary-ticket preview, merged-thread display and full email-reply-to-ticket ingestion have **not** been implemented or runtime-tested. These require additional integration and security review before release.
