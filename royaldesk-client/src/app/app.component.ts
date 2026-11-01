import { Component } from '@angular/core';
import { LifecycleComponent } from './features/lifecycle/lifecycle.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LifecycleComponent],
  template: '<app-lifecycle />',
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
      background: #f7f8fa;
    }

    /* The lifecycle shell is the single owner of the application footer. */
    :host ::ng-deep app-asset-request footer {
      display: none !important;
    }

    /* The information panel belongs only to views that contain a register/table. */
    :host ::ng-deep app-lifecycle .shared-info {
      display: none !important;
    }

    :host ::ng-deep app-lifecycle .workspace:has(.table-card) .shared-info {
      display: flex !important;
    }
  `]
})
export class AppComponent {}
