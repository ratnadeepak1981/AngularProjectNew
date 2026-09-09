import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../../shared/components/data-table/models/table-column.model';
import { LabBooking } from '../../../../../core/models/lab/lab-booking.model';

@Component({
  selector: 'app-lab-bookings-history',
  standalone: true,
  imports: [CommonModule, DataTableComponent],
  templateUrl: './lab-bookings-history.component.html',
  styleUrl: './lab-bookings-history.component.css',
})
export class LabBookingsHistoryComponent {
  @Input() bookings: LabBooking[] = [];
  @Input() loading: boolean = false;
  @Input() pageSize: number = 5;

  public readonly columns: TableColumn[] = [
    { key: 'id', header: 'Booking Ref', sortable: true, filterable: true, type: 'text', align: 'left', width: '90px' },
    { key: 'labName', header: 'Campus Laboratory', sortable: true, filterable: true, type: 'custom', align: 'left' },
    { key: 'studentName', header: 'Student Name', sortable: true, filterable: true, type: 'text', align: 'left' },
    { key: 'studentId', header: 'Student Index', sortable: true, filterable: true, type: 'text', align: 'left' },
    { key: 'seatNumber', header: 'Station / Seat ID', sortable: true, filterable: true, type: 'custom', align: 'left' },
    { key: 'bookingDate', header: 'Booking Date', sortable: true, filterable: true, type: 'text', align: 'left' },
    { key: 'timeSlot', header: 'Reservation Slot', sortable: true, filterable: true, type: 'text', align: 'left' },
    {
      key: 'status',
      header: 'Reservation Status',
      sortable: true,
      filterable: true,
      type: 'badge',
      align: 'left',
      badgeMap: {
        Confirmed: {
          label: '✓ Confirmed',
          class: 'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-50 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800',
        },
        Held: {
          label: '⏳ Held (Protected)',
          class: 'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800',
        },
        Pending: {
          label: '⏳ Pending',
          class: 'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800',
        },
        Cancelled: {
          label: '✕ Cancelled',
          class: 'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-rose-50 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800',
        },
      },
    },
  ];

  public isScienceLab(row: LabBooking): boolean {
    const type = (row.labType || '').toLowerCase();
    const name = (row.labName || '').toLowerCase();
    const seat = (row.seatNumber || '').toLowerCase();
    return type.includes('science') || name.includes('science') || seat.includes('bench') || seat === 'n/a';
  }
}
