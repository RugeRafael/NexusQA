import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { TrainingRoutingModule } from './training-routing.module';
import { TrainingComponent } from './training.component';
import { MatTooltipModule } from '@angular/material/tooltip';

@NgModule({
  declarations: [TrainingComponent],
  imports: [
    CommonModule,
  ReactiveFormsModule,
  TrainingRoutingModule,
  MatIconModule,
  MatButtonModule,
  MatProgressSpinnerModule,
  MatInputModule,
  MatSnackBarModule,
  MatSelectModule,
  MatTableModule,
  MatChipsModule,
  MatTooltipModule
  ]
})
export class TrainingModule {}