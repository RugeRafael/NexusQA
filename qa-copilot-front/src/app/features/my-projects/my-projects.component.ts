import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProjectService } from '../../core/services/project.service';
import { AuthService } from '../../core/services/auth.service';
import { SignalRService } from '../../core/services/signalr.service';
import { Project } from '../../core/models/project.model';

@Component({
  selector: 'app-my-projects',
  templateUrl: './my-projects.component.html',
  styleUrls: ['./my-projects.component.scss'],
  standalone: false
})
export class MyProjectsComponent implements OnInit {
  projects: Project[] = [];
  loading = false;
  userName = '';
  selectedProject: Project | null = null;

  constructor(
    private projectService: ProjectService,
    private authService: AuthService,
    private signalRService: SignalRService,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userName = this.authService.getCurrentUser()?.userName || '';
    this.loadMyProjects();

    this.signalRService.projectAssigned$.subscribe(notification => {
      if (notification) {
        this.snackBar.open(
          `Nuevo proyecto asignado: ${notification.projectName}`,
          'Ver', { duration: 5000 }
        );
        this.loadMyProjects();
      }
    });
  }

  loadMyProjects(): void {
    this.loading = true;
    this.projectService.getMyProjects().subscribe({
      next: (projects) => {
  console.log('Proyectos recibidos:', projects);
  console.log('Status del primero:', projects[0]?.status);
  this.projects = projects;
  this.loading = false;
  this.cdr.detectChanges();
},
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectProject(project: Project): void {
    this.selectedProject = this.selectedProject?.id === project.id ? null : project;
  }

  navigateToDocuments(project: Project): void {
    this.router.navigate(['/documents']);
  }

  navigateToTestCases(project: Project): void {
    this.router.navigate(['/testcases']);
  }

  getStatusColor(status: string): string {
    const colors: Record<string, string> = {
      'Active': '#10B981',
      'OnHold': '#F59E0B',
      'Completed': '#3B82F6',
      'Cancelled': '#EF4444'
    };
    return colors[status] || '#64748B';
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      'Active': 'Activo',
      'OnHold': 'En espera',
      'Completed': 'Completado',
      'Cancelled': 'Cancelado'
    };
    return labels[status] || status;
  }

  getStatusIcon(status: string): string {
    const icons: Record<string, string> = {
      'Active': 'play_circle',
      'OnHold': 'pause_circle',
      'Completed': 'check_circle',
      'Cancelled': 'cancel'
    };
    return icons[status] || 'circle';
  }

  formatDate(date: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('es-CO', {
      day: '2-digit', month: 'long', year: 'numeric'
    });
  }

  getDaysRemaining(endDate: string): string {
    if (!endDate) return 'Sin fecha límite';
    const end = new Date(endDate);
    const now = new Date();
    const diff = Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    if (diff < 0) return 'Vencido';
    if (diff === 0) return 'Vence hoy';
    if (diff === 1) return '1 día restante';
    return `${diff} días restantes`;
  }

  getDaysColor(endDate: string): string {
    if (!endDate) return '#64748B';
    const end = new Date(endDate);
    const now = new Date();
    const diff = Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    if (diff < 0) return '#EF4444';
    if (diff <= 7) return '#F59E0B';
    return '#10B981';
  }

  get activeProjects(): number {
    return this.projects.filter(p => p.status === 'Active').length;
  }

  get completedProjects(): number {
    return this.projects.filter(p => p.status === 'Completed').length;
  }
}