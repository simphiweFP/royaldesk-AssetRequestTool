# RoyalDesk — Store Room as the Stock Source of Truth

Branch: `feature/royaldesk-asset-lifecycle`

## Implemented

- Store items own usable stock counts and a movement ledger.
- Tagged assets remain the source of serial numbers, asset tags, status, individual condition, and employee assignment.
- `StoreAssetLinks` connects exactly one Asset Register record to one existing asset-tracked Store Room item; previously captured records are never auto-merged by matching names.
- Store Room now shows a **Damaged & Repair Items** table (including unlinked assets) and an explicit **Link Existing Asset** action.
- Asset Register shows the Store Room item and IT custody status.
- Assigning a linked available asset issues one unit from its stock record within the same database transaction.
- Returning a linked usable asset adds one usable unit; returning a damaged asset records physical custody and a **Quarantine** movement, without adding it to usable stock.
- Requesting a write-off quarantines a linked available item when physically in the store. Rejected write-offs remain in repair / unavailable until explicitly requalified.
- Approved disposal records a linked **Write-Off** movement and removes the item from physical Store Room custody without double-decrementing usable stock.
- Marking a repaired linked asset Available through the operational-status workflow adds it back to usable stock, subject to condition checks and active write-off protection.
- Manual Issue / Return / Write-Off stock movements for asset-tracked items are blocked to prevent duplicate quantity changes.
- Negative **Stock Correction -** permits reconciling legacy balances, without going below reservations or already reconciled available asset tags.

## Existing data: reconciliation required

**No existing Store Room item, stock quantity or asset has been automatically changed or silently linked by this migration.**

1. Check Store Room stock quantities against physical usable items.
2. For each serial-numbered asset, select its actual stock item in **Store Inventory**.
3. Select the exact asset tag in **Link Asset Register to Store Room**.
4. Confirm physical custody accurately. Assigned equipment must remain marked outside the IT Store until returned.
5. For a usable Available asset, the system requires enough previously received, unlinked, usable units to support the link. Do not create a duplicate store item.
6. If historic available stock includes damaged items, perform an audited **Stock Correction -** before linking/issuing more equipment.
7. Confirm the resulting status in **Damaged & Repair Items**, **Asset Register**, and **Recent Store Movements**.

## Important boundaries

- `QuantityOnHand` now represents **usable units physically in the store**, not damaged quarantined assets or equipment assigned to employees. Quarantined assets are visible separately.
- Old unlinked assets remain visible and are intentionally **not auto-reconciled**; automatic matching by names would risk data loss or duplicate stock.
- Stock reservations remain at the store-item level. When stock is reserved, release or explicitly reconcile the reservation before assigning a particular tagged asset; reservation-to-specific-asset allocation is not implemented.
- Creating a new asset still uses the existing Asset Register form. Explicitly link it to its Store Room stock item after creation; no stock is added simply by registering the asset.
- This feature does not add automatic purchasing, SAP stock reconciliation, a repair vendor workflow, or ticket-driven reservations.
- An approved write-off requires confirmed physical custody to record disposal of a linked asset.
- Test the migration against a backup of the existing SQLite database. Verify staff permissions and the full issue / damaged-return / repair / disposal journey in the real UI before going live.

## Automated checks

GitHub Actions builds Angular and .NET and runs repository tests. Added an integration test for Store Room to Asset Register linking, stock issue, damaged return, approved write-off, and double-link prevention.
