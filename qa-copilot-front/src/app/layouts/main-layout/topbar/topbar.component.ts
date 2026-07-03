import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
  standalone: false
})
export class TopbarComponent implements OnInit {
  @Input() sidebarCollapsed = false;
  @Output() toggleSidebar = new EventEmitter<void>();

  userName = '';
  userRole = '';
  notifications: any[] = [];
  unreadCount = 0;
  isDarkTheme = false;

  constructor(
    private authService: AuthService,
    private signalRService: SignalRService,
    private themeService: ThemeService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.userName = user?.userName || '';
    this.userRole = user?.role || '';
    this.isDarkTheme = this.themeService.currentTheme;

    this.themeService.isDark$.subscribe(dark => {
      this.isDarkTheme = dark;
    });

    this.signalRService.notification$.subscribe(notification => {
      if (notification) {
        this.notifications.unshift(notification);
        this.unreadCount++;
      }
    });

    this.signalRService.projectAssigned$.subscribe(notification => {
      if (notification) {
        this.notifications.unshift({
          title: 'Proyecto asignado',
          message: `Se te asignó: ${notification.projectName}`,
          createdAt: new Date()
        });
        this.unreadCount++;
      }
    });
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  logout(): void {
    this.signalRService.stopConnection();
    this.authService.logout();
  }

  clearNotifications(): void {
    this.notifications = [];
    this.unreadCount = 0;
  }

  navigateToProfile(): void {
    this.router.navigate(['/profile']);
  }

  getRoleLabel(): string {
    const labels: Record<string, string> = {
      'Admin': 'Administrador',
      'Senior': 'Senior QA',
      'QAEngineer': 'QA Engineer',
      'Viewer': 'Auditor'
    };
    return labels[this.userRole] || this.userRole;
  }

  getRoleColor(): string {
    const colors: Record<string, string> = {
      'Admin': '#ef4444',
      'Senior': '#f59e0b',
      'QAEngineer': '#3b82f6',
      'Viewer': '#64748b'
    };
    return colors[this.userRole] || '#64748b';
  }
}