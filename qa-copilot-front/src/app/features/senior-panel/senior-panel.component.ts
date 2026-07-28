import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { UsersService } from '../../core/services/users.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-senior-panel',
  templateUrl: './senior-panel.component.html',
  styleUrls: ['./senior-panel.component.scss'],
  standalone: false
})
export class SeniorPanelComponent implements OnInit {
  // Panel equipo
  teamData: any[] = [];
  configs: any[] = [];
  loading = false;
  selectedUser: any = null;
  viewMode: 'team' | 'detail' = 'team';
  activeTab: 'equipo' | 'usuarios' | 'permisos' = 'equipo';

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

  // Gestión de usuarios
  users: any[] = [];
  loadingUsers = false;
  showCreateForm = false;
  showResetPassword = false;
  selectedUserId = '';
  newPassword = '';
  roles = ['Administrador', 'IngenieroSenior', 'IngenieroQA', 'Scrum'];
  newUser = { fullName: '', email: '', password: '', role: 'IngenieroQA' };

  // Gestión de permisos
  allPermissions: any[] = [];
  loadingPermissions = false;
  savingPermissions: Record<string, boolean> = {};
  moduleLabels: Record<string, string> = {
    'dashboard': 'Dashboard',
    'my-projects': 'Mis Proyectos',
    'documents': 'Documentos',
    'testcases': 'Generar Casos',
    'testplan': 'Plan de Pruebas',
    'history': 'Historial',
    'chat': 'Chat QA',
    'projects': 'Proyectos',
    'reports': 'Informes',
    'analytics': 'Actividad QA',
    'senior-panel': 'Panel Senior',
    'training': 'Entrenamiento IA',
    'metrics': 'Metricas'
  };

