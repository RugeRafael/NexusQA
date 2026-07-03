import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { TestplanRoutingModule } from './testplan-routing.module';
import { TestplanComponent } from './testplan.component';

@NgModule({
  declarations: [TestplanComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TestplanRoutingModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatInputModule,
    MatSnackBarModule,
    MatChipsModule,
    MatTabsModule
  ]
})
export class TestplanModule {}