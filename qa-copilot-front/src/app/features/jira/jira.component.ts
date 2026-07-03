import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { JiraService } from '../../core/services/jira.service';

@Component({
  selector: 'app-jira',
  templateUrl: './jira.component.html',
  styleUrls: ['./jira.component.scss'],
  standalone: false
})
export class JiraComponent implements OnInit {
  connected: boolean | null = null;
  testingConnection = false;
  issues: any[] = [];
  loadingIssues = false;
  creatingTestCase = false;
  creatingBug = false;
  selectedTab = 0;
  lastCreated: any = null;

  testCaseForm!: FormGroup;
  bugForm!: FormGroup;

  displayedColumns = ['key', 'summary', 'type', 'priority', 'status', 'actions'];

  priorities = ['Highest', 'High', 'Medium', 'Low', 'Lowest'];

  constructor(
    private jiraService: JiraService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.testCaseForm = this.fb.group({
      summary: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', Validators.required],
      priority: ['Medium']
    });

    this.bugForm = this.fb.group({
      summary: ['', [Validators.required, Validators.minLength(5)]],
      description: ['', Validators.required],
      stepsToReproduce: ['', Validators.required],
      priority: ['High']
    });

    this.testConnection();
  }

  testConnection(): void {
    this.testingConnection = true;
    this.jiraService.testConnection().subscribe({
      next: (data) => {
        this.connected = data?.connected ?? false;
        this.testingConnection = false;
        if (this.connected) this.loadIssues();
        this.cdr.detectChanges();
      },
      error: () => {
        this.connected = false;
        this.testingConnection = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadIssues(): void {
    this.loadingIssues = true;
    this.jiraService.getIssues(20).subscribe({
      next: (issues) => {
        this.issues = Array.isArray(issues) ? issues : [];
        this.loadingIssues = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingIssues = false;
        this.cdr.detectChanges();
      }
    });
  }

  createTestCase(): void {
    if (this.testCaseForm.invalid) return;
    this.creatingTestCase = true;
    const { summary, description, priority } = this.testCaseForm.value;

    this.jiraService.createTestCase(summary, description, priority).subscribe({
      next: (data) => {
        this.creatingTestCase = false;
        this.lastCreated = data;
        this.testCaseForm.reset({ priority: 'Medium' });
        this.snackBar.open(`✅ Issue ${data.key} creado en Jira`, 'Abrir', {
          duration: 5000
        }).onAction().subscribe(() => window.open(data.url, '_blank'));
        this.loadIssues();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.creatingTestCase = false;
        this.snackBar.open(
          err.error?.message || 'Error al crear issue en Jira',
          'Cerrar', { duration: 4000 }
        );
        this.cdr.detectChanges();
      }
    });
  }

  createBug(): void {
    if (this.bugForm.invalid) return;
    this.creatingBug = true;
    const { summary, description, stepsToReproduce, priority } = this.bugForm.value;

    this.jiraService.createBug(summary, description, stepsToReproduce, priority).subscribe({
      next: (data) => {
        this.creatingBug = false;
        this.lastCreated = data;
        this.bugForm.reset({ priority: 'High' });
        this.snackBar.open(`🐛 Bug ${data.key} creado en Jira`, 'Abrir', {
          duration: 5000
        }).onAction().subscribe(() => window.open(data.url, '_blank'));
        this.loadIssues();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.creatingBug = false;
        this.snackBar.open(
          err.error?.message || 'Error al crear bug en Jira',
          'Cerrar', { duration: 4000 }
        );
        this.cdr.detectChanges();
      }
    });
  }

  openIssue(url: string): void {
    window.open(url, '_blank');
  }

  getPriorityColor(priority: string): string {
    const colors: Record<string, string> = {
      'Highest': '#EF4444',
      'High': '#F97316',
      'Medium': '#F59E0B',
      'Low': '#3B82F6',
      'Lowest': '#64748B'
    };
    return colors[priority] || '#64748B';
  }

  getTypeIcon(type: string): string {
    const icons: Record<string, string> = {
      'Bug': 'bug_report',
      'Task': 'task_alt',
      'Story': 'auto_stories',
      'Epic': 'bolt'
    };
    return icons[type] || 'circle';
  }

  getTypeColor(type: string): string {
    const colors: Record<string, string> = {
      'Bug': '#EF4444',
      'Task': '#3B82F6',
      'Story': '#10B981',
      'Epic': '#8B5CF6'
    };
    return colors[type] || '#64748B';
  }

  getStatusColor(status: string): string {
    const s = status?.toLowerCase();
    if (s?.includes('done') || s?.includes('closed')) return '#10B981';
    if (s?.includes('progress') || s?.includes('review')) return '#F59E0B';
    return '#64748B';
  }

  formatDate(date: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('es-CO', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }
}