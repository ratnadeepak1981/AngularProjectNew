import { HttpClient, HttpParams, HttpHeaders, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  private readonly http = inject(HttpClient);
  
   public get baseUrl(): string 
   {
    // 1. Still checks local storage first for manual overrides
    const customUrl = localStorage.getItem('API_BASE_URL');
    if (customUrl) return customUrl;

    // 2. Delegate the URL resolution logic entirely to the environment configuration
    return environment.getApiUrl();
  }

  /*
  public get baseUrl(): string {
    const customUrl = localStorage.getItem('API_BASE_URL');
    if (customUrl) return customUrl;

    if (typeof window !== 'undefined') {
      const port = window.location.port;
      if (port === '7089' || port === '5000' || port === '5016') {
        return `${window.location.origin}/api`;
      }
    }

    return 'https://localhost:7089/api';
  }
    */

  /**
   * Master Catalog of API Endpoints
   */
  public readonly routes = {
    auth: {
      login: '/auth/login',
      refreshToken: '/auth/refresh-token',
      revokeToken: '/auth/revoke-token',
    },
    password: {
      policy: '/password/policy',
      forgotPassword: '/password/forgot-password',
      verifyResetOtp: '/password/verify-reset-otp',
      resetPassword: '/password/reset-password',
      changePassword: '/password/change-password',
    },
    account: {
      verifyEmail: '/account/verify-email',
      resendVerification: '/account/resend-verification',
      sendPhoneOtp: '/account/send-phone-otp',
      verifyPhoneOtp: '/account/verify-phone-otp',
      deactivateCheck: (studentId: number) => `/account/deactivate-check/${studentId}`,
      deactivate: (studentId: number) => `/account/deactivate/${studentId}`,
      reactivate: (studentId: number) => `/account/reactivate/${studentId}`,
    },
    students: {
      register: '/students/register',
      getProfile: (id: number) => `/students/${id}`,
      updateProfile: (id: number) => `/students/${id}`,
      directory: '/students',
      delete: (id: number) => `/students/${id}`,
      resetPassword: (id: number) => `/students/${id}/reset-password`,
      masterByIndex: (idx: string) => `/student-master/verify/${encodeURIComponent(idx)}`,
      masterList: '/student-master',
      masterImport: '/student-master/import',
    },
    faculties: {
      list: '/faculties',
      create: '/faculties',
      update: (id: number) => `/faculties/${id}`,
      delete: (id: number) => `/faculties/${id}`,
    },
    hostel: {
      selectHostels: '/hostel-applications/hostels',
      submit: '/hostel-applications',
      studentApps: '/hostel-applications/student',
      pendingApps: '/hostel-applications/pending',
      allApps: '/hostel-applications/all',
      updateStatus: (id: number) => `/hostel-applications/${id}/status`,
      assignRoom: (id: number) => `/hostel-applications/${id}/assign-room`,
      hostels: '/hostels',
      updateHostel: (id: number) => `/hostels/${id}`,
      deleteHostel: (id: number) => `/hostels/${id}`,
      rooms: (hostelId: number) => `/hostels/${hostelId}/rooms`,
      updateRoom: (id: number) => `/rooms/${id}`,
      deleteRoom: (id: number) => `/rooms/${id}`,
    },
    labs: {
      list: '/labs',
      studentBookings: (studentId: number) => `/lab-bookings/student/${studentId}`,
    },
    events: {
      list: '/events',
      create: '/events',
      register: '/events/register',
      cancelRegistration: (id: number) => `/events/${id}/register`,
      myRegistrations: '/events',
      registrations: (id: number) => `/events/${id}/registrations`,
    },
    venues: {
      list: '/venues',
      create: '/venues',
      update: (id: number) => `/venues/${id}`,
      delete: (id: number) => `/venues/${id}`,
      availability: (id: number) => `/venues/${id}/availability`,
    },
    complaints: {
      studentList: '/complaints/student',
      adminList: '/complaints',
      submit: '/complaints',
      updateStatus: (id: number) => `/complaints/${id}/status`,
      categories: '/complaint-categories',
      createCategory: '/complaint-categories',
      updateCategory: (id: number) => `/complaint-categories/${id}`,
      deleteCategory: (id: number) => `/complaint-categories/${id}`,
    },
    certificates: {
      studentList: '/certificate-requests/student',
      adminList: '/certificate-requests',
      submit: '/certificate-requests',
      updateStatus: (id: number) => `/certificate-requests/${id}/status`,
      types: '/certificate-types',
      createType: '/certificate-types',
      updateType: (id: number) => `/certificate-types/${id}`,
      deleteType: (id: number) => `/certificate-types/${id}`,
    },
    billing: {
      ledger: '/billing/ledger',
      requestOtp: (id: number) => `/billing/payments/${id}/request-otp`,
      pay: (id: number) => `/billing/payments/${id}/pay`,
      assignFee: '/billing/fees/assign',
      cancelUnpaid: (id: number) => `/billing/fee-payments/${id}`,
      feeTypes: '/fee-types',
      createFeeType: '/fee-types',
      deleteFeeType: (id: number) => `/fee-types/${id}`,
      toggleFeeTypeStatus: (id: number) => `/fee-types/${id}/toggle-status`,
    },
    notifications: {
      studentFeed: (id: number) => `/notifications/student/${id}`,
      markRead: (id: number) => `/notifications/${id}/read`,
      adminMonitor: '/notifications/admin-audit-log',
    },
    system: {
      allSettings: '/admin/system-settings/all',
      updateBatch: '/admin/system-settings/batch',
      holdMinutes: '/admin/system-settings/reservation-hold-minutes',
      pageSize: '/admin/system-settings/default-page-size',
    },
    auditLogs: {
      list: '/admin/audit-logs',
      detail: (id: number) => `/admin/audit-logs/${id}`,
      acknowledge: (id: number) => `/admin/audit-logs/${id}/acknowledge`,
      acknowledgeAll: '/admin/audit-logs/acknowledge-all',
    },
  };

  get<T>(
    path: string,
    params?: Record<string, string | number | boolean>,
    options?: { headers?: HttpHeaders; context?: HttpContext }
  ): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          httpParams = httpParams.set(key, value.toString());
        }
      });
    }
    return this.http.get<T>(`${this.baseUrl}${path}`, { params: httpParams, ...options });
  }

  post<T>(path: string, body: any, options?: { headers?: HttpHeaders; context?: HttpContext }): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${path}`, body, options);
  }

  put<T>(path: string, body: any, options?: { headers?: HttpHeaders; context?: HttpContext }): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${path}`, body, options);
  }

  patch<T>(path: string, body: any, options?: { headers?: HttpHeaders; context?: HttpContext }): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}${path}`, body, options);
  }

  delete<T>(path: string, options?: { headers?: HttpHeaders; context?: HttpContext }): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${path}`, options);
  }

  upload<T>(path: string, formData: FormData, options?: { headers?: HttpHeaders; context?: HttpContext }): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${path}`, formData, options);
  }
}
