import { Component, OnInit, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ReportService } from '../../core/services/report.service';
import { JiraService } from '../../core/services/jira.service';
import { DocumentService } from '../../core/services/document.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
  standalone: false
})
export class ReportsComponent implements OnInit, OnDestroy {
  selectedTab = 0;
  generating = false;
  uploadingToJira = false;
  uploadingToDocs = false;
  userName = '';

  comparisonForm!: FormGroup;
  completionForm!: FormGroup;
  innovationForm!: FormGroup;

  comparisonReport: any = null;
  completionReport: any = null;
  innovationReport: any = null;

  comparisonBlobUrl: SafeResourceUrl | null = null;
  completionBlobUrl: SafeResourceUrl | null = null;
  innovationBlobUrl: SafeResourceUrl | null = null;

  private blobUrls: string[] = [];

  comparisonFile: File | null = null;
  completionFile: File | null = null;
  innovationFile: File | null = null;

  jiraProjects: any[] = [];
  loadingProjects = false;
  selectedJiraProject = '';
  jiraBugs: any[] = [];
  loadingJiraBugs = false;
  issueUrl = '';
  loadingByUrl = false;

  acceptedFormats = '.html,.htm,.md,.xlsx,.xls,.docx,.txt';

  constructor(
    private fb: FormBuilder,
    private reportService: ReportService,
    private jiraService: JiraService,
    private documentService: DocumentService,
    private authService: AuthService,
    private sanitizer: DomSanitizer,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.userName = user?.userName || '';

    this.comparisonForm = this.fb.group({
      projectName: ['', Validators.required],
      qaEngineer: [this.userName, Validators.required],
      version: ['1.0', Validators.required],
      period: ['', Validators.required],
      requirements: [''],
      testCases: [''],
      additionalContext: [''],
      jiraUrl: ['']
    });

    this.completionForm = this.fb.group({
      projectName: ['', Validators.required],
      qaEngineer: [this.userName, Validators.required],
      version: ['1.0', Validators.required],
      period: ['', Validators.required],
      additionalContext: [''],
      jiraUrl: ['']
    });

    this.innovationForm = this.fb.group({
      projectName: ['', Validators.required],
      qaEngineer: [this.userName, Validators.required],
      version: ['1.0', Validators.required],
      period: ['', Validators.required],
      additionalContext: ['', Validators.minLength(10)],
      jiraUrl: ['']
    });
  }

  ngOnDestroy(): void {
    this.blobUrls.forEach(url => URL.revokeObjectURL(url));
  }

