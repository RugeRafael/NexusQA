import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DocumentService } from '../../core/services/document.service';
import { TestcaseService } from '../../core/services/testcase.service';
import { Document, TestCaseResponse } from '../../core/models/document.model';

@Component({
  selector: 'app-testcases',
  templateUrl: './testcases.component.html',
  styleUrls: ['./testcases.component.scss'],
  standalone: false
})
export class TestcasesComponent implements OnInit {
  form!: FormGroup;
  documents: Document[] = [];
  result: TestCaseResponse | null = null;
  generating = false;
  loadingDocs = false;

  constructor(
    private fb: FormBuilder,
    private documentService: DocumentService,
    private testcaseService: TestcaseService,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      documentId: ['', Validators.required],
      additionalContext: ['']
    });

    this.loadingDocs = true;
    this.documentService.getMyDocuments().subscribe({
      next: (docs) => {
        this.documents = docs;
        this.loadingDocs = false;

        const docId = this.route.snapshot.queryParams['documentId'];
        if (docId) this.form.patchValue({ documentId: docId });

        this.cdr.detectChanges();
      },
      error: () => { this.loadingDocs = false; this.cdr.detectChanges(); }
    });
  }

  generate(): void {
    if (this.form.invalid) return;
    this.generating = true;
    this.result = null;

    this.testcaseService.generate(this.form.value).subscribe({
      next: (res) => {
        this.result = res;
        this.generating = false;
        this.snackBar.open(`${res.totalTestCases} casos generados exitosamente`, 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.generating = false;
        this.snackBar.open(err.error?.message || 'Error al generar casos', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  copyToClipboard(): void {
    if (this.result?.generatedContent) {
      navigator.clipboard.writeText(this.result.generatedContent);
      this.snackBar.open('Copiado al portapapeles', 'Cerrar', { duration: 2000 });
    }
  }

  getConfidenceColor(): string {
    const score = (this.result?.confidenceScore || 0) * 100;
    if (score >= 80) return '#10B981';
    if (score >= 60) return '#F59E0B';
    return '#EF4444';
  }
}