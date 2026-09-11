import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AssetRequestService } from '../../core/services/asset-request.service';
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
  private readonly formBuilder = inject(FormBuilder);

  readonly currentUser = {
    name: 'Demo User',
    username: 'demo.user',
    initials: 'DU'
  };

  readonly branches = [
    'Durban Passenger',
    'Durban Commercial',
    'Phoenix',
    'Alberton',
    'Blackheath'
  ];

  readonly departments = ['IT', 'Sales', 'Finance', 'Operations', 'Human Resources'];
  readonly itemTypes = ['Laptop', 'Desktop', 'Monitor', 'Mouse', 'Keyboard', 'Headset'];

  readonly form = this.formBuilder.nonNullable.group({
    branch: ['', Validators.required],
    department: ['', Validators.required],
    itemType: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]]
  });

  submitting = false;
  submittedRequest: AssetRequestResponse | null = null;
  errorMessage = '';

  get requestPreview(): CreateAssetRequest {
    return this.form.getRawValue();
  }

  get reasonLength(): number {
    return this.form.controls.reason.value.length;
  }

  decreaseQuantity(): void {
    const quantity = this.form.controls.quantity.value;

    if (quantity > 1) {
      this.form.controls.quantity.setValue(quantity - 1);
    }
  }

  increaseQuantity(): void {
    const quantity = this.form.controls.quantity.value;

    if (quantity < 10) {
      this.form.controls.quantity.setValue(quantity + 1);
    }
  }

  submit(): void {
    this.errorMessage = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;

    this.service.create(this.form.getRawValue())
      .pipe(finalize(() => this.submitting = false))
      .subscribe({
        next: response => {
          this.submittedRequest = response;
        },
        error: () => {
          this.errorMessage =
            'The request could not be submitted. Confirm that the API is running and try again.';
        }
      });
  }

  submitAnotherRequest(): void {
    this.submittedRequest = null;
    this.errorMessage = '';
    this.form.reset({
      branch: '',
      department: '',
      itemType: '',
      quantity: 1,
      reason: ''
    });
  }
}
