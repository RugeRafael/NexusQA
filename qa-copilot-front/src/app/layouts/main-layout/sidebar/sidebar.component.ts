import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  roles: string[];
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

  navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', route: '/dashboard', roles: ['Admin', 'Senior', 'QAEngineer', 'Viewer'] },
    { label: 'Mis Proyectos', icon: 'folder_open', route: '/projects/my', roles: ['QAEngineer', 'Senior', 'Admin'] },
    { label: 'Documentos', icon: 'description', route: '/documents', roles: ['QAEngineer', 'Senior', 'Admin'] },
    { label: 'Generar Casos', icon: 'auto_awesome', route: '/testcases', roles: ['QAEngineer', 'Senior', 'Admin'] },
    { label: 'Plan de Pruebas', icon: 'assignment', route: '/testplan', roles: ['QAEngineer', 'Senior', 'Admin'] },
    { label: 'Historial', icon: 'history', route: '/history', roles: ['QAEngineer', 'Senior', 'Admin'] },
    { label: 'Chat QA', icon: 'smart_toy', route: '/chat', roles: ['QAEngineer', 'Senior', 'Admin'] },
    { label: 'Proyectos', icon: 'work', route: '/projects', roles: ['Senior', 'Admin'] },
    { label: 'Informes', icon: 'summarize', route: '/reports', roles: ['Senior', 'Admin'] },
    { label: 'Actividad QA', icon: 'insights', route: '/analytics', roles: ['Senior', 'Admin'] },
    { label: 'Panel Senior', icon: 'dashboard_customize', route: '/senior-panel', roles: ['Senior', 'Admin'] },
    { label: 'Entrenamiento IA', icon: 'model_training', route: '/training', roles: ['Admin'] },
    { label: 'MÃ©tricas', icon: 'bar_chart', route: '/metrics', roles: ['Admin'] },
  ];

  constructor(private router: Router, private authService: AuthService) {}

  ngOnInit(): void {
    this.userRole = this.authService.getUserRole();
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.currentRoute = event.urlAfterRedirects;
    });
    this.currentRoute = this.router.url;
  }

  get filteredNavItems(): NavItem[] {
    return this.navItems.filter(item => item.roles.includes(this.userRole));
  }

  isActive(route: string): boolean {
    return this.currentRoute.startsWith(route);
  }

  navigate(route: string): void {
    this.router.navigate([route]);
  }
}