  private createBlobUrl(htmlContent: string): SafeResourceUrl {
    const blob = new Blob([htmlContent], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    this.blobUrls.push(url);
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  private getActiveJiraUrl(): string {
    if (this.selectedTab === 0) return this.comparisonForm.get('jiraUrl')?.value || '';
    if (this.selectedTab === 1) return this.completionForm.get('jiraUrl')?.value || '';
    return this.innovationForm.get('jiraUrl')?.value || '';
  }

  getProjectKeyFromForm(formName: 'comparison' | 'completion' | 'innovation'): string {
    let url = '';
    if (formName === 'comparison') url = this.comparisonForm.get('jiraUrl')?.value || '';
    else if (formName === 'completion') url = this.completionForm.get('jiraUrl')?.value || '';
    else url = this.innovationForm.get('jiraUrl')?.value || '';
    return this.jiraService.extractProjectKeyFromUrl(url);
  }

  onFileSelected(event: Event, type: 'comparison' | 'completion' | 'innovation'): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (type === 'comparison') this.comparisonFile = file;
    else if (type === 'completion') this.completionFile = file;
    else this.innovationFile = file;
    this.snackBar.open(`Documento cargado: ${file.name}`, 'Cerrar', { duration: 3000 });
    this.cdr.detectChanges();
  }

  clearFile(type: 'comparison' | 'completion' | 'innovation'): void {
    if (type === 'comparison') this.comparisonFile = null;
    else if (type === 'completion') this.completionFile = null;
    else this.innovationFile = null;
    this.cdr.detectChanges();
  }

  loadJiraProjects(): void {
    this.loadingProjects = true;
    this.jiraService.getProjects().subscribe({
      next: (projects) => {
        this.jiraProjects = Array.isArray(projects) ? projects : [];
        this.loadingProjects = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingProjects = false;
        this.snackBar.open('Error cargando proyectos', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  loadJiraBugsByProject(): void {
    if (!this.selectedJiraProject) return;
    this.loadingJiraBugs = true;
    this.jiraService.getBugsByProject(this.selectedJiraProject).subscribe({
      next: (bugs) => {
        this.jiraBugs = Array.isArray(bugs) ? bugs : [];
        this.loadingJiraBugs = false;
        this.snackBar.open(`${this.jiraBugs.length} bugs de ${this.selectedJiraProject}`, 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingJiraBugs = false;
        this.snackBar.open('Error cargando bugs', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  loadBugsByUrl(): void {
    if (!this.issueUrl) return;
    this.loadingByUrl = true;
    this.jiraService.getBugsByUrl(this.issueUrl).subscribe({
      next: (data) => {
        this.jiraBugs = data.bugs || [];
        this.loadingByUrl = false;
        this.snackBar.open(
          `${this.jiraBugs.length} items cargados de ${data.issueKey}`,
          'Cerrar', { duration: 3000 }
        );
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingByUrl = false;
        this.snackBar.open('Error cargando actividad', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  generateComparison(): void {
    if (this.comparisonForm.invalid) return;
    this.generating = true;
    const v = this.comparisonForm.value;
    this.reportService.generateComparison({
      projectName: v.projectName,
      qaEngineer: v.qaEngineer,
      version: v.version,
      period: v.period,
      requirements: v.requirements || '',
      testCases: v.testCases || '',
      additionalContext: v.additionalContext || ''
    }, this.comparisonFile || undefined).subscribe({
      next: (data) => {
        this.comparisonReport = data;
        this.comparisonBlobUrl = this.createBlobUrl(data.htmlContent);
        this.generating = false;
        this.cdr.detectChanges();
        setTimeout(() => document.getElementById('comparison-preview')
          ?.scrollIntoView({ behavior: 'smooth' }), 300);
      },
      error: () => {
        this.generating = false;
        this.snackBar.open('Error generando informe', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  generateCompletion(): void {
    if (this.completionForm.invalid) return;
    this.generating = true;
    const v = this.completionForm.value;

    const total = this.jiraBugs.length;
    const passed = this.jiraBugs.filter((b: any) =>
      ['Finalizada', 'Exitoso', 'Done', 'Finalizado'].includes(b.status)).length;
    const failed = this.jiraBugs.filter((b: any) =>
      b.issueType?.includes('Bug') &&
      !['Finalizada', 'Exitoso', 'Done', 'Finalizado', 'Cancelado'].includes(b.status)).length;
    const blocked = this.jiraBugs.filter((b: any) =>
      ['Por hacer', 'Bloqueado'].includes(b.status)).length;

    this.reportService.generateCompletion({
      projectName: v.projectName,
      qaEngineer: v.qaEngineer,
      version: v.version,
      period: v.period,
      totalTestCases: total || 1,
      passedTestCases: passed,
      failedTestCases: failed,
      blockedTestCases: blocked,
      totalExecutionTimeMinutes: 0,
      defects: '',
      additionalContext: v.additionalContext || '',
      jiraBugs: this.jiraBugs
    }, this.completionFile || undefined).subscribe({
      next: (data) => {
        this.completionReport = data;
        this.completionBlobUrl = this.createBlobUrl(data.htmlContent);
        this.generating = false;
        this.cdr.detectChanges();
        setTimeout(() => document.getElementById('completion-preview')
          ?.scrollIntoView({ behavior: 'smooth' }), 300);
      },
      error: () => {
        this.generating = false;
        this.snackBar.open('Error generando informe', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  generateInnovation(): void {
    if (this.innovationForm.invalid) return;
    this.generating = true;
    const v = this.innovationForm.value;
    this.reportService.generateInnovation({
      projectName: v.projectName,
      qaEngineer: v.qaEngineer,
      version: v.version,
      period: v.period,
      additionalContext: v.additionalContext
    }, this.innovationFile || undefined).subscribe({
      next: (data) => {
        this.innovationReport = data;
        this.innovationBlobUrl = this.createBlobUrl(data.htmlContent);
        this.generating = false;
        this.cdr.detectChanges();
        setTimeout(() => document.getElementById('innovation-preview')
          ?.scrollIntoView({ behavior: 'smooth' }), 300);
      },
      error: () => {
        this.generating = false;
        this.snackBar.open('Error generando informe', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  get currentReport(): any {
    if (this.selectedTab === 0) return this.comparisonReport;
    if (this.selectedTab === 1) return this.completionReport;
    return this.innovationReport;
  }

  exportToHtml(): void {
    const report = this.currentReport;
    if (!report) return;
    const blob = new Blob([report.htmlContent], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${(report.title || 'informe').replace(/ /g, '_')}_${new Date().toISOString().slice(0, 10)}.html`;
    a.click();
    URL.revokeObjectURL(url);
    this.snackBar.open('Descargado como HTML', 'Cerrar', { duration: 3000 });
  }

  openInNewTab(): void {
    const report = this.currentReport;
    if (!report) return;
    const blob = new Blob([report.htmlContent], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 10000);
  }

  exportToPdf(): void {
    this.openInNewTab();
    this.snackBar.open('Usa Ctrl+P en la nueva pestaña para guardar como PDF', 'Cerrar', { duration: 5000 });
  }

  uploadToJira(): void {
    const report = this.currentReport;
    if (!report) return;
    this.uploadingToJira = true;
    this.jiraService.createTestCase(
      report.title,
      `Informe QA generado por QA Copilot\nFecha: ${new Date().toLocaleDateString()}`,
      'Medium'
    ).subscribe({
      next: (data) => {
        this.uploadingToJira = false;
        this.snackBar.open(`Subido a Jira — ${data.key}`, 'Abrir', { duration: 6000 })
          .onAction().subscribe(() => window.open(data.url, '_blank'));
        this.cdr.detectChanges();
      },
      error: () => {
        this.uploadingToJira = false;
        this.snackBar.open('Error subiendo a Jira', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  uploadToDocuments(): void {
    const report = this.currentReport;
    if (!report) return;
    this.uploadingToDocs = true;

    const activeJiraUrl = this.getActiveJiraUrl();

    if (activeJiraUrl && activeJiraUrl.trim()) {
      const projectKey = this.jiraService.extractProjectKeyFromUrl(activeJiraUrl);
      if (!projectKey) {
        this.snackBar.open(
          'No se pudo extraer el proyecto de la URL. Formato esperado: .../projects/SEQ/...',
          'Cerrar', { duration: 5000 }
        );
        this.uploadingToDocs = false;
        return;
      }
      this.jiraService.uploadToProject(
        activeJiraUrl,
        report.title,
        `Informe QA generado por QA Copilot — ${new Date().toLocaleDateString()}`
      ).subscribe({
        next: (data) => {
          this.uploadingToDocs = false;
          this.snackBar.open(
            `Subido al proyecto ${data.projectKey} — ${data.key}`,
            'Abrir', { duration: 6000 }
          ).onAction().subscribe(() => window.open(data.url, '_blank'));
          this.cdr.detectChanges();
        },
        error: () => {
          this.uploadingToDocs = false;
          this.snackBar.open(
            'Error subiendo a Jira — verifica la URL del proyecto',
            'Cerrar', { duration: 4000 }
          );
          this.cdr.detectChanges();
        }
      });
    } else {
      const blob = new Blob([report.htmlContent], { type: 'text/html' });
      const fileName = `${(report.title || 'informe').replace(/ /g, '_')}_${new Date().toISOString().slice(0, 10)}.html`;
      const file = new File([blob], fileName, { type: 'text/html' });
      this.documentService.upload(file, `Informe QA — ${report.title}`).subscribe({
        next: () => {
          this.uploadingToDocs = false;
          this.snackBar.open('Guardado en Documentos', 'Cerrar', { duration: 3000 });
          this.cdr.detectChanges();
        },
        error: () => {
          this.uploadingToDocs = false;
          this.snackBar.open('Error guardando documento', 'Cerrar', { duration: 3000 });
          this.cdr.detectChanges();
        }
      });
    }
  }

  get activeJiraUrlLabel(): string {
    const url = this.getActiveJiraUrl();
    if (!url) return 'Guardar';
    const key = this.jiraService.extractProjectKeyFromUrl(url);
    return key ? `Cargar a ${key}` : 'Cargar a Jira';
  }
}
