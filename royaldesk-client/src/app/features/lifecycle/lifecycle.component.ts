import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { HelpDeskTicketsComponent } from '../help-desk/help-desk-tickets.component';
import { StoreRoomComponent } from '../store-room/store-room.component';
import { BusinessValueComponent } from '../business-value/business-value.component';
import { AccessManagementComponent } from '../staff-access/access-management.component';
import { Asset, Assignment, Branch, Dashboard, Department, Employee, LifecycleTask, WriteOff } from '../../core/models/lifecycle.model';
import { LifecycleService } from '../../core/services/lifecycle.service';
import { StaffAccessService } from '../../core/services/staff-access.service';

@Component({
  selector: 'app-lifecycle',
  standalone: true,
  imports: [CommonModule, FormsModule, HelpDeskTicketsComponent, StoreRoomComponent, BusinessValueComponent, AccessManagementComponent],
  templateUrl: './lifecycle.component.html',
  styleUrl: './lifecycle.component.scss'
})
export class LifecycleComponent implements OnInit {
  private readonly api = inject(LifecycleService);
  readonly access = inject(StaffAccessService);

  section = 'dashboard';
  loading = true;
  error = '';
  dashboard?: Dashboard;
  branches: Branch[] = [];
  departments: Department[] = [];
  employees: Employee[] = [];
  assets: Asset[] = [];
  assignments: Assignment[] = [];
  writeOffs: WriteOff[] = [];
  tasks: LifecycleTask[] = [];
  selectedEmployeeId?: number;
  lifecycleType: 'Onboarding' | 'Offboarding' = 'Onboarding';
  assetSearch = '';

  employeeForm = { employeeNumber: '', firstName: '', lastName: '', email: '', username: '', jobTitle: '', branchId: undefined as number | undefined, departmentId: undefined as number | undefined, startDate: '' };
  assetForm = { assetTag: '', serialNumber: '', category: 'Laptop', make: '', model: '', branchId: undefined as number | undefined, departmentId: undefined as number | undefined, condition: 'Good', purchaseDate: '', purchaseValue: undefined as number | undefined, warrantyExpiry: '', fullyDepreciated: false, notes: '' };
  assignmentForm = { assetId: undefined as number | undefined, employeeId: undefined as number | undefined, condition: 'Good' };
  writeOffForm = { assetId: undefined as number | undefined, reason: 'Beyond economical repair', condition: 'Damaged', currentValue: undefined as number | undefined, notes: '' };

  ngOnInit(): void {
    this.section = this.firstAllowedSection();
    this.refresh();
  }

  can(page: string): boolean { return this.access.can(page); }
  get staffName(): string { return this.access.session?.displayName || 'RoyalDesk Staff'; }

  private firstAllowedSection(): string {
    const order: Array<[string,string]> = [
      ['dashboard','dashboard'],['tickets','tickets'],['store','store'],['assets','assets'],['assignments','assignments'],
      ['employees','employees'],['onboarding','onboarding'],['offboarding','offboarding'],['writeoffs','writeoffs'],
      ['business-value','reports'],['access-management','access-management']
    ];
    return order.find(([page]) => this.can(page))?.[1] || 'access-management';
  }

  get pageTitle(): string {
    const titles: Record<string, string> = {
      dashboard: 'Operations Dashboard', tickets: 'IT Service Desk', store: 'Store Room', assets: 'Asset Register',
      employees: 'Employees', assignments: 'Assignments & Returns', onboarding: 'Employee Onboarding', offboarding: 'Employee Offboarding',
      writeoffs: 'Write-Offs & E-Waste', reports: 'Business Value', 'access-management': 'Access Management'
    };
    return titles[this.section] ?? 'RoyalDesk';
  }

  get pageSubtitle(): string {
    const subtitles: Record<string, string> = {
      dashboard: 'A clear view of company assets, people and lifecycle activity.',
      tickets: 'Own, investigate and resolve IT requests with clear accountability.',
      store: 'Control IT stock, reservations and issues with ticket-linked traceability.',
      assets: 'Register, search and manage company equipment in one place.',
      employees: 'Maintain employee records used across the asset lifecycle.',
      assignments: 'Assign equipment, capture acceptance and manage returns.',
      onboarding: 'Track accounts, access, equipment and employee readiness.',
      offboarding: 'Recover company assets and close employee access responsibly.',
      writeoffs: 'Control approvals, disposal and the company e-waste register.',
      reports: 'Measure IT demand, response, reuse, stock activity and operational value.',
      'access-management': 'Create staff passwords and choose exactly which RoyalDesk pages each person can access.'
    };
    return subtitles[this.section] ?? 'RoyalDesk staff portal.';
  }

  get filteredAssets(): Asset[] {
    const q = this.assetSearch.trim().toLowerCase();
    if (!q) return this.assets;
    return this.assets.filter(a => [a.assetTag, a.serialNumber, a.category, a.make, a.model, a.assignedEmployeeName, a.status].some(v => v?.toLowerCase().includes(q)));
  }

  get availableAssets(): Asset[] { return this.assets.filter(a => a.status === 'Available'); }

