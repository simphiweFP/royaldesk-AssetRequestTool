import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AssetRequestResponse, CreateAssetRequest } from '../models/asset-request.model';

@Injectable({ providedIn: 'root' })
export class AssetRequestService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = 'https://localhost:7080/api/asset-requests';

  createWithEvidence(request: CreateAssetRequest, files: File[]): Observable<AssetRequestResponse> {
    const body = new FormData();
    body.append('requestJson', JSON.stringify(request));
    for (const file of files) body.append('files', file, file.name);
    return this.http.post<AssetRequestResponse>(`${this.endpoint}/with-evidence`, body);
  }

  create(request: CreateAssetRequest): Observable<AssetRequestResponse> {
    return this.http.post<AssetRequestResponse>(this.endpoint, request);
  }
}
