import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TestcasesComponent } from './testcases.component';

const routes: Routes = [{ path: '', component: TestcasesComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TestcasesRoutingModule {}