import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../services/event.service';
import { CampusEvent } from '../../../../core/models/event/event.model';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { EventCardComponent } from '../../../../shared/components/cards/event-card/event-card.component';
import { EventCardModel } from '../../../../shared/components/cards/models/event-card.model';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { AlertModalComponent } from '../../../../shared/components/dialogs/alert-modal/alert-modal.component';

@Component({
  selector: 'app-student-event-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    EventCardComponent,
    ConfirmModalComponent,
    AlertModalComponent,
  ],
  templateUrl: './student-event-page.component.html',
  styleUrl: './student-event-page.component.css',
})
export class StudentEventPageComponent implements OnInit {
  private readonly eventService = inject(EventService);
  public readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);

  // Core Data Signals
  public readonly rawEvents = signal<CampusEvent[]>([]);
  public readonly isLoading = signal<boolean>(true);
  public readonly isActionProcessing = signal<boolean>(false);
  public readonly processingEventId = signal<number | null>(null);

  // Filter & Search Signals
  public readonly searchTerm = signal<string>('');
  public readonly activeFilter = signal<'all' | 'registered' | 'available'>('all');

  // Confirmation Modal State
  public readonly isConfirmOpen = signal<boolean>(false);
  public readonly confirmTitle = signal<string>('');
  public readonly confirmMessage = signal<string>('');
  public readonly confirmIcon = signal<string>('📅');
  public readonly confirmVariant = signal<'primary' | 'danger' | 'warning'>('primary');
  public readonly confirmButtonText = signal<string>('Confirm');
  public readonly confirmButtonIcon = signal<string>('✓');
  private pendingConfirmAction: (() => void) | null = null;

  // Alert Modal State
  public readonly isAlertOpen = signal<boolean>(false);
  public readonly alertTitle = signal<string>('');
  public readonly alertMessage = signal<string>('');
  public readonly alertIcon = signal<string>('⚠️');
  public readonly alertVariant = signal<'danger' | 'warning' | 'info' | 'success'>('warning');

  // ----------------------------------------------------
  // Computed Properties & Transformed Event Cards
  // ----------------------------------------------------
  public readonly currentStudentId = computed<number>(() => {
    const p = this.authService.userProfile();
    return p?.id || 0;
  });

  public readonly eventCards = computed<EventCardModel[]>(() => {
    const list = this.rawEvents();
    const studentId = this.currentStudentId();

    return list.map((e) => {
      const isReg =
        e.isRegistered === true ||
        (Array.isArray(e.registeredStudentIds) &&
          e.registeredStudentIds.some((id) => Number(id) === Number(studentId) && Number(studentId) > 0));

      const isFull = !isReg && e.capacity > 0 && e.registeredCount >= e.capacity;

      return {
        id: e.id,
        title: e.title,
        venueName: e.venueName || 'Main Campus Venue',
        startDateTime: e.startDateTime || '',
        endDateTime: e.endDateTime || '',
        capacity: e.capacity,
        registeredCount: e.registeredCount || e.currentAttendeesCount || 0,
        description: e.description,
        isRegistered: isReg,
        isFull: isFull,
      };
    });
  });

  public readonly totalRegisteredCount = computed<number>(() => {
    return this.eventCards().filter((e) => e.isRegistered).length;
  });

  public readonly totalAvailableCount = computed<number>(() => {
    return this.eventCards().filter((e) => !e.isRegistered && !e.isFull).length;
  });

  public readonly filteredEvents = computed<EventCardModel[]>(() => {
    let list = this.eventCards();
    const search = this.searchTerm().toLowerCase().trim();
    const tab = this.activeFilter();

    // 1. Apply Search
    if (search) {
      list = list.filter(
        (e) =>
          e.title.toLowerCase().includes(search) ||
          e.venueName.toLowerCase().includes(search) ||
          (e.description && e.description.toLowerCase().includes(search))
      );
    }

    // 2. Apply Tab Filter
    if (tab === 'registered') {
      list = list.filter((e) => e.isRegistered);
    } else if (tab === 'available') {
      list = list.filter((e) => !e.isRegistered && !e.isFull);
    }

    return list;
  });

  // ----------------------------------------------------
  // Lifecycle & Data Fetching
  // ----------------------------------------------------
  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(): void {
    this.isLoading.set(true);
    this.eventService.getFormattedEvents().subscribe({
      next: (events) => {
        this.rawEvents.set(events);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.toast.error(err?.error?.message || err?.message || 'Failed to load upcoming events.');
      },
    });
  }

  // ----------------------------------------------------
  // Event Registration Workflow
  // ----------------------------------------------------
  promptRegisterEvent(eventId: number): void {
    const selected = this.eventCards().find((e) => e.id === eventId);
    if (!selected) return;

    if (selected.isFull) {
      this.showAlert(
        'Event Fully Booked [BRD Rule #7]',
        `"${selected.title}" has reached its maximum seating capacity limit of ${selected.capacity} students. No further seats are available.`,
        '🚫',
        'warning'
      );
      return;
    }

    this.confirmTitle.set('Confirm Event Registration');
    this.confirmMessage.set(
      `Are you sure you want to register for "${selected.title}"? A confirmed seat will be allocated for you at "${selected.venueName}".`
    );
    this.confirmIcon.set('📅');
    this.confirmVariant.set('primary');
    this.confirmButtonText.set('Register for Event');
    this.confirmButtonIcon.set('📅');

    this.pendingConfirmAction = () => {
      this.isActionProcessing.set(true);
      this.processingEventId.set(eventId);

      this.eventService.registerForEvent(eventId).subscribe({
        next: () => {
          this.toast.success(`Successfully registered for "${selected.title}"! Your seat is secured.`);
          this.isActionProcessing.set(false);
          this.processingEventId.set(null);
          this.loadEvents();
        },
        error: (err) => {
          this.isActionProcessing.set(false);
          this.processingEventId.set(null);
          const msg = err?.error?.message || err?.message || 'Failed to register for event.';

          if (err.status === 409 || msg.toLowerCase().includes('capacity') || msg.toLowerCase().includes('full')) {
            this.showAlert(
              'Registration Full [BRD Rule #7]',
              `Registration Conflict: This event has reached its maximum seating capacity limits.`,
              '🚫',
              'danger'
            );
          } else if (msg.toLowerCase().includes('already registered') || msg.toLowerCase().includes('duplicate')) {
            this.showAlert(
              'Duplicate Registration [BRD Rule #1]',
              'You are already registered for this university campus event.',
              'ℹ️',
              'info'
            );
          } else {
            this.showAlert('Registration Error', msg, '⚠️', 'danger');
          }
          this.loadEvents();
        },
      });
    };

    this.isConfirmOpen.set(true);
  }

  // ----------------------------------------------------
  // Event Cancellation Workflow
  // ----------------------------------------------------
  promptCancelRegistration(eventId: number): void {
    const selected = this.eventCards().find((e) => e.id === eventId);
    if (!selected) return;

    this.confirmTitle.set('Cancel Event Registration');
    this.confirmMessage.set(
      `Are you sure you want to cancel your registration for "${selected.title}"? Your allocated seat will be released immediately.`
    );
    this.confirmIcon.set('⚠️');
    this.confirmVariant.set('danger');
    this.confirmButtonText.set('Cancel Registration');
    this.confirmButtonIcon.set('✕');

    this.pendingConfirmAction = () => {
      this.isActionProcessing.set(true);
      this.processingEventId.set(eventId);

      this.eventService.cancelRegistration(eventId).subscribe({
        next: () => {
          this.toast.success(`Your registration for "${selected.title}" has been cancelled. Seat released.`);
          this.isActionProcessing.set(false);
          this.processingEventId.set(null);
          this.loadEvents();
        },
        error: (err) => {
          this.isActionProcessing.set(false);
          this.processingEventId.set(null);
          this.toast.error(err?.error?.message || err?.message || 'Failed to cancel event registration.');
          this.loadEvents();
        },
      });
    };

    this.isConfirmOpen.set(true);
  }

  // ----------------------------------------------------
  // Modal Actions
  // ----------------------------------------------------
  onConfirmAction(): void {
    this.isConfirmOpen.set(false);
    if (this.pendingConfirmAction) {
      this.pendingConfirmAction();
      this.pendingConfirmAction = null;
    }
  }

  onCancelConfirm(): void {
    this.isConfirmOpen.set(false);
    this.pendingConfirmAction = null;
  }

  showAlert(
    title: string,
    message: string,
    icon: string = '⚠️',
    variant: 'danger' | 'warning' | 'info' | 'success' = 'warning'
  ): void {
    this.alertTitle.set(title);
    this.alertMessage.set(message);
    this.alertIcon.set(icon);
    this.alertVariant.set(variant);
    this.isAlertOpen.set(true);
  }

  closeAlert(): void {
    this.isAlertOpen.set(false);
  }
}
