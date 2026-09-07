import { Injectable, inject } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../../core/interceptors/error-interceptor';
import { Lab } from '../../../../core/models/lab/lab.model';
import { LabSeat } from '../../../../core/models/lab/lab-seat.model';
import { LabBooking, LabMatrixLayoutResponse } from '../../../../core/models/lab/lab-booking.model';

export type LabLayoutResponse = LabMatrixLayoutResponse;
export type LabBookingRecord = LabBooking;

export interface RawApiLab {
  id?: number;
  name?: string;
  labType?: string;
  capacity?: number;
  isActive?: boolean;
  requiresSeatSelection?: boolean;
  seatsBuilt?: number;
  totalSeats?: number;
  totalRows?: number;
  totalColumns?: number;
  seats?: unknown[];
}

export interface RawApiSeat {
  id?: number;
  seatNumber?: string;
  SeatNumber?: string;
  rowIndex?: number;
  RowIndex?: number;
  columnIndex?: number;
  ColumnIndex?: number;
  status?: string;
  Status?: string;
  isBroken?: boolean;
  equipmentDetails?: string;
}

export interface RawApiLayoutPayload {
  totalRows?: number;
  TotalRows?: number;
  totalColumns?: number;
  TotalColumns?: number;
  seats?: RawApiSeat[];
  Seats?: RawApiSeat[];
  data?: RawApiLayoutPayload;
}

@Injectable({
  providedIn: 'root',
})
export class LabManagementService {
  private readonly api = inject(ApiService);

  /**
   * Get all campus laboratories with enriched live workstation counts
   */
  getLabs(): Observable<Lab[]> {
    return this.api.get<RawApiLab[] | { data: RawApiLab[] }>('/labs').pipe(
      switchMap((res) => {
        const labsArray: RawApiLab[] = Array.isArray(res) ? res : (res as { data: RawApiLab[] })?.data || [];
        const baseLabs: Lab[] = labsArray.map((l: RawApiLab): Lab => ({
          id: l.id || 0,
          name: l.name || 'Laboratory',
          labType: l.labType || 'Computer',
          capacity: l.capacity || 24,
          isActive: l.isActive ?? true,
          requiresSeatSelection: l.requiresSeatSelection ?? (l.labType === 'Computer' || l.labType === 'computer'),
          seatsBuilt: (l as any).seatsBuilt ?? (l as any).SeatsBuilt ?? (l as any).totalSeats ?? (l.seats ? l.seats.length : 0),
          totalRows: l.totalRows || 4,
          totalColumns: l.totalColumns || 3,
        }));

        const computerLabs = baseLabs.filter((lab) => lab.requiresSeatSelection);
        if (computerLabs.length === 0) {
          return of(baseLabs);
        }

        const layoutRequests = computerLabs.map((lab) =>
          this.getLabLayout(lab.id).pipe(
            map((layout) => ({
              labId: lab.id,
              count: layout.seats?.length || 0,
              totalRows: layout.totalRows,
              totalColumns: layout.totalColumns,
            })),
            catchError(() => of({ labId: lab.id, count: 0, totalRows: lab.totalRows, totalColumns: lab.totalColumns }))
          )
        );

        return forkJoin(layoutRequests).pipe(
          map((results) => {
            const countMap = new Map(results.map((r) => [r.labId, r]));
            return baseLabs.map((lab) => {
              const enriched = countMap.get(lab.id);
              if (enriched) {
                return {
                  ...lab,
                  seatsBuilt: enriched.count > 0 ? enriched.count : lab.seatsBuilt,
                  totalRows: enriched.totalRows || lab.totalRows,
                  totalColumns: enriched.totalColumns || lab.totalColumns,
                };
              }
              return lab;
            });
          })
        );
      }),
      catchError((err: unknown) => {
        console.error('Failed to fetch labs directory:', err);
        return of([]);
      })
    );
  }

  /**
   * Create a new campus laboratory
   */
  createLab(data: { name: string; labType: string; capacity: number; totalRows?: number; totalColumns?: number }): Observable<boolean> {
    return this.api.post<unknown>('/labs', data).pipe(
      map(() => true),
      catchError((err: unknown) => {
        console.error('Failed to create laboratory:', err);
        return of(false);
      })
    );
  }

