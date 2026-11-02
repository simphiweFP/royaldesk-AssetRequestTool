import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { AssetRequestComponent } from '../asset-request/asset-request.component';
import { TicketTrackingComponent } from '../ticket-tracking/ticket-tracking.component';
import { RequestType } from '../../core/models/asset-request.model';

@Component({
  selector: 'app-employee-portal',
  standalone: true,
  imports: [CommonModule, AssetRequestComponent, TicketTrackingComponent],
  templateUrl: './employee-portal.component.html',
  styleUrl: './employee-portal.component.scss'
})
export class EmployeePortalComponent {
  mode: 'home' | 'request' | 'track' = this.initialMode();
  requestType: RequestType = this.initialRequestType();

  private initialMode(): 'home' | 'request' | 'track' {
    const path = window.location.pathname.toLowerCase();
    if (path.startsWith('/track')) return 'track';
    if (path.startsWith('/problem') || path.startsWith('/request') || path.startsWith('/service') || path.startsWith('/enquiry')) return 'request';
    return 'home';
  }

  private initialRequestType(): RequestType {
    const path = window.location.pathname.toLowerCase();
    if (path.startsWith('/problem')) return 'Incident';
    if (path.startsWith('/service') || path.startsWith('/enquiry')) return 'Service';
    return 'Equipment';
  }

  openRequest(type: RequestType): void {
    this.requestType = type;
    this.mode = 'request';
  }

  openStaff(): void { window.location.href = '/portal'; }
}
