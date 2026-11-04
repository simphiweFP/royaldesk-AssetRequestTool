import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StaffAccessService } from '../../core/services/staff-access.service';

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

  readonly setupMode = window.location.pathname.toLowerCase().replace(/\/$/, '') === '/portal/setup';
  username = '';
  bootstrapRequired = false;
  loading = true;
  error = '';
  password = '';
  setup = { displayName: '', username: '', password: '', confirmPassword: '', bootstrapKey: '' };

  ngOnInit(): void {
    if (!this.setupMode) { this.loading = false; return; }
    this.access.bootstrapRequired().subscribe({
      next: result => {
        this.bootstrapRequired = result.required;
        if (!result.required) this.error = 'Administrator setup is already complete. Use Staff Access to sign in.';
        this.loading = false;
      },
      error: () => { this.error = 'Could not connect to RoyalDesk access service.'; this.loading = false; }
    });
  }

  unlock(): void {
    if (!this.username.trim() || !this.password || this.loading) return;
    this.error = '';
    this.loading = true;
    this.access.login(this.username.trim(), this.password).subscribe({
      next: () => { this.password = ''; this.loading = false; this.unlocked.emit(); },
      error: () => { this.error = 'Incorrect username or password, or access is unavailable.'; this.loading = false; }
    });
  }

  bootstrap(): void {
    if (!this.setupMode || !this.bootstrapRequired || this.loading) return;
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
        window.location.href = '/portal';
      },
      error: e => { this.error = e?.error?.detail || 'Could not create the first RoyalDesk administrator.'; this.loading = false; }
    });
  }

  employeePortal(): void { window.location.href = '/'; }
}
