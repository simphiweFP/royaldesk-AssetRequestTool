import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { StoreItem, StoreMovement, StoreLinkedAsset } from '../../core/models/store-room.model';
import { StoreRoomService } from '../../core/services/store-room.service';

@Component({
  selector: 'app-store-room',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './store-room.component.html',
  styleUrl: './store-room.component.scss'
})
export class StoreRoomComponent implements OnInit {
  private readonly api = inject(StoreRoomService);

  items: StoreItem[] = [];
  movements: StoreMovement[] = [];
  assets: StoreLinkedAsset[] = [];
  assetToLinkId?: number;
  physicallyInStore = true;
  selectedItemId?: number;
  loading = false;
  error = '';
  search = '';

  itemForm = {
    name: '', category: 'Peripheral', sku: '', location: 'IT Store',
    openingQuantity: 0, minimumStock: 2, isAssetTracked: false
  };

  movementForm = {
    movementType: 'Stock In', quantity: 1, ticketId: undefined as number | undefined,
    reference: '', notes: ''
  };

  readonly movementTypes = ['Stock In', 'Reserve', 'Release Reservation', 'Issue', 'Return', 'Adjustment', 'Write-Off'];

  ngOnInit(): void { this.load(); }

  get filteredItems(): StoreItem[] {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.items;
    return this.items.filter(i => [i.name, i.category, i.sku, i.location, i.stockStatus].some(v => v?.toLowerCase().includes(q)));
  }

  get damagedAssets(): StoreLinkedAsset[] {
    return this.assets.filter(a =>
      a.condition.toLowerCase() === 'damaged' ||
      ['in repair', 'pending write-off', 'approved for write-off'].includes(a.status.toLowerCase()));
  }
  get unlinkedAssets(): StoreLinkedAsset[] { return this.assets.filter(a => !a.storeItemId && a.status !== 'Disposed'); }
  get selectedItem(): StoreItem | undefined { return this.items.find(i => i.id === this.selectedItemId); }
  get totalUnits(): number { return this.items.reduce((sum, item) => sum + item.quantityOnHand, 0); }
  get availableUnits(): number { return this.items.reduce((sum, item) => sum + item.availableQuantity, 0); }
  get reservedUnits(): number { return this.items.reduce((sum, item) => sum + item.reservedQuantity, 0); }
  get lowStockCount(): number { return this.items.filter(item => item.availableQuantity <= item.minimumStock).length; }

  load(): void {
    this.loading = true;
    this.error = '';
    forkJoin({ items: this.api.items(), movements: this.api.movements(), assets: this.api.assetInventory() }).subscribe({
      next: result => { this.items = result.items; this.movements = result.movements; this.assets = result.assets; this.loading = false; },
      error: e => { this.error = e?.error?.detail || e?.error || 'Could not load Store Room.'; this.loading = false; }
    });
  }

  createItem(): void {
    if (!this.itemForm.name.trim() || !this.itemForm.category.trim()) {
      this.error = 'Item name and category are required.';
      return;
    }
    this.api.createItem({
      ...this.itemForm,
      sku: this.itemForm.sku || null,
      location: this.itemForm.location || null
    }).subscribe({
      next: () => {
        this.itemForm = { name: '', category: 'Peripheral', sku: '', location: 'IT Store', openingQuantity: 0, minimumStock: 2, isAssetTracked: false };
        this.load();
      },
      error: e => this.error = e?.error?.detail || e?.error || 'Could not add store item.'
    });
  }

  selectItem(item: StoreItem): void {
    this.selectedItemId = item.id;
  }

  linkAsset(): void {
    if (!this.selectedItemId || !this.assetToLinkId) {
      this.error = 'Select an asset-tracked store item and an unlinked asset.';
      return;
    }
    this.api.linkAsset(this.selectedItemId, this.assetToLinkId, this.physicallyInStore).subscribe({
      next: () => { this.assetToLinkId = undefined; this.load(); },
      error: e => this.error = e?.error?.detail || e?.error || 'Could not link asset to Store Room.'
    });
  }

  applyMovement(): void {
    if (!this.selectedItemId || this.movementForm.quantity <= 0) {
      this.error = 'Select a store item and enter a valid quantity.';
      return;
    }
    this.api.move(this.selectedItemId, {
      movementType: this.movementForm.movementType,
      quantity: this.movementForm.quantity,
      ticketId: this.movementForm.ticketId || null,
      reference: this.movementForm.reference || null,
      notes: this.movementForm.notes || null
    }).subscribe({
      next: () => {
        this.movementForm = { movementType: 'Stock In', quantity: 1, ticketId: undefined, reference: '', notes: '' };
        this.load();
      },
      error: e => this.error = e?.error?.detail || e?.error || 'Could not record stock movement.'
    });
  }
}
