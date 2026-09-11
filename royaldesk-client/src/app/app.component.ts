import { Component } from '@angular/core';
import { AssetRequestComponent } from './features/asset-request/asset-request.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [AssetRequestComponent],
  template: '<app-asset-request />'
})
export class AppComponent {}