  /**
   * Get 1-indexed 2D Matrix Layout for a lab
   */
  getLabLayout(labId: number): Observable<LabMatrixLayoutResponse> {
    const today = new Date().toISOString().split('T')[0];
    return this.api.get<RawApiLayoutPayload>(`/lab-bookings/layout/${labId}?date=${today}&timeSlot=09%3A00%20-%2011%3A00%20AM`).pipe(
      map((res: RawApiLayoutPayload) => {
        const payload: RawApiLayoutPayload = res?.data || res || {};
        const rawSeats: RawApiSeat[] = payload.seats || payload.Seats || [];
        const seats: LabSeat[] = rawSeats.map((s: RawApiSeat): LabSeat => ({
          id: s.id || 0,
          labId: labId,
          seatNumber: s.seatNumber || s.SeatNumber || `LAB${labId}-PC`,
          rowIndex: s.rowIndex || s.RowIndex || 1,
          columnIndex: s.columnIndex || s.ColumnIndex || 1,
          status: (s.status || s.Status || 'Available') as LabSeat['status'],
          isBroken: s.isBroken || s.status === 'Broken',
          equipmentDetails: s.equipmentDetails || 'Standard PC Workstation',
          maintenanceStatus: s.status === 'Broken' ? 'Maintenance Needed' : 'Operational',
        }));

        return {
          totalRows: payload.totalRows ?? payload.TotalRows ?? 4,
          totalColumns: payload.totalColumns ?? payload.TotalColumns ?? 3,
          seats: seats,
        };
      }),
      catchError((err: unknown) => {
        console.error('Failed to fetch lab layout matrix:', err);
        return of({ totalRows: 4, totalColumns: 3, seats: [] });
      })
    );
  }

  /**
   * Get 1-indexed 2D Matrix Layout for a lab on a specific date
   */
  getLabLayoutForDate(labId: number, date: string): Observable<LabMatrixLayoutResponse> {
    return this.api.get<RawApiLayoutPayload>(`/lab-bookings/layout/${labId}?date=${date}&timeSlot=09%3A00%20-%2011%3A00%20AM`).pipe(
      map((res: RawApiLayoutPayload) => {
        const payload: RawApiLayoutPayload = res?.data || res || {};
        const rawSeats: RawApiSeat[] = payload.seats || payload.Seats || [];
        const seats: LabSeat[] = rawSeats.map((s: RawApiSeat): LabSeat => ({
          id: s.id || 0,
          labId: labId,
          seatNumber: s.seatNumber || s.SeatNumber || `LAB${labId}-PC`,
          rowIndex: s.rowIndex || s.RowIndex || 1,
          columnIndex: s.columnIndex || s.ColumnIndex || 1,
          status: (s.status || s.Status || 'Available') as LabSeat['status'],
          isBroken: s.isBroken || s.status === 'Broken',
          equipmentDetails: s.equipmentDetails || 'Standard PC Workstation',
          maintenanceStatus: s.status === 'Broken' ? 'Maintenance Needed' : 'Operational',
        }));

        return {
          totalRows: payload.totalRows ?? payload.TotalRows ?? 4,
          totalColumns: payload.totalColumns ?? payload.TotalColumns ?? 3,
          seats: seats,
        };
      }),
      catchError((err: unknown) => {
        console.error('Failed to fetch date-wise lab layout matrix:', err);
        return of({ totalRows: 4, totalColumns: 3, seats: [] });
      })
    );
  }

  /**
   * Add a workstation seat at (rowIndex, columnIndex)
   */
  addSeat(labId: number, seatNumber: string, rowIndex: number = 1, columnIndex: number = 1, equipmentDetails?: string): Observable<boolean> {
    return this.api.post<any>(`/labs/${labId}/seats`, {
      seatNumber,
      rowIndex,
      columnIndex,
      equipmentDetails,
    }).pipe(
      map(() => true),
      catchError((err) => {
        console.error('Failed to add workstation seat:', err);
        return of(false);
      })
    );
  }

  /**
   * Remove a workstation seat
   */
  removeSeat(labId: number, seatId: number): Observable<boolean> {
    return this.api.delete<any>(`/labs/${labId}/seats/${seatId}`).pipe(
      map(() => true),
      catchError((err) => {
        console.error('Failed to remove workstation seat:', err);
        return of(false);
      })
    );
  }

  /**
   * Get Lab Reservations & Seat Hold Audit History
   */
  getBookingsHistory(): Observable<LabBooking[]> {
    const context = new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true);
    return this.api.get<any>('/lab-bookings/audit-history', undefined, { context }).pipe(
      map((res: any) => {
        return Array.isArray(res) ? res : res?.data || [];
      }),
      catchError(() => {
        // Mock fallback audit data if server route pending
        return of([
          {
            id: 101,
            labId: 1,
            labName: 'Computer Lab 101',
            studentId: 2001,
            studentName: 'Alex Morgan',
            seatNumber: 'LAB1-PC-R1C1',
            bookingDate: '2026-09-01',
            timeSlot: '09:00 - 11:00 AM',
            status: 'Confirmed',
            createdAt: '2026-08-30 10:15',
          },
          {
            id: 102,
            labId: 1,
            labName: 'Computer Lab 101',
            studentId: 2004,
            studentName: 'Samantha Reed',
            seatNumber: 'LAB1-PC-R3C4',
            bookingDate: '2026-09-01',
            timeSlot: '09:00 - 11:00 AM',
            status: 'Held',
            createdAt: '2026-08-30 14:20',
          },
        ]);
      })
    );
  }
}
