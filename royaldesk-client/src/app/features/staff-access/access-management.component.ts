import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StaffAccessService, StaffProfile } from '../../core/services/staff-access.service';

@Component({
  selector: 'app-access-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './access-management.component.html',
  styleUrl: './access-management.component.scss'
})
export class AccessManagementComponent implements OnInit {
  private readonly access = inject(StaffAccessService);
  readonly pageOptions = [
    ['dashboard','Dashboard'],['tickets','IT Tickets'],['store','Store Room'],['assets','Asset Register'],
    ['assignments','Assignments & Returns'],['employees','Employees'],['onboarding','Onboarding'],
    ['offboarding','Offboarding'],['writeoffs','Write-Offs & E-Waste'],['business-value','Business Value'],
    ['access-management','Access Management']
  ];

  profiles: StaffProfile[] = [];
  selected?: StaffProfile;
  error = '';
  createForm = { displayName: '', username: '', password: '', pages: [] as string[] };
  editForm = { displayName: '', username: '', isActive: true, pages: [] as string[] };

  ngOnInit(): void { this.load(); }

  load(): void {
    this.access.profiles().subscribe({
      next: profiles => { this.profiles = profiles; if (this.selected) this.select(profiles.find(x => x.id === this.selected?.id)); },
      error: e => this.error = e?.error?.detail || 'Could not load staff access profiles.'
    });
  }

  select(profile?: StaffProfile): void {
    this.selected = profile;
    if (!profile) return;
    this.editForm = { displayName: profile.displayName, username: profile.username || '', isActive: profile.isActive, pages: [...profile.pages] };
  }

  toggle(list: string[], page: string, checked: boolean): void {
    if (checked && !list.includes(page)) list.push(page);
    if (!checked) {
      const index = list.indexOf(page);
      if (index >= 0) list.splice(index, 1);
    }
  }

  create(): void {
    this.error = '';
    this.access.createProfile({
      displayName: this.createForm.displayName,
      username: this.createForm.username || null,
      password: this.createForm.password,
      pages: this.createForm.pages
    }).subscribe({
      next: () => { this.createForm = { displayName: '', username: '', password: '', pages: [] }; this.load(); },
      error: e => this.error = e?.error?.detail || e?.error || 'Could not create staff access.'
    });
  }

  save(): void {
    if (!this.selected) return;
    this.access.updateProfile(this.selected.id, {
      displayName: this.editForm.displayName,
      username: this.editForm.username || null,
      isActive: this.editForm.isActive,
      pages: this.editForm.pages
    }).subscribe({ next: () => this.load(), error: e => this.error = e?.error?.detail || 'Could not save access changes.' });
  }

  resetPassword(): void {
    if (!this.selected) return;
    const password = window.prompt(`New password for ${this.selected.displayName}:`, '');
    if (!password) return;
    this.access.resetPassword(this.selected.id, password).subscribe({ next: () => this.load(), error: e => this.error = e?.error?.detail || 'Could not reset password.' });
  }
}
