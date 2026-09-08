import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { TabComponent, TabItem } from '../../../../shared/components/tab-component/tab.component';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { DatePickerComponent } from '../../../../shared/components/date-picker/date-picker.component';
import { ToastService } from '../../../../core/services/toast.service';
import { SystemSettingsService } from '../../../../core/services/system-settings.service';

import { Lab } from '../../../../core/models/lab/lab.model';
import { LabSeat } from '../../../../core/models/lab/lab-seat.model';
import { LabBookingRecord, LabManagementService } from '../services/lab-management.service';
import { LabDetailsComponent } from '../components/lab-details/lab-details.component';
import { LabGridMatrixComponent } from '../../../lab-shared/components/lab-grid-matrix/lab-grid-matrix.component';
import { BookingSelectorsComponent } from '../../../lab-shared/components/booking-selectors/booking-selectors.component';
import { LabBookingsHistoryComponent } from '../components/lab-bookings-history/lab-bookings-history.component';

@Component({
  selector: 'app-lab-management-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    TabComponent,
    ActionButtonComponent,
    DatePickerComponent,
    LabDetailsComponent,
    LabGridMatrixComponent,
    BookingSelectorsComponent,
    LabBookingsHistoryComponent,
  ],
  templateUrl: './lab-management-page.component.html',
  styleUrl: './lab-management-page.component.css',
})
export class LabManagementPageComponent implements OnInit {
  private readonly labService = inject(LabManagementService);
  private readonly settingsService = inject(SystemSettingsService);
  private readonly toast = inject(ToastService);

  // State Signals
  public readonly labs = signal<Lab[]>([]);
  public readonly selectedLab = signal<Lab | null>(null);
  public readonly seats = signal<LabSeat[]>([]);
  public readonly bookingsHistory = signal<LabBookingRecord[]>([]);
  public readonly isLoading = signal(false);
  public readonly pageSize = signal<number>(5);

  // Active Tab State
  public readonly activeTabId = signal<string>('general-layout');

  // Tab Definition Items
  public readonly tabs: TabItem[] = [
    { id: 'general-layout', label: 'General Lab Layout', icon: '🗺️' },
    { id: 'datewise-layout', label: 'Date-wise Lab Layout', icon: '📅' },
    { id: 'bookings-history', label: 'Booking History', icon: '📋' },
  ];

  // Zoom State Signals (Tab 1 and Tab 2)
  public readonly generalLayoutZoom = signal<number>(100);
  public readonly datewiseLayoutZoom = signal<number>(100);

  // Date-wise Layout State
  public readonly datewiseDate = signal<string>('');
  public readonly datewiseSeats = signal<LabSeat[]>([]);
  public readonly datewiseSelectedLab = signal<Lab | null>(null);
  public readonly datewiseTimeSlot = signal<string>('09:00 - 11:00 AM');

  ngOnInit(): void {
    this.settingsService.getAllSettings().subscribe({
      next: (res) => {
        const dict = (res as any)?.data || res;
        if (dict) {
          const raw = dict['DefaultPageSize'] || dict['PageSize'] || dict['AdminDefaultPageSize'];
          if (raw) {
            const parsed = parseInt(raw, 10);
            if (!isNaN(parsed) && parsed > 0) {
              this.pageSize.set(parsed);
            }
          }
        }
      },
      error: () => {
        this.pageSize.set(5);
      },
    });
    this.loadLabs();
    this.loadBookings();
  }

  loadLabs(): void {
    this.isLoading.set(true);
    this.labService.getLabs().subscribe((data) => {
      this.labs.set(data);
      this.isLoading.set(false);
      if (data.length > 0 && !this.selectedLab()) {
        this.selectLab(data[0]);
      }
    });
  }

  loadBookings(): void {
    this.labService.getBookingsHistory().subscribe((data) => {
      this.bookingsHistory.set(data);
    });
  }

  selectLab(lab: Lab): void {
    this.selectedLab.set(lab);
    this.loadLabMatrix(lab.id);
  }

  loadLabMatrix(labId: number): void {
    this.labService.getLabLayout(labId).subscribe((layout) => {
      this.seats.set(layout.seats);
      const current = this.selectedLab();
      if (current && current.id === labId) {
        this.selectedLab.set({
          ...current,
          totalRows: layout.totalRows,
          totalColumns: layout.totalColumns,
        });
      }
    });
  }

  // Modal State Signals
  public readonly isSpecsModalOpen = signal(false);
  public readonly isGridModalOpen = signal(false);

  openSpecsModal(lab: Lab): void {
    this.selectLab(lab);
    this.isSpecsModalOpen.set(true);
  }

