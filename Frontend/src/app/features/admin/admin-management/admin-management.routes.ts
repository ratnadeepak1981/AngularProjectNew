import { Routes } from '@angular/router';

export const ADMIN_MANAGEMENT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/admin-management-page.component').then(
        (m) => m.AdminManagementPageComponent
      ),
  },
];
