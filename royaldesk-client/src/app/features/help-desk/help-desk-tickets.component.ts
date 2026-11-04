import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HelpDeskTicket, TicketStatusHistory } from '../../core/models/helpdesk.model';
import { HelpDeskService } from '../../core/services/helpdesk.service';
import { JustIctService } from '../../core/services/just-ict.service';
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
  private readonly justIct = inject(JustIctService);

  readonly statuses = ['All', 'New', 'Assigned', 'In Progress', 'Waiting for User', 'Waiting for Third Party', 'Escalated to Just ICT', 'Resolved', 'Closed'];
  readonly requestTypes = ['All', 'Incident', 'Equipment', 'Service'];
  mode: 'team' | 'track' = 'team';
  tickets: HelpDeskTicket[] = [];
  history: TicketStatusHistory[] = [];
  attachments: { id: number; fileName: string; contentType: string; sizeBytes: number }[] = [];
  selected?: HelpDeskTicket;
  statusFilter = 'All';
  requestTypeFilter = 'All';
  search = '';
  loading = false;
  error = '';
  showEscalation = false;
  escalating = false;
  escalationTicket?: HelpDeskTicket;
  escalationForm = { teamViewerId: '', issueDescription: '', troubleshooting: '', additionalNotes: '', emailBody: '' };

  ngOnInit(): void { this.load(); }

  get filteredTickets(): HelpDeskTicket[] {
    const q = this.search.trim().toLowerCase();
    return this.tickets.filter(t => {
      const typeMatches = this.requestTypeFilter === 'All' || t.requestType === this.requestTypeFilter;
      if (!typeMatches) return false;
      if (!q) return true;
      return [t.ticketNumber, t.requestType, t.requesterName, t.requesterEmail, t.branch, t.department, t.itemType, t.assignedToName, t.status]
        .some(v => v?.toLowerCase().includes(q));
    });
  }

  get newCount(): number { return this.tickets.filter(t => t.status === 'New').length; }
  get activeCount(): number { return this.tickets.filter(t => ['Assigned', 'In Progress'].includes(t.status)).length; }
  get waitingCount(): number { return this.tickets.filter(t => t.status.startsWith('Waiting') || t.status.startsWith('Escalated')).length; }
  get resolvedCount(): number { return this.tickets.filter(t => t.status === 'Resolved').length; }

  typeLabel(type: string): string {
    if (type === 'Incident') return 'Problem';
    if (type === 'Service') return 'IT Service';
    if (type === 'Equipment') return 'Equipment';
    return type;
  }

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
    this.attachments = [];
    this.api.attachments(ticket.id).subscribe({ next: files => this.attachments = files, error: () => { this.attachments = []; this.error = 'Could not load ticket attachments.'; } });
    this.api.history(ticket.id).subscribe({ next: history => this.history = history, error: () => this.history = [] });
  }

  downloadAttachment(ticketId: number, file: { id: number; fileName: string }): void {
    this.api.downloadAttachment(ticketId, file.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = file.fileName;
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
      },
      error: () => this.error = 'Could not download this attachment.'
    });
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

  openEscalation(ticket: HelpDeskTicket): void {
    this.escalationTicket = ticket;
    this.escalationForm = {
      teamViewerId: '', issueDescription: ticket.reason, troubleshooting: '', additionalNotes: '',
      emailBody: this.buildEscalationEmail(ticket, '', '', '')
    };
    this.showEscalation = true;
  }

  closeEscalation(): void { if (!this.escalating) { this.showEscalation = false; this.escalationTicket = undefined; } }

  refreshEscalationEmail(): void {
    if (!this.escalationTicket) return;
    this.escalationForm.emailBody = this.buildEscalationEmail(this.escalationTicket, this.escalationForm.teamViewerId, this.escalationForm.troubleshooting, this.escalationForm.additionalNotes);
  }

  submitEscalation(): void {
    const ticket = this.escalationTicket; const form = this.escalationForm;
    if (!ticket) return;
    if (!form.teamViewerId.trim() || !form.issueDescription.trim() || !form.troubleshooting.trim() || !form.emailBody.trim()) {
      this.error = 'TeamViewer ID, issue description, troubleshooting and email text are required for Just ICT escalation.'; return;
    }
    this.escalating = true; this.error = '';
    this.justIct.escalate(ticket.id, { teamViewerId: form.teamViewerId.trim(), issueDescription: form.issueDescription.trim(), troubleshooting: form.troubleshooting.trim(), additionalNotes: form.additionalNotes.trim() || undefined, emailBody: form.emailBody.trim() }).subscribe({
      next: () => { this.escalating = false; this.showEscalation = false; this.escalationTicket = undefined; this.reloadSelected(ticket.id); },
      error: e => { this.escalating = false; this.error = e?.error?.detail || e?.error || 'Could not escalate ticket to Just ICT.'; }
    });
  }

  resolve(ticket: HelpDeskTicket): void {
    const comment = window.prompt('Resolution summary:', 'Issue resolved by Royal Tyres IT.');
    if (!comment) return;
    this.api.updateStatus(ticket.id, 'Resolved', comment, true).subscribe({ next: () => this.reloadSelected(ticket.id), error: e => this.error = e?.error?.detail || e?.error || 'Could not resolve ticket.' });
  }

  private buildEscalationEmail(ticket: HelpDeskTicket, remoteId: string, troubleshooting: string, notes: string): string {
    return `RoyalDesk IT Support Escalation\n\nTicket: ${ticket.ticketNumber}\nType: ${this.typeLabel(ticket.requestType)}\nEmployee: ${ticket.requesterName}\nBranch: ${ticket.branch}\nDepartment: ${ticket.department}\nCategory: ${ticket.itemType}\n\nIssue:\n${ticket.reason}\n\nRoyal Tyres IT troubleshooting completed:\n${troubleshooting || '[Enter troubleshooting completed]'}\n\nTeamViewer ID:\n${remoteId || '[Enter TeamViewer ID]'}${notes ? `\n\nAdditional Notes:\n${notes}` : ''}\n\nPlease assist with the above issue.\n\nRoyal Tyres IT`;
  }

  private reloadSelected(id: number): void {
    this.api.ticket(id).subscribe({ next: ticket => { this.selected = ticket; this.load(); this.api.history(id).subscribe({ next: history => this.history = history }); } });
  }
}
