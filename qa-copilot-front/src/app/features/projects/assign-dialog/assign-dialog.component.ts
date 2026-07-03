import { Component, OnInit, Inject, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserService } from '../../../core/services/user.service';
import { ProjectService } from '../../../core/services/project.service';

@Component({
  selector: 'app-assign-dialog',
  templateUrl: './assign-dialog.component.html',
  styleUrls: ['./assign-dialog.component.scss'],
  standalone: false
})
export class AssignDialogComponent implements OnInit {
  form!: FormGroup;
  users: any[] = [];
  loading = false;
  assigning = false;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: { project: any },
    private dialogRef: MatDialogRef<AssignDialogComponent>,
    private fb: FormBuilder,
    private userService: UserService,
    private projectService: ProjectService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      userId: ['', Validators.required],
      notes: ['']
    });

    this.loading = true;
    this.userService.getQAEngineers().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  assign(): void {
    if (this.form.invalid) return;
    this.assigning = true;

    this.projectService.assign({
      projectId: this.data.project.id,
      userId: this.form.value.userId,
      notes: this.form.value.notes || undefined
    }).subscribe({
      next: () => {
        this.assigning = false;
        this.snackBar.open('QA asignado exitosamente', 'Cerrar', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.assigning = false;
        this.snackBar.open(
          err.error?.message || 'Error al asignar QA',
          'Cerrar',
          { duration: 3000 }
        );
        this.cdr.detectChanges();
      }
    });
  }

  getRoleColor(role: string): string {
    const colors: Record<string, string> = {
      'Admin': '#EF4444',
      'Senior': '#F59E0B',
      'QAEngineer': '#3B82F6',
      'Viewer': '#64748B'
    };
    return colors[role] || '#64748B';
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}