import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { TestplanService } from '../../core/services/testplan.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-testplan',
  templateUrl: './testplan.component.html',
  styleUrls: ['./testplan.component.scss'],
  standalone: false
})
export class TestplanComponent implements OnInit {
  form!: FormGroup;
  analyzing = false;
  saving = false;
  result: any = null;
  analysisId: string | null = null;
  reportSaved = false;
  isDragging = false;
  selectedFile: File | null = null;
  history: any[] = [];
  showHistory = false;

  constructor(
    private fb: FormBuilder,
    private testplanService: TestplanService,
    private http: HttpClient,
    private snackBar: MatSnackBar,
    private sanitizer: DomSanitizer,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      planContent: ['', [Validators.required, Validators.minLength(50)]],
      projectName: ['']
    });
    this.loadHistory();
  }

  onDragOver(event: DragEvent): void { event.preventDefault(); this.isDragging = true; }
  onDragLeave(): void { this.isDragging = false; }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    const file = event.dataTransfer?.files[0];
    if (file) this.processFile(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) this.processFile(input.files[0]);
  }

  processFile(file: File): void {
    this.selectedFile = file;
    const reader = new FileReader();
    reader.onload = (e) => {
      this.form.patchValue({ planContent: e.target?.result as string });
      this.cdr.detectChanges();
    };
    reader.readAsText(file);
  }

  analyze(): void {
    if (this.form.invalid) return;
    this.analyzing = true;
    this.result = null;
    this.analysisId = null;
    this.reportSaved = false;

    this.testplanService.analyzePlanText(
      this.form.value.planContent,
      this.form.value.projectName
    ).subscribe({
      next: (res) => {
        this.result = res;
        this.analysisId = res.analysis_id;
        this.analyzing = false;
        this.loadHistory();
        this.cdr.detectChanges();
      },
      error: () => {
        this.analyzing = false;
        this.snackBar.open('Error al analizar el plan', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  generateAndSaveReport(): void {
    if (!this.result || !this.analysisId) return;
    this.saving = true;

    const html = this.buildReportHtml();

    // Guardar en servidor
    this.http.post(`${environment.apiUrl}/api/testplan/${this.analysisId}/save-report`,
      { htmlContent: html }
    ).subscribe({
      next: () => {
        this.reportSaved = true;
        this.saving = false;
        this.snackBar.open('Informe guardado correctamente', 'Cerrar', { duration: 3000 });
        // Descargar automáticamente
        this.downloadHtml(html);
        this.loadHistory();
        this.cdr.detectChanges();
      },
      error: () => {
        this.saving = false;
        // Si falla el guardado, igual descargamos
        this.downloadHtml(html);
        this.snackBar.open('Informe descargado (error al guardar en servidor)', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  downloadFromHistory(id: string, fileName: string): void {
    const link = document.createElement('a');
    link.href = `${environment.apiUrl}/api/testplan/${id}/download-report`;
    link.download = `Analisis_${fileName}.html`;
    // Necesita token - usar fetch con auth
    fetch(link.href, {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token') || sessionStorage.getItem('token') || ''}` }
    }).then(r => r.blob()).then(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Analisis_${fileName}.html`;
      a.click();
      URL.revokeObjectURL(url);
    }).catch(() => {
      this.snackBar.open('Error al descargar el informe', 'Cerrar', { duration: 3000 });
    });
  }

  private downloadHtml(html: string): void {
    const blob = new Blob([html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Analisis_${this.form.value.projectName || 'TestPlan'}_${new Date().toISOString().slice(0,10)}.html`;
    a.click();
    URL.revokeObjectURL(url);
  }

  loadHistory(): void {
    this.testplanService.getHistory().subscribe({
      next: (data) => {
        this.history = Array.isArray(data) ? data : [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  renderMarkdown(text: string): SafeHtml {
    if (!text) return this.sanitizer.bypassSecurityTrustHtml('');
    let html = text
      .replace(/^### (.+)$/gm, '<h3>$1</h3>')
      .replace(/^## (.+)$/gm, '<h2>$1</h2>')
      .replace(/^# (.+)$/gm, '<h1>$1</h1>')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/^- (.+)$/gm, '<li>$1</li>')
      .replace(/(`[^`]+`)/g, '<code>$1</code>')
      .replace(/\n\n/g, '</p><p>')
      .replace(/\n/g, '<br>');
    return this.sanitizer.bypassSecurityTrustHtml('<p>' + html + '</p>');
  }

  private buildReportHtml(): string {
    const time = this.parseTimeEstimation();
    const timeRows = Object.entries(time).map(([k, v]) =>
      `<tr><td>${k}</td><td><strong>${v}</strong></td></tr>`).join('');

    return `<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<title>Análisis de Plan de Pruebas - ${this.form.value.projectName || 'NexusQA'}</title>
<style>
  body { font-family: 'Segoe UI', sans-serif; margin: 0; background: #F8FAFC; color: #0A0F1E; }
  .header { background: linear-gradient(135deg, #0A0F1E, #1e3a5f); color: white; padding: 40px 48px; }
  .header h1 { margin: 0 0 8px; font-size: 2rem; }
  .header p { margin: 0; opacity: 0.7; }
  .badge { display: inline-block; padding: 6px 16px; border-radius: 20px; font-weight: 700; font-size: 0.9rem; margin-top: 16px; }
  .viable { background: #DCFCE7; color: #16a34a; }
  .not-viable { background: #FEE2E2; color: #dc2626; }
  .content { max-width: 900px; margin: 32px auto; padding: 0 24px; }
  .card { background: white; border-radius: 12px; border: 1px solid #E2E8F0; padding: 24px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
  .card h2 { font-size: 1.1rem; color: #0A0F1E; margin: 0 0 16px; padding-bottom: 12px; border-bottom: 2px solid #6366f1; }
  table { width: 100%; border-collapse: collapse; }
  th { background: #F1F5F9; padding: 10px 14px; text-align: left; font-size: 0.8rem; text-transform: uppercase; color: #64748B; }
  td { padding: 10px 14px; border-bottom: 1px solid #F1F5F9; }
  .confidence { font-size: 2rem; font-weight: 700; color: #6366f1; }
  .analysis-text { line-height: 1.8; white-space: pre-wrap; font-size: 0.9rem; }
  .footer { text-align: center; padding: 24px; color: #94A3B8; font-size: 0.8rem; }
  @media print { body { background: white; } }
</style>
</head>
<body>
<div class="header">
  <p>NexusQA — Análisis de Plan de Pruebas</p>
  <h1>${this.form.value.projectName || 'Plan de Pruebas'}</h1>
  <p>Generado el ${new Date().toLocaleDateString('es-CO', { day: '2-digit', month: 'long', year: 'numeric' })}</p>
  <span class="badge ${this.result.is_viable ? 'viable' : 'not-viable'}">
    ${this.result.is_viable ? '✓ Plan VIABLE' : '✗ Plan NO VIABLE'}
  </span>
</div>
<div class="content">
  <div class="card">
    <h2>Confianza del Análisis</h2>
    <div class="confidence">${this.getConfidencePercent()}%</div>
    <p style="color:#64748B;margin-top:4px">Nivel de confianza de la evaluación IA</p>
  </div>
  <div class="card">
    <h2>Estimación de Tiempos</h2>
    <table>
      <thead><tr><th>Fase</th><th>Estimación</th></tr></thead>
      <tbody>${timeRows}</tbody>
    </table>
  </div>
  <div class="card">
    <h2>Cumplimiento ISTQB</h2>
    <p>${this.result.istqb_compliance_notes || 'Ver análisis completo.'}</p>
  </div>
  <div class="card">
    <h2>Cumplimiento ISO 29119</h2>
    <p>${this.result.iso29119_compliance_notes || 'Ver análisis completo.'}</p>
  </div>
  <div class="card">
    <h2>Análisis Completo</h2>
    <div class="analysis-text">${this.result.ai_analysis_result || ''}</div>
  </div>
</div>
<div class="footer">
  Generado por NexusQA · ${this.result.model_used || 'AI'} · ${new Date().toISOString()}
</div>
</body>
</html>`;
  }

  getViabilityColor(): string {
    if (!this.result) return '#64748B';
    return this.result.is_viable ? '#10B981' : '#EF4444';
  }

  getViabilityLabel(): string {
    if (!this.result) return '';
    return this.result.is_viable ? 'Plan VIABLE' : 'Plan NO VIABLE';
  }

  getConfidencePercent(): number {
    return Math.round((this.result?.confidence_score || 0) * 100);
  }

  parseTimeEstimation(): any {
    try { return JSON.parse(this.result?.estimated_time_json || '{}'); }
    catch { return {}; }
  }

  formatDate(date: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