  closeSpecsModal(): void {
    this.isSpecsModalOpen.set(false);
  }

  openGridModal(lab: Lab): void {
    this.selectLab(lab);
    this.isGridModalOpen.set(true);
  }

  closeGridModal(): void {
    this.isGridModalOpen.set(false);
  }

  inspectLabLayout(lab: Lab): void {
    this.openGridModal(lab);
  }

  onTabChange(tabId: string): void {
    this.activeTabId.set(tabId);
  }

  onAddSeat(event: { seatNumber: string; row: number; col: number }): void {
    const lab = this.selectedLab();
    if (!lab) return;

    this.labService.addSeat(lab.id, event.seatNumber, event.row, event.col).subscribe((success) => {
      if (success) {
        this.toast.success(`Workstation ${event.seatNumber} registered at Cell Address (${event.row}, ${event.col}).`);
        this.loadLabMatrix(lab.id);
        this.loadLabs();
      } else {
        this.toast.error('Unable to add workstation seat.');
      }
    });
  }

  onRemoveSeat(seatId: number): void {
    const lab = this.selectedLab();
    if (!lab) return;

    this.labService.removeSeat(lab.id, seatId).subscribe((success) => {
      if (success) {
        this.toast.success('Workstation seat deactivated.');
        this.loadLabMatrix(lab.id);
        this.loadLabs();
      } else {
        this.toast.error('Unable to deactivate seat. Please verify active bookings.');
      }
    });
  }

  onCreateLabSubmitted(data: { name: string; labType: string; capacity: number; totalRows: number; totalColumns: number }): void {
    this.labService.createLab(data).subscribe((success) => {
      if (success) {
        this.toast.success(`Laboratory profile '${data.name}' created successfully.`);
        this.loadLabs();
      } else {
        this.toast.error('Failed to create laboratory profile.');
      }
    });
  }

  // --- Zoom Controls ---

  zoomIn(tab: 'general' | 'datewise'): void {
    const current = tab === 'general' ? this.generalLayoutZoom() : this.datewiseLayoutZoom();
    const next = Math.min(current + 10, 200);
    if (tab === 'general') {
      this.generalLayoutZoom.set(next);
    } else {
      this.datewiseLayoutZoom.set(next);
    }
  }

  zoomOut(tab: 'general' | 'datewise'): void {
    const current = tab === 'general' ? this.generalLayoutZoom() : this.datewiseLayoutZoom();
    const next = Math.max(current - 10, 50);
    if (tab === 'general') {
      this.generalLayoutZoom.set(next);
    } else {
      this.datewiseLayoutZoom.set(next);
    }
  }

  resetZoom(tab: 'general' | 'datewise'): void {
    if (tab === 'general') {
      this.generalLayoutZoom.set(100);
    } else {
      this.datewiseLayoutZoom.set(100);
    }
  }

  // --- Date-wise Layout ---

  onLabSelectForDatewise(labId: number): void {
    const lab = this.labs().find((l) => l.id === labId);
    if (lab) {
      this.datewiseSelectedLab.set(lab);
      if (this.datewiseDate()) {
        this.loadDatewiseLayout(lab.id, this.datewiseDate(), this.datewiseTimeSlot());
      }
    }
  }

  onDatewiseDateChange(date: string): void {
    this.datewiseDate.set(date);
    const lab = this.datewiseSelectedLab();
    if (lab && date) {
      this.loadDatewiseLayout(lab.id, date, this.datewiseTimeSlot());
    }
  }

  onDatewiseTimeSlotChange(slot: string): void {
    this.datewiseTimeSlot.set(slot);
    const lab = this.datewiseSelectedLab();
    if (lab && this.datewiseDate()) {
      this.loadDatewiseLayout(lab.id, this.datewiseDate(), slot);
    }
  }

  onDatewiseRefresh(): void {
    const lab = this.datewiseSelectedLab();
    if (lab && this.datewiseDate()) {
      this.loadDatewiseLayout(lab.id, this.datewiseDate(), this.datewiseTimeSlot());
    }
  }

  loadDatewiseLayout(labId: number, date: string, timeSlot: string = '09:00 - 11:00 AM'): void {
    this.labService.getLabLayoutForDate(labId, date, timeSlot).subscribe((layout) => {
      this.datewiseSeats.set(layout.seats);
      const current = this.datewiseSelectedLab();
      if (current && current.id === labId) {
        this.datewiseSelectedLab.set({
          ...current,
          totalRows: layout.totalRows,
          totalColumns: layout.totalColumns,
        });
      }
    });
  }
}
