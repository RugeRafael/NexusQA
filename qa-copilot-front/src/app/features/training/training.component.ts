import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { map } from 'rxjs';

@Component({
  selector: 'app-training',
  templateUrl: './training.component.html',
  styleUrls: ['./training.component.scss'],
  standalone: false
})
export class TrainingComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  documents: any[] = [];
  projects: any[] = [];
  uploading = false;
  loading = false;
  isDragging = false;
  selectedProjectFilter: string = 'all';
  displayedColumns = ['fileName', 'project', 'category', 'status', 'uploadedAt', 'actions'];

  categories = [
    { value: 'standards', label: 'Estándares (ISTQB, ISO 29119)' },
    { value: 'company', label: 'Documentación empresa' },
    { value: 'processes', label: 'Procesos internos' },
    { value: 'templates', label: 'Plantillas QA' },
    { value: 'other', label: 'Otro' }
  ];

  private pollingInterval: any = null;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      category: ['company', Validators.required],
      description: [''],
      projectId: [null]
    });
    this.loadProjects();
    this.loadDocuments();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadProjects(): void {
    this.http.get<any>(environment.apiUrl + '/api/projects').pipe(
      map(r => r.data || r)
    ).subscribe({
      next: (projects) => {
        this.projects = Array.isArray(projects) ? projects : [];
      },
      error: () => { this.projects = []; }
    });
  }

  loadDocuments(): void {
    this.loading = true;
    let url = environment.apiUrl + '/api/training';
    if (this.selectedProjectFilter && this.selectedProjectFilter !== 'all') {
      url += '?projectId=' + this.selectedProjectFilter;
    }

    this.http.get<any>(url).pipe(
      map(r => r.data || r)
    ).subscribe({
      next: (docs) => {
        this.documents = Array.isArray(docs) ? docs : [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.documents = [];
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onProjectFilterChange(): void {
    this.loadDocuments();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(): void {
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    const file = event.dataTransfer?.files[0];
    if (file) this.uploadFile(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) this.uploadFile(input.files[0]);
  }

  uploadFile(file: File): void {
    if (file.size > 10 * 1024 * 1024) {
      this.snackBar.open('El archivo no puede superar 10MB', 'Cerrar', { duration: 3000 });
      return;
    }

    this.uploading = true;
    const formData = new FormData();
    formData.append('file', file);
    formData.append('category', this.form.value.category);
    if (this.form.value.description) formData.append('description', this.form.value.description);
    if (this.form.value.projectId) formData.append('projectId', this.form.value.projectId);

    this.http.post<any>(environment.apiUrl + '/api/training/upload', formData).subscribe({
      next: () => {
        this.uploading = false;
        this.snackBar.open('Documento subido — indexando en RAG...', 'Cerrar', { duration: 4000 });
        this.loadDocuments();
        this.startPolling();
      },
      error: () => {
        this.uploading = false;
        this.snackBar.open('Error al subir el documento', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  startPolling(): void {
    this.stopPolling();
    let attempts = 0;
    this.pollingInterval = setInterval(() => {
      attempts++;
      this.loadDocuments();
      const hasPending = this.documents.some((d: any) => d.status === 'Pending');
      if (!hasPending || attempts >= 10) {
        this.stopPolling();
        if (!hasPending) {
          this.snackBar.open('Documento indexado en RAG exitosamente', 'Cerrar', { duration: 3000 });
        }
      }
    }, 3000);
  }

  stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
    }
  }

  toggleStatus(doc: any): void {
    const newStatus = doc.status === 'Active' ? 'Pending' : 'Active';
    this.http.put<any>(environment.apiUrl + '/api/training/' + doc.id + '/status', { status: newStatus }).subscribe({
      next: () => {
        doc.status = newStatus;
        this.snackBar.open('Documento ' + (newStatus === 'Active' ? 'activado' : 'desactivado'), 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      },
      error: () => this.snackBar.open('Error al cambiar estado', 'Cerrar', { duration: 3000 })
    });
  }

  deleteDocument(id: string): void {
    this.http.delete<any>(environment.apiUrl + '/api/training/' + id).subscribe({
      next: () => {
        this.snackBar.open('Documento eliminado', 'Cerrar', { duration: 3000 });
        this.loadDocuments();
      },
      error: () => this.snackBar.open('Error al eliminar el documento', 'Cerrar', { duration: 3000 })
    });
  }

  getCategoryLabel(value: string): string {
    return this.categories.find(c => c.value === value)?.label || value;
  }

  getStatusColor(status: string): string {
    const colors: Record<string, string> = {
      'Pending': '#f59e0b', 'Processing': '#3b82f6',
      'Active': '#22c55e', 'Error': '#ef4444'
    };
    return colors[status] || '#64748b';
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      'Pending': 'Pendiente', 'Processing': 'Procesando',
      'Active': 'Activo', 'Error': 'Error'
    };
    return labels[status] || status;
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('es-CO', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }
}
