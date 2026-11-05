import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { StoreItem, StoreLinkedAsset } from '../models/store-room.model';
import { HelpDeskTicket, ITTeamMember, PublicTicketDetail, PublicTicketSummary, TicketStatusHistory, EquipmentDelivery, EquipmentDeliveryEvent } from '../models/helpdesk.model';

@Injectable({ providedIn: 'root' })
export class HelpDeskService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://localhost:7080/api/helpdesk';
  private readonly publicBase = 'https://localhost:7080/api/public/tickets';

  tickets(status?: string): Observable<HelpDeskTicket[]> {
    const suffix = status && status !== 'All' ? `?status=${encodeURIComponent(status)}` : '';
    return this.http.get<HelpDeskTicket[]>(`${this.base}/tickets${suffix}`, { withCredentials: true });
  }

  ticket(id: number): Observable<HelpDeskTicket> {
    return this.http.get<HelpDeskTicket>(`${this.base}/tickets/${id}`, { withCredentials: true });
  }

  attachments(id: number): Observable<{ id: number; fileName: string; contentType: string; sizeBytes: number }[]> {
    return this.http.get<{ id: number; fileName: string; contentType: string; sizeBytes: number }[]>(`${this.base}/tickets/${id}/attachments`, { withCredentials: true });
  }

  downloadAttachment(ticketId: number, attachmentId: number): Observable<Blob> {
    return this.http.get(`${this.base}/tickets/${ticketId}/attachments/${attachmentId}`, { withCredentials: true, responseType: 'blob' });
  }

  history(id: number): Observable<TicketStatusHistory[]> {
    return this.http.get<TicketStatusHistory[]>(`${this.base}/tickets/${id}/history`, { withCredentials: true });
  }

  team(): Observable<ITTeamMember[]> {
    return this.http.get<ITTeamMember[]>(`${this.base}/team`, { withCredentials: true });
  }

  assignToMe(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/tickets/${id}/assign-to-me`, {}, { withCredentials: true });
  }

  start(id: number): Observable<void> {
    return this.http.post<void>(`${this.base}/tickets/${id}/start`, {}, { withCredentials: true });
  }

  updateStatus(id: number, status: string, comment?: string, publicVisible = true): Observable<void> {
    return this.http.patch<void>(`${this.base}/tickets/${id}/status`, { status, comment, publicVisible }, { withCredentials: true });
  }

  deliveryStock(): Observable<StoreItem[]> {
    return this.http.get<StoreItem[]>(`${this.base}/delivery-stock`, { withCredentials: true });
  }

  deliveryAssets(storeItemId: number): Observable<StoreLinkedAsset[]> {
    return this.http.get<StoreLinkedAsset[]>(`${this.base}/delivery-assets/${storeItemId}`, { withCredentials: true });
  }

  delivery(ticketId: number): Observable<EquipmentDelivery> {
    return this.http.get<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery`, { withCredentials: true });
  }

  openDeliveries(): Observable<EquipmentDelivery[]> {
    return this.http.get<EquipmentDelivery[]>(`${this.base}/deliveries/open`, { withCredentials: true });
  }

  deliveryHistory(ticketId: number): Observable<EquipmentDeliveryEvent[]> {
    return this.http.get<EquipmentDeliveryEvent[]>(`${this.base}/tickets/${ticketId}/delivery/history`, { withCredentials: true });
  }

  prepareDelivery(ticketId: number, payload: {
    storeItemId: number; assetId: number | null; deliveryRequired: boolean;
    recipientName: string; localHandoverReason: string | null;
  }): Observable<EquipmentDelivery> {
    return this.http.post<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery/prepare`, payload, { withCredentials: true });
  }

  retryDelivery(ticketId: number): Observable<EquipmentDelivery> {
    return this.http.post<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery/retry`, {}, { withCredentials: true });
  }

  pickupDelivery(ticketId: number, driverName: string, trackingReference: string): Observable<EquipmentDelivery> {
    return this.http.post<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery/pickup`,
      { driverName, trackingReference }, { withCredentials: true });
  }

  arriveDelivery(ticketId: number): Observable<EquipmentDelivery> {
    return this.http.post<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery/arrived`, {}, { withCredentials: true });
  }

  uploadDeliveryProof(ticketId: number, file: File): Observable<void> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<void>(`${this.base}/tickets/${ticketId}/delivery/proof`, body, { withCredentials: true });
  }

  confirmReceipt(ticketId: number, quantity: number, receivedBy: string, receiptReference: string, notes: string): Observable<EquipmentDelivery> {
    return this.http.post<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery/confirm`,
      { quantity, receivedBy, receiptReference, notes }, { withCredentials: true });
  }

  failDelivery(ticketId: number, notes: string): Observable<EquipmentDelivery> {
    return this.http.post<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery/fail`, { notes }, { withCredentials: true });
  }

  returnDelivery(ticketId: number, notes: string): Observable<EquipmentDelivery> {
    return this.http.post<EquipmentDelivery>(`${this.base}/tickets/${ticketId}/delivery/return`, { notes }, { withCredentials: true });
  }

  searchPublic(name: string, department: string): Observable<PublicTicketSummary[]> {
    const query = `?name=${encodeURIComponent(name)}&department=${encodeURIComponent(department)}`;
    return this.http.get<PublicTicketSummary[]>(`${this.publicBase}/search${query}`);
  }

  verifyPublic(ticketNumber: string, email?: string, trackingPin?: string): Observable<PublicTicketDetail> {
    return this.http.post<PublicTicketDetail>(`${this.publicBase}/verify`, {
      ticketNumber,
      email: email || null,
      trackingPin: trackingPin || null
    });
  }
}
