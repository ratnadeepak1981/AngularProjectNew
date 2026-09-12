import { Injectable, inject } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../../core/interceptors/error-interceptor';

import { AdminEventItem } from '../../../../core/models/event/event.model';
import { AdminVenueItem } from '../../../../core/models/event/venue.model';
import { EventAttendeeItem } from '../../../../core/models/event/event-registration.model';
import { CreateEventPayload } from '../../../../core/models/event/create-event-payload.model';
import { CreateVenuePayload } from '../../../../core/models/event/create-venue-payload.model';

export type { AdminEventItem, AdminVenueItem, CreateEventPayload, CreateVenuePayload, EventAttendeeItem };

@Injectable({
  providedIn: 'root',
})
export class EventManagementService {
  private readonly api = inject(ApiService);

  // Events API
  getEvents(): Observable<ApiResponse<AdminEventItem[]> | AdminEventItem[]> {
    return this.api.get<ApiResponse<AdminEventItem[]> | AdminEventItem[]>(this.api.routes.events.list);
  }

  getFormattedEvents(): Observable<AdminEventItem[]> {
    return this.getEvents().pipe(
      map((res: any) => {
        const payload = res?.data || res || [];
        return Array.isArray(payload) ? payload : (payload.items || []);
      })
    );
  }

  createEvent(payload: CreateEventPayload): Observable<ApiResponse<AdminEventItem>> {
    return this.api.post<ApiResponse<AdminEventItem>>(
      this.api.routes.events.create,
      payload,
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  getEventRegistrations(eventId: number): Observable<ApiResponse<EventAttendeeItem[]> | EventAttendeeItem[]> {
    return this.api.get<ApiResponse<EventAttendeeItem[]> | EventAttendeeItem[]>(this.api.routes.events.registrations(eventId));
  }

  getFormattedEventRegistrations(eventId: number): Observable<EventAttendeeItem[]> {
    return this.getEventRegistrations(eventId).pipe(
      map((res: any) => {
        const payload = res?.data || res || [];
        return Array.isArray(payload) ? payload : (payload.items || []);
      })
    );
  }

  // Venues API
  getVenues(): Observable<ApiResponse<AdminVenueItem[]> | AdminVenueItem[]> {
    return this.api.get<ApiResponse<AdminVenueItem[]> | AdminVenueItem[]>(this.api.routes.venues.list);
  }

  getFormattedVenues(): Observable<AdminVenueItem[]> {
    return this.getVenues().pipe(
      map((res: any) => {
        const payload = res?.data || res || [];
        return Array.isArray(payload) ? payload : (payload.items || []);
      })
    );
  }

  createVenue(payload: CreateVenuePayload): Observable<ApiResponse<AdminVenueItem>> {
    return this.api.post<ApiResponse<AdminVenueItem>>(
      this.api.routes.venues.create,
      payload,
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  updateVenue(id: number, payload: { name: string; venueType: string; capacity: number; isActive: boolean }): Observable<ApiResponse<AdminVenueItem>> {
    return this.api.put<ApiResponse<AdminVenueItem>>(
      this.api.routes.venues.update(id),
      payload,
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  deleteVenue(id: number): Observable<ApiResponse<any>> {
    return this.api.delete<ApiResponse<any>>(
      this.api.routes.venues.delete(id),
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }
}