  isAdmin = false;
  isSeniorOrAdmin = false;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private usersService: UsersService,
    private permissionsService: PermissionsService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTeam();
    this.loadConfigs();
    const user = this.authService.getCurrentUser() as any;
    this.isAdmin = user?.dashboardType === 'admin';
    this.isSeniorOrAdmin = user?.dashboardType === 'admin' || user?.dashboardType === 'senior';
    if (this.isSeniorOrAdmin) {
      this.loadUsers();
      this.loadPermissions();
    }
  }

  // ── GESTIÓN DE USUARIOS ────────────────────────────────────────

  loadUsers(): void {
    this.loadingUsers = true;
    this.usersService.getAll().subscribe({
      next: (users) => { this.users = users; this.loadingUsers = false; this.cdr.detectChanges(); },
      error: () => { this.loadingUsers = false; this.snackBar.open('Error cargando usuarios', 'Cerrar', { duration: 3000 }); this.cdr.detectChanges(); }
    });
  }

  createUser(): void {
    if (!this.newUser.fullName || !this.newUser.email || !this.newUser.password) {
      this.snackBar.open('Completa todos los campos', 'Cerrar', { duration: 3000 }); return;
    }
    this.usersService.create(this.newUser).subscribe({
      next: () => {
        this.snackBar.open('Usuario creado exitosamente', 'Cerrar', { duration: 3000 });
        this.showCreateForm = false;
        this.newUser = { fullName: '', email: '', password: '', role: 'IngenieroQA' };
        this.loadUsers(); this.cdr.detectChanges();
      },
      error: (err) => { this.snackBar.open(err?.error?.message || 'Error creando usuario', 'Cerrar', { duration: 4000 }); this.cdr.detectChanges(); }
    });
  }

  changeRole(user: any, role: string): void {
    this.usersService.updateRole(user.id, role).subscribe({
      next: () => { this.snackBar.open(`Rol actualizado a ${role}`, 'Cerrar', { duration: 3000 }); this.loadUsers(); this.cdr.detectChanges(); },
      error: () => { this.snackBar.open('Error actualizando rol', 'Cerrar', { duration: 3000 }); this.cdr.detectChanges(); }
    });
  }

  toggleUserActive(user: any): void {
    this.usersService.toggleActive(user.id).subscribe({
      next: () => { this.snackBar.open(user.isActive ? 'Usuario desactivado' : 'Usuario activado', 'Cerrar', { duration: 3000 }); this.loadUsers(); this.cdr.detectChanges(); },
      error: () => { this.snackBar.open('Error cambiando estado', 'Cerrar', { duration: 3000 }); this.cdr.detectChanges(); }
    });
  }

  openResetPassword(userId: string): void {
    this.selectedUserId = userId; this.newPassword = ''; this.showResetPassword = true; this.cdr.detectChanges();
  }

  confirmResetPassword(): void {
    if (!this.newPassword || this.newPassword.length < 6) { this.snackBar.open('Minimo 6 caracteres', 'Cerrar', { duration: 3000 }); return; }
    this.usersService.resetPassword(this.selectedUserId, this.newPassword).subscribe({
      next: () => { this.snackBar.open('Contrasena restablecida', 'Cerrar', { duration: 3000 }); this.showResetPassword = false; this.cdr.detectChanges(); },
      error: () => { this.snackBar.open('Error restableciendo contrasena', 'Cerrar', { duration: 3000 }); this.cdr.detectChanges(); }
    });
  }

  getRoleBadgeClass(role: string): string {
    if (role === 'Admin' || role === 'Administrador') return 'role-admin';
    if (role === 'Senior' || role === 'IngenieroSenior') return 'role-senior';
    return 'role-junior';
  }

  // ── GESTIÓN DE PERMISOS ────────────────────────────────────────

  loadPermissions(): void {
    this.loadingPermissions = true;
    this.permissionsService.getAllPermissions().subscribe({
      next: (perms) => { this.allPermissions = perms; this.loadingPermissions = false; this.cdr.detectChanges(); },
      error: () => { this.loadingPermissions = false; this.snackBar.open('Error cargando permisos', 'Cerrar', { duration: 3000 }); this.cdr.detectChanges(); }
    });
  }

  togglePermission(roleData: any, moduleKey: string): void {
    const module = roleData.modules.find((m: any) => m.moduleKey === moduleKey);
    if (module) module.isEnabled = !module.isEnabled;
    this.cdr.detectChanges();
  }

  isModuleEnabled(roleData: any, moduleKey: string): boolean {
    return roleData.modules?.find((m: any) => m.moduleKey === moduleKey)?.isEnabled ?? false;
  }

  savePermissions(roleData: any): void {
    this.savingPermissions[roleData.role] = true;
    this.permissionsService.updateRolePermissions(roleData.role, roleData.modules).subscribe({
      next: () => {
        this.savingPermissions[roleData.role] = false;
        this.snackBar.open(`Permisos de ${roleData.role} guardados`, 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      },
      error: () => {
        this.savingPermissions[roleData.role] = false;
        this.snackBar.open('Error guardando permisos', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  get moduleKeys(): string[] {
    return Object.keys(this.moduleLabels);
  }

  // ── PANEL EQUIPO ───────────────────────────────────────────────

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
    if (this.viewMode === 'detail' && this.selectedUser) this.loadUserDetail(this.selectedUser.userId);
  }

  loadTeam(): void {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/team`, { headers: this.getHeaders(), params: this.getDateParams() }).subscribe({
      next: (res) => { this.teamData = res.data || []; this.loading = false; this.cdr.detectChanges(); },
      error: () => { this.loading = false; this.snackBar.open('Error cargando datos del equipo', 'Cerrar', { duration: 3000 }); this.cdr.detectChanges(); }
    });
  }

  loadConfigs(): void {
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/config`, { headers: this.getHeaders() }).subscribe({
      next: (res) => { this.configs = res.data || []; this.cdr.detectChanges(); }
    });
  }

  loadUserDetail(userId: string): void {
    this.http.get<any>(`${environment.apiUrl}/api/senior-panel/user/${userId}`, { headers: this.getHeaders(), params: this.getDateParams() }).subscribe({
      next: (res) => { this.selectedUser = res.data; this.cdr.detectChanges(); }
    });
  }

  getConfig(userId: string): any {
    return this.configs.find(c => c.userId === userId) || { indicatorsEnabled: false, metaDocumentos: 3 };
  }

  toggleIndicators(user: any): void {
    const config = this.getConfig(user.userId);
    const newValue = !config.indicatorsEnabled;
    this.http.post<any>(`${environment.apiUrl}/api/senior-panel/config`, { userId: user.userId, indicatorsEnabled: newValue }, { headers: this.getHeaders() }).subscribe({
      next: () => { this.snackBar.open(`Indicadores ${newValue ? 'activados' : 'desactivados'} para ${user.userName}`, 'Cerrar', { duration: 3000 }); this.loadConfigs(); this.cdr.detectChanges(); },
      error: () => { this.snackBar.open('Error actualizando configuracion', 'Cerrar', { duration: 3000 }); }
    });
  }

  updateMetaDocs(user: any, meta: number): void {
    const config = this.getConfig(user.userId);
    this.http.post<any>(`${environment.apiUrl}/api/senior-panel/config`, { userId: user.userId, indicatorsEnabled: config.indicatorsEnabled, metaDocumentos: meta }, { headers: this.getHeaders() }).subscribe({
      next: () => { this.snackBar.open('Meta actualizada', 'Cerrar', { duration: 2000 }); this.loadConfigs(); }
    });
  }

  viewDetail(user: any): void { this.selectedUser = user; this.viewMode = 'detail'; this.loadUserDetail(user.userId); this.cdr.detectChanges(); }
  backToTeam(): void { this.viewMode = 'team'; this.selectedUser = null; this.cdr.detectChanges(); }
  getScoreColor(score: number): string { return score >= 80 ? '#22c55e' : score >= 60 ? '#f59e0b' : '#ef4444'; }
  getScoreClass(score: number): string { return score >= 80 ? 'excellent' : score >= 60 ? 'good' : 'needs-improvement'; }
  getScoreLabel(score: number): string { return score >= 80 ? 'Excelente' : score >= 60 ? 'Bueno' : score >= 40 ? 'Regular' : 'Por mejorar'; }
  getRoleIcon(role: string): string { return role === 'Admin' || role === 'Senior' || role === 'Administrador' || role === 'IngenieroSenior' ? 'manage_accounts' : 'engineering'; }
}
