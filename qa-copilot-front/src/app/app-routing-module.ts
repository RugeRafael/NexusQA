import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { RoleGuard } from './core/guards/role.guard';

const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule),
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/dashboard.module').then((m) => m.DashboardModule),
      },
      {
        path: 'documents',
        loadChildren: () =>
          import('./features/documents/documents.module').then((m) => m.DocumentsModule),
      },
      {
        path: 'testcases',
        loadChildren: () =>
          import('./features/testcases/testcases.module').then((m) => m.TestcasesModule),
      },
      {
        path: 'history',
        loadChildren: () =>
          import('./features/history/history.module').then((m) => m.HistoryModule),
      },
      {
        path: 'chat',
        loadChildren: () => import('./features/chat/chat.module').then((m) => m.ChatModule),
      },
      {
        path: 'reports',
        loadChildren: () =>
          import('./features/reports/reports.module').then((m) => m.ReportsModule),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Senior', 'QAEngineer'] },
      },
      {
        path: 'projects',
        loadChildren: () =>
          import('./features/projects/projects.module').then((m) => m.ProjectsModule),
      },
      {
        path: 'testplan',
        loadChildren: () =>
          import('./features/testplan/testplan.module').then((m) => m.TestplanModule),
      },
      {
        path: 'analytics',
        loadChildren: () =>
          import('./features/analytics/analytics.module').then((m) => m.AnalyticsModule),
      },
      {
        path: 'training',
        loadChildren: () =>
          import('./features/training/training.module').then((m) => m.TrainingModule),
      },
      {
        path: 'metrics',
        loadChildren: () =>
          import('./features/metrics/metrics.module').then((m) => m.MetricsModule),
      },
      {
        path: 'projects/my',
        loadChildren: () =>
          import('./features/my-projects/my-projects.module').then((m) => m.MyProjectsModule),
      },
      {
        path: 'jira',
        loadChildren: () => import('./features/jira/jira.module').then((m) => m.JiraModule),
      },
      {
        path: 'reports',
        loadChildren: () =>
          import('./features/reports/reports.module').then((m) => m.ReportsModule),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Senior', 'QAEngineer'] },
      },
      {
        path: 'senior-panel',
        loadChildren: () =>
          import('./features/senior-panel/senior-panel-module').then((m) => m.SeniorPanelModule),
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['Admin', 'Senior'] },
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: 'auth/login' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}

