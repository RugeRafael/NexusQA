import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { TestcasesRoutingModule } from './testcases-routing.module';
import { TestcasesComponent } from './testcases.component';

@NgModule({
  declarations: [TestcasesComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    TestcasesRoutingModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSelectModule,
    MatInputModule,
    MatCardModule
  ]
})
export class TestcasesModule {}