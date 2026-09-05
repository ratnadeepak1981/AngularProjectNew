import { Routes } from '@angular/router';
import { ReportsDashboardComponent } from './pages/reports-dashboard/reports-dashboard.component';

export const ADMIN_REPORTS_ROUTES: Routes = [
  {
    path: '',
    component: ReportsDashboardComponent,
  },
  {
    path: ':tab',
    component: ReportsDashboardComponent,
  },
];
