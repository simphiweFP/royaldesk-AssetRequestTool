import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { StoreItem, StoreMovement } from '../models/store-room.model';

@Injectable({ providedIn: 'root' })
export class StoreRoomService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://localhost:7080/api/store-room';
  private readonly headers = new HttpHeaders({ Authorization: 'Basic ' + btoa('demo.user:RoyalDesk123!') });

  items(): Observable<StoreItem[]> {
    return this.http.get<StoreItem[]>(`${this.base}/items`, { headers: this.headers });
  }

  movements(): Observable<StoreMovement[]> {
    return this.http.get<StoreMovement[]>(`${this.base}/movements`, { headers: this.headers });
  }

  createItem(payload: {
    name: string;
    category: string;
    sku?: string | null;
    location?: string | null;
    openingQuantity: number;
    minimumStock: number;
    isAssetTracked: boolean;
  }): Observable<StoreItem> {
    return this.http.post<StoreItem>(`${this.base}/items`, payload, { headers: this.headers });
  }

  move(itemId: number, payload: {
    movementType: string;
    quantity: number;
    ticketId?: number | null;
    reference?: string | null;
    notes?: string | null;
  }): Observable<void> {
    return this.http.post<void>(`${this.base}/items/${itemId}/movements`, payload, { headers: this.headers });
  }
}
