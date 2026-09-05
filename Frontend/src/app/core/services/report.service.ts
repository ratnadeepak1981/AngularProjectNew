import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, forkJoin, map, catchError, of } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/common/api-response.model';
import { ReportFilter } from '../models/reports/report-filter.model';
import { PagedReportResult } from '../models/reports/paged-report-result.model';
import {
  InstitutionalKpiReport,
  FacultyStudentSummary,
  StudentReportItem,
  HostelOccupancySummary,
  UnallocatedStudentItem,
  LabUtilizationSummary,
  LabBookingReportItem,
  FeeTypeSummary,
  BillingLedgerReportItem,
  ComplaintCategorySummary,
  ComplaintReportItem,
  CertificateTypeSummary,
  CertificateReportItem,
  EventSummary,
  EventAttendeeReportItem,
  NotificationTypeSummary,
  NotificationReportItem,
  ReportFilterLookups,
} from '../models/reports/report-domain.models';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly apiService = inject(ApiService);

  private get baseUrl(): string {
    return `${this.apiService.baseUrl}/admin/reports`;
  }

  private buildParams(filter?: ReportFilter): HttpParams {
    let params = new HttpParams();
    if (!filter) return params;

    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom);
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo);
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.drilldownKey) params = params.set('drilldownKey', filter.drilldownKey);
    if (filter.drilldownId) params = params.set('drilldownId', filter.drilldownId);
    if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortDirection) params = params.set('sortDirection', filter.sortDirection);

    filter.facultyIds?.forEach((id) => (params = params.append('facultyIds', id.toString())));
    filter.statuses?.forEach((status) => (params = params.append('statuses', status)));
    filter.hostelIds?.forEach((id) => (params = params.append('hostelIds', id.toString())));
    filter.labIds?.forEach((id) => (params = params.append('labIds', id.toString())));
    filter.categoryIds?.forEach((id) => (params = params.append('categoryIds', id.toString())));
    filter.certificateTypeIds?.forEach((id) => (params = params.append('certificateTypeIds', id.toString())));
    filter.feeTypeIds?.forEach((id) => (params = params.append('feeTypeIds', id.toString())));
    filter.eventIds?.forEach((id) => (params = params.append('eventIds', id.toString())));

    return params;
  }

  getKpiSummary(filter?: ReportFilter): Observable<ApiResponse<InstitutionalKpiReport>> {
    return this.http.get<ApiResponse<InstitutionalKpiReport>>(`${this.baseUrl}/kpi-summary`, {
      params: this.buildParams(filter),
    });
  }

  getStudentReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<StudentReportItem, FacultyStudentSummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<StudentReportItem, FacultyStudentSummary>>>(`${this.baseUrl}/students`, {
      params: this.buildParams(filter),
    });
  }

  getHostelReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<UnallocatedStudentItem, HostelOccupancySummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<UnallocatedStudentItem, HostelOccupancySummary>>>(`${this.baseUrl}/hostels`, {
      params: this.buildParams(filter),
    });
  }

  getLabReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<LabBookingReportItem, LabUtilizationSummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<LabBookingReportItem, LabUtilizationSummary>>>(`${this.baseUrl}/labs`, {
      params: this.buildParams(filter),
    });
  }

  getBillingReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<BillingLedgerReportItem, FeeTypeSummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<BillingLedgerReportItem, FeeTypeSummary>>>(`${this.baseUrl}/billing`, {
      params: this.buildParams(filter),
    });
  }

  getComplaintReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<ComplaintReportItem, ComplaintCategorySummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<ComplaintReportItem, ComplaintCategorySummary>>>(`${this.baseUrl}/complaints`, {
      params: this.buildParams(filter),
    });
  }

  getCertificateReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<CertificateReportItem, CertificateTypeSummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<CertificateReportItem, CertificateTypeSummary>>>(`${this.baseUrl}/certificates`, {
      params: this.buildParams(filter),
    });
  }

  getEventReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<EventAttendeeReportItem, EventSummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<EventAttendeeReportItem, EventSummary>>>(`${this.baseUrl}/events`, {
      params: this.buildParams(filter),
    });
  }

  getNotificationReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<NotificationReportItem, NotificationTypeSummary>>> {
    return this.http.get<ApiResponse<PagedReportResult<NotificationReportItem, NotificationTypeSummary>>>(`${this.baseUrl}/notifications`, {
      params: this.buildParams(filter),
    });
  }

  getHostelRoomsReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<any, any>>> {
    return this.http.get<ApiResponse<PagedReportResult<any, any>>>(`${this.baseUrl}/hostel-rooms`, {
      params: this.buildParams(filter),
    });
  }

  getPendingHostelApplicationsReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<any, any>>> {
    return this.http.get<ApiResponse<PagedReportResult<any, any>>>(`${this.baseUrl}/hostel-pending-applications`, {
      params: this.buildParams(filter),
    });
  }

  getLabDirectoryReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<any, any>>> {
    return this.http.get<ApiResponse<PagedReportResult<any, any>>>(`${this.baseUrl}/lab-directory`, {
      params: this.buildParams(filter),
    });
  }

  getVenueUtilizationReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<any, any>>> {
    return this.http.get<ApiResponse<PagedReportResult<any, any>>>(`${this.baseUrl}/venues`, {
      params: this.buildParams(filter),
    });
  }

  getPendingStudentRegistrationsReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<any, any>>> {
    return this.http.get<ApiResponse<PagedReportResult<any, any>>>(`${this.baseUrl}/student-pending-registrations`, {
      params: this.buildParams(filter),
    });
  }

  getCertificateTypesCatalogReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<any, any>>> {
    return this.http.get<ApiResponse<PagedReportResult<any, any>>>(`${this.baseUrl}/certificate-types`, {
      params: this.buildParams(filter),
    });
  }

  getComplaintCategoriesSlaReport(filter?: ReportFilter): Observable<ApiResponse<PagedReportResult<any, any>>> {
    return this.http.get<ApiResponse<PagedReportResult<any, any>>>(`${this.baseUrl}/complaint-categories-sla`, {
      params: this.buildParams(filter),
    });
  }

  getFilterLookups(): Observable<ReportFilterLookups> {
    return forkJoin({
      faculties: this.apiService.get<ApiResponse<any[]>>(this.apiService.routes.faculties.list).pipe(
        map((r) => (r.data || []).map((x: any) => ({ id: x.id, name: x.name }))),
        catchError(() => of([]))
      ),
      hostels: this.apiService.get<ApiResponse<any[]>>(this.apiService.routes.hostel.selectHostels).pipe(
        map((r) => (r.data || []).map((x: any) => ({ id: x.id, name: x.name || x.hostelName }))),
        catchError(() => of([]))
      ),
      labs: this.apiService.get<ApiResponse<any[]>>(this.apiService.routes.labs.list).pipe(
        map((r) => (r.data || []).map((x: any) => ({ id: x.id, name: x.name }))),
        catchError(() => of([]))
      ),
      feeTypes: this.apiService.get<ApiResponse<any[]>>(this.apiService.routes.billing.feeTypes).pipe(
        map((r) => (r.data || []).map((x: any) => ({ id: x.id, name: x.name }))),
        catchError(() => of([]))
      ),
      complaintCategories: this.apiService.get<ApiResponse<any[]>>(this.apiService.routes.complaints.categories).pipe(
        map((r) => (r.data || []).map((x: any) => ({ id: x.id, name: x.name }))),
        catchError(() => of([]))
      ),
      certificateTypes: this.apiService.get<ApiResponse<any[]>>(this.apiService.routes.certificates.types).pipe(
        map((r) => (r.data || []).map((x: any) => ({ id: x.id, name: x.name }))),
        catchError(() => of([]))
      ),
      events: this.apiService.get<ApiResponse<any[]>>(this.apiService.routes.events.list).pipe(
        map((r) => (r.data || []).map((x: any) => ({ id: x.id, name: x.title || x.name }))),
        catchError(() => of([]))
      ),
    });
  }
}
