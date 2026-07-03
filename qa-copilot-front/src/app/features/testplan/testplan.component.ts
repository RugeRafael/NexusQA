import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TestplanService } from '../../core/services/testplan.service';

@Component({
  selector: 'app-testplan',
  templateUrl: './testplan.component.html',
  styleUrls: ['./testplan.component.scss'],
  standalone: false
})
export class TestplanComponent implements OnInit {
  form!: FormGroup;
  analyzing = false;
  result: any = null;
  isDragging = false;
  selectedFile: File | null = null;
  activeTab = 0;

  constructor(
    private fb: FormBuilder,
    private testplanService: TestplanService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      planContent: ['', [Validators.required, Validators.minLength(50)]],
      projectName: ['']
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
      const content = e.target?.result as string;
      this.form.patchValue({ planContent: content });
      this.cdr.detectChanges();
    };
    reader.readAsText(file);
  }

  analyze(): void {
    if (this.form.invalid) return;
    this.analyzing = true;
    this.result = null;

    this.testplanService.analyzePlanText(
      this.form.value.planContent,
      this.form.value.projectName
    ).subscribe({
      next: (res) => {
        this.result = res;
        this.analyzing = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.analyzing = false;
        this.snackBar.open('Error al analizar el plan', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
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
    try {
      return JSON.parse(this.result?.estimated_time_json || '{}');
    } catch {
      return {};
    }
  }
}