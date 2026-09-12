import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { PagedResponse } from '../../../../core/models/common/paged-response.model';
import { FeePaymentItem } from '../../../../core/models/billing/fee-payment-item.model';
import { FeeTypeItem } from '../../../../core/models/billing/fee-type-item.model';
import { AssignFeePayload } from '../../../../core/models/billing/assign-fee-payload.model';

export type { FeePaymentItem, FeeTypeItem, AssignFeePayload };

@Injectable({
  providedIn: 'root',
})
export class AdminBillingService {
  private readonly apiService = inject(ApiService);

  getFeeLedger(page: number = 1, size: number = 5): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(this.apiService.routes.billing.ledger, {
      pageNumber: page,
      pageSize: size,
    });
  }

  getFormattedFeeLedger(page: number = 1, size: number = 5): Observable<PagedResponse<FeePaymentItem>> {
    return this.getFeeLedger(page, size).pipe(
      map((res: any) => {
        const payload = res?.data || res || {};
        const items: FeePaymentItem[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);
        const total = payload.totalRecords || payload.totalCount || payload.totalItems || items.length;
        const totalPages = Math.ceil(total / size);
        return {
          items,
          totalRecords: total,
          pageNumber: page,
          pageSize: size,
          totalPages,
          hasPreviousPage: page > 1,
          hasNextPage: page < totalPages,
        };
      })
    );
  }

  getFeeTypes(): Observable<ApiResponse<FeeTypeItem[]>> {
    return this.apiService.get<ApiResponse<FeeTypeItem[]>>(this.apiService.routes.billing.feeTypes);
  }

  getFormattedFeeTypes(): Observable<FeeTypeItem[]> {
    return this.getFeeTypes().pipe(
      map((res: any) => {
        const payload = res?.data || res || [];
        const incoming: any[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);
        
        const list: FeeTypeItem[] = incoming.map((item: any) => ({
          id: item.id || item.Id || 0,
          name: item.name || item.Name || 'Fee Type',
          isActive: item.isActive !== undefined ? item.isActive : (item.IsActive !== undefined ? item.IsActive : true),
        }));

        return list.length > 0 ? list : [
          { id: 1, name: 'Tuition Fee', isActive: true },
          { id: 2, name: 'Semester Fee', isActive: true },
          { id: 3, name: 'Exam Fee', isActive: true },
          { id: 4, name: 'Lab Fine', isActive: true },
        ];
      })
    );
  }

  getStudentsDirectory(): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(this.apiService.routes.students.directory);
  }

  searchStudents(search: string = '', faculty: string = '', page: number = 1, size: number = 10): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(this.apiService.routes.students.directory, {
      search,
      faculty,
      pageNumber: page,
      pageSize: size,
    });
  }

  getFaculties(): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(this.apiService.routes.faculties.list);
  }

  assignFee(payload: AssignFeePayload): Observable<ApiResponse<any>> {
    return this.apiService.post<ApiResponse<any>>(this.apiService.routes.billing.assignFee, payload);
  }

  createFeeType(name: string): Observable<ApiResponse<FeeTypeItem>> {
    return this.apiService.post<ApiResponse<FeeTypeItem>>(this.apiService.routes.billing.createFeeType, { name });
  }

  deactivateFeeType(id: number): Observable<ApiResponse<any>> {
    return this.apiService.delete<ApiResponse<any>>(this.apiService.routes.billing.deleteFeeType(id));
  }

  toggleFeeTypeStatus(id: number): Observable<ApiResponse<any>> {
    return this.apiService.put<ApiResponse<any>>(this.apiService.routes.billing.toggleFeeTypeStatus(id), {});
  }

  cancelUnpaidFee(id: number): Observable<ApiResponse<any>> {
    return this.apiService.delete<ApiResponse<any>>(this.apiService.routes.billing.cancelUnpaid(id));
  }
}
