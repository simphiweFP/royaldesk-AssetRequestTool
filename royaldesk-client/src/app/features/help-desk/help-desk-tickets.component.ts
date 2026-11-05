import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HelpDeskTicket, TicketStatusHistory, EquipmentDelivery, EquipmentDeliveryEvent } from '../../core/models/helpdesk.model';
import { HelpDeskService } from '../../core/services/helpdesk.service';
import { JustIctService } from '../../core/services/just-ict.service';
import { StoreItem, StoreLinkedAsset } from '../../core/models/store-room.model';
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

  readonly statuses = ['All', 'New', 'Assigned', 'In Progress', 'Waiting for User', 'Waiting for Third Party', 'Escalated to Just ICT', 'Awaiting Local Handover', 'Waiting for Driver Pickup', 'In Transit', 'Awaiting Receipt Confirmation', 'Partially Received', 'Delivery Failed', 'Returned to Sender', 'Done', 'Resolved', 'Closed'];
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

  outstandingDeliveries: EquipmentDelivery[] = [];
  get awaitingPickupCount(): number { return this.outstandingDeliveries.filter(d => d.status==='Waiting for Driver Pickup').length; }
  get inTransitCount(): number { return this.outstandingDeliveries.filter(d => ['In Transit','Awaiting Receipt Confirmation','Partially Received'].includes(d.status)).length; }
  get deliveryExceptionCount(): number { return this.outstandingDeliveries.filter(d => ['Delivery Failed','Returned to Sender'].includes(d.status)).length; }

  openDeliveryTicket(delivery: EquipmentDelivery): void {
    const cached=this.tickets.find(t=>t.id===delivery.ticketId);
    if(cached) { this.select(cached); return; }
    this.api.ticket(delivery.ticketId).subscribe({
      next: ticket=>this.select(ticket),
      error: ()=>this.error='Could not open the delivery ticket.'
    });
  }

  delivery: EquipmentDelivery | null = null;
  deliveryEvents: EquipmentDeliveryEvent[] = [];
  deliveryStock: StoreItem[] = [];
  eligibleDeliveryAssets: StoreLinkedAsset[] = [];
  deliveryBusy = false;
  deliveryError = '';
  deliveryForm = {
    storeItemId: 0, assetId: 0, recipientName: '',
    deliveryRequired: true, localHandoverReason: '',
    driverName: '', trackingReference: '',
    receivedBy: '', receiptReference: '', receivedQuantity: 1, notes: ''
  };
  get selectedDeliveryStock(): StoreItem | undefined {
    return this.deliveryStock.find(item => item.id === this.deliveryForm.storeItemId);
  }
  get remainingDeliveryQuantity(): number {
    return this.delivery ? this.delivery.quantity - this.delivery.receivedQuantity : 0;
  }

  onDeliveryStockChanged(): void {
    this.deliveryForm.assetId = 0;
    this.eligibleDeliveryAssets = [];
    if (!this.deliveryForm.storeItemId) return;
    const itemId = this.deliveryForm.storeItemId;
    this.api.deliveryAssets(itemId).subscribe({
      next: assets => { if (this.deliveryForm.storeItemId === itemId) this.eligibleDeliveryAssets = assets; },
      error: () => this.deliveryError = 'Could not load available asset tags for this stock item.'
    });
  }

  loadDelivery(ticket: HelpDeskTicket): void {
    this.delivery = null;
    this.deliveryEvents = [];
    this.deliveryError = '';
    if (ticket.requestType !== 'Equipment') return;
    this.deliveryForm = {
      storeItemId: 0, assetId: 0, recipientName: ticket.requesterName,
      deliveryRequired: ticket.branch.trim().toLowerCase() !== 'phoenix', localHandoverReason: '',
      driverName: '', trackingReference: '', receivedBy: '', receiptReference: '',
      receivedQuantity: ticket.quantity, notes: ''
    };
    this.api.deliveryStock().subscribe({
      next: items => { if (this.selected?.id === ticket.id) this.deliveryStock = items; },
      error: () => this.deliveryError = 'Could not load Store Room equipment.'
    });
    this.api.delivery(ticket.id).subscribe({
      next: d => { if (this.selected?.id === ticket.id) {
        this.delivery = d; this.deliveryForm.receivedQuantity = d.quantity - d.receivedQuantity;
        this.loadDeliveryEvents(ticket.id);
      }},
      error: e => { if (this.selected?.id === ticket.id && e?.status !== 404) this.deliveryError = 'Could not load delivery status.'; }
    });
  }

  loadDeliveryEvents(id: number): void {
    this.api.deliveryHistory(id).subscribe({
      next: events => { if (this.selected?.id === id) this.deliveryEvents = events; },
      error: () => this.deliveryEvents = []
    });
  }

  prepareDelivery(): void {
    const t = this.selected, f = this.deliveryForm;
    if (!t || !f.storeItemId || !f.recipientName.trim()) {
      this.deliveryError = 'Choose Store Room stock and enter the recipient.'; return;
    }
    this.runDelivery(this.api.prepareDelivery(t.id, {
      storeItemId: f.storeItemId, assetId: f.assetId || null,
      deliveryRequired: f.deliveryRequired, recipientName: f.recipientName.trim(),
      localHandoverReason: f.localHandoverReason.trim() || null
    }), t.id);
  }

  retryDelivery(): void {
    if (this.selected) this.runDelivery(this.api.retryDelivery(this.selected.id), this.selected.id);
  }

  pickupDelivery(): void {
    if (!this.selected || !this.deliveryForm.driverName.trim()) {
      this.deliveryError = 'Enter the name of the driver collecting the item.'; return;
    }
    this.runDelivery(this.api.pickupDelivery(this.selected.id,
      this.deliveryForm.driverName, this.deliveryForm.trackingReference), this.selected.id);
  }

  arriveDelivery(): void {
    if (this.selected) this.runDelivery(this.api.arriveDelivery(this.selected.id), this.selected.id);
  }

  confirmDelivery(): void {
    if (!this.selected || !this.deliveryForm.receivedBy.trim() || !this.deliveryForm.receiptReference.trim() ||
        this.deliveryForm.receivedQuantity < 1 || this.deliveryForm.receivedQuantity > this.remainingDeliveryQuantity) {
      this.deliveryError = 'Provide the receiving person, proof/reference and a valid received quantity.'; return;
    }
    const f = this.deliveryForm;
    this.runDelivery(this.api.confirmReceipt(this.selected.id,
      f.receivedQuantity, f.receivedBy.trim(), f.receiptReference.trim(), f.notes), this.selected.id);
  }

  failDelivery(): void {
    if (!this.selected || !this.deliveryForm.notes.trim()) {
      this.deliveryError = 'Describe the failed delivery.'; return;
    }
    this.runDelivery(this.api.failDelivery(this.selected.id, this.deliveryForm.notes.trim()), this.selected.id);
  }

  returnFailedDelivery(): void {
    if (!this.selected || !this.deliveryForm.notes.trim()) {
      this.deliveryError = 'Record why the remaining items were physically returned to Store Room.'; return;
    }
    this.runDelivery(this.api.returnDelivery(this.selected.id, this.deliveryForm.notes.trim()), this.selected.id);
  }

  private runDelivery(operation: import('rxjs').Observable<EquipmentDelivery>, id: number): void {
    this.deliveryBusy = true;
    this.deliveryError = '';
    operation.subscribe({
      next: d => { this.deliveryBusy = false; if (this.selected?.id === id) {
        this.delivery = d;
        this.deliveryForm.receivedQuantity = d.quantity - d.receivedQuantity;
        this.reloadSelected(id);
      }},
      error: e => { this.deliveryBusy = false;
        this.deliveryError = e?.error?.detail || 'Could not update delivery. Check the stock, status and required fields.';
      }
    });
  }

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
  get waitingCount(): number { return this.tickets.filter(t => t.status.startsWith('Waiting') || t.status.startsWith('Escalated') || t.status==='Awaiting Receipt Confirmation').length; }
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
    this.api.openDeliveries().subscribe({
      next: records=>this.outstandingDeliveries=records,
      error: ()=>{ this.outstandingDeliveries=[]; }
    });
    this.api.tickets(this.statusFilter).subscribe({
      next: tickets => { this.tickets = tickets; this.loading = false; },
      error: e => { this.error = e?.error?.detail || e?.error || 'Could not load IT tickets.'; this.loading = false; }
    });
  }

  select(ticket: HelpDeskTicket): void {
    this.selected = ticket;
    this.loadDelivery(ticket);
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
    this.api.ticket(id).subscribe({ next: ticket => { this.selected = ticket; this.loadDelivery(ticket); this.load(); this.api.history(id).subscribe({ next: history => this.history = history }); } });
  }
}
