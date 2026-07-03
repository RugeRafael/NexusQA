import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SeniorPanelComponent } from './senior-panel.component';

const routes: Routes = [{ path: '', component: SeniorPanelComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class SeniorPanelRoutingModule {}
