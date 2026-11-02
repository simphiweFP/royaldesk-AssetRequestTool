import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { EmployeePortalComponent } from './features/employee-portal/employee-portal.component';
import { LifecycleComponent } from './features/lifecycle/lifecycle.component';
import { StaffAccessGateComponent } from './features/staff-access/staff-access-gate.component';
import { StaffAccessService } from './core/services/staff-access.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, EmployeePortalComponent, LifecycleComponent, StaffAccessGateComponent],
  template: `
    <app-employee-portal *ngIf="!isStaffPortal"></app-employee-portal>
    <div class="session-check" *ngIf="isStaffPortal && checkingSession">Checking RoyalDesk access…</div>
    <app-staff-access-gate *ngIf="isStaffPortal && !checkingSession && !unlocked" (unlocked)="onUnlocked()"></app-staff-access-gate>
    <app-lifecycle *ngIf="isStaffPortal && !checkingSession && unlocked"></app-lifecycle>
  `,
  styles: [`
    :host { display:block; min-height:100vh; }
    .session-check { min-height:100vh; display:grid; place-items:center; color:#69758b; font:13px Arial,sans-serif; background:#f7f8fa; }
  `]
})
export class AppComponent implements OnInit {
  private readonly access = inject(StaffAccessService);
  readonly isStaffPortal = window.location.pathname.toLowerCase().startsWith('/portal');
  checkingSession = this.isStaffPortal;
  unlocked = false;

  ngOnInit(): void {
    if (!this.isStaffPortal) return;
    this.access.restore().subscribe({
      next: () => { this.unlocked = true; this.checkingSession = false; },
      error: () => { this.access.clearLocalSession(); this.unlocked = false; this.checkingSession = false; }
    });
  }

  onUnlocked(): void { this.unlocked = true; }
}
