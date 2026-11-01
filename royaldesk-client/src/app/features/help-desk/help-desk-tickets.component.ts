import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HelpDeskTicket, TicketStatusHistory } from '../../core/models/helpdesk.model';
import { HelpDeskService } from '../../core/services/helpdesk.service';
import { TicketTrackingComponent } from '../ticket-tracking/ticket-tracking.component';

@Component({
  selector: 'app-help-desk-tickets',
  standalone: true,
  imports: [CommonModule, FormsModule, TicketTrackingComponent],
  templateUrl: './help-desk-tickets.component.html',
  styleUrl: './help-desk-tickets.component.scss'
})
export class HelpDeskTicketsComponent implements OnInit {
  private readonly api = inject(HelpDeskService);

  readonly statuses = ['All', 'New', 'Assigned', 'In Progress', 'Waiting for User', 'Waiting for Third Party', 'Escalated to Just ICT', 'Resolved', 'Closed'];
  mode: 'team' | 'track' = 'team';
  tickets: HelpDeskTicket[] = [];
  history: TicketStatusHistory[] = [];
  selected?: HelpDeskTicket;
  statusFilter = 'All';
  search = '';
  loading = false;
  error = '';

  ngOnInit(): void { this.load(); }

  get filteredTickets(): HelpDeskTicket[] {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.tickets;
    return this.tickets.filter(t => [t.ticketNumber, t.requesterName, t.requesterEmail, t.branch, t.department, t.itemType, t.assignedToName, t.status]
      .some(v => v?.toLowerCase().includes(q)));
  }

  get newCount(): number { return this.tickets.filter(t => t.status === 'New').length; }
  get activeCount(): number { return this.tickets.filter(t => ['Assigned', 'In Progress'].includes(t.status)).length; }
  get waitingCount(): number { return this.tickets.filter(t => t.status.startsWith('Waiting') || t.status.startsWith('Escalated')).length; }
  get resolvedCount(): number { return this.tickets.filter(t => t.status === 'Resolved').length; }

  load(): void {
    this.loading = true;
    this.error = '';
    this.api.tickets(this.statusFilter).subscribe({
      next: tickets => { this.tickets = tickets; this.loading = false; },
      error: e => { this.error = e?.error?.detail || e?.error || 'Could not load IT tickets.'; this.loading = false; }
    });
  }

  select(ticket: HelpDeskTicket): void {
    this.selected = ticket;
    this.history = [];
    this.api.history(ticket.id).subscribe({ next: history => this.history = history, error: () => this.history = [] });
  }

  assignToMe(ticket: HelpDeskTicket): void {
    this.api.assignToMe(ticket.id).subscribe({ next: () => this.reloadSelected(ticket.id), error: e => this.error = e?.error?.detail || e?.error || 'Could not assign ticket.' });
  }

  start(ticket: HelpDeskTicket): void {
    this.api.start(ticket.id).subscribe({ next: () => this.reloadSelected(ticket.id), error: e => this.error = e?.error?.detail || e?.error || 'Could not start investigation.' });
  }

  setWaitingForUser(ticket: HelpDeskTicket): void {
    const comment = window.prompt('What do you need from the requester?', 'Waiting for additional information from requester.');
    if (!comment) return;
    this.api.updateStatus(ticket.id, 'Waiting for User', comment, true).subscribe({ next: () => this.reloadSelected(ticket.id), error: e => this.error = e?.error?.detail || e?.error || 'Could not update ticket.' });
  }

  resolve(ticket: HelpDeskTicket): void {
    const comment = window.prompt('Resolution summary:', 'Issue resolved by Royal Tyres IT.');
    if (!comment) return;
    this.api.updateStatus(ticket.id, 'Resolved', comment, true).subscribe({ next: () => this.reloadSelected(ticket.id), error: e => this.error = e?.error?.detail || e?.error || 'Could not resolve ticket.' });
  }

  private reloadSelected(id: number): void {
    this.api.ticket(id).subscribe({
      next: ticket => {
        this.selected = ticket;
        this.load();
        this.api.history(id).subscribe({ next: history => this.history = history });
      }
    });
  }
}
