import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HelpDeskTicket, ITTeamMember, PublicTicketDetail, PublicTicketSummary, TicketStatusHistory } from '../models/helpdesk.model';

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
