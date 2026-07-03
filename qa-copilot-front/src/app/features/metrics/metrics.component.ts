import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { map } from 'rxjs';

@Component({
  selector: 'app-metrics',
  templateUrl: './metrics.component.html',
  styleUrls: ['./metrics.component.scss'],
  standalone: false
})
export class MetricsComponent implements OnInit {
  dashboard: any = null;
  users: any[] = [];
  loading = false;
  loadingUsers = false;
  selectedTab = 'overview';

  displayedColumnsActivity = ['module', 'totalActions', 'successCount', 'failureCount', 'successRate'];
  displayedColumnsUsers = ['fullName', 'email', 'role', 'totalDocuments', 'totalTestCases', 'lastActivity'];

  constructor(
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
    this.loadUsers();
  }

  loadDashboard(): void {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/api/metrics/dashboard`).pipe(
      map(r => r.data || r)
    ).subscribe({
      next: (data) => {
        this.dashboard = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadUsers(): void {
    this.loadingUsers = true;
    this.http.get<any>(`${environment.apiUrl}/api/users`).pipe(
      map(r => r.data || r)
    ).subscribe({
      next: (data) => {
        this.users = Array.isArray(data) ? data : [];
        this.loadingUsers = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingUsers = false;
        this.cdr.detectChanges();
      }
    });
  }

  getSuccessRate(item: any): string {
    if (!item.totalActions) return '0%';
    return Math.round((item.successCount / item.totalActions) * 100) + '%';
  }

  getSuccessRateColor(item: any): string {
    if (!item.totalActions) return '#64748B';
    const rate = (item.successCount / item.totalActions) * 100;
    if (rate >= 80) return '#10B981';
    if (rate >= 60) return '#F59E0B';
    return '#EF4444';
  }

  getRoleColor(role: string): string {
    const colors: Record<string, string> = {
      'Admin': '#EF4444',
      'Senior': '#F59E0B',
      'QAEngineer': '#3B82F6',
      'Viewer': '#64748B'
    };
    return colors[role] || '#64748B';
  }

  getRoleLabel(role: string): string {
    const labels: Record<string, string> = {
      'Admin': 'Administrador',
      'Senior': 'Senior QA',
      'QAEngineer': 'QA Engineer',
      'Viewer': 'Auditor'
    };
    return labels[role] || role;
  }

  formatDate(date: string): string {
    if (!date) return 'Sin actividad';
    return new Date(date).toLocaleDateString('es-CO', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  getModuleIcon(module: string): string {
    const icons: Record<string, string> = {
      'Documents': 'description',
      'TestCases': 'auto_awesome',
      'Auth': 'lock',
      'Metrics': 'bar_chart',
      'Projects': 'work',
      'Chat': 'smart_toy',
      'History': 'history'
    };
    return icons[module] || 'circle';
  }

  getModuleColor(module: string): string {
    const colors: Record<string, string> = {
      'Documents': '#3B82F6',
      'TestCases': '#10B981',
      'Auth': '#EF4444',
      'Metrics': '#F59E0B',
      'Projects': '#8B5CF6',
      'Chat': '#06B6D4',
      'History': '#F97316'
    };
    return colors[module] || '#64748B';
  }

  get confidencePercent(): string {
    return this.dashboard?.averageConfidenceScore
      ? (this.dashboard.averageConfidenceScore * 100).toFixed(1) + '%'
      : '0%';
  }

  get totalSuccess(): number {
    return this.dashboard?.activityByModule?.reduce(
      (acc: number, m: any) => acc + m.successCount, 0) || 0;
  }

  get totalFailures(): number {
    return this.dashboard?.activityByModule?.reduce(
      (acc: number, m: any) => acc + m.failureCount, 0) || 0;
  }

  get globalSuccessRate(): string {
    const total = this.totalSuccess + this.totalFailures;
    if (!total) return '0%';
    return Math.round((this.totalSuccess / total) * 100) + '%';
  }

  get maxModuleActions(): number {
    return Math.max(
      ...(this.dashboard?.activityByModule?.map((m: any) => m.totalActions) || [1]), 1
    );
  }
}