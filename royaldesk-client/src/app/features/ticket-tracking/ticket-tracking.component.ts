import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PublicTicketDetail, PublicTicketSummary } from '../../core/models/helpdesk.model';
import { HelpDeskService } from '../../core/services/helpdesk.service';

@Component({
  selector: 'app-ticket-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ticket-tracking.component.html',
  styleUrl: './ticket-tracking.component.scss'
})
export class TicketTrackingComponent {
  private readonly api = inject(HelpDeskService);

  name = '';
  department = '';
  verification = '';
  loading = false;
  verifying = false;
  error = '';
  results: PublicTicketSummary[] = [];
  selected?: PublicTicketSummary;
  detail?: PublicTicketDetail;

  search(): void {
    if (this.name.trim().length < 2 || this.department.trim().length < 2) {
      this.error = 'Enter your name and department to search for your requests.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.selected = undefined;
    this.detail = undefined;
    this.verification = '';

    this.api.searchPublic(this.name.trim(), this.department.trim()).subscribe({
      next: results => {
        this.results = results;
        this.loading = false;
        if (!results.length) this.error = 'No RoyalDesk requests were found for that name and department.';
      },
      error: () => {
        this.loading = false;
        this.error = 'Could not search for your requests. Please try again.';
      }
    });
  }

  select(ticket: PublicTicketSummary): void {
    this.selected = ticket;
    this.detail = undefined;
    this.verification = '';
    this.error = '';
  }

  verify(): void {
    if (!this.selected || !this.verification.trim()) {
      this.error = 'Enter the email used on the request or your tracking PIN.';
      return;
    }

    this.verifying = true;
    this.error = '';
    const value = this.verification.trim();
    const isEmail = value.includes('@');

    this.api.verifyPublic(
      this.selected.ticketNumber,
      isEmail ? value : undefined,
      isEmail ? undefined : value
    ).subscribe({
      next: detail => {
        this.detail = detail;
        this.verifying = false;
      },
      error: () => {
        this.verifying = false;
        this.error = 'Verification did not match this ticket. Check your email or tracking PIN and try again.';
      }
    });
  }

  reset(): void {
    this.results = [];
    this.selected = undefined;
    this.detail = undefined;
    this.verification = '';
    this.error = '';
  }
}
