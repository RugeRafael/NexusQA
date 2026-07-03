import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-senior-panel',
  templateUrl: './senior-panel.component.html',
  styleUrls: ['./senior-panel.component.scss'],
  standalone: false
})
export class SeniorPanelComponent implements OnInit {
  teamData: any[] = [];
  configs: any[] = [];
  loading = false;
  selectedUser: any = null;
  viewMode: 'team' | 'detail' = 'team';

  filterAll = true;
  selectedYear: number = new Date().getFullYear();
  selectedMonth: number = new Date().getMonth() + 1;

  years: number[] = Array.from({length: 4}, (_, i) => new Date().getFullYear() - i);
  months = [
    {value:1,label:'Enero'},{value:2,label:'Febrero'},{value:3,label:'Marzo'},
    {value:4,label:'Abril'},{value:5,label:'Mayo'},{value:6,label:'Junio'},
    {value:7,label:'Julio'},{value:8,label:'Agosto'},{value:9,label:'Septiembre'},
    {value:10,label:'Octubre'},{value:11,label:'Noviembre'},{value:12,label:'Diciembre'}
  ];

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTeam();
    this.loadConfigs();
  }

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  private getDateParams(): HttpParams {
    let params = new HttpParams();
    if (!this.filterAll) {
      params = params.set('year', String(this.selectedYear));
      params = params.set('month', String(this.selectedMonth));
    }
    return params;
  }

  get currentPeriodLabel(): string {
    if (this.filterAll) return 'Todo el tiempo';
    const m = this.months.find(x => x.value === Number(this.selectedMonth));
    return m ? `${m.label} ${this.selectedYear}` : 'Todo el tiempo';
  }

  onFilterChange(): void {
    this.selectedMonth = Number(this.selectedMonth);
    this.selectedYear = Number(this.selectedYear);
    this.loadTeam();
    if (this.viewMode === 'detail' && this.selectedUser) {
      this.loadUserDetail(this.selectedUser.userId);
    }
  }

  loadTeam(): void {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/team`,
      { headers: this.getHeaders(), params: this.getDateParams() }
    ).subscribe({
      next: (res) => {
        this.teamData = res.data || [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.snackBar.open('Error cargando datos del equipo', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  loadConfigs(): void {
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/config`,
      { headers: this.getHeaders() }
    ).subscribe({
      next: (res) => {
        this.configs = res.data || [];
        this.cdr.detectChanges();
      }
    });
  }

  loadUserDetail(userId: string): void {
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/user/${userId}`,
      { headers: this.getHeaders(), params: this.getDateParams() }
    ).subscribe({
      next: (res) => {
        this.selectedUser = res.data;
        this.cdr.detectChanges();
      }
    });
  }

  getConfig(userId: string): any {
    return this.configs.find(c => c.userId === userId) || { indicatorsEnabled: false, metaDocumentos: 3 };
  }

  toggleIndicators(user: any): void {
    const config = this.getConfig(user.userId);
    const newValue = !config.indicatorsEnabled;
    this.http.post<any>(`${environment.apiUrl}/api/senior-panel/config`,
      { userId: user.userId, indicatorsEnabled: newValue },
      { headers: this.getHeaders() }
    ).subscribe({
      next: () => {
        this.snackBar.open(
          `Indicadores ${newValue ? 'activados' : 'desactivados'} para ${user.userName}`,
          'Cerrar', { duration: 3000 }
        );
        this.loadConfigs();
        this.cdr.detectChanges();
      },
      error: () => {
        this.snackBar.open('Error actualizando configuracion', 'Cerrar', { duration: 3000 });
      }
    });
  }

  updateMetaDocs(user: any, meta: number): void {
    const config = this.getConfig(user.userId);
    this.http.post<any>(`${environment.apiUrl}/api/senior-panel/config`,
      { userId: user.userId, indicatorsEnabled: config.indicatorsEnabled, metaDocumentos: meta },
      { headers: this.getHeaders() }
    ).subscribe({
      next: () => {
        this.snackBar.open('Meta actualizada', 'Cerrar', { duration: 2000 });
        this.loadConfigs();
      }
    });
  }

  viewDetail(user: any): void {
    this.selectedUser = user;
    this.viewMode = 'detail';
    this.loadUserDetail(user.userId);
    this.cdr.detectChanges();
  }

  backToTeam(): void {
    this.viewMode = 'team';
    this.selectedUser = null;
    this.cdr.detectChanges();
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#22c55e';
    if (score >= 60) return '#f59e0b';
    return '#ef4444';
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

  getRoleIcon(role: string): string {
    if (role === 'Admin' || role === 'Senior') return 'manage_accounts';
    return 'engineering';
  }
}
