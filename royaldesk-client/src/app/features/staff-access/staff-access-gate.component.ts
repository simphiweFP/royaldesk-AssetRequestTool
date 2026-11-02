import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StaffAccessService, StaffSelector } from '../../core/services/staff-access.service';

@Component({
  selector: 'app-staff-access-gate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './staff-access-gate.component.html',
  styleUrl: './staff-access-gate.component.scss'
})
export class StaffAccessGateComponent implements OnInit {
  private readonly access = inject(StaffAccessService);
  @Output() unlocked = new EventEmitter<void>();

  profiles: StaffSelector[] = [];
  bootstrapRequired = false;
  loading = true;
  error = '';
  staffAccessId?: number;
  password = '';
  setup = { displayName: '', username: '', password: '', confirmPassword: '', bootstrapKey: '' };

  ngOnInit(): void {
    this.access.bootstrapRequired().subscribe({
      next: result => {
        this.bootstrapRequired = result.required;
        if (result.required) this.loading = false;
        else this.loadProfiles();
      },
      error: () => { this.error = 'Could not connect to RoyalDesk access service.'; this.loading = false; }
    });
  }

  private loadProfiles(): void {
    this.access.publicProfiles().subscribe({
      next: profiles => { this.profiles = profiles; this.staffAccessId = profiles[0]?.id; this.loading = false; },
      error: () => { this.error = 'Could not load staff access profiles.'; this.loading = false; }
    });
  }

  unlock(): void {
    if (!this.staffAccessId || !this.password) return;
    this.error = '';
    this.loading = true;
    this.access.unlock(this.staffAccessId, this.password).subscribe({
      next: () => { this.password = ''; this.loading = false; this.unlocked.emit(); },
      error: e => { this.error = e?.error?.detail || 'Incorrect password or access is unavailable.'; this.loading = false; }
    });
  }

  bootstrap(): void {
    if (!this.setup.displayName || !this.setup.password || !this.setup.bootstrapKey) return;
    if (this.setup.password !== this.setup.confirmPassword) { this.error = 'Passwords do not match.'; return; }
    this.loading = true;
    this.error = '';
    this.access.bootstrap({
      displayName: this.setup.displayName,
      username: this.setup.username || null,
      password: this.setup.password,
      bootstrapKey: this.setup.bootstrapKey
    }).subscribe({
      next: () => {
        this.bootstrapRequired = false;
        this.setup = { displayName: '', username: '', password: '', confirmPassword: '', bootstrapKey: '' };
        this.loadProfiles();
      },
      error: e => { this.error = e?.error?.detail || e?.error || 'Could not create the first RoyalDesk administrator.'; this.loading = false; }
    });
  }

  employeePortal(): void { window.location.href = '/'; }
}
