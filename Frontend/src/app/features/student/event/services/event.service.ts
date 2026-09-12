import { Injectable, inject } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../../core/interceptors/error-interceptor';
import { CampusEvent } from '../../../../core/models/event/event.model';

@Injectable({
  providedIn: 'root',
})
export class EventService {
  private readonly api = inject(ApiService);

  getAvailableEvents(): Observable<ApiResponse<CampusEvent[]> | CampusEvent[]> {
    return this.api.get<ApiResponse<CampusEvent[]> | CampusEvent[]>(this.api.routes.events.list);
  }

  getFormattedEvents(): Observable<CampusEvent[]> {
    return this.getAvailableEvents().pipe(
      map((res: any) => {
        const data = res?.data || res || [];
        return Array.isArray(data) ? data : [];
      })
    );
  }

  registerForEvent(eventId: number): Observable<ApiResponse<any>> {
    return this.api.post<ApiResponse<any>>(
      this.api.routes.events.register,
      { eventId },
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  cancelRegistration(eventId: number): Observable<ApiResponse<any>> {
    return this.api.delete<ApiResponse<any>>(
      this.api.routes.events.cancelRegistration(eventId),
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }
}
