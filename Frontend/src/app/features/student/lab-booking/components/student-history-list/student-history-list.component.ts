import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../../shared/components/data-table/models/table-column.model';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { ActionButtonComponent } from '../../../../../shared/components/action-button/action-button.component';
import { LabBooking } from '../../../../../core/models/lab/lab-booking.model';

@Component({
  selector: 'app-student-history-list',
  standalone: true,
  imports: [CommonModule, DataTableComponent, StatusBadgeComponent, ActionButtonComponent],
  templateUrl: './student-history-list.component.html',
  styleUrl: './student-history-list.component.css',
})
export class StudentHistoryListComponent {
  public readonly bookings = input<LabBooking[]>([]);
  public readonly isLoading = input<boolean>(false);

  public readonly cancelBooking = output<LabBooking>();
  public readonly refresh = output<void>();

  public readonly bookingColumns: TableColumn<LabBooking>[] = [
    { key: 'labName', header: 'Laboratory', sortable: true, filterable: true, type: 'custom' },
    { key: 'seatNumber', header: 'Workstation Seat / Slot', sortable: true, filterable: true, type: 'custom' },
    { key: 'bookingDate', header: 'Booking Date', sortable: true, filterable: true },
    { key: 'timeSlot', header: 'Session Time Slot', sortable: true, filterable: true },
    { key: 'status', header: 'Status', sortable: true, filterable: true, type: 'badge' },
    { key: 'actions', header: 'Actions', sortable: false, filterable: false, type: 'actions', align: 'right' },
  ];

  public isScienceLab(row: LabBooking): boolean {
    const type = (row.labType || '').toLowerCase();
    const name = (row.labName || '').toLowerCase();
    const seat = (row.seatNumber || '').toLowerCase();
    return type.includes('science') || name.includes('science') || seat.includes('bench') || seat === 'n/a';
  }

  public onCancelClick(booking: LabBooking): void {
    this.cancelBooking.emit(booking);
  }

  public onRefreshClick(): void {
    this.refresh.emit();
  }
}
