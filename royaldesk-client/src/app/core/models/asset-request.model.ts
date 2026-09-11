export interface CreateAssetRequest {
  branch: string;
  department: string;
  itemType: string;
  quantity: number;
  reason: string;
}

export interface AssetRequestResponse extends CreateAssetRequest {
  id: number;
  requestedBy: string;
  requestedAtUtc: string;
}
