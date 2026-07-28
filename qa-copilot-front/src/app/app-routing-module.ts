import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { PermissionGuard } from './core/guards/permission.guard';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';

const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule),
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule),
      },
      {
        path: 'documents',
        loadChildren: () => import('./features/documents/documents.module').then(m => m.DocumentsModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'documents' }
      },
      {
        path: 'testcases',
        loadChildren: () => import('./features/testcases/testcases.module').then(m => m.TestcasesModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'testcases' }
      },
      {
        path: 'history',
        loadChildren: () => import('./features/history/history.module').then(m => m.HistoryModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'history' }
      },
      {
        path: 'chat',
        loadChildren: () => import('./features/chat/chat.module').then(m => m.ChatModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'chat' }
      },
      {
        path: 'reports',
        loadChildren: () => import('./features/reports/reports.module').then(m => m.ReportsModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'reports' }
      },
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.module').then(m => m.ProjectsModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'projects' }
      },
      {
        path: 'projects/my',
        loadChildren: () => import('./features/my-projects/my-projects.module').then(m => m.MyProjectsModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'my-projects' }
      },
      {
        path: 'testplan',
        loadChildren: () => import('./features/testplan/testplan.module').then(m => m.TestplanModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'testplan' }
      },
      {
        path: 'analytics',
        loadChildren: () => import('./features/analytics/analytics.module').then(m => m.AnalyticsModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'analytics' }
      },
      {
        path: 'training',
        loadChildren: () => import('./features/training/training.module').then(m => m.TrainingModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'training' }
      },
      {
        path: 'metrics',
        loadChildren: () => import('./features/metrics/metrics.module').then(m => m.MetricsModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'metrics' }
      },
      {
        path: 'jira',
        loadChildren: () => import('./features/jira/jira.module').then(m => m.JiraModule),
      },
      {
        path: 'senior-panel',
        loadChildren: () => import('./features/senior-panel/senior-panel-module').then(m => m.SeniorPanelModule),
        canActivate: [PermissionGuard], data: { moduleKey: 'senior-panel' }
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
