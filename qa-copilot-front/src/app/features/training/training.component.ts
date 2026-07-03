import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
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
export class TrainingComponent implements OnInit {
  form!: FormGroup;
  documents: any[] = [];
  uploading = false;
  loading = false;
  isDragging = false;
  displayedColumns = ['fileName', 'category', 'status', 'uploadedAt', 'actions'];

  categories = [
    { value: 'standards', label: 'Estándares (ISTQB, ISO 29119)' },
    { value: 'company', label: 'Documentación empresa' },
    { value: 'processes', label: 'Procesos internos' },
    { value: 'templates', label: 'Plantillas QA' },
    { value: 'other', label: 'Otro' }
  ];

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      category: ['company', Validators.required],
      description: ['']
    });
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/api/training`).pipe(
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
    if (this.form.value.description) {
      formData.append('description', this.form.value.description);
    }

    this.http.post<any>(`${environment.apiUrl}/api/training/upload`, formData).subscribe({
      next: (res) => {
        const doc = res.data || res;
        this.documents.unshift(doc);
        this.uploading = false;
        this.snackBar.open('Documento de entrenamiento subido', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      },
      error: () => {
        this.uploading = false;
        this.snackBar.open('Error al subir el documento', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  getCategoryLabel(value: string): string {
    return this.categories.find(c => c.value === value)?.label || value;
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