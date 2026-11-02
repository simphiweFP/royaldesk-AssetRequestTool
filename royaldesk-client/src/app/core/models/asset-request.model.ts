export type RequestType = 'Incident' | 'Equipment' | 'Service';

export interface CreateAssetRequest {
  requestType: RequestType;
  requesterName: string;
  requesterEmail?: string;
  branch: string;
  department: string;
  itemType: string;
  quantity: number;
  reason: string;
}

export interface AssetRequestResponse extends CreateAssetRequest {
  id: number;
  ticketNumber: string;
  trackingPin?: string;
  status: string;
  requestedBy: string;
  requestedAtUtc: string;
}
