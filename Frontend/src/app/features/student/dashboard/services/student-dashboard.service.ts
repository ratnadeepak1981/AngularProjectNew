import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiService } from '../../../../core/services/api.service';
import { Notification } from '../../../../core/models/system/notification.model';
import { UpdateStudentProfileRequest } from '../../../../core/models/auth/student-profile.model';
import { StudentDashboardMetricsSummary } from '../../../../core/models/dashboard/student-dashboard-metrics.model';

export type { StudentDashboardMetricsSummary };

@Injectable({
  providedIn: 'root',
})
export class StudentDashboardService {
  private readonly apiService = inject(ApiService);

  getStudentProfile(studentId: number): Observable<any> {
    return this.apiService.get<any>(this.apiService.routes.students.getProfile(studentId));
  }

  updateStudentProfile(
    studentId: number,
    payload: UpdateStudentProfileRequest
  ): Observable<any> {
    return this.apiService.put(this.apiService.routes.students.updateProfile(studentId), payload);
  }

  getDashboardMetrics(studentId: number): Observable<StudentDashboardMetricsSummary> {
    const hostel$ = this.apiService.get<any>(this.apiService.routes.hostel.studentApps).pipe(
      map((res) => {
        const apps = res?.data || res;
        const latest = Array.isArray(apps) && apps.length > 0 ? apps[0] : null;
        if (latest) {
          const st = latest.status || 'Pending';
          return st === 'RoomAssigned'
            ? `Assigned (${latest.assignedRoom?.roomNumber || 'Room'})`
            : st;
        }
        return 'No Application';
      }),
      catchError(() => of('No Application'))
    );

    const labs$ = this.apiService.get<any>(this.apiService.routes.labs.studentBookings(studentId)).pipe(
      map((res) => {
        const list = res?.data || res;
        return Array.isArray(list) ? list.length : list?.items?.length || 0;
      }),
      catchError(() => of(0))
    );

    const billing$ = this.apiService.get<any>(this.apiService.routes.billing.ledger).pipe(
      map((res) => {
        const ledger = res?.data || res || [];
        if (!Array.isArray(ledger)) return 0;
        return ledger
          .filter((item: any) => {
            const status = String(item.status || item.Status || '').toUpperCase().trim();
            return status === 'OUTSTANDING' || status === 'UNPAID';
          })
          .reduce((sum: number, item: any) => sum + (item.amount || item.Amount || 0), 0);
      }),
      catchError(() => of(0))
    );

    const events$ = this.apiService.get<any>(this.apiService.routes.events.list).pipe(
      map((res) => {
        const list = res?.data || res || [];
        if (!Array.isArray(list)) return 0;
        return list.filter((e: any) => {
          const studentIds = e.registeredStudentIds || e.RegisteredStudentIds || [];
          return (
            (Array.isArray(studentIds) && studentIds.length > 0) ||
            e.isRegistered === true ||
            e.IsRegistered === true
          );
        }).length;
      }),
      catchError(() => of(0))
    );

    const certs$ = this.apiService.get<any>(this.apiService.routes.certificates.studentList).pipe(
      map((res) => {
        const certs = res?.data || res;
        const latest = Array.isArray(certs) && certs.length > 0 ? certs[0] : null;
        return latest ? latest.status || 'Pending' : 'No Requests';
      }),
      catchError(() => of('No Requests'))
    );

    const complaints$ = this.apiService.get<any>(this.apiService.routes.complaints.studentList).pipe(
      map((res) => {
        const complaints = res?.data || res;
        const latest = Array.isArray(complaints) && complaints.length > 0 ? complaints[0] : null;
        return latest ? latest.status || 'In Review' : 'No Complaints';
      }),
      catchError(() => of('No Complaints'))
    );

    const notifications$ = this.apiService
      .get<any>(this.apiService.routes.notifications.studentFeed(studentId))
      .pipe(
        map((res) => {
          const notifs = res?.data || res || [];
          return Array.isArray(notifs) ? notifs.slice(0, 4) : [];
        }),
        catchError(() => of([]))
      );

    return forkJoin({
      hostelStatus: hostel$,
      activeLabBookings: labs$,
      outstandingFees: billing$,
      registeredEvents: events$,
      certificateStatus: certs$,
      complaintStatus: complaints$,
      recentNotifications: notifications$,
    });
  }
}
