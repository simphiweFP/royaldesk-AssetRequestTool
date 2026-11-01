import { Component } from '@angular/core';
import { LifecycleComponent } from './features/lifecycle/lifecycle.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LifecycleComponent],
  template: `
    <app-lifecycle #lifecycle />

    <section class="shared-lower-shell" aria-label="RoyalDesk information and footer">
      @if (lifecycle.section !== 'request') {
        <div class="shared-info">
          <div class="info-circle">i</div>
          <div>
            RoyalDesk keeps requests, equipment and employee lifecycle activity in one controlled workflow.
            <br />
            Use the navigation to manage each stage from request through disposal.
          </div>
          <div class="tyre-decoration"></div>
        </div>
      }

      <footer class="royaldesk-footer">
        <div class="footer-inner">
          <img
            src="assets/images/royal-tyres-logo-footer-final.png"
            alt="Royal Tyres"
            class="footer-logo"
          />

          <div class="footer-middle">
            <strong>RoyalDesk &nbsp;|&nbsp; IT Asset Request Portal</strong>
            <span>Your trusted tyre workshop since 1939.</span>
          </div>

          <div class="footer-right">
            <div>Privacy Policy &nbsp;|&nbsp; Cookie Notice &nbsp;|&nbsp; Contact Us</div>
            <span>© 2026 Royal Tyres. All rights reserved.</span>
          </div>
        </div>
      </footer>
    </section>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
      background: #f7f8fa;
      font-family: Arial, Helvetica, sans-serif;
    }

    :host ::ng-deep app-asset-request footer {
      display: none !important;
    }

    .shared-lower-shell {
      margin-left: 182px;
      background: #f7f8fa;
    }

    .shared-info {
      position: relative;
      width: min(1220px, calc(100% - 64px));
      min-height: 108px;
      margin: 0 auto 22px;
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

    .info-circle {
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

    .tyre-decoration {
      position: absolute;
      width: 145px;
      height: 145px;
      right: -26px;
      bottom: -62px;
      border: 18px solid rgba(183, 194, 210, .21);
      border-radius: 50%;
      box-sizing: border-box;
    }

    .royaldesk-footer {
      min-height: 105px;
      background: linear-gradient(90deg, #174899, #0e3c8a, #052a70);
      color: #fff;
    }

    .footer-inner {
      width: min(1220px, calc(100% - 64px));
      min-height: 105px;
      margin: 0 auto;
      display: grid;
      grid-template-columns: 290px 1fr 350px;
      align-items: center;
    }

    .footer-logo {
      width: 220px;
      height: 67px;
      object-fit: contain;
      object-position: left center;
    }

    .footer-middle {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 5px;
      font-size: 12px;
    }

    .footer-middle span {
      font-size: 10px;
      opacity: .78;
    }

    .footer-right {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 9px;
      font-size: 10px;
    }

    .footer-right span {
      opacity: .8;
    }

    @media (max-width: 900px) {
      .shared-lower-shell {
        margin-left: 0;
      }

      .shared-info,
      .footer-inner {
        width: min(100% - 32px, 1220px);
      }
    }

    @media (max-width: 650px) {
      .shared-info,
      .footer-inner {
        width: calc(100% - 24px);
      }

      .footer-inner {
        padding: 18px 0;
        grid-template-columns: 1fr;
        gap: 15px;
        justify-items: center;
        text-align: center;
      }

      .footer-right {
        align-items: center;
      }
    }
  `]
})
export class AppComponent {}
