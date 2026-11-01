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

    :host ::ng-deep app-asset-request footer,
    :host ::ng-deep app-lifecycle .shared-info {
      display: none !important;
    }

    /* Request Equipment is the layout reference. */
    :host ::ng-deep app-lifecycle .page-section {
      width: min(1220px, calc(100% - 64px)) !important;
      margin: 22px auto !important;
      gap: 20px !important;
      min-height: 0 !important;
    }

    /* Keep one constant gap between page content and the footer on every view. */
    :host ::ng-deep app-lifecycle .shared-footer {
      margin-top: 0 !important;
    }

    :host ::ng-deep app-lifecycle .page-section,
    :host ::ng-deep app-lifecycle .request-embedded {
      margin-bottom: 22px !important;
    }

    :host ::ng-deep app-lifecycle .grid.two {
      grid-template-columns: 520px minmax(0, 1fr) !important;
      gap: 20px !important;
      align-items: start !important;
    }

    :host ::ng-deep app-lifecycle .operational-grid {
      align-items: start !important;
    }

    :host ::ng-deep app-lifecycle .card {
      min-height: 0 !important;
      padding: 18px !important;
      border: 1px solid #dce2ea !important;
      border-radius: 8px !important;
      background: #fff !important;
      box-shadow: 0 1px 5px rgba(24, 44, 80, .035) !important;
    }

    /* Never stretch the form/table cards just to make columns equal. */
    :host ::ng-deep app-lifecycle .balanced-card,
    :host ::ng-deep app-lifecycle .stable-table-card,
    :host ::ng-deep app-lifecycle .operational-grid > .form-card,
    :host ::ng-deep app-lifecycle .view-column > .table-card {
      min-height: 0 !important;
      height: auto !important;
    }

    :host ::ng-deep app-lifecycle .view-column {
      min-width: 0;
      display: flex;
      flex-direction: column;
      gap: 14px;
      align-self: start;
    }

    /* Assignments and Write-Offs use the same left-form/right-view pattern. */
    :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) {
      grid-template-columns: 520px minmax(0, 1fr) !important;
      grid-template-rows: auto auto !important;
      column-gap: 20px !important;
      row-gap: 14px !important;
      align-items: start !important;
    }

    :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) > .compact-form-card {
      grid-column: 1 !important;
      grid-row: 1 / span 2 !important;
    }

    :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) > .table-card {
      grid-column: 2 !important;
      grid-row: 1 !important;
    }

    :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) > .full-view-info {
      grid-column: 2 !important;
      grid-row: 2 !important;
      width: 100% !important;
      align-self: start !important;
    }

    /* Let empty tables look polished without becoming taller than the form. */
    :host ::ng-deep app-lifecycle .table-wrap {
      min-height: 0 !important;
      max-height: 530px !important;
    }

    :host ::ng-deep app-lifecycle .table-empty {
      min-height: 170px !important;
      padding: 26px 20px !important;
    }

    /* Exact compact information box from Request IT Equipment. */
    :host ::ng-deep app-lifecycle .information-box.lifecycle-information-box {
      position: relative;
      width: 100%;
      height: 108px !important;
      min-height: 108px !important;
      max-height: 108px !important;
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

    :host ::ng-deep app-lifecycle .card-heading {
      margin-bottom: 16px !important;
      gap: 13px !important;
    }

    :host ::ng-deep app-lifecycle .heading-symbol {
      color: #143f91 !important;
      font-size: 23px !important;
    }

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
      margin: 0 !important;
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
      gap: 6px !important;
      color: #17243f !important;
      font-size: 12px !important;
      font-weight: 700 !important;
    }

    :host ::ng-deep app-lifecycle .form-grid {
      gap: 12px !important;
    }

    :host ::ng-deep app-lifecycle input,
    :host ::ng-deep app-lifecycle select,
    :host ::ng-deep app-lifecycle textarea {
      width: 100% !important;
      height: 40px !important;
      min-height: 40px !important;
      padding: 0 12px !important;
      border: 1px solid #cbd4df !important;
      border-radius: 6px !important;
      background: #fff !important;
      color: #1d2941 !important;
      font-size: 13px !important;
      outline: none !important;
    }

    :host ::ng-deep app-lifecycle textarea {
      height: 57px !important;
      padding: 11px 12px !important;
    }

    :host ::ng-deep app-lifecycle input:focus,
    :host ::ng-deep app-lifecycle select:focus,
    :host ::ng-deep app-lifecycle textarea:focus {
      border-color: #143f91 !important;
      box-shadow: 0 0 0 2px rgba(20, 63, 145, .08) !important;
    }

    :host ::ng-deep app-lifecycle .form-card > .primary,
    :host ::ng-deep app-lifecycle .compact-form-card > .primary {
      width: 100% !important;
      height: 47px !important;
      min-height: 47px !important;
      margin-top: 14px !important;
      border: 0 !important;
      border-radius: 6px !important;
      background: #f11d2f !important;
      color: #fff !important;
      font-size: 15px !important;
      font-weight: 700 !important;
    }

    :host ::ng-deep app-lifecycle .form-card > .primary:hover,
    :host ::ng-deep app-lifecycle .compact-form-card > .primary:hover {
      background: #dc1929 !important;
    }

    :host ::ng-deep app-lifecycle th {
      padding: 11px 10px !important;
      color: #69758b !important;
      background: #fff !important;
      border-bottom: 1px solid #dce2ea !important;
      font-size: 10px !important;
      font-weight: 700 !important;
    }

    :host ::ng-deep app-lifecycle td {
      padding: 12px 10px !important;
      color: #17243f !important;
      border-bottom: 1px solid #e4e8ef !important;
      font-size: 12px !important;
    }

    :host ::ng-deep app-lifecycle .pill,
    :host ::ng-deep app-lifecycle .refresh {
      color: #143f91 !important;
    }

    @media (max-width: 900px) {
      :host ::ng-deep app-lifecycle .grid.two,
      :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) {
        grid-template-columns: 1fr !important;
      }

      :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) > .compact-form-card,
      :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) > .table-card,
      :host ::ng-deep app-lifecycle .page-section:has(> .compact-form-card):has(> .table-card) > .full-view-info {
        grid-column: 1 !important;
        grid-row: auto !important;
      }

      :host ::ng-deep app-lifecycle .view-column {
        width: 100%;
      }
    }
  `]
})
export class AppComponent {}
