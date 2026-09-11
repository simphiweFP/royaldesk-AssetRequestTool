import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AssetRequestResponse,
  CreateAssetRequest
} from '../models/asset-request.model';

@Injectable({ providedIn: 'root' })
export class AssetRequestService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = 'https://localhost:7047/api/asset-requests';

  create(request: CreateAssetRequest): Observable<AssetRequestResponse> {
    const credentials = btoa('demo.user:RoyalDesk123!');
    const headers = new HttpHeaders({
      Authorization: 'Basic ' + credentials
    });

    return this.http.post<AssetRequestResponse>(this.endpoint, request, { headers });
  }
}
