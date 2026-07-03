import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { AnalyticsService } from '../../core/services/analytics.service';
import { SignalRService } from '../../core/services/signalr.service';

@Component({
  selector: 'app-analytics',
  templateUrl: './analytics.component.html',
  styleUrls: ['./analytics.component.scss'],
  standalone: false
})
export class AnalyticsComponent implements OnInit, OnDestroy {
  metrics: any = null;
  liveActivities: any[] = [];
  loading = false;
  displayedColumns = ['module', 'totalActions', 'successCount', 'failureCount'];
  private subs = new Subscription();

  constructor(
    private analyticsService: AnalyticsService,
    private signalRService: SignalRService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadMetrics();

    this.subs.add(
      this.signalRService.teamActivity$.subscribe(activity => {
        if (activity) {
          this.liveActivities.unshift({
            ...activity,
            time: new Date()
          });
          if (this.liveActivities.length > 20) this.liveActivities.pop();
          this.cdr.detectChanges();
        }
      })
    );
  }

  loadMetrics(): void {
    this.loading = true;
    this.analyticsService.getDashboard().subscribe({
      next: (data) => {
        this.metrics = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  getSuccessRate(item: any): number {
    if (!item.totalActions) return 0;
    return Math.round((item.successCount / item.totalActions) * 100);
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  getModuleColor(module: string): string {
    const colors: Record<string, string> = {
      'Documents': '#3B82F6',
      'TestCases': '#10B981',
      'Auth': '#EF4444',
      'Metrics': '#F59E0B',
      'Projects': '#8B5CF6',
      'Chat': '#06B6D4',
    };
    return colors[module] || '#64748B';
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }
}