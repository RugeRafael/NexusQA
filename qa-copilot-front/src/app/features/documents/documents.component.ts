import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DocumentService } from '../../core/services/document.service';
import { Document } from '../../core/models/document.model';

@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.scss'],
  standalone: false
})
export class DocumentsComponent implements OnInit {
  documents: Document[] = [];
  loading = false;
  uploading = false;
  isDragging = false;
  selectedFile: File | null = null;
  displayedColumns = ['fileName', 'contentType', 'fileSizeBytes', 'status', 'uploadedAt', 'actions'];

  constructor(
    private documentService: DocumentService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
    this.documentService.getMyDocuments().subscribe({
      next: (docs) => {
        this.documents = docs;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
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
    const allowed = ['application/pdf',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'];

    if (!allowed.includes(file.type)) {
      this.snackBar.open('Solo se permiten archivos PDF, DOCX o XLSX', 'Cerrar', { duration: 3000 });
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.snackBar.open('El archivo no puede superar 10MB', 'Cerrar', { duration: 3000 });
      return;
    }

    this.uploading = true;
    this.documentService.upload(file).subscribe({
      next: (doc) => {
        this.documents.unshift(doc);
        this.uploading = false;
        this.snackBar.open('Documento subido exitosamente', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.uploading = false;
        this.snackBar.open(err.error?.message || 'Error al subir el documento', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString('es-CO', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  getStatusColor(status: string): string {
    const colors: Record<string, string> = {
      'Uploaded': '#3B82F6',
      'Processing': '#F59E0B',
      'Processed': '#10B981',
      'Failed': '#EF4444'
    };
    return colors[status] || '#64748B';
  }
}