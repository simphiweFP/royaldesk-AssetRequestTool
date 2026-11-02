export interface BusinessValueBreakdown {
  name: string;
  count: number;
}

export interface TechnicianWorkload {
  technicianName: string;
  assigned: number;
  active: number;
  resolved: number;
}

export interface BusinessValueSummary {
  totalTickets: number;
  openTickets: number;
  resolvedTickets: number;
  escalatedTickets: number;
  averageResponseMinutes: number;
  averageResolutionMinutes: number;
  totalStoreUnits: number;
  reservedStoreUnits: number;
  lowStockItems: number;
  storeUnitsIssued: number;
  totalAssets: number;
  availableAssets: number;
  availableAssetValue: number;
  estimatedReuseValue: number;
  ticketsByStatus: BusinessValueBreakdown[];
  ticketsByCategory: BusinessValueBreakdown[];
  technicianWorkload: TechnicianWorkload[];
  storeMovementsByType: BusinessValueBreakdown[];
}
