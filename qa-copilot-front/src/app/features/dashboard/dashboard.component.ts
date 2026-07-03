import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { MetricsService } from '../../core/services/metrics.service';
import { SignalRService } from '../../core/services/signalr.service';
import { ProjectService } from '../../core/services/project.service';
import { DocumentService } from '../../core/services/document.service';
import { TestcaseService } from '../../core/services/testcase.service';
import { DashboardMetrics } from '../../core/models/metrics.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy {
  metrics: DashboardMetrics | null = null;
  recentActivities: any[] = [];
  myProjects: any[] = [];
  myDocuments: any[] = [];
  myHistory: any[] = [];
  loading = true;
  userRole = '';
  userName = '';
  greeting = '';

  // Indicadores QA del Panel Senior
  myIndicators: any = null;
  indicatorsEnabled = false;
  loadingIndicators = false;
  teamSummary: any[] = [];

  // Filtro por mes en widget QA
  indicatorYear: number = new Date().getFullYear();
  indicatorMonth: number = new Date().getMonth() + 1;
  indicatorFilterAll = true;
  years: number[] = Array.from({length: 4}, (_, i) => new Date().getFullYear() - i);
  months = [
    {value:1,label:'Enero'},{value:2,label:'Febrero'},{value:3,label:'Marzo'},
    {value:4,label:'Abril'},{value:5,label:'Mayo'},{value:6,label:'Junio'},
    {value:7,label:'Julio'},{value:8,label:'Agosto'},{value:9,label:'Septiembre'},
    {value:10,label:'Octubre'},{value:11,label:'Noviembre'},{value:12,label:'Diciembre'}
  ];

  private subs = new Subscription();

  constructor(
    private authService: AuthService,
    private metricsService: MetricsService,
    private signalRService: SignalRService,
    private projectService: ProjectService,
    private documentService: DocumentService,
    private testcaseService: TestcaseService,
    private http: HttpClient,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.userRole = user?.role || '';
    this.userName = user?.userName || '';
    this.greeting = this.getGreeting();

    this.loadDataByRole();

    this.subs.add(
      this.signalRService.teamActivity$.subscribe(activity => {
        if (activity) {
          this.recentActivities.unshift(activity);
          if (this.recentActivities.length > 10) this.recentActivities.pop();
          this.cdr.detectChanges();
        }
      })
    );
  }

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  loadDataByRole(): void {
    this.loading = true;

    if (this.userRole === 'Admin') {
      this.metricsService.getDashboard().subscribe({
        next: (data) => { this.metrics = data; this.loading = false; this.cdr.detectChanges(); },
        error: () => { this.loading = false; this.cdr.detectChanges(); }
      });
      this.loadTeamSummary();

    } else if (this.userRole === 'Senior') {
      this.metricsService.getDashboard().subscribe({
        next: (data) => { this.metrics = data; this.loading = false; this.cdr.detectChanges(); },
        error: () => { this.loading = false; this.cdr.detectChanges(); }
      });
      this.projectService.getAll().subscribe({
        next: (projects) => { this.myProjects = projects; this.cdr.detectChanges(); },
        error: () => {}
      });
      this.loadTeamSummary();

    } else if (this.userRole === 'QAEngineer') {
      this.projectService.getMyProjects().subscribe({
        next: (projects) => { this.myProjects = projects; this.cdr.detectChanges(); },
        error: () => {}
      });
      this.documentService.getMyDocuments().subscribe({
        next: (docs) => { this.myDocuments = docs; this.cdr.detectChanges(); },
        error: () => {}
      });
      this.testcaseService.getHistory(1, 5).subscribe({
        next: (data) => { this.myHistory = data.items || []; this.cdr.detectChanges(); },
        error: () => {}
      });
      this.loading = false;
      this.loadMyIndicators();
      this.cdr.detectChanges();

    } else {
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  get indicatorPeriodLabel(): string {
    if (this.indicatorFilterAll) return 'Todo el tiempo';
    const m = this.months.find(x => x.value === this.indicatorMonth);
    return `${m?.label} ${this.indicatorYear}`;
  }

  onIndicatorFilterChange(): void {
    this.indicatorMonth = Number(this.indicatorMonth);
    this.indicatorYear = Number(this.indicatorYear);
    this.loadMyIndicators();
  }

  loadMyIndicators(): void {
    this.loadingIndicators = true;
    let params: any = {};
    if (!this.indicatorFilterAll) {
      params = { year: this.indicatorYear, month: this.indicatorMonth };
    }
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/my-indicators`,
      { headers: this.getHeaders(), params }
    ).subscribe({
      next: (res) => {
        const data = res.data;
        if (data && data.enabled === false) {
          this.indicatorsEnabled = false;
        } else if (data) {
          this.indicatorsEnabled = true;
          this.myIndicators = data;
        }
        this.loadingIndicators = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingIndicators = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadTeamSummary(): void {
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/team`,
      { headers: this.getHeaders() }
    ).subscribe({
      next: (res) => {
        this.teamSummary = (res.data || []).slice(0, 5);
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  getScoreClass(score: number): string {
    if (score >= 80) return 'excellent';
    if (score >= 60) return 'good';
    return 'needs-improvement';
  }

  getScoreLabel(score: number): string {
    if (score >= 80) return 'Excelente';
    if (score >= 60) return 'Bueno';
    if (score >= 40) return 'Regular';
    return 'Por mejorar';
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#22c55e';
    if (score >= 60) return '#f59e0b';
    return '#ef4444';
  }

  getGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Buenos dÃ­as';
    if (hour < 18) return 'Buenas tardes';
    return 'Buenas noches';
  }

  navigate(path: string): void {
    this.router.navigate([path]);
  }

  getStatusColor(status: string): string {
    const colors: Record<string, string> = {
      'Active': '#10B981', 'OnHold': '#F59E0B',
      'Completed': '#3B82F6', 'Cancelled': '#EF4444'
    };
    return colors[status] || '#64748B';
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      'Active': 'Activo', 'OnHold': 'En espera',
      'Completed': 'Completado', 'Cancelled': 'Cancelado'
    };
    return labels[status] || status;
  }

  formatDate(date: string): string {
    if (!date) return 'â€”';
    return new Date(date).toLocaleDateString('es-CO', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }

  get confidencePercent(): string {
    return this.metrics?.averageConfidenceScore
      ? (this.metrics.averageConfidenceScore * 100).toFixed(1) + '%'
      : '0%';
  }

  get adminKpis() {
    return [
      { title: 'Documentos', value: this.metrics?.totalDocuments ?? 0, icon: 'description', color: '#3B82F6', route: '/documents' },
      { title: 'Casos Generados', value: this.metrics?.totalTestCasesGenerated ?? 0, icon: 'auto_awesome', color: '#10B981', route: '/history' },
      { title: 'Confianza IA', value: this.confidencePercent, icon: 'verified', color: '#F59E0B', route: '/metrics' },
      { title: 'Usuarios Activos', value: this.metrics?.totalUsers ?? 0, icon: 'group', color: '#8B5CF6', route: '/metrics' }
    ];
  }

  get seniorKpis() {
    return [
      { title: 'Total Proyectos', value: this.myProjects.length, icon: 'work', color: '#3B82F6', route: '/projects' },
      { title: 'Proyectos Activos', value: this.myProjects.filter(p => p.status === 'Active').length, icon: 'play_circle', color: '#10B981', route: '/projects' },
      { title: 'Documentos Sistema', value: this.metrics?.totalDocuments ?? 0, icon: 'description', color: '#F59E0B', route: '/documents' },
      { title: 'Casos Generados', value: this.metrics?.totalTestCasesGenerated ?? 0, icon: 'auto_awesome', color: '#8B5CF6', route: '/history' }
    ];
  }

  get qaKpis() {
    return [
      { title: 'Mis Proyectos', value: this.myProjects.length, icon: 'work', color: '#3B82F6', route: '/projects/my' },
      { title: 'Mis Documentos', value: this.myDocuments.length, icon: 'description', color: '#10B981', route: '/documents' },
      { title: 'Casos Generados', value: this.myHistory.length, icon: 'auto_awesome', color: '#F59E0B', route: '/history' },
      { title: 'Proyectos Activos', value: this.myProjects.filter(p => p.status === 'Active').length, icon: 'play_circle', color: '#8B5CF6', route: '/projects/my' }
    ];
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }
}



