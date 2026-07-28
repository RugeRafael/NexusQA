import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { SeniorPanelRoutingModule } from './senior-panel-routing-module';
import { SeniorPanelComponent } from './senior-panel.component';
import { TeamFilterPipe } from '../../shared/pipes/team-filter.pipe';

@NgModule({
  declarations: [SeniorPanelComponent, TeamFilterPipe],
  imports: [
    CommonModule, FormsModule, SeniorPanelRoutingModule,
    MatButtonModule, MatIconModule, MatSlideToggleModule,
    MatSnackBarModule, MatProgressSpinnerModule, MatTooltipModule,
    MatSelectModule, MatFormFieldModule, MatInputModule
  ],
})
export class SeniorPanelModule {}
