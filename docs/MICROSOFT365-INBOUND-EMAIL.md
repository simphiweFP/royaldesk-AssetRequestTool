# RoyalDesk Microsoft 365 inbound support email

## Confirmed mailbox

`itteam@royaltyres.co.za` is the requested Microsoft 365 intake mailbox.

The repository now includes a **disabled configuration entry** and a conservative rules-based classifier. **There is no live Microsoft Graph connection, running inbox worker, message storage, email reply processing or automatic ticket creation in this commit.** Do not set `Enabled=true` and assume inbox processing is operational.

## Prerequisites for the full feature

1. Confirm `itteam@royaltyres.co.za` is an Exchange Online mailbox (shared or licensed), not only an alias, distribution list or group.
2. Have an authorised Microsoft 365/Exchange administrator register a dedicated RoyalDesk application in Entra ID and provide tenant ID and client/application ID. Keep any certificate/private key or client secret only in server-side secure configuration, never in Git.
3. Grant only the required Graph mailbox access. Prefer Exchange Online **RBAC for Applications** scoped to the single `itteam@royaltyres.co.za` mailbox. Do not casually combine unrestricted tenant-wide Entra `Mail.Read` permission with a scoped RBAC assignment; effective permissions can be a union. Verify scope using `Test-ServicePrincipalAuthorization`.
4. Test Graph access to the mailbox Inbox using an app-only token. Start with read-only access; grant send permission separately only when response emails are approved.
5. Use `GET /users/{user-id}/mailFolders/inbox/messages/delta` for incremental ingest and durable `@odata.deltaLink`. Include `Prefer: IdType="ImmutableId"` on requests to stabilise message IDs.
6. Store mailbox ID + immutable message ID + `internetMessageId` with unique constraints; process duplicate Graph notifications idempotently.
7. Associate replies with existing tickets only through a validated, previously stored mail thread association (`In-Reply-To`, `References`, stored outgoing headers), not the ticket number alone.
8. Safely stage messages for triage if branch, department or requester identity cannot be validated. Incoming sender address alone must never grant access to a ticket.
9. Sanitize HTML bodies, block risky attachments, cap size/count, scan files, and protect against auto-replies, mailing lists, loops, spoofing and backdated replay.
10. Keep an audit log and job health metrics. Provide a reliable retry/dead-letter path. Never auto-approve equipment/access requests.

Useful Microsoft documentation:
- Graph message delta: https://learn.microsoft.com/en-us/graph/api/message-delta
- Exchange Online Application RBAC: https://learn.microsoft.com/en-us/exchange/permissions-exo/application-rbac
- Immutable IDs: https://learn.microsoft.com/en-us/graph/outlook-immutable-id

## Classification policy (implemented as suggestion helper)

`RoyalDesk.Api/EmailIntake/EmailTicketClassifier.cs` uses phrases to propose a combination of existing ticket request type and category. The category must exist in `CreateAssetRequestValidator`. Ambiguous or unmatched messages return Incident/Other with `RequiresTriage=true`. This helper does **not** currently read live mail or create a database record.

Recommended behaviour once the inbox worker is implemented:
- Form submissions continue as they work now.
- New email with high-confidence category and verified required fields -> submit via existing safe ticket creation service.
- Email without verified branch/department -> stage in an Inbox Triage table; don't manufacture values to pass validation.
- Replies -> append to authenticated conversation rather than opening another ticket.
- Obvious duplicates -> suggest merge; keep original messages and history.
- Automatic email acknowledgements -> use existing outbound queue only after message processing commits successfully.

## Configuration

`appsettings.json` intentionally contains no secrets:

```json
"Microsoft365EmailIntake": {
  "Enabled": false,
  "Mailbox": "itteam@royaltyres.co.za",
  "PollIntervalSeconds": 60,
  "InitialSync": "TriageOnly"
}
```

The `Enabled` setting is **reserved for future worker integration** and currently has no worker registered to consume it.

Server-only values needed for a future worker: `Microsoft365EmailIntake__TenantId`, `Microsoft365EmailIntake__ClientId`, and a secure credential reference, preferably a certificate in an appropriate secret store. Never paste a tenant secret in source code or a chat.

## Remaining work before claiming email-to-ticket works

- Secure Graph token acquisition and delta worker.
- Database migrations for InboxMessage, SyncState, attachment metadata, and thread identifiers.
- UI to review unclassified messages and missing required fields.
- Threaded public replies, notification outbox and failure handling.
- Real integration tests against a dedicated test mailbox.
- Microsoft 365 admin consent and scoped permissions.

Keep all of this additive on `feature/royaldesk-asset-lifecycle` and do not alter existing email notifications, ticket merges or asset workflows.
