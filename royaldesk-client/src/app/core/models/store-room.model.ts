export interface StoreItem {
  id: number;
  name: string;
  category: string;
  sku?: string;
  location?: string;
  quantityOnHand: number;
  reservedQuantity: number;
  quarantinedQuantity: number;
  minimumStock: number;
  isAssetTracked: boolean;
  isActive: boolean;
  availableQuantity: number;
  stockStatus: string;
}

export interface StoreMovement {
  id: number;
  storeItemId: number;
  itemName: string;
  ticketId?: number;
  ticketNumber?: string;
  movementType: string;
  quantity: number;
  reference?: string;
  notes?: string;
  performedBy: string;
  performedAtUtc: string;
}

export interface StoreLinkedAsset {
  id: number;
  assetTag: string;
  serialNumber?: string | null;
  category: string;
  make?: string | null;
  model?: string | null;
  status: string;
  condition: string;
  storeItemId?: number | null;
  storeItemName?: string | null;
  physicallyInStore: boolean;
  assignedEmployee?: string | null;
}
