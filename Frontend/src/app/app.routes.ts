import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { LoginComponent } from './features/auth/login/login.component';
import { authGuard } from './core/guards/auth-guard';
import { adminGuard } from './core/guards/admin-guard';
import { studentGuard } from './core/guards/student-guard';

import { StudentRegistrationComponent } from './features/auth/student-registration/student-registration.component';

export const routes: Routes = [
  { path: '', redirectTo: 'auth/login', pathMatch: 'full' },

  // Public Authentication Routes
  {
    path: 'auth',
    component: AuthLayoutComponent,
    children: [
      { path: 'login', component: LoginComponent },
      { path: 'register', component: StudentRegistrationComponent },
      { path: '', redirectTo: 'login', pathMatch: 'full' },
    ],
  },

  // Protected Core Application Routes
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'admin/dashboard',
        loadChildren: () =>
          import('./features/admin/dashboard/admin-dashboard.routes').then(
            (m) => m.ADMIN_DASHBOARD_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/studentmaster',
        loadChildren: () =>
          import('./features/admin/student-master/student-master.routes').then(
            (m) => m.STUDENT_MASTER_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/students',
        loadChildren: () =>
          import('./features/admin/student-master/student-master.routes').then(
            (m) => m.STUDENT_MASTER_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/hostel-management',
        loadChildren: () =>
          import('./features/admin/hostel-management/admin-hostel.routes').then(
            (m) => m.ADMIN_HOSTEL_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/hostels',
        loadChildren: () =>
          import('./features/admin/hostel-management/admin-hostel.routes').then(
            (m) => m.ADMIN_HOSTEL_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/event-management',
        loadChildren: () =>
          import('./features/admin/event-management/admin-event.routes').then(
            (m) => m.ADMIN_EVENT_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/events',
        loadChildren: () =>
          import('./features/admin/event-management/admin-event.routes').then(
            (m) => m.ADMIN_EVENT_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/billing-management',
        loadComponent: () =>
          import('./features/admin/billing-management/pages/billing-dashboard/billing-dashboard.component').then(
            (m) => m.BillingDashboardComponent
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/fees',
        redirectTo: 'admin/billing-management',
        pathMatch: 'full',
      },
      {
        path: 'admin/notification-monitor',
        loadComponent: () =>
          import(
            './features/admin/notification-monitor/pages/notification-monitor-page.component.component'
          ).then((m) => m.NotificationMonitorPageComponentComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/notifications',
        redirectTo: 'admin/notification-monitor',
        pathMatch: 'full',
      },
      {
        path: 'admin/faculty-management',
        loadComponent: () =>
          import(
            './features/admin/faculty-management/pages/faculty-management.page/faculty-management.page.component'
          ).then((m) => m.FacultyManagementPageComponent),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/faculties',
        redirectTo: 'admin/faculty-management',
        pathMatch: 'full',
      },
      {
        path: 'admin/system-settings',
        loadComponent: () =>
          import('./features/admin/system-settings/pages/system-settings-page.component').then(
            (m) => m.SystemSettingsPageComponent
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/settings',
        redirectTo: 'admin/system-settings',
        pathMatch: 'full',
      },
      {
        path: 'admin/audit-logs',
        loadChildren: () =>
          import('./features/admin/audit-logs/admin-audit.routes').then(
            (m) => m.ADMIN_AUDIT_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/audit',
        redirectTo: 'admin/audit-logs',
        pathMatch: 'full',
      },
      {
        path: 'admin/lab-management',
        loadChildren: () =>
          import('./features/admin/lab-management/admin-lab.routes').then(
            (m) => m.ADMIN_LAB_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/labs',
        redirectTo: 'admin/lab-management',
        pathMatch: 'full',
      },
      {
        path: 'admin/certificate-management',
        loadChildren: () =>
          import('./features/admin/certificate-management/admin-certificate.routes').then(
            (m) => m.ADMIN_CERTIFICATE_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/certificates',
        redirectTo: 'admin/certificate-management',
        pathMatch: 'full',
      },
      {
        path: 'admin/complaint-management',
        loadChildren: () =>
          import('./features/admin/complaint-management/admin-complaint.routes').then(
            (m) => m.ADMIN_COMPLAINT_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/complaints',
        redirectTo: 'admin/complaint-management',
        pathMatch: 'full',
      },
      {
        path: 'admin/reports',
        loadChildren: () =>
          import('./features/admin/reports/admin-reports.routes').then(
            (m) => m.ADMIN_REPORTS_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/analytics',
        loadChildren: () =>
          import('./features/admin/reports/admin-reports.routes').then(
            (m) => m.ADMIN_REPORTS_ROUTES
          ),
        canActivate: [adminGuard],
      },
      {
        path: 'student/dashboard',
        loadChildren: () =>
          import('./features/student/dashboard/student-dashboard.routes').then(
            (m) => m.STUDENT_DASHBOARD_ROUTES
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/settings',
        loadComponent: () =>
          import('./features/student/settings/pages/student-settings-page.component').then(
            (m) => m.StudentSettingsPageComponent
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/preferences',
        redirectTo: 'student/settings',
        pathMatch: 'full',
      },
      {
        path: 'student/certificate',
        loadChildren: () =>
          import('./features/student/certificate/student-certificate.routes').then(
            (m) => m.STUDENT_CERTIFICATE_ROUTES
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/certificates',
        redirectTo: 'student/certificate',
        pathMatch: 'full',
      },
      {
        path: 'student/complaint',
        loadChildren: () =>
          import('./features/student/complaint/student-complaint.routes').then(
            (m) => m.STUDENT_COMPLAINT_ROUTES
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/complaints',
        redirectTo: 'student/complaint',
        pathMatch: 'full',
      },
      {
        path: 'student/hostel',
        loadComponent: () =>
          import('./features/student/hostel-application/pages/hostel-application-page.component').then(
            (m) => m.HostelApplicationPageComponent
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/hostel-application',
        redirectTo: 'student/hostel',
        pathMatch: 'full',
      },
      {
        path: 'student/events',
        loadChildren: () =>
          import('./features/student/event/student-event.routes').then(
            (m) => m.STUDENT_EVENT_ROUTES
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/event',
        redirectTo: 'student/events',
        pathMatch: 'full',
      },
      {
        path: 'student/billing',
        loadComponent: () =>
          import('./features/student/billing/pages/student-billing-page.component').then(
            (m) => m.StudentBillingPageComponent
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/billing/checkout/:id',
        loadComponent: () =>
          import('./features/student/billing/pages/student-payment-page.component').then(
            (m) => m.StudentPaymentPageComponent
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/notification',
        loadComponent: () =>
          import(
            './features/student/notification/pages/student-notification-page/student-notification-page.component'
          ).then((m) => m.StudentNotificationPageComponent),
        canActivate: [studentGuard],
      },
      {
        path: 'student/notifications',
        redirectTo: 'student/notification',
        pathMatch: 'full',
      },
      {
        path: 'student/lab-booking',
        loadChildren: () =>
          import('./features/student/lab-booking/student-booking.routes').then(
            (m) => m.STUDENT_BOOKING_ROUTES
          ),
        canActivate: [studentGuard],
      },
      {
        path: 'student/labbooking',
        redirectTo: 'student/lab-booking',
        pathMatch: 'full',
      },
    ],
  },

  { path: '**', redirectTo: 'auth/login' },
];
