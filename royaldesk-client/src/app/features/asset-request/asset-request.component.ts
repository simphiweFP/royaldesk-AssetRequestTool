import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
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
    name: 'Simphiwe Dlamuka',
    username: 'demo.user',
    initials: 'SD'
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

  // The API owns request validation for this assessment so invalid requests can
  // reach the endpoint and demonstrate the 400 Bad Request path.
  readonly form = this.formBuilder.nonNullable.group({
    branch: [''],
    department: [''],
    itemType: [''],
    quantity: [1],
    reason: ['']
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
    this.submitting = true;

    this.service.create(this.form.getRawValue())
      .pipe(finalize(() => this.submitting = false))
      .subscribe({
        next: response => {
          this.submittedRequest = response;
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 400) {
            this.errorMessage = this.getValidationMessage(error);
            return;
          }

          if (error.status === 401) {
            this.errorMessage = 'Authentication failed. The API rejected the request with 401 Unauthorized.';
            return;
          }

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

  private getValidationMessage(error: HttpErrorResponse): string {
    const errors = error.error?.errors as Record<string, string[]> | undefined;

    if (!errors) {
      return 'The API rejected the request because one or more fields are invalid.';
    }

    const messages = Object.values(errors).flat();
    return messages.length > 0
      ? messages.join(' ')
      : 'The API rejected the request because one or more fields are invalid.';
  }
}
