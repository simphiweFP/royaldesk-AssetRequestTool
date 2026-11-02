import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { AssetRequestComponent } from '../asset-request/asset-request.component';
import { TicketTrackingComponent } from '../ticket-tracking/ticket-tracking.component';

@Component({
  selector: 'app-employee-portal',
  standalone: true,
  imports: [CommonModule, AssetRequestComponent, TicketTrackingComponent],
  templateUrl: './employee-portal.component.html',
  styleUrl: './employee-portal.component.scss'
})
export class EmployeePortalComponent {
  mode: 'home' | 'request' | 'track' = this.initialMode();

  private initialMode(): 'home' | 'request' | 'track' {
    const path = window.location.pathname.toLowerCase();
    if (path.startsWith('/track')) return 'track';
    if (path.startsWith('/request') || path.startsWith('/enquiry')) return 'request';
    return 'home';
  }

  openStaff(): void { window.location.href = '/portal'; }
}
