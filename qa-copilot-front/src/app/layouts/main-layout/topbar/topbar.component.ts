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
    this.themeService.isDark$.subscribe(dark => { this.isDarkTheme = dark; });

    this.signalRService.notification$.subscribe(notification => {
      if (notification) { this.notifications.unshift(notification); this.unreadCount++; }
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

  toggleTheme(): void { this.themeService.toggle(); }

  logout(): void {
    this.signalRService.stopConnection();
    this.authService.logout();
  }

  clearNotifications(): void { this.notifications = []; this.unreadCount = 0; }

  navigateToProfile(): void { this.router.navigate(['/profile']); }

  // Muestra el rol tal como viene del servidor — sin hardcodear labels
  getRoleLabel(): string {
    return this.userRole || 'Usuario';
  }

  // Color basado en posición del rol — sin hardcodear roles específicos
  getRoleColor(): string {
    const colors = ['#ef4444', '#f59e0b', '#3b82f6', '#8b5cf6', '#10b981', '#6366f1'];
    if (!this.userRole) return '#6366f1';
    // Hash simple del nombre del rol para color consistente
    let hash = 0;
    for (let i = 0; i < this.userRole.length; i++) {
      hash = this.userRole.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  }
}
