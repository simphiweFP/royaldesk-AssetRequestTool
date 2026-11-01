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
      color: #17243f;
      font-family: Arial, Helvetica, sans-serif;
    }

    /* The lifecycle shell is the single owner of the application footer. */
    :host ::ng-deep app-asset-request footer,
    :host ::ng-deep app-lifecycle .shared-info {
      display: none !important;
    }

    /* Match the Request IT Equipment right-column structure. */
    :host ::ng-deep app-lifecycle .view-column {
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 14px;
    }

    :host ::ng-deep app-lifecycle .information-box.lifecycle-information-box {
      position: relative;
      min-height: 108px;
      padding: 19px;
      display: flex;
      align-items: center;
      gap: 14px;
      overflow: hidden;
      border: 1px solid #e0e5ed;
      border-radius: 8px;
      background: linear-gradient(100deg, #f5f8fd, #f7f8fa);
      color: #53627a;
      font-size: 12px;
      line-height: 1.45;
      box-sizing: border-box;
    }

    :host ::ng-deep app-lifecycle .information-box.lifecycle-information-box .info-circle {
      width: 44px;
      height: 44px;
      flex: 0 0 44px;
      display: grid;
      place-items: center;
      border-radius: 50%;
      background: #bcc7d6;
      color: #fff;
      font-size: 21px;
      font-weight: 700;
    }

    :host ::ng-deep app-lifecycle .information-box.lifecycle-information-box .tyre-decoration {
      position: absolute;
      width: 145px;
      height: 145px;
      right: -26px;
      bottom: -62px;
      border: 18px solid rgba(183, 194, 210, .21);
      border-radius: 50%;
      box-sizing: border-box;
    }

    :host ::ng-deep app-lifecycle .full-view-info {
      width: 100%;
    }

    /* Exact Request IT Equipment type and colour hierarchy. */
    :host ::ng-deep app-lifecycle .card-heading h2,
    :host ::ng-deep app-lifecycle .card-heading h3,
    :host ::ng-deep app-lifecycle td strong,
    :host ::ng-deep app-lifecycle .checklist strong,
    :host ::ng-deep app-lifecycle .table-empty strong,
    :host ::ng-deep app-lifecycle .lifecycle-empty strong {
      color: #0f347d !important;
    }

    :host ::ng-deep app-lifecycle .card-heading h2,
    :host ::ng-deep app-lifecycle .card-heading h3 {
      font-size: 21px !important;
      font-weight: 700 !important;
      line-height: 1.15 !important;
    }

    :host ::ng-deep app-lifecycle .card-heading p,
    :host ::ng-deep app-lifecycle .card p,
    :host ::ng-deep app-lifecycle td small,
    :host ::ng-deep app-lifecycle .table-empty p,
    :host ::ng-deep app-lifecycle .lifecycle-empty p {
      color: #69758b !important;
      font-size: 12px !important;
      line-height: 1.45 !important;
    }

    :host ::ng-deep app-lifecycle label {
      color: #17243f !important;
      font-size: 12px !important;
      font-weight: 700 !important;
    }

    :host ::ng-deep app-lifecycle input,
    :host ::ng-deep app-lifecycle select,
    :host ::ng-deep app-lifecycle textarea {
      border-color: #cbd4df !important;
      background: #fff !important;
      color: #1d2941 !important;
      font-size: 13px !important;
    }

    :host ::ng-deep app-lifecycle input:focus,
    :host ::ng-deep app-lifecycle select:focus,
    :host ::ng-deep app-lifecycle textarea:focus {
      border-color: #143f91 !important;
      box-shadow: 0 0 0 2px rgba(20, 63, 145, .08) !important;
    }

    :host ::ng-deep app-lifecycle th {
      color: #69758b !important;
      background: #fff !important;
      border-bottom-color: #dce2ea !important;
      font-size: 10px !important;
    }

    :host ::ng-deep app-lifecycle td {
      color: #17243f !important;
      border-bottom-color: #e4e8ef !important;
      font-size: 12px !important;
    }

    :host ::ng-deep app-lifecycle .heading-symbol,
    :host ::ng-deep app-lifecycle .pill,
    :host ::ng-deep app-lifecycle .refresh {
      color: #143f91 !important;
    }

    :host ::ng-deep app-lifecycle .form-card > .primary,
    :host ::ng-deep app-lifecycle .compact-form-card > .primary {
      background: #f11d2f !important;
      color: #fff !important;
    }

    @media (max-width: 900px) {
      :host ::ng-deep app-lifecycle .view-column {
        width: 100%;
      }
    }
  `]
})
export class AppComponent {}
