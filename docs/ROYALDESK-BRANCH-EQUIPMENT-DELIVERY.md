# RoyalDesk — Branch Equipment Delivery & Receipt

Implementation branch: `feature/royaldesk-asset-lifecycle`

## Business rule

IT equipment requests for branches outside Phoenix suggest **Delivery Required** by default. IT can change the choice; local handover for a remote branch requires an explicit reason. **A delivery is not Done until confirmed receipt has been recorded**, including a named recipient, receipt quantity and a receipt/POD reference.

Delivery is part of an existing equipment ticket, not a separate application.

## Operational workflow

1. A technician assigns and starts an Equipment ticket.
2. IT chooses a real Store Room item and recipient. Serial-numbered equipment requires an available, reconciled, linked asset tag. Stock is **reserved**, not issued.
3. **Delivery Required:** `Waiting for Driver Pickup`. IT records driver and optional waybill; pickup atomically issues stock, transfers tagged asset custody out of IT Store and sets `In Transit`.
4. IT records driver arrival at the branch: `Awaiting Receipt Confirmation`.
5. IT captures the branch recipient's confirmation, quantity and delivery reference. Partial receipt stays `Partially Received`; only the last confirmed quantity moves to `Done`.
6. **Local handover:** `Awaiting Local Handover`; IT directly records a signed/acknowledged recipient handover, issuing the stock and moving to `Done` in one transaction.
7. **Failed delivery:** IT logs the exception; stock remains outside the store until the undelivered units are physically returned. Return-to-sender puts usable stock back or damaged stock in quarantine. The ticket remains unfinished; IT can reserve the remaining quantity for a retry.
8. **Cancellation before pickup:** reservation is released and ticket/delivery status becomes `Cancelled`.
9. After confirmed delivery of a tagged asset, IT may use the existing **Assignments & Returns** module to formally assign the asset to an employee. This does **not** issue stock twice.

## Controls and inventory accuracy

- `EquipmentDeliveries` has a unique ticket association; updates, ledger movements, ticket status and audit events are transactionally committed together.
- Ticket `Resolved`, `Closed` or other manual status overrides are blocked while delivery is outstanding. `Done` is set from receipt confirmation only.
- Store Room `QuantityOnHand` is usable stock, `ReservedQuantity` is reserved stock, and `QuarantinedQuantity` is physically damaged/held stock.
- Store Room already owns ticket-linked movement history. This feature extends it with reservation, issue, return, quarantine, and cancellation/retry.
- Full receipts and multi-unit partial receipts are supported for non-serialised items. A **serialised/asset-tracked equipment delivery is limited to one exact asset tag per ticket**; multi-unit tagged requests must be split into separate fulfilments before delivery.
- Available tagged assets must be linked to a verified Store Room item. Assets changed to damaged or obsolete before pickup cannot be dispatched.
- Files uploaded as optional signed POD or delivery photos appear in existing IT ticket attachments; PDF/image validation applies.
- The IT ticket queue includes outstanding pickups, transit/unconfirmed receipt, and exceptions, plus counts for each.

## Current boundaries and production acceptance

- The **IT ticket user records a branch-reported receipt** using the recipient name and POD/reference. This version does not give branch recipients their own authenticated portal, OTP link, driver account, or verified electronic signature. That is a separate identity and permissions feature; do not describe an IT-entered name as independent branch verification.
- There is no external carrier tracking integration, SAP sync, scheduled overdue alerts, or configurable SLA threshold yet. The queue exposes last-updated timestamps for monitoring.
- Signed proof file upload is optional, but the receipt reference and recipient name are required.
- `Delivered` tagged equipment is not automatically assigned to an employee; assign it afterwards via Assignments & Returns once the recipient is known.
- Failed deliveries of damaged equipment remain in quarantine; IT should assess stock condition before a replacement is dispatched.
- Existing requests and inventory are never silently auto-dispatched or auto-linked.
- **Before live deployment:** back up SQLite, apply migrations against a staging copy, reconcile usable and quarantined quantities, exercise Phoenix handover and remote branch scenarios, verify staff page permissions and email/public tracking with real accounts, and inspect responsive layouts.
- This branch is not merged to `main`, and CI build/test success does not establish deployment or browser/UAT validation.

## Suggested business acceptance scenarios

- Remote item: prepare 3 units, reserve 3; pickup issues 3 once; branch receives 1 then 2; Done only after final receipt.
- Pending delivery cannot be resolved through the ordinary ticket status endpoint or lifecycle workflow.
- Returned usable items increase usable stock; damaged returns increase quarantine only.
- Failed shipment can be retried for its remaining unreceived quantity without creating a new ticket and without clearing previous proof/audit history.
- Cancelled uncollected shipment releases all reserved stock, without issuing any equipment.
- Phoenix local handover issues stock only when receipt is confirmed.
- Tagged asset is In Transit after driver collection, Delivered after receipt and Assigned after explicit employee assignment; stock is deducted only at original collection.
