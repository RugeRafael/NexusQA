import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { ProjectService } from '../../core/services/project.service';
import { SignalRService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { Project } from '../../core/models/project.model';
import { AssignDialogComponent } from './assign-dialog/assign-dialog.component';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.scss'],
  standalone: false
})
export class ProjectsComponent implements OnInit {
  projects: Project[] = [];
  myProjects: Project[] = [];
  loading = false;
  showCreateForm = false;
  createForm!: FormGroup;
  userRole = '';
  displayedColumns = ['name', 'status', 'startDate', 'totalAssignedQAs', 'createdByUserName', 'actions'];

  constructor(
    private projectService: ProjectService,
    private authService: AuthService,
    private signalRService: SignalRService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.userRole = this.authService.getUserRole();

    this.createForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['']
    });

    this.loadProjects();

    this.signalRService.projectAssigned$.subscribe(notification => {
      if (notification) {
        this.snackBar.open(`Proyecto asignado: ${notification.projectName}`, 'Ver', { duration: 5000 });
        this.loadProjects();
      }
    });
  }

  loadProjects(): void {
    this.loading = true;
    if (this.isAdminOrSenior) {
      this.projectService.getAll().subscribe({
        next: (projects) => {
          this.projects = projects;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => { this.loading = false; this.cdr.detectChanges(); }
      });
    } else {
      this.projectService.getMyProjects().subscribe({
        next: (projects) => {
          this.myProjects = projects;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => { this.loading = false; this.cdr.detectChanges(); }
      });
    }
  }

  createProject(): void {
    if (this.createForm.invalid) return;

    const formValue = this.createForm.value;
    const request = {
      name: formValue.name,
      description: formValue.description,
      startDate: formValue.startDate,
      endDate: formValue.endDate || null
    };

    this.projectService.create(request).subscribe({
      next: (project) => {
        this.projects.unshift(project);
        this.showCreateForm = false;
        this.createForm.reset();
        this.snackBar.open('Proyecto creado exitosamente', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.snackBar.open(err.error?.message || 'Error al crear proyecto', 'Cerrar', { duration: 3000 });
      }
    });
  }

  openAssignDialog(project: Project): void {
    const dialogRef = this.dialog.open(AssignDialogComponent, {
      data: { project },
      width: '500px',
      disableClose: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadProjects();
      }
    });
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

  formatDate(date: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('es-CO', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }

  get isAdminOrSenior(): boolean {
    return ['Admin', 'Senior'].includes(this.userRole);
  }

  get displayProjects(): Project[] {
    return this.isAdminOrSenior ? this.projects : this.myProjects;
  }
}