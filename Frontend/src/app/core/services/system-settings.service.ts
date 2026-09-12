import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse } from '../models/common/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class SystemSettingsService {
  private readonly api = inject(ApiService);

  public readonly defaultPageSize = signal<number>(5);
  public readonly isLoaded = signal<boolean>(false);

  constructor() {
    if (this.hasAuthToken()) {
      this.loadDefaultPageSize();
    }
  }

  private hasAuthToken(): boolean {
    return typeof window !== 'undefined' && !!localStorage.getItem('portal_jwt_token');
  }

  public loadDefaultPageSize(): void {
    if (!this.hasAuthToken()) return;

    this.getDefaultPageSize().subscribe({
      next: () => {
        this.isLoaded.set(true);
      },
      error: () => {
        this.isLoaded.set(true);
      },
    });
  }

  public getDefaultPageSize(): Observable<ApiResponse<{ pageSize: number }> | null> {
    return this.api.get<ApiResponse<{ pageSize: number }>>(this.api.routes.system.pageSize).pipe(
      tap((res: any) => {
        const size = res?.data?.pageSize ?? res?.pageSize;
        if (size && typeof size === 'number' && size > 0) {
          this.defaultPageSize.set(size);
        }
      }),
      catchError((err) => {
        console.warn('SystemSettingsService: Could not load default page size setting, using fallback (5).', err?.message);
        return of(null);
      })
    );
  }

  public updateDefaultPageSize(pageSize: number): Observable<ApiResponse<{ message: string; pageSize: number }>> {
    return this.api.put<ApiResponse<{ message: string; pageSize: number }>>(
      this.api.routes.system.pageSize,
      { pageSize }
    ).pipe(
      tap(() => {
        if (pageSize > 0) {
          this.defaultPageSize.set(pageSize);
        }
      })
    );
  }

  public getAllSettings(): Observable<ApiResponse<Record<string, string>>> {
    return this.api.get<ApiResponse<Record<string, string>>>(this.api.routes.system.allSettings).pipe(
      tap((res: any) => {
        const dict = res?.data || res || {};
        const rawSize = dict['DefaultPageSize'] || dict['PageSize'] || dict['AdminDefaultPageSize'];
        if (rawSize) {
          const parsed = parseInt(rawSize, 10);
          if (!isNaN(parsed) && parsed > 0) {
            this.defaultPageSize.set(parsed);
          }
        }
      })
    );
  }

  public updateSettingsBatch(payload: Record<string, string>): Observable<ApiResponse<Record<string, string>>> {
    return this.api.put<ApiResponse<Record<string, string>>>(this.api.routes.system.updateBatch, payload).pipe(
      tap(() => {
        if (payload['DefaultPageSize']) {
          const parsed = parseInt(payload['DefaultPageSize'], 10);
          if (!isNaN(parsed) && parsed > 0) {
            this.defaultPageSize.set(parsed);
          }
        }
      })
    );
  }

  public getHoldMinutes(): Observable<ApiResponse<{ holdMinutes: number }>> {
    return this.api.get<ApiResponse<{ holdMinutes: number }>>(this.api.routes.system.holdMinutes);
  }

  public updateHoldMinutes(holdMinutes: number): Observable<ApiResponse<{ message: string; holdMinutes: number }>> {
    return this.api.put<ApiResponse<{ message: string; holdMinutes: number }>>(
      this.api.routes.system.holdMinutes,
      { holdMinutes }
    );
  }
}
