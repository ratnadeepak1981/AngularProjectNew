import { Injectable, inject } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../../core/interceptors/error-interceptor';
import { Lab } from '../../../../core/models/lab/lab.model';
import { LabSeat } from '../../../../core/models/lab/lab-seat.model';
import { CreateHoldPayload, LabBooking, LabMatrixLayoutResponse } from '../../../../core/models/lab/lab-booking.model';

@Injectable({
  providedIn: 'root',
})
export class LabBookingService {
  private readonly api = inject(ApiService);

  /**
   * Fetch all active campus laboratories for student selection
   */
  getLabs(): Observable<Lab[]> {
    return this.api.get<any>('/labs').pipe(
      map((res) => {
        const labsArray = Array.isArray(res) ? res : res?.data || [];
        return labsArray.map((l: any): Lab => ({
          id: l.id || 0,
          name: l.name || 'Laboratory',
          labType: l.labType || 'Computer',
          capacity: l.capacity || 24,
          isActive: l.isActive ?? true,
          requiresSeatSelection: l.requiresSeatSelection ?? (l.labType === 'Computer' || l.labType === 'computer'),
          seatsBuilt: l.seatsBuilt ?? l.totalSeats ?? (l.seats ? l.seats.length : 0),
          totalRows: l.totalRows || 4,
          totalColumns: l.totalColumns || 3,
        }));
      }),
      catchError((err: unknown) => {
        console.error('Failed to fetch campus labs list:', err);
        return of([]);
      })
    );
  }

  /**
   * Fetch 2D workstation grid matrix layout for a specific date and time slot
   */
  getLabLayout(labId: number, date: string, timeSlot: string): Observable<LabMatrixLayoutResponse> {
    const layoutUrl = `/lab-bookings/layout/${labId}?date=${encodeURIComponent(date)}&timeSlot=${encodeURIComponent(timeSlot)}`;
    return this.api.get<any>(layoutUrl).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        const rawSeats: any[] = payload.seats || payload.Seats || [];
        const seats: LabSeat[] = rawSeats.map((s: any): LabSeat => ({
          id: s.id || s.Id || 0,
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
        console.error('Failed to fetch seat layout matrix for slot:', err);
        return of({ totalRows: 4, totalColumns: 3, seats: [] });
      })
    );
  }

  /**
   * Place temporary hold on a workstation seat
   */
  createHold(payload: CreateHoldPayload): Observable<{ success: boolean; data?: LabBooking; message?: string }> {
    return this.api.post<any>('/lab-bookings', payload).pipe(
      map((res) => {
        const raw = res?.data || res || {};
        const booking: LabBooking = {
          id: raw.id || raw.Id || 0,
          studentId: raw.studentId || raw.StudentId || payload.studentId,
          labId: raw.labId || raw.LabId || payload.labId,
          labName: raw.labName || raw.LabName || 'Campus Lab',
          labType: raw.labType || raw.LabType || 'Computer',
          seatId: raw.seatId || raw.SeatId || payload.seatId,
          seatNumber: raw.seatNumber || raw.SeatNumber || (payload.seatId ? `PC #${payload.seatId}` : 'Workstation'),
          bookingDate: raw.bookingDate ? raw.bookingDate.split('T')[0] : payload.bookingDate,
          timeSlot: raw.timeSlot || payload.timeSlot,
          status: raw.status || 'Held',
          expiresAt: raw.expiresAt || new Date(Date.now() + 15 * 60000).toISOString(),
        };
        return {
          success: true,
          data: booking,
        };
      }),
      catchError((err: any) => {
        const msg = err?.error?.message || err?.error || 'Failed to place reservation hold on target seat.';
        return of({ success: false, message: msg });
      })
    );
  }

  /**
   * Confirm temporary 15-minute held seat reservation
   */
  confirmBooking(bookingId: number): Observable<{ success: boolean; message?: string }> {
    return this.api.put<any>(`/lab-bookings/${bookingId}/confirm`, {}).pipe(
      map(() => ({ success: true })),
      catchError((err: any) => {
        const msg = err?.error?.message || err?.error || 'Reservation hold expired or confirmation failed.';
        return of({ success: false, message: msg });
      })
    );
  }

  confirmHold(bookingId: number, studentId?: number): Observable<{ success: boolean; message?: string }> {
    return this.confirmBooking(bookingId);
  }

  /**
   * Cancel an upcoming or active lab booking
   */
  cancelBooking(bookingId: number, studentId: number): Observable<boolean> {
    return this.api.delete<any>(`/lab-bookings/${bookingId}?studentId=${studentId}`).pipe(
      map(() => true),
      catchError((err: unknown) => {
        console.error('Failed to cancel lab booking:', err);
        return of(false);
      })
    );
  }

  /**
   * Fetch authenticated student's lab booking history
   */
  getMyBookings(studentId: number): Observable<LabBooking[]> {
    return this.api.get<any>(`/lab-bookings/student/${studentId}`).pipe(
      map((res) => {
        const items = Array.isArray(res) ? res : res?.data || [];
        return items.map((b: any): LabBooking => ({
          id: b.id || 0,
          studentId: b.studentId || studentId,
          labId: b.labId,
          labName: b.labName || b.lab?.name || 'Campus Lab',
          labType: b.labType || b.lab?.labType || 'Computer',
          seatId: b.seatId,
          seatNumber: b.seatNumber || (b.seatId ? `PC #${b.seatId}` : 'N/A'),
          bookingDate: b.bookingDate ? b.bookingDate.split('T')[0] : 'N/A',
          timeSlot: b.timeSlot || '09:00 - 11:00 AM',
          status: b.status || 'Confirmed',
          expiresAt: b.expiresAt,
          createdAt: b.createdAt,
        }));
      }),
      catchError((err: unknown) => {
        console.error('Failed to fetch student lab booking history:', err);
        return of([]);
      })
    );
  }

  /**
   * Read System Hold Minutes Setting with fail-safe fallback to 15 mins
   */
  getSystemHoldMinutes(): Observable<number> {
    const context = new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true);
    return this.api.get<any>('/admin/system-settings/reservation-hold-minutes', undefined, { context }).pipe(
      map((res) => {
        const payload = res?.data || res || {};
        const mins = payload.holdMinutes ?? payload.HoldMinutes ?? res?.holdMinutes ?? res?.HoldMinutes;
        return mins ? Number(mins) : 15;
      }),
      catchError(() => of(15))
    );
  }

  getSystemSettings(): Observable<Record<string, string>> {
    const context = new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true);
    return this.api.get<any>('/admin/system-settings/all', undefined, { context }).pipe(
      map((res) => (res?.data || res || {}) as Record<string, string>),
      catchError(() => of({}))
    );
  }
}
