import { Component, OnInit, Input, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { PermissionsService } from '../../../core/services/permissions.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  moduleKey: string;
  badge?: number;
}

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
  standalone: false
})
export class SidebarComponent implements OnInit {
  @Input() isCollapsed = false;
  @Output() toggleSidebar = new EventEmitter<void>();

  currentRoute = '';
  userRole = '';
  allowedModules: string[] = [];

  navItems: NavItem[] = [
    { label: 'Dashboard',        icon: 'dashboard',           route: '/dashboard',    moduleKey: 'dashboard' },
    { label: 'Mis Proyectos',    icon: 'folder_open',         route: '/projects/my',  moduleKey: 'my-projects' },
    { label: 'Documentos',       icon: 'description',         route: '/documents',    moduleKey: 'documents' },
    { label: 'Generar Casos',    icon: 'auto_awesome',        route: '/testcases',    moduleKey: 'testcases' },
    { label: 'Plan de Pruebas',  icon: 'assignment',          route: '/testplan',     moduleKey: 'testplan' },
    { label: 'Historial',        icon: 'history',             route: '/history',      moduleKey: 'history' },
    { label: 'Chat QA',          icon: 'smart_toy',           route: '/chat',         moduleKey: 'chat' },
    { label: 'Proyectos',        icon: 'work',                route: '/projects',     moduleKey: 'projects' },
    { label: 'Informes',         icon: 'summarize',           route: '/reports',      moduleKey: 'reports' },
    { label: 'Actividad QA',     icon: 'insights',            route: '/analytics',    moduleKey: 'analytics' },
    { label: 'Panel Senior',     icon: 'dashboard_customize', route: '/senior-panel', moduleKey: 'senior-panel' },
    { label: 'Entrenamiento IA', icon: 'model_training',      route: '/training',     moduleKey: 'training' },
    { label: 'Métricas',         icon: 'bar_chart',           route: '/metrics',      moduleKey: 'metrics' },
  ];

  constructor(
    private router: Router,
    private authService: AuthService,
    private permissionsService: PermissionsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole = this.authService.getUserRole();

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.currentRoute = event.urlAfterRedirects;
      this.cdr.detectChanges();
    });
    this.currentRoute = this.router.url;

    // Cargar módulos permitidos — con caché para carga instantánea
    this.permissionsService.getMyModules().subscribe({
      next: (modules) => {
        this.allowedModules = modules;
        this.cdr.detectChanges();
      },
      error: () => {
        this.allowedModules = ['dashboard'];
        this.cdr.detectChanges();
      }
    });
  }

  get filteredNavItems(): NavItem[] {
    if (this.allowedModules.length === 0) return [];
    return this.navItems.filter(item => this.allowedModules.includes(item.moduleKey));
  }

  isActive(route: string): boolean {
    return this.currentRoute.startsWith(route);
  }

  navigate(route: string): void {
    this.router.navigate([route]);
  }
}
