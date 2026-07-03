import { Component, Input, OnChanges } from '@angular/core';
import { ModuleActivity } from '../../../core/models/metrics.model';

@Component({
  selector: 'app-activity-chart',
  templateUrl: './activity-chart.component.html',
  styleUrls: ['./activity-chart.component.scss'],
  standalone: false
})
export class ActivityChartComponent implements OnChanges {
  @Input() data: ModuleActivity[] = [];
  @Input() loading = false;

  chartData: any[] = [];
  maxValue = 0;

  readonly colors = [
    '#3B82F6', '#10B981', '#F59E0B', '#EF4444',
    '#8B5CF6', '#06B6D4', '#F97316', '#EC4899'
  ];

  ngOnChanges(): void {
    if (this.data?.length) {
      this.maxValue = Math.max(...this.data.map(d => d.totalActions), 1);
      this.chartData = this.data.map((item, i) => ({
        ...item,
        color: this.colors[i % this.colors.length],
        percentage: Math.round((item.totalActions / this.maxValue) * 100)
      }));
    }
  }
}