  refresh(): void {
    this.loading = true;
    this.error = '';
    const needsAssets = this.can('assets') || this.can('assignments') || this.can('writeoffs');
    forkJoin({
      dashboard: this.can('dashboard') ? this.api.dashboard() : of(undefined),
      branches: this.api.branches(),
      departments: this.api.departments(),
      employees: this.api.employees(),
      assets: needsAssets ? this.api.assets() : of([] as Asset[]),
      assignments: this.can('assignments') ? this.api.assignments() : of([] as Assignment[]),
      writeOffs: this.can('writeoffs') ? this.api.writeOffs() : of([] as WriteOff[])
    }).subscribe({
      next: result => { Object.assign(this, result); this.loading = false; },
      error: err => { this.error = err?.error?.detail || err?.error || 'Could not load RoyalDesk.'; this.loading = false; }
    });
  }

  open(section: string): void { this.section = section; this.error = ''; }

  lock(): void {
    this.access.lock().subscribe({
      next: () => window.location.href = '/portal',
      error: () => { this.access.clearLocalSession(); window.location.href = '/portal'; }
    });
  }

  createEmployee(): void {
    this.api.createEmployee({ ...this.employeeForm, branchId: this.employeeForm.branchId || null, departmentId: this.employeeForm.departmentId || null, startDate: this.employeeForm.startDate || null }).subscribe({
      next: () => { this.employeeForm = { employeeNumber: '', firstName: '', lastName: '', email: '', username: '', jobTitle: '', branchId: undefined, departmentId: undefined, startDate: '' }; this.refresh(); },
      error: e => this.error = e?.error || 'Could not create employee.'
    });
  }

  createAsset(): void {
    const f = this.assetForm;
    this.api.createAsset({ ...f, branchId: f.branchId || null, departmentId: f.departmentId || null, purchaseDate: f.purchaseDate || null, warrantyExpiry: f.warrantyExpiry || null, purchaseValue: f.purchaseValue ?? null }).subscribe({
      next: () => { this.assetForm.assetTag=''; this.assetForm.serialNumber=''; this.assetForm.make=''; this.assetForm.model=''; this.assetForm.purchaseValue=undefined; this.assetForm.notes=''; this.refresh(); },
      error: e => this.error = e?.error || 'Could not create asset.'
    });
  }

  assignAsset(): void {
    if (!this.assignmentForm.assetId || !this.assignmentForm.employeeId) return;
    this.api.assign(this.assignmentForm).subscribe({ next: () => { this.assignmentForm.assetId=undefined; this.assignmentForm.employeeId=undefined; this.refresh(); }, error: e => this.error = e?.error?.detail || e?.error || 'Could not assign asset.' });
  }

  accept(assignment: Assignment): void {
    const signatureName = window.prompt('Employee full name for digital acceptance:', assignment.employeeName);
    if (!signatureName) return;
    this.api.accept(assignment.id, signatureName).subscribe({ next: () => this.refresh(), error: e => this.error = e?.error || 'Could not record acceptance.' });
  }

  returnAsset(assignment: Assignment): void {
    const reason = window.prompt('Return reason (Resignation, Transfer, Replacement, Damaged, Upgrade):', 'Replacement');
    if (!reason) return;
    const condition = window.prompt('Condition on return (Good, Fair, Damaged, Obsolete):', 'Good');
    if (!condition) return;
    this.api.returnAsset(assignment.id, reason, condition).subscribe({ next: () => this.refresh(), error: e => this.error = e?.error?.detail || e?.error || 'Could not return asset.' });
  }

  downloadAcceptance(assignment: Assignment): void {
    this.api.acceptancePdf(assignment.id).subscribe(blob => {
      const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = `Employee_Acceptance_${assignment.assetTag}_${assignment.id}.pdf`; a.click(); URL.revokeObjectURL(url);
    });
  }

  loadTasks(type: 'Onboarding' | 'Offboarding'): void {
    this.lifecycleType = type;
    if (!this.selectedEmployeeId) { this.tasks = []; return; }
    this.api.tasks(this.selectedEmployeeId, type).subscribe({ next: tasks => this.tasks = tasks, error: e => this.error = e?.error || 'Could not load checklist.' });
  }

  toggleTask(task: LifecycleTask): void {
    this.api.updateTask(task.id, !task.isCompleted).subscribe({ next: () => this.loadTasks(this.lifecycleType), error: e => this.error = e?.error || 'Could not update task.' });
  }

  createWriteOff(): void {
    if (!this.writeOffForm.assetId) return;
    this.api.createWriteOff(this.writeOffForm).subscribe({ next: () => { this.writeOffForm.assetId=undefined; this.writeOffForm.notes=''; this.refresh(); }, error: e => this.error = e?.error?.detail || e?.error || 'Could not create write-off.' });
  }

  approveWriteOff(item: WriteOff, approved: boolean): void {
    this.api.approveWriteOff(item.id, approved).subscribe({ next: () => this.refresh(), error: e => this.error = e?.error || 'Could not update approval.' });
  }

  dispose(item: WriteOff): void {
    const disposalCompany = window.prompt('Disposal company (optional):', '') || '';
    const certificateReference = window.prompt('Certificate/reference (optional):', '') || '';
    this.api.disposeWriteOff(item.id, { disposalMethod: 'E-Waste', disposalCompany, disposalDate: new Date().toISOString().slice(0,10), certificateReference, notes: 'Disposed from RoyalDesk' }).subscribe({ next: () => this.refresh(), error: e => this.error = e?.error?.detail || e?.error || 'Could not dispose asset.' });
  }
}
