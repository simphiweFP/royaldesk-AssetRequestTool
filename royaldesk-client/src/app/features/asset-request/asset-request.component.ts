import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { AssetRequestService } from '../../core/services/asset-request.service';
import { LifecycleService } from '../../core/services/lifecycle.service';
import { Employee } from '../../core/models/lifecycle.model';
import { AssetRequestResponse, CreateAssetRequest } from '../../core/models/asset-request.model';

@Component({
  selector: 'app-asset-request',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './asset-request.component.html',
  styleUrl: './asset-request.component.scss'
})
export class AssetRequestComponent {
  private readonly service = inject(AssetRequestService);
  private readonly lifecycleService = inject(LifecycleService);
  private readonly formBuilder = inject(FormBuilder);

  readonly currentUser = {
    name: 'Employee Portal',
    username: 'employee',
    initials: 'IT'
  };

  employees: Employee[] = [];

  readonly branches = [
    'Durban Passenger',
    'Durban Commercial',
    'Phoenix',
    'Alberton',
    'Blackheath'
  ];

  readonly departments = [
    'Administration',
    'IT',
    'Sales',
    'Finance',
    'Operations',
    'Human Resources',
    'Workshop'
  ];

  readonly itemTypes = [
    'Equipment Request',
    'Hardware',
    'Printer / Scanner',
    'Network / Internet / Wi-Fi',
    'Email / Outlook',
    'SAP',
    'CisTech',
    'VPN',
    'User Account / Password / Access',
    'Shared Folder',
    'Software Installation',
    'Telephone / VoIP',
    'PC Performance',
    'Monitor / Peripheral',
    'Boardroom / TV / Casting',
    'Website',
    'General IT Enquiry',
    'Other Infrastructure'
  ];

  readonly reasonOptions = [
    'IT Issue / Incident',
    'New Starter',
    'Replacement',
    'Upgrade',
    'Damaged Equipment',
    'Additional Equipment',
    'Access Required',
    'General Enquiry',
    'Other'
  ];

  readonly form = this.formBuilder.nonNullable.group({
    requesterName: [''],
    requesterEmail: [''],
    branch: [''],
    department: [''],
    itemType: [''],
    quantity: [1],
    reason: [''],
    additionalNotes: ['']
  });

  submitting = false;
  submittedRequest: AssetRequestResponse | null = null;
  errorMessage = '';
  fieldErrors: Record<string, string> = {};
  showContactModal = false;

  constructor() {
    this.lifecycleService.employees().subscribe({
      next: employees => { this.employees = employees.filter(e => e.employmentStatus !== 'Inactive'); },
      error: () => { this.employees = []; }
    });
  }

  get requestPreview() { return this.form.getRawValue(); }
  get employeeNames(): string[] { return this.employees.map(e => `${e.firstName} ${e.lastName}`.trim()); }

  onRequesterNameInput(): void {
    this.clearFieldError('requestername');
    const enteredName = this.form.controls.requesterName.value.trim();
    const employee = this.employees.find(
      e => `${e.firstName} ${e.lastName}`.trim().localeCompare(enteredName, undefined, { sensitivity: 'accent' }) === 0
    );
    if (!employee) return;

    this.form.patchValue({
      requesterEmail: employee.email ?? '',
      branch: employee.branchName && this.branches.includes(employee.branchName) ? employee.branchName : this.form.controls.branch.value,
      department: employee.departmentName && this.departments.includes(employee.departmentName) ? employee.departmentName : this.form.controls.department.value
    });
  }

  decreaseQuantity(): void {
    const quantity = this.form.controls.quantity.value;
    if (quantity > 1) { this.form.controls.quantity.setValue(quantity - 1); delete this.fieldErrors['quantity']; }
  }

  increaseQuantity(): void {
    const quantity = this.form.controls.quantity.value;
    if (quantity < 10) { this.form.controls.quantity.setValue(quantity + 1); delete this.fieldErrors['quantity']; }
  }

  openContactModal(): void { this.showContactModal = true; }
  closeContactModal(): void { this.showContactModal = false; }

  submit(): void {
    this.errorMessage = '';
    this.fieldErrors = {};
    this.submitting = true;
    const value = this.form.getRawValue();
    const notes = value.additionalNotes.trim();
    const request: CreateAssetRequest = {
      requesterName: value.requesterName.trim(),
      requesterEmail: value.requesterEmail.trim() || undefined,
      branch: value.branch,
      department: value.department,
      itemType: value.itemType,
      quantity: value.quantity,
      reason: notes ? `${value.reason} - ${notes}` : value.reason
    };

    this.service.create(request).pipe(finalize(() => { this.submitting = false; })).subscribe({
      next: response => { this.submittedRequest = response; },
      error: (error: HttpErrorResponse) => {
        if (error.status === 400) { this.setValidationErrors(error); return; }
        this.errorMessage = 'The request could not be submitted. Confirm that the API is running and try again.';
      }
    });
  }

  submitAnotherRequest(): void {
    this.submittedRequest = null;
    this.errorMessage = '';
    this.fieldErrors = {};
    this.form.reset({ requesterName: '', requesterEmail: '', branch: '', department: '', itemType: '', quantity: 1, reason: '', additionalNotes: '' });
  }

  clearFieldError(field: string): void { delete this.fieldErrors[field.toLowerCase()]; }

  private setValidationErrors(error: HttpErrorResponse): void {
    const errors = error.error?.errors as Record<string, string[]> | undefined;
    if (!errors) { this.errorMessage = 'The API rejected the request because one or more fields are invalid.'; return; }
    this.fieldErrors = {};
    Object.entries(errors).forEach(([field, messages]) => { this.fieldErrors[field.toLowerCase()] = messages.join(' '); });
  }
}
