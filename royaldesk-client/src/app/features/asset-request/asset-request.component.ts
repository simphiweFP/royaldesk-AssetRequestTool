import { Component, Input, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { AssetRequestService } from '../../core/services/asset-request.service';
import { LifecycleService } from '../../core/services/lifecycle.service';
import { PublicEmployeeDirectory } from '../../core/models/lifecycle.model';
import { AssetRequestResponse, CreateAssetRequest, RequestType } from '../../core/models/asset-request.model';

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

  @Input() requestType: RequestType = 'Equipment';

  readonly currentUser = { name: 'Employee Portal', username: 'employee', initials: 'IT' };
  employees: PublicEmployeeDirectory[] = [];

  readonly branches = ['Durban Passenger','Durban Commercial','Phoenix','Alberton','Blackheath'];
  readonly departments = ['Administration','IT','Sales','Finance','Operations','Human Resources','Workshop'];
  readonly equipmentTypes = ['Laptop','Monitor','Keyboard','Mouse','Headset','Printer','Barcode Scanner','POS Peripheral'];
  readonly incidentCategories = ['SAP','CisTech','Email','Hardware','Network','Printer','Software','Telephone','Windows','Other'];
  readonly serviceCategories = ['Access / Permissions','Software Installation','Stock Code Creation','Shared Folder','User Account','Custom Software','IT Advice','Other'];
  readonly equipmentReasons = ['New Starter','Replacement','Upgrade','Damaged Equipment','Additional Equipment'];

  readonly form = this.formBuilder.nonNullable.group({
    requesterName:[''], requesterEmail:[''], branch:[''], department:[''], itemType:[''], quantity:[1], reason:[''], additionalNotes:['']
  });

  submitting = false;
  submittedRequest: AssetRequestResponse | null = null;
  errorMessage = '';
  fieldErrors: Record<string,string> = {};
  showContactModal = false;
  attachments: File[] = [];
  attachmentError = '';
  readonly maxAttachments = 5;
  readonly maxAttachmentBytes = 8 * 1024 * 1024;
  get isStockCode(): boolean { return this.requestType === 'Service' && this.form.controls.itemType.value === 'Stock Code Creation'; }
  get showsAttachments(): boolean { return this.requestType === 'Incident' || this.isStockCode; }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = Array.from(input.files ?? []);
    input.value = '';
    this.attachmentError = '';
    const allowed = this.isStockCode
      ? ['.pdf', '.doc', '.docx', '.xls', '.xlsx']
      : ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.jpg', '.jpeg', '.png', '.webp'];
    for (const file of selected) {
      const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
      if (!allowed.includes(ext) || file.size === 0 || file.size > this.maxAttachmentBytes) {
        this.attachmentError = 'Select an allowed file smaller than 8 MB.';
        return;
      }
      if (this.attachments.length >= this.maxAttachments ||
          this.attachments.reduce((sum, entry) => sum + entry.size, 0) + file.size > 25 * 1024 * 1024) {
        this.attachmentError = 'Maximum 5 files and 25 MB per request.';
        return;
      }
      this.attachments.push(file);
    }
  }
  removeAttachment(index: number): void { this.attachments.splice(index, 1); this.attachmentError = ''; }


  constructor() {
    this.lifecycleService.publicDirectory().subscribe({ next: employees => this.employees = employees, error: () => this.employees = [] });
  }

  get pageTitle(): string {
    if (this.requestType === 'Incident') return 'Report an IT Problem';
    if (this.requestType === 'Service') return 'Request IT Service';
    return 'Request IT Equipment';
  }

  get pageDescription(): string {
    if (this.requestType === 'Incident') return 'Tell IT what is not working and give enough detail for us to start helping.';
    if (this.requestType === 'Service') return 'Request access, software, stock code creation, IT advice or another IT service.';
    return 'Request the IT equipment you need to keep Royal Tyres moving.';
  }

  get categoryLabel(): string {
    if (this.requestType === 'Incident') return 'Problem Category';
    if (this.requestType === 'Service') return 'Service Type';
    return 'Equipment';
  }

  get categoryOptions(): string[] {
    if (this.requestType === 'Incident') return this.incidentCategories;
    if (this.requestType === 'Service') return this.serviceCategories;
    return this.equipmentTypes;
  }

  get isEquipment(): boolean { return this.requestType === 'Equipment'; }
  get requestPreview() { return this.form.getRawValue(); }
  get employeeNames(): string[] { return this.employees.map(e => e.name); }

  onRequesterNameInput(): void {
    this.clearFieldError('requestername');
    const enteredName = this.form.controls.requesterName.value.trim();
    const employee = this.employees.find(e => e.name.localeCompare(enteredName, undefined, { sensitivity: 'accent' }) === 0);
    if (!employee) return;
    this.form.patchValue({
      requesterEmail: employee.email ?? '',
      branch: employee.branch && this.branches.includes(employee.branch) ? employee.branch : this.form.controls.branch.value,
      department: employee.department && this.departments.includes(employee.department) ? employee.department : this.form.controls.department.value
    });
  }

  decreaseQuantity(): void { const q=this.form.controls.quantity.value; if(q>1){this.form.controls.quantity.setValue(q-1);delete this.fieldErrors['quantity'];} }
  increaseQuantity(): void { const q=this.form.controls.quantity.value; if(q<10){this.form.controls.quantity.setValue(q+1);delete this.fieldErrors['quantity'];} }
  openContactModal(): void { this.showContactModal=true; }
  closeContactModal(): void { this.showContactModal=false; }

  submit(): void {
    this.errorMessage=''; this.fieldErrors={};
    if (this.isStockCode && !this.attachments.length) {
      this.attachmentError = 'Upload the stock code creation document before submitting.';
      return;
    }
    if (this.attachmentError) return;
    this.submitting=true;
    const value=this.form.getRawValue();
    const notes=value.additionalNotes.trim();
    const reason = this.isEquipment
      ? (notes ? `${value.reason} - ${notes}` : value.reason)
      : [value.reason.trim(), notes].filter(Boolean).join(' - ');

    const request:CreateAssetRequest={
      requestType:this.requestType,
      requesterName:value.requesterName.trim(),
      requesterEmail:value.requesterEmail.trim()||undefined,
      branch:value.branch,
      department:value.department,
      itemType:value.itemType,
      quantity:this.isEquipment ? value.quantity : 1,
      reason
    };

    this.service.createWithEvidence(request, this.showsAttachments ? this.attachments : []).pipe(finalize(()=>this.submitting=false)).subscribe({
      next:response=>{this.submittedRequest=response;this.attachments=[];},
      error:(error:HttpErrorResponse)=>{ if(error.status===400){this.setValidationErrors(error);return;} this.errorMessage=error.error?.detail || 'The request could not be submitted. Confirm that the API is running and try again.'; }
    });
  }

  submitAnotherRequest(): void {
    this.submittedRequest=null; this.errorMessage=''; this.fieldErrors={}; this.attachments=[]; this.attachmentError='';
    this.form.reset({requesterName:'',requesterEmail:'',branch:'',department:'',itemType:'',quantity:1,reason:'',additionalNotes:''});
  }

  clearFieldError(field:string):void{delete this.fieldErrors[field.toLowerCase()];}
  private setValidationErrors(error:HttpErrorResponse):void{
    const errors=error.error?.errors as Record<string,string[]>|undefined;
    if(!errors){this.errorMessage='The API rejected the request because one or more fields are invalid.';return;}
    this.fieldErrors={}; Object.entries(errors).forEach(([field,messages])=>this.fieldErrors[field.toLowerCase()]=messages.join(' '));
  }
}
