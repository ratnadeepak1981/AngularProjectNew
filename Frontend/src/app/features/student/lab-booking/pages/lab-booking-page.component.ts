import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { TabComponent } from '../../../../shared/components/tab-component/tab.component';
import { TabItem } from '../../../../shared/components/tab-component/models/tab-item.model';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { ToastContainerComponent } from '../../../../shared/components/toast-container/toast-container.component';
import { DashboardCardComponent } from '../../../../shared/components/cards/dashboard-card/dashboard-card.component';

import { BookingSelectorsComponent } from '../components/booking-selectors/booking-selectors.component';
import { ActiveHoldTimerComponent } from '../components/active-hold-timer/active-hold-timer.component';
import { StudentHistoryListComponent } from '../components/student-history-list/student-history-list.component';
import { LabGridMatrixComponent } from '../../../lab-shared/components/lab-grid-matrix/lab-grid-matrix.component';
import { LabBookingService } from '../services/lab-booking.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';

import { Lab } from '../../../../core/models/lab/lab.model';
import { LabSeat } from '../../../../core/models/lab/lab-seat.model';
import { LabBooking } from '../../../../core/models/lab/lab-booking.model';

@Component({
  selector: 'app-lab-booking-page',
  standalone: true,
  imports: [
    CommonModule,
    PageHeaderComponent,
    TabComponent,
    StatusBadgeComponent,
    ConfirmModalComponent,
    ToastContainerComponent,
    DashboardCardComponent,
    BookingSelectorsComponent,
    ActiveHoldTimerComponent,
    StudentHistoryListComponent,
    LabGridMatrixComponent,
  ],
  templateUrl: './lab-booking-page.component.html',
  styleUrl: './lab-booking-page.component.css',
})
export class LabBookingPageComponent implements OnInit {
  private readonly labBookingService = inject(LabBookingService);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);

  // Active User Context
  public readonly currentStudentId = signal<number>(1);

  // Tab State
  public readonly activeTabId = signal<string>('booking');
  public readonly sectionTabs: TabItem[] = [
    { id: 'booking', label: 'Book Workstation Seat', icon: '💻' },
    { id: 'history', label: 'My Booking History', icon: '📋' },
  ];

  // Selection Controls State
  public readonly labs = signal<Lab[]>([]);
  public readonly selectedLabId = signal<number>(0);
  public readonly selectedDate = signal<string>(new Date().toISOString().split('T')[0]);
  public readonly selectedTimeSlot = signal<string>('09:00 - 11:00 AM');

  // Matrix Layout & Seat State
  public readonly isLoadingLabs = signal<boolean>(false);
  public readonly isLoadingLayout = signal<boolean>(false);
  public readonly layoutTotalRows = signal<number>(4);
  public readonly layoutTotalColumns = signal<number>(3);
  public readonly layoutSeats = signal<LabSeat[]>([]);
  public readonly selectedSeat = signal<LabSeat | null>(null);
  public readonly isMatrixModalOpen = signal<boolean>(false);

  // Active Hold & Confirmation State
  public readonly maxDailySlots = signal<number>(2);
  public readonly systemHoldMinutes = signal<number>(15);
  public readonly activeHoldBooking = signal<LabBooking | null>(null);
  public readonly myBookingsHistory = signal<LabBooking[]>([]);
  public readonly isLoadingHistory = signal<boolean>(false);
  public readonly isSubmitting = signal<boolean>(false);

  // Confirm Modal Dialog Controls
  public readonly isConfirmModalOpen = signal<boolean>(false);
  public readonly modalTitle = signal<string>('Confirm Action');
  public readonly modalMessage = signal<string>('');
  public readonly modalIcon = signal<string>('🛡️');
  public readonly modalIconVariant = signal<'danger' | 'warning' | 'primary' | 'info'>('primary');
  public readonly modalConfirmText = signal<string>('Proceed');
  public readonly modalConfirmIcon = signal<string>('✅');
  public readonly modalConfirmVariant = signal<'danger' | 'primary' | 'warning'>('primary');
  public modalActionType: 'hold' | 'confirmHold' | 'cancelHold' | 'cancelHistoryBooking' | 'confirmScienceBooking' = 'hold';
  public targetBookingIdToCancel: number | null = null;

  // Computed Helpers
  public readonly selectedLab = computed<Lab | undefined>(() => {
    return this.labs().find((l) => l.id === this.selectedLabId());
  });

  public readonly totalSeatsCount = computed<number>(() => {
    return this.layoutSeats().length;
  });

  public readonly availableSeatsCount = computed<number>(() => {
    return this.layoutSeats().filter((s) => s.status === 'Available').length;
  });

  public readonly heldSeatsCount = computed<number>(() => {
    return this.layoutSeats().filter((s) => s.status === 'Held').length;
  });

  public readonly occupiedSeatsCount = computed<number>(() => {
    return this.layoutSeats().filter((s) => s.status === 'Occupied').length;
  });

  public readonly brokenSeatsCount = computed<number>(() => {
    return this.layoutSeats().filter((s) => s.status === 'Broken' || s.isBroken).length;
  });

  public readonly effectiveTotalCapacity = computed<number>(() => {
    const lab = this.selectedLab();
    if (!lab) return 0;
    return lab.capacity || this.totalSeatsCount();
  });

  public readonly effectiveAvailableCount = computed<number>(() => {
    if (this.isComputerLab()) {
      return this.availableSeatsCount();
    }
    const cap = this.effectiveTotalCapacity();
    const activeBooked = this.myBookingsHistory().filter(
      (b: LabBooking) => b.labId === this.selectedLabId() && b.bookingDate === this.selectedDate() && b.timeSlot === this.selectedTimeSlot() && b.status !== 'Cancelled'
    ).length;
    return Math.max(0, cap - activeBooked);
  });

  public readonly effectiveOccupiedCount = computed<number>(() => {
    if (this.isComputerLab()) {
      return this.occupiedSeatsCount();
    }
    return this.myBookingsHistory().filter(
      (b: LabBooking) => b.labId === this.selectedLabId() && b.bookingDate === this.selectedDate() && b.timeSlot === this.selectedTimeSlot() && b.status === 'Confirmed'
    ).length;
  });

  public readonly isComputerLab = computed<boolean>(() => {
    const lab = this.selectedLab();
    if (!lab) return true;
    return lab.requiresSeatSelection ?? (lab.labType === 'Computer' || lab.labType === 'computer');
  });

  ngOnInit(): void {
    const user = this.authService.userProfile();
    if (user?.id) {
      this.currentStudentId.set(user.id);
    }

    this.loadSystemSettings();
    this.loadLabs();
    this.loadMyBookings();
  }

  public onTabChange(tabId: string): void {
    this.activeTabId.set(tabId);
    if (tabId === 'history') {
      this.loadMyBookings();
    }
  }

  private loadSystemSettings(): void {
    this.labBookingService.getSystemHoldMinutes().subscribe((mins) => {
      this.systemHoldMinutes.set(mins || 15);
    });
    this.labBookingService.getSystemSettings().subscribe((dict) => {
      if (dict['MaxDailySlots']) {
        const slots = parseInt(dict['MaxDailySlots'], 10);
        if (slots > 0) this.maxDailySlots.set(slots);
      }
      if (dict['LabBookingHoldMinutes']) {
        const mins = parseInt(dict['LabBookingHoldMinutes'], 10);
        if (mins > 0) this.systemHoldMinutes.set(mins);
      }
    });
  }

  public loadLabs(): void {
    this.isLoadingLabs.set(true);
    this.labBookingService.getLabs().subscribe({
      next: (data) => {
        this.labs.set(data);
        this.isLoadingLabs.set(false);

        if (data.length > 0 && (!this.selectedLabId() || !data.some((l) => l.id === this.selectedLabId()))) {
          this.selectedLabId.set(data[0].id);
          this.loadLayout();
        }
      },
      error: () => this.isLoadingLabs.set(false),
    });
  }

  public onLabChange(labId: number): void {
    this.selectedLabId.set(labId);
    this.selectedSeat.set(null);
    this.loadLayout();
  }

  public onDateChange(dateStr: string): void {
    this.selectedDate.set(dateStr);
    this.selectedSeat.set(null);
    this.loadLayout();
  }

  public onTimeSlotChange(slotStr: string): void {
    this.selectedTimeSlot.set(slotStr);
    this.selectedSeat.set(null);
    this.loadLayout();
  }

  public loadLayout(): void {
    const labId = this.selectedLabId();
    const date = this.selectedDate();
    const slot = this.selectedTimeSlot();

    if (!labId || !date || !slot) return;

    this.isLoadingLayout.set(true);
    this.labBookingService.getLabLayout(labId, date, slot).subscribe({
      next: (res) => {
        this.layoutTotalRows.set(res.totalRows);
        this.layoutTotalColumns.set(res.totalColumns);
        this.layoutSeats.set(res.seats);
        this.isLoadingLayout.set(false);
      },
      error: () => this.isLoadingLayout.set(false),
    });
  }

  public loadMyBookings(): void {
    this.isLoadingHistory.set(true);
    this.labBookingService.getMyBookings(this.currentStudentId()).subscribe({
      next: (bookings) => {
        this.myBookingsHistory.set(bookings);
        this.isLoadingHistory.set(false);

        // Check if there is an active valid held booking for this student
        const now = Date.now();
        const parseUtcTime = (iso: string) => {
          if (!iso) return 0;
          const formatted = iso.endsWith('Z') ? iso : `${iso}Z`;
          return new Date(formatted).getTime();
        };

        const activeHold = bookings.find((b) => {
          if (b.status !== 'Held') return false;
          if (!b.expiresAt) return true;
          return parseUtcTime(b.expiresAt) > now;
        });

        if (activeHold) {
          if (!activeHold.seatNumber || activeHold.seatNumber === 'N/A') {
            const current = this.selectedSeat()?.seatNumber;
            if (current) activeHold.seatNumber = current;
          }
          this.activeHoldBooking.set(activeHold);
        } else {
          this.activeHoldBooking.set(null);
        }
      },
      error: () => this.isLoadingHistory.set(false),
    });
  }

  public onHoldTimerExpired(): void {
    const active = this.activeHoldBooking();
    if (!active) return;
    this.toast.info('Reservation hold lock expired. Seat returned to pool.');
    this.executeCancelHold();
  }

  public openMatrixModal(): void {
    if (this.layoutSeats().length === 0) {
      this.toast.warning('No seats configured for this laboratory yet.');
      return;
    }
    this.isMatrixModalOpen.set(true);
  }

  public closeMatrixModal(): void {
    this.isMatrixModalOpen.set(false);
  }

  public onSeatSelectedFromMatrix(seat: LabSeat): void {
    this.isMatrixModalOpen.set(false);
    this.onSeatClick(seat);
  }

  // Seat Click Handler
  public onSeatClick(seat: LabSeat): void {
    if (seat.isBroken || seat.status === 'Broken') {
      this.toast.error(`Workstation ${seat.seatNumber} is marked out-of-order for maintenance.`);
      return;
    }
    if (seat.status !== 'Available') {
      this.toast.warning(`Workstation ${seat.seatNumber} is currently ${seat.status.toLowerCase()}.`);
      return;
    }

    // Daily Limit Check: Dynamically bound from System Settings
    const sameDateBookings = this.myBookingsHistory().filter(
      (b) => b.bookingDate === this.selectedDate() && (b.status === 'Confirmed' || b.status === 'Held')
    );
    if (sameDateBookings.length >= this.maxDailySlots()) {
      this.toast.warning(
        `Daily Limit Reached: Maximum ${this.maxDailySlots()} slots (${this.maxDailySlots() * 2} hours total) allowed per student on ${this.selectedDate()}.`
      );
      return;
    }

    this.selectedSeat.set(seat);
    this.promptHoldConfirmation(seat);
  }

  // 1. Prompt Hold Confirmation Modal
  public promptHoldConfirmation(seat: LabSeat): void {
    const labName = this.selectedLab()?.name || 'Selected Laboratory';
    this.modalActionType = 'hold';
    this.modalTitle.set('Confirm Reservation Hold');
    this.modalMessage.set(
      `Do you want to place a ${this.systemHoldMinutes()}-minute temporary hold on Workstation Seat ${seat.seatNumber} in ${labName} for date ${this.selectedDate()} (${this.selectedTimeSlot()})?`
    );
    this.modalIcon.set('⚡');
    this.modalIconVariant.set('primary');
    this.modalConfirmText.set(`Reserve ${this.systemHoldMinutes()}-Min Hold`);
    this.modalConfirmIcon.set('⚡');
    this.modalConfirmVariant.set('primary');
    this.isConfirmModalOpen.set(true);
  }

  // 2. Prompt Confirm Booking Modal
  public promptConfirmBookingModal(): void {
    const active = this.activeHoldBooking();
    if (!active) return;

    this.modalActionType = 'confirmHold';
    this.modalTitle.set('Confirm Workstation Booking');
    this.modalMessage.set(
      `Are you sure you want to finalize and lock in your reservation for ${active.seatNumber || 'Selected Seat'} on ${active.bookingDate} (${active.timeSlot})?`
    );
    this.modalIcon.set('✅');
    this.modalIconVariant.set('primary');
    this.modalConfirmText.set('Confirm Booking');
    this.modalConfirmIcon.set('✅');
    this.modalConfirmVariant.set('primary');
    this.isConfirmModalOpen.set(true);
  }

  // 3. Prompt Cancel Hold Modal
  public promptCancelHoldModal(): void {
    this.modalActionType = 'cancelHold';
    this.modalTitle.set('Release Reservation Hold');
    this.modalMessage.set('Are you sure you want to release your active 15-minute hold and return the seat back to the pool?');
    this.modalIcon.set('🚫');
    this.modalIconVariant.set('warning');
    this.modalConfirmText.set('Release Hold');
    this.modalConfirmIcon.set('🚫');
    this.modalConfirmVariant.set('warning');
    this.isConfirmModalOpen.set(true);
  }

  // 4. Prompt Cancel History Booking Modal
  public promptCancelHistoryBookingModal(booking: LabBooking): void {
    this.targetBookingIdToCancel = booking.id;
    this.modalActionType = 'cancelHistoryBooking';
    this.modalTitle.set('Cancel Workstation Booking');
    this.modalMessage.set(
      `Are you sure you want to cancel your confirmed reservation for ${booking.labName} (${booking.seatNumber}) on ${booking.bookingDate}?`
    );
    this.modalIcon.set('🗑️');
    this.modalIconVariant.set('danger');
    this.modalConfirmText.set('Cancel Booking');
    this.modalConfirmIcon.set('🗑️');
    this.modalConfirmVariant.set('danger');
    this.isConfirmModalOpen.set(true);
  }

  // Execute Modal Confirmation
  public onModalConfirm(): void {
    if (this.isSubmitting()) return;
    this.isConfirmModalOpen.set(false);

    if (this.modalActionType === 'hold') {
      this.executeCreateHold();
    } else if (this.modalActionType === 'confirmHold') {
      this.executeConfirmBooking();
    } else if (this.modalActionType === 'confirmScienceBooking') {
      this.executeConfirmScienceBooking();
    } else if (this.modalActionType === 'cancelHold') {
      this.executeCancelHold();
    } else if (this.modalActionType === 'cancelHistoryBooking') {
      this.executeCancelHistoryBooking();
    }
  }

  public onModalCancel(): void {
    this.isConfirmModalOpen.set(false);
  }

  private executeCreateHold(): void {
    const seat = this.selectedSeat();
    const labId = this.selectedLabId();
    const isComp = this.isComputerLab();

    if (isComp && !seat) {
      this.toast.warning('Please select an available workstation seat first.');
      return;
    }

    this.isSubmitting.set(true);
    this.labBookingService
      .createHold({
        labId: labId,
        studentId: this.currentStudentId(),
        seatId: isComp ? seat?.id : undefined,
        bookingDate: this.selectedDate(),
        timeSlot: this.selectedTimeSlot(),
      })
      .subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.success && res.data) {
            if (seat?.seatNumber && (!res.data.seatNumber || res.data.seatNumber === 'N/A')) {
              res.data.seatNumber = seat.seatNumber;
            }
            this.activeHoldBooking.set(res.data);
            this.toast.success(
              `Temporary ${this.systemHoldMinutes()}-minute reservation hold placed on Workstation ${seat?.seatNumber || 'Bench Slot'}!`
            );
            this.loadLayout();
            this.loadMyBookings();
          } else {
            this.toast.error(res.message || 'Failed to place reservation hold.');
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.loadLayout();
          this.loadMyBookings();
        },
      });
  }

  private executeConfirmBooking(): void {
    const active = this.activeHoldBooking();
    if (!active) return;

    this.isSubmitting.set(true);
    this.labBookingService.confirmBooking(active.id).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.success) {
          this.toast.success(`Workstation reservation confirmed successfully for ${active.seatNumber || 'Selected Seat'}!`);
          this.activeHoldBooking.set(null);
          this.selectedSeat.set(null);
          this.loadLayout();
          this.loadMyBookings();
        } else {
          this.toast.error(res.message || 'Confirmation failed or lock window expired.');
          this.loadLayout();
          this.loadMyBookings();
        }
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.loadLayout();
        this.loadMyBookings();
      },
    });
  }

  private executeCancelHold(): void {
    const active = this.activeHoldBooking();
    if (!active) return;

    this.isSubmitting.set(true);
    this.labBookingService.cancelBooking(active.id, this.currentStudentId()).subscribe({
      next: (ok) => {
        this.isSubmitting.set(false);
        if (ok) {
          this.toast.success('Reservation hold released successfully.');
          this.activeHoldBooking.set(null);
          this.selectedSeat.set(null);
          this.loadLayout();
          this.loadMyBookings();
        } else {
          this.toast.error('Failed to release hold.');
        }
      },
      error: () => this.isSubmitting.set(false),
    });
  }

  private executeCancelHistoryBooking(): void {
    if (!this.targetBookingIdToCancel) return;

    this.isSubmitting.set(true);
    this.labBookingService.cancelBooking(this.targetBookingIdToCancel, this.currentStudentId()).subscribe({
      next: (ok) => {
        this.isSubmitting.set(false);
        if (ok) {
          this.toast.success('Workstation booking cancelled successfully.');
          this.loadLayout();
          this.loadMyBookings();
        } else {
          this.toast.error('Failed to cancel booking.');
        }
      },
      error: () => this.isSubmitting.set(false),
    });
  }

  // Science Lab Direct Slot Confirmation (No 15-Minute Hold Timer Required)
  public onScienceSlotSelect(slotIndex: number): void {
    // Daily Limit Check: Dynamically bound from System Settings
    const sameDateBookings = this.myBookingsHistory().filter(
      (b) => b.bookingDate === this.selectedDate() && (b.status === 'Confirmed' || b.status === 'Held')
    );
    if (sameDateBookings.length >= this.maxDailySlots()) {
      this.toast.warning(
        `Daily Limit Reached: Maximum ${this.maxDailySlots()} slots (${this.maxDailySlots() * 2} hours total) allowed per student on ${this.selectedDate()}.`
      );
      return;
    }

    const labName = this.selectedLab()?.name || 'Science Laboratory';
    this.modalActionType = 'confirmScienceBooking';
    this.modalTitle.set('Confirm Science Lab Slot Reservation');
    this.modalMessage.set(
      `Do you want to confirm a session slot reservation in ${labName} for date ${this.selectedDate()} (${this.selectedTimeSlot()})?`
    );
    this.modalIcon.set('⚡');
    this.modalIconVariant.set('primary');
    this.modalConfirmText.set('Confirm Reservation');
    this.modalConfirmIcon.set('✅');
    this.modalConfirmVariant.set('primary');
    this.isConfirmModalOpen.set(true);
  }

  private executeConfirmScienceBooking(): void {
    const labId = this.selectedLabId();
    this.isSubmitting.set(true);

    this.labBookingService
      .createHold({
        labId: labId,
        studentId: this.currentStudentId(),
        bookingDate: this.selectedDate(),
        timeSlot: this.selectedTimeSlot(),
      })
      .subscribe({
        next: (res) => {
          if (res.success && res.data?.id) {
            // Directly confirm the hold immediately for Science Labs
            this.labBookingService.confirmHold(res.data.id, this.currentStudentId()).subscribe({
              next: (confirmRes) => {
                this.isSubmitting.set(false);
                if (confirmRes.success) {
                  this.toast.success('Science laboratory slot reservation confirmed successfully!');
                  this.loadLayout();
                  this.loadMyBookings();
                } else {
                  this.toast.error(confirmRes.message || 'Failed to confirm reservation.');
                }
              },
              error: () => this.isSubmitting.set(false),
            });
          } else {
            this.isSubmitting.set(false);
            this.toast.error(res.message || 'Failed to place slot reservation.');
          }
        },
        error: () => this.isSubmitting.set(false),
      });
  }
}
