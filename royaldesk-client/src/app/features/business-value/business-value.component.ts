import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { BusinessValueSummary } from '../../core/models/business-value.model';
import { LifecycleService } from '../../core/services/lifecycle.service';

@Component({
  selector: 'app-business-value',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './business-value.component.html',
  styleUrl: './business-value.component.scss'
})
export class BusinessValueComponent implements OnInit {
  private readonly api = inject(LifecycleService);
  data?: BusinessValueSummary;
  loading = true;
  error = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.error = '';
    this.api.businessValue().subscribe({
      next: data => { this.data = data; this.loading = false; },
      error: e => { this.error = e?.error?.detail || e?.error || 'Could not load business value reporting.'; this.loading = false; }
    });
  }

  minutes(value: number): string {
    if (!value) return '0 min';
    if (value < 60) return `${Math.round(value)} min`;
    const hours = value / 60;
    return hours < 24 ? `${hours.toFixed(1)} hrs` : `${(hours / 24).toFixed(1)} days`;
  }
}
