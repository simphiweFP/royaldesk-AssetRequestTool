import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EscalateToJustIctRequest, TicketEscalation } from '../models/helpdesk.model';

@Injectable({ providedIn: 'root' })
export class JustIctService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://localhost:7080/api/helpdesk';
  private readonly headers = new HttpHeaders({ Authorization: 'Basic ' + btoa('demo.user:RoyalDesk123!') });

  escalate(ticketId: number, request: EscalateToJustIctRequest): Observable<TicketEscalation> {
    return this.http.post<TicketEscalation>(`${this.base}/tickets/${ticketId}/escalate-just-ict`, request, { headers: this.headers });
  }
}
