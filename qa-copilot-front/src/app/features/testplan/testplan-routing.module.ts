import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TestplanComponent } from './testplan.component';

const routes: Routes = [{ path: '', component: TestplanComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TestplanRoutingModule {}