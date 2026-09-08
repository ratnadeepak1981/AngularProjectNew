import { HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { Certificate, CertificateType, SubmitCertificateRequestDto } from '../../../../core/models/certificate/certificate.model';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../../core/interceptors/error-interceptor';

@Injectable({
  providedIn: 'root',
})

export class CertificateRequestService {
  private readonly api = inject(ApiService);

  getMyRequests(): Observable<Certificate[]> {
    return this.api
      .get<ApiResponse<Certificate[]> | Certificate[]>(this.api.routes.certificates.studentList)
      .pipe(
        map((res: any) => {
          if (Array.isArray(res)) return res;
          return res?.data || res?.Data || [];
        })
      );
  }

  submitRequest(dto: SubmitCertificateRequestDto): Observable<Certificate> {
    return this.api
      .post<ApiResponse<Certificate>>(this.api.routes.certificates.submit, dto, {
        context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true),
      })
      .pipe(
        map((res: any) => res?.data || res?.Data || res)
      );
  }

  getCertificateTypes(): Observable<CertificateType[]> {
    return this.api
      .get<ApiResponse<CertificateType[]> | CertificateType[]>(this.api.routes.certificates.types)
      .pipe(
        map((res: any) => {
          if (Array.isArray(res)) return res;
          return (res?.data || res?.Data || []).filter((t: CertificateType) => t.isActive);
        })
      );
  }
}
