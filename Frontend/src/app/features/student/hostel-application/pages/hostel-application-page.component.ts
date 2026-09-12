import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HostelApplicationService } from '../services/hostel-application.service';
import { SystemSettingsService } from '../../../../core/services/system-settings.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { HousingApplication } from '../../../../core/models/hostel/hostel-application.model';

@Component({
  selector: 'app-hostel-application-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ConfirmModalComponent, PageHeaderComponent],
  templateUrl: './hostel-application-page.component.html',
  styleUrl: './hostel-application-page.component.css',
})
export class HostelApplicationPageComponent implements OnInit {
  private readonly hostelService = inject(HostelApplicationService);
  private readonly systemSettingsService = inject(SystemSettingsService);
  private readonly toast = inject(ToastService);
  public readonly authService = inject(AuthService);

  // Data State Signals
  public readonly hostelsList = signal<any[]>([]);
  public readonly myApplications = signal<HousingApplication[]>([]);
  public readonly isLoading = signal<boolean>(false);
  public readonly isSubmitting = signal<boolean>(false);

  // Dynamic System Settings Form State Signals
  public readonly selectedHostelId = signal<number>(0);
  public readonly termSemester = signal<string>('2025/2026 / Semester 1');
  public readonly termStartDate = signal<string>('2025-09-01');
  public readonly termEndDate = signal<string>('2026-01-31');
  public readonly specialRequirements = signal<string>('');

  // BRD Cross-field Validator: Term End Date must follow Term Start Date
  public readonly isTermDateRangeInvalid = computed<boolean>(() => {
    const start = this.termStartDate();
    const end = this.termEndDate();
    if (!start || !end) return false;
    return new Date(end) <= new Date(start);
  });

  // Confirmation Modal Signals
  public readonly isConfirmOpen = signal<boolean>(false);
  public readonly confirmTitle = signal<string>('Confirm Housing Application');
  public readonly confirmMessage = signal<string>('');
  public readonly confirmIcon = signal<string>('🏢');
  public readonly confirmVariant = signal<'primary' | 'danger' | 'warning'>('primary');
  public readonly confirmButtonText = signal<string>('Confirm & Submit');
  public readonly confirmButtonIcon = signal<string>('✓');

  // Business Rule: One active application per student
  public readonly activeApplication = computed<HousingApplication | null>(() => {
    const list = this.myApplications();
    return list.find((a) => a.status !== 'Rejected' && a.status !== 'Cancelled') || null;
  });

  public readonly hasActiveApplication = computed<boolean>(() => {
    return this.activeApplication() !== null;
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);

    // Dynamic pre-fill from System Settings
    this.systemSettingsService.getAllSettings().subscribe({
      next: (res) => {
        const dict = res.data;
        if (dict) {
          const year = dict['AcademicYear'] || '2025/2026';
          const sem = dict['Semester'] || 'Semester 1';
          this.termSemester.set(`${year} / ${sem}`);

          if (dict['SemesterStartDate']) {
            this.termStartDate.set(dict['SemesterStartDate']);
          } else if (dict['AcademicYearStartDate']) {
            this.termStartDate.set(dict['AcademicYearStartDate']);
          }

          if (dict['SemesterEndDate']) {
            this.termEndDate.set(dict['SemesterEndDate']);
          } else if (dict['AcademicYearEndDate']) {
            this.termEndDate.set(dict['AcademicYearEndDate']);
          }
        }
      },
    });

    this.hostelService.getFormattedHostelsLookup(1, 100).subscribe({
      next: (items) => {
        this.hostelsList.set(items);
      },
      error: () => {
        this.hostelsList.set([]);
      },
    });

    this.hostelService.getMyFormattedApplications().subscribe({
      next: (formatted) => {
        this.myApplications.set(formatted);
        this.isLoading.set(false);
      },
      error: () => {
        this.myApplications.set([]);
        this.isLoading.set(false);
      },
    });
  }

  getSelectedHostelName(): string {
    const id = this.selectedHostelId();
    const found = this.hostelsList().find((h) => h.id === id);
    return found ? found.name : 'Selected Facility';
  }

  promptSubmitApplication(): void {
    if (this.hasActiveApplication()) {
      this.toast.error('Operation Blocked. You already have an active housing application in progress.');
      return;
    }

    const hostelId = this.selectedHostelId();
    if (!hostelId || hostelId === 0) {
      this.toast.error('Please select a preferred campus hostel.');
      return;
    }

    const term = this.termSemester().trim();
    if (!term) {
      this.toast.error('Please enter the academic term / semester.');
      return;
    }

    if (this.isTermDateRangeInvalid()) {
      this.toast.error('Validation Error: Housing Term End Date must be strictly after the Start Date.');
      return;
    }

    const hostelName = this.getSelectedHostelName();
    this.confirmTitle.set('Confirm Housing Application');
    this.confirmMessage.set(
      `Are you sure you want to submit a housing accommodation request for ${hostelName} for academic term "${term}"?`
    );
    this.confirmIcon.set('🏢');
    this.confirmVariant.set('primary');
    this.confirmButtonText.set('Confirm & Submit');
    this.confirmButtonIcon.set('✓');
    this.isConfirmOpen.set(true);
  }

  onConfirmSubmit(): void {
    if (this.isSubmitting()) return;
    this.isSubmitting.set(true);

    const hostelId = this.selectedHostelId();
    const payload = {
      hostelId: hostelId,
      preferredHostelId: hostelId,
      termSemester: this.termSemester().trim(),
      specialRequirements: this.specialRequirements().trim() || undefined,
    };

    this.hostelService.submitApplication(payload).subscribe({
      next: () => {
        this.isConfirmOpen.set(false);
        this.toast.success('Housing application submitted successfully! Your request has been queued for administrator review.');
        this.isSubmitting.set(false);
        this.selectedHostelId.set(0);
        this.specialRequirements.set('');
        this.loadData();
      },
      error: (err) => {
        this.isConfirmOpen.set(false);
        this.toast.error(err?.error?.message || err?.message || 'Failed to submit housing application.');
        this.isSubmitting.set(false);
      },
    });
  }

  onCancelConfirm(): void {
    this.isConfirmOpen.set(false);
  }
}
