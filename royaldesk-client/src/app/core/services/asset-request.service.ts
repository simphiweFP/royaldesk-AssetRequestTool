import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpEventType, HttpResponse } from '@angular/common/http';
import { filter, map, tap } from 'rxjs';
import { Observable } from 'rxjs';
import { AssetRequestResponse, CreateAssetRequest } from '../models/asset-request.model';

@Injectable({ providedIn: 'root' })
export class AssetRequestService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = 'https://localhost:7080/api/asset-requests';

  createWithEvidence(request: CreateAssetRequest, files: File[], onProgress?: (percent: number) => void): Observable<AssetRequestResponse> {
    const body = new FormData();
    body.append('requestJson', JSON.stringify(request));
    for (const file of files) body.append('files', file, file.name);
    return this.http.post<AssetRequestResponse>(`${this.endpoint}/with-evidence`, body, { observe: 'events', reportProgress: true }).pipe(
      tap(event => {
        if (event.type === HttpEventType.UploadProgress && event.total && onProgress)
          onProgress(Math.round(event.loaded * 100 / event.total));
      }),
      filter((event): event is HttpResponse<AssetRequestResponse> => event.type === HttpEventType.Response),
      map(event => event.body!)
    );
  }

  create(request: CreateAssetRequest): Observable<AssetRequestResponse> {
    return this.http.post<AssetRequestResponse>(this.endpoint, request);
  }
}
