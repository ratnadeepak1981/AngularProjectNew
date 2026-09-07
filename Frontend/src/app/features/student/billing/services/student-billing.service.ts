import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { FeePaymentItem } from '../../../../core/models/billing/fee-payment-item.model';

export type { FeePaymentItem };

export interface StudentLedgerSummary {
  items: FeePaymentItem[];
  outstandingFormatted: string;
  paidFormatted: string;
}

export interface PaymentReceiptResponse {
  receiptNumber?: string;
  transactionId?: string;
  status?: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root',
})
export class StudentBillingService {
  private readonly apiService = inject(ApiService);

  getStudentLedger(): Observable<ApiResponse<FeePaymentItem[]> | FeePaymentItem[]> {
    return this.apiService.get<ApiResponse<FeePaymentItem[]> | FeePaymentItem[]>(this.apiService.routes.billing.ledger);
  }

  getFormattedLedger(): Observable<StudentLedgerSummary> {
    return this.getStudentLedger().pipe(
      map((res: any) => {
        const payload = res?.data || res || [];
        const items: FeePaymentItem[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);

        const outstanding = items
          .filter((i) => (i.status || '').toLowerCase() !== 'paid')
          .reduce((sum, item) => sum + (Number(item.amount) || 0), 0);

        const paid = items
          .filter((i) => (i.status || '').toLowerCase() === 'paid')
          .reduce((sum, item) => sum + (Number(item.amount) || 0), 0);

        return {
          items,
          outstandingFormatted: `$${outstanding.toFixed(2)}`,
          paidFormatted: `$${paid.toFixed(2)}`,
        };
      })
    );
  }

  getInvoiceDetails(invoiceId: number): Observable<FeePaymentItem | null> {
    return this.getStudentLedger().pipe(
      map((res: any) => {
        const payload = res?.data || res || [];
        const items: FeePaymentItem[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);
        return items.find((i) => i.id === invoiceId) || null;
      })
    );
  }

  requestPaymentOtp(id: number): Observable<ApiResponse<any> | any> {
    return this.apiService.post<ApiResponse<any>>(this.apiService.routes.billing.requestOtp(id), {});
  }

  payInvoice(id: number, payload: any = {}): Observable<ApiResponse<PaymentReceiptResponse> | any> {
    return this.apiService.post<ApiResponse<PaymentReceiptResponse>>(this.apiService.routes.billing.pay(id), payload);
  }
}
