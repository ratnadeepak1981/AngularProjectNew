import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiService } from '../../../../core/services/api.service';

export interface AdminDashboardMetricsSummary {
  pendingHostels: number;
  pendingComplaints: number;
  pendingCertificates: number;
  totalStudents: number;
  pendingFeesCount: number;
  pendingFeesAmount: number;
  totalPaidFeesAmount: number;
  totalLabs: number;
  totalFaculties: number;
  unreadSecurityAlerts: number;
}

@Injectable({
  providedIn: 'root',
})
export class AdminDashboardService {
  private readonly apiService = inject(ApiService);

  getAdminDashboardMetrics(): Observable<AdminDashboardMetricsSummary> {
    const hostels$ = this.apiService.get<any>(this.apiService.routes.hostel.pendingApps).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        return payload.totalRecords ?? payload.totalCount ?? (Array.isArray(payload) ? payload.length : (payload.items?.length || 0));
      }),
      catchError(() => of(0))
    );

    const complaints$ = this.apiService.get<any>(this.apiService.routes.complaints.adminList, { status: 'Pending' }).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        return payload.totalRecords ?? payload.totalCount ?? (Array.isArray(payload) ? payload.length : (payload.items?.length || 0));
      }),
      catchError(() => of(0))
    );

    const certs$ = this.apiService.get<any>(this.apiService.routes.certificates.adminList, { status: 'Pending' }).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        return payload.totalRecords ?? payload.totalCount ?? (Array.isArray(payload) ? payload.length : (payload.items?.length || 0));
      }),
      catchError(() => of(0))
    );

    const students$ = this.apiService.get<any>(this.apiService.routes.students.directory).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        return payload.totalRecords ?? payload.totalCount ?? (Array.isArray(payload) ? payload.length : (payload.items?.length || 0));
      }),
      catchError(() => of(0))
    );

    const billing$ = this.apiService.get<any>(this.apiService.routes.billing.ledger, { pageSize: 100 }).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        const items: any[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);
        const unpaid = items.filter((i) => (i.status || i.Status || '').toLowerCase() === 'unpaid');
        const paid = items.filter((i) => (i.status || i.Status || '').toLowerCase() === 'paid');
        const unpaidSum = unpaid.reduce((acc, i) => acc + Number(i.amount || i.Amount || 0), 0);
        const paidSum = paid.reduce((acc, i) => acc + Number(i.amount || i.Amount || 0), 0);
        return {
          unpaidCount: unpaid.length,
          unpaidAmount: unpaidSum,
          paidAmount: paidSum,
        };
      }),
      catchError(() => of({ unpaidCount: 0, unpaidAmount: 0, paidAmount: 0 }))
    );

    const labs$ = this.apiService.get<any>(this.apiService.routes.labs.list).pipe(
      map((res) => {
        const payload = res?.data || res || [];
        return Array.isArray(payload) ? payload.length : (payload.items?.length || 0);
      }),
      catchError(() => of(0))
    );

    const faculties$ = this.apiService.get<any>(this.apiService.routes.faculties.list).pipe(
      map((res) => {
        const payload = res?.data || res || [];
        return Array.isArray(payload) ? payload.length : (payload.items?.length || 0);
      }),
      catchError(() => of(0))
    );

    const securityAlerts$ = this.apiService.get<any>(this.apiService.routes.auditLogs.list, { isSuccess: false, isReviewed: false, pageSize: 1 }).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        return payload.totalCount ?? payload.totalRecords ?? (Array.isArray(payload) ? payload.length : (payload.items?.length || 0));
      }),
      catchError(() => of(0))
    );

    return forkJoin({
      pendingHostels: hostels$,
      pendingComplaints: complaints$,
      pendingCertificates: certs$,
      totalStudents: students$,
      billing: billing$,
      totalLabs: labs$,
      totalFaculties: faculties$,
      unreadSecurityAlerts: securityAlerts$,
    }).pipe(
      map((res) => ({
        pendingHostels: res.pendingHostels,
        pendingComplaints: res.pendingComplaints,
        pendingCertificates: res.pendingCertificates,
        totalStudents: res.totalStudents,
        pendingFeesCount: res.billing.unpaidCount,
        pendingFeesAmount: res.billing.unpaidAmount,
        totalPaidFeesAmount: res.billing.paidAmount,
        totalLabs: res.totalLabs,
        totalFaculties: res.totalFaculties,
        unreadSecurityAlerts: res.unreadSecurityAlerts,
      }))
    );
  }

  getHoldMinutes(): Observable<number> {
    return this.apiService.get<any>(this.apiService.routes.system.holdMinutes).pipe(
      map((res) => res?.data?.holdMinutes ?? res?.holdMinutes ?? 15),
      catchError(() => of(15))
    );
  }

  saveHoldMinutes(mins: number): Observable<any> {
    return this.apiService.put(this.apiService.routes.system.holdMinutes, { holdMinutes: mins });
  }

  getAllSystemSettings(): Observable<Record<string, string>> {
    return this.apiService.get<any>(this.apiService.routes.system.allSettings).pipe(
      map((res) => res?.data ?? res ?? {}),
      catchError(() => of({}))
    );
  }

  getDefaultPageSize(): Observable<number> {
    return this.apiService.get<any>(this.apiService.routes.system.pageSize).pipe(
      map((res) => res?.data?.pageSize ?? res?.pageSize ?? 10),
      catchError(() => of(10))
    );
  }

  saveDefaultPageSize(size: number): Observable<any> {
    return this.apiService.put(this.apiService.routes.system.pageSize, { pageSize: size });
  }
}
