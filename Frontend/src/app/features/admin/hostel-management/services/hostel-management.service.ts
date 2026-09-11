import { Injectable, inject } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../../core/interceptors/error-interceptor';

import { HostelRoom } from '../../../../core/models/hostel/room.model';
import { HostelBuilding } from '../../../../core/models/hostel/hostel.model';
import { HousingApplication } from '../../../../core/models/hostel/hostel-application.model';

export type { HostelRoom, HostelBuilding, HousingApplication };

export interface HousingApplicationsGroup {
  all: HousingApplication[];
  pending: HousingApplication[];
}

@Injectable({
  providedIn: 'root',
})
export class HostelManagementService {
  private readonly apiService = inject(ApiService);

  getPendingApplications(page: number = 1, size: number = 10): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(this.apiService.routes.hostel.pendingApps, {
      pageNumber: page,
      pageSize: size,
    });
  }

  getAllApplications(): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(this.apiService.routes.hostel.allApps);
  }

  getFormattedApplications(): Observable<HousingApplicationsGroup> {
    return this.getAllApplications().pipe(
      map((res: any) => {
        const payload = res?.data || res || [];
        const items: any[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);
        const formatted: HousingApplication[] = items.map((a: any) => ({
          id: a.id || a.Id,
          studentId: a.studentId || a.StudentId,
          studentIndexNumber: a.studentIndexNumber || a.student?.indexNumber || a.Student?.IndexNumber || 'N/A',
          studentName: a.studentName || a.student?.fullName || a.Student?.FullName || 'Student',
          preferredHostelId: a.preferredHostelId || a.PreferredHostelId || (a.preferredHostel ? a.preferredHostel.id : 0),
          preferredHostelName: a.hostelName || a.preferredHostel?.name || a.PreferredHostel?.Name || 'Hostel',
          termSemester: a.termSemester || a.TermSemester || 'Year 1 - Sem 1',
          specialRequirements: a.specialRequirements || a.SpecialRequirements || 'None',
          status: a.status || a.Status || 'Pending',
          assignedRoomId: a.assignedRoomId || a.AssignedRoomId || a.room?.id,
          assignedRoomNumber: a.roomNumber || a.room?.roomNumber || a.Room?.RoomNumber,
          createdAt: a.createdAt || a.CreatedAt,
        }));

        return {
          all: formatted,
          pending: formatted.filter((a) => a.status === 'Pending'),
        };
      })
    );
  }

  getHostelsMasterList(page: number = 1, size: number = 100): Observable<ApiResponse<any>> {
    return this.apiService.get<ApiResponse<any>>(this.apiService.routes.hostel.selectHostels, {
      pageNumber: page,
      pageSize: size,
    });
  }

  getFormattedHostelsList(page: number = 1, size: number = 100): Observable<HostelBuilding[]> {
    return this.getHostelsMasterList(page, size).pipe(
      map((res: any) => {
        const payload = res?.data || res || {};
        return payload.items || payload.Items || (Array.isArray(payload) ? payload : []);
      })
    );
  }

  updateApplicationStatus(id: number, status: 'Approved' | 'Rejected'): Observable<ApiResponse<any>> {
    return this.apiService.put<ApiResponse<any>>(this.apiService.routes.hostel.updateStatus(id), {
      status,
    });
  }

  assignRoom(applicationId: number, roomId: number): Observable<ApiResponse<any>> {
    return this.apiService.put<ApiResponse<any>>(this.apiService.routes.hostel.assignRoom(applicationId), {
      roomId,
    });
  }

  createHostel(name: string): Observable<ApiResponse<HostelBuilding>> {
    return this.apiService.post<ApiResponse<HostelBuilding>>(
      this.apiService.routes.hostel.hostels,
      { name },
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  updateHostel(id: number, name: string, location: string = 'Campus Main', isActive: boolean = true): Observable<ApiResponse<HostelBuilding>> {
    return this.apiService.put<ApiResponse<HostelBuilding>>(
      this.apiService.routes.hostel.updateHostel(id),
      { name, location, isActive },
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  deleteHostel(id: number): Observable<ApiResponse<any>> {
    return this.apiService.delete<ApiResponse<any>>(
      this.apiService.routes.hostel.deleteHostel(id),
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  createRoom(hostelId: number, roomNumber: string, maxCapacity: number): Observable<ApiResponse<HostelRoom>> {
    return this.apiService.post<ApiResponse<HostelRoom>>(
      this.apiService.routes.hostel.rooms(hostelId),
      { roomNumber, maxCapacity },
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  updateRoom(id: number, roomNumber: string, maxCapacity: number, isActive: boolean = true): Observable<ApiResponse<HostelRoom>> {
    return this.apiService.put<ApiResponse<HostelRoom>>(
      this.apiService.routes.hostel.updateRoom(id),
      { roomNumber, maxCapacity, isActive },
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  deleteRoom(id: number): Observable<ApiResponse<any>> {
    return this.apiService.delete<ApiResponse<any>>(
      this.apiService.routes.hostel.deleteRoom(id),
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }
}
