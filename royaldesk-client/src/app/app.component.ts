import { Component } from '@angular/core';
import { LifecycleComponent } from './features/lifecycle/lifecycle.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LifecycleComponent],
  template: '<app-lifecycle />'
})
export class AppComponent {}
