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
  mode: 'home' | 'request' | 'track' = 'home';
  openStaff(): void { window.location.href = '/portal'; }
}
