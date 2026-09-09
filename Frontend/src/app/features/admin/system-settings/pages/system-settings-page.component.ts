import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ControlPanelCardComponent } from '../../../../shared/components/cards/control-panel-card/control-panel-card.component';
import { SearchComponent } from '../../../../shared/components/tables-utilities/search/search.component';
import { ToastContainerComponent } from '../../../../shared/components/toast-container/toast-container.component';
import { ToastService } from '../../../../core/services/toast.service';
import { SystemSettingsService } from '../services/system-settings.service';
import { ThemeService } from '../../../../core/services/theme.service';
import { SystemSettingPanelCard } from '../../../../core/models/system/system-setting.model';

import { SelectDropdownComponent } from '../../../../shared/components/select-dropdown/select-dropdown.component';
import { DatePickerComponent } from '../../../../shared/components/date-picker/date-picker.component';
import { DropdownOption } from '../../../../core/models/common/dropdown-option.model';

@Component({
  selector: 'app-system-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    ControlPanelCardComponent,
    SearchComponent,
    ToastContainerComponent,
    SelectDropdownComponent,
    DatePickerComponent,
  ],
  templateUrl: './system-settings-page.component.html',
})
export class SystemSettingsPageComponent implements OnInit {
  private readonly settingsService = inject(SystemSettingsService);
  private readonly themeService = inject(ThemeService);
  private readonly toast = inject(ToastService);

  public readonly searchQuery = signal<string>('');
  public readonly selectedCategoryFilter = signal<string>('ALL');
  public readonly isLoading = signal<boolean>(false);
  public readonly showSnapshotStrip = signal<boolean>(true);

  public toggleSnapshotStrip(): void {
    this.showSnapshotStrip.update((v) => !v);
  }

  public readonly themeOptions = ThemeService.THEMES;

  public setCategoryFilter(category: string): void {
    this.selectedCategoryFilter.set(category);
  }

  public readonly categoryFilterOptions: { label: string; value: string }[] = [
    { label: 'All Setting Categories', value: 'ALL' },
    { label: '🏢 Organization & Academic', value: 'organization' },
    { label: '🧪 Laboratory & Seat Matrix', value: 'laboratory' },
    { label: '🔐 Security & Authentication', value: 'security' },
    { label: '⚙️ Application & Themes', value: 'application' },
    { label: '💰 Finance & Fee Structure', value: 'finance' },
    { label: '🎓 Student & Registration', value: 'student' },
    { label: '🏠 Hostel & Room Allocation', value: 'hostel' },
    { label: '🎪 Events & Venues', value: 'events' },
    { label: '🔔 Notifications & Alerts', value: 'notifications' },
  ];

  // Settings State Signals
  public readonly institutionName = signal<string>('University of Knowledge (UOK)');
  public readonly holdMinutes = signal<number>(15);
  public readonly maxDailySlots = signal<number>(2);
  public readonly requireSeatSelection = signal<boolean>(true);
  public readonly computerLabSeatSelection = signal<boolean>(true);
  public readonly scienceLabSeatSelection = signal<boolean>(false);
  public readonly selectedLabTypeConfig = signal<string>('computer-cs');
  public readonly academicYear = signal<string>('2025/2026');
  public readonly academicYearsList = signal<string[]>(['2024/2025', '2025/2026', '2026/2027']);
  public readonly newAcademicYearInput = signal<string>('');

  // Date-Linked Academic Calendar Signals
  public readonly academicYearStartDate = signal<string>('2025-09-01');
  public readonly academicYearEndDate = signal<string>('2026-08-31');
  public readonly semesterStartDate = signal<string>('2025-09-01');
  public readonly semesterEndDate = signal<string>('2026-01-31');

  public readonly semester = signal<string>('Semester 1');
  public readonly semestersList = signal<string[]>(['Semester 1', 'Semester 2', 'Summer Trimester', 'Special Term']);
  public readonly newSemesterInput = signal<string>('');

  public readonly minPasswordLength = signal<number>(8);
  public readonly maxFailedLogins = signal<number>(5);
  public readonly passwordComplexityTier = signal<string>('strong');
  public readonly passwordExpiryDays = signal<number>(90);
  public readonly passwordReuseHistoryLimit = signal<number>(5);
  public readonly otpValidityMinutes = signal<number>(3);
  public readonly maxOtpResendAttempts = signal<number>(5);

  public readonly passwordComplexityOptions: DropdownOption[] = [
    { value: 'basic', label: '🔓 Basic — Length Only (8+ Characters)' },
    { value: 'medium', label: '⚡ Medium — Letters & Numbers (A-Z, a-z, 0-9)' },
    { value: 'strong', label: '🛡️ Strong — Recommended (Upper, Lower, Number & Symbol)' },
    { value: 'strict', label: '🔒 Strict Enterprise — All Rules + Min 12 Characters' },
  ];

  public readonly themeColor = signal<string>(this.themeService.currentTheme());
  public readonly fontSize = signal<string>('Medium');
  public readonly headerGradient = signal<string>('Navy-Indigo');
  public readonly defaultPageSize = signal<number>(5);

  // Finance & Fee Structure Signals
  public readonly lateFeeGracePeriodDays = signal<number>(7);
  public readonly defaultCurrency = signal<string>('LKR');
  public readonly taxPercentage = signal<number>(0);
  public readonly enableOnlinePaymentGateway = signal<boolean>(true);

  // Student & Registration Types Signals
  public readonly studentIdPrefixFormat = signal<string>('STU/2026/');
  public readonly maxActiveRegistrationsPerStudent = signal<number>(10);
  public readonly requireEmailVerificationOnRegistration = signal<boolean>(true);

  // Hostel & Room Allocation Signals
  public readonly hostelApplicationWindowDays = signal<number>(30);
  public readonly maxRoomOccupancyCap = signal<number>(4);
  public readonly autoApproveHostelApplications = signal<boolean>(false);

  // Campus Events & Venues Signals
  public readonly maxAdvanceVenueBookingDays = signal<number>(60);
  public readonly requireAdminApprovalForVenueBooking = signal<boolean>(true);
  public readonly eventRegistrationCancellationDeadlineHours = signal<number>(24);

  // Notifications & Templates Signals
  public readonly enableEmailNotifications = signal<boolean>(true);
  public readonly enableSmsNotifications = signal<boolean>(false);
  public readonly notificationRetentionDays = signal<number>(90);

  // Category Modal State
  public readonly isModalOpen = signal<boolean>(false);
  public readonly activeCategory = signal<SystemSettingPanelCard | null>(null);

  // Deactivation / Reactivation State Mapping
  public readonly activeStatusMap = signal<Record<string, boolean>>({
    Organization: true,
    Laboratory: true,
    Security: true,
    Application: true,
    Finance: true,
    Student: true,
    Hostel: true,
    Events: true,
    Notifications: true,
  });

  public readonly masterCategoryCards: SystemSettingPanelCard[] = [
    {
      id: 'organization',
      title: '🏢 Organization & Academic Calendar',
      icon: '🏢',
      colorTheme: 'blue',
      description: 'Configure institution name, academic years, active semesters, start/end dates, and departments.',
      summaryItems: ['Institution Name', 'Academic Calendar', 'Semester 1/2', 'Term Date Ranges'],
      settingsCount: 5,
    },
    {
      id: 'laboratory',
      title: '🧪 Laboratory & Seat Matrix Rules',
      icon: '🧪',
      colorTheme: 'emerald',
      description: 'System reservation hold timeout (15 mins), max daily slots (2 slots / 4 hrs), seat matrix toggle.',
      summaryItems: ['15-Min Hold Timeout', 'Max 2 Daily Slots', 'Seat Matrix (T/F)', 'Advance Booking'],
      settingsCount: 4,
    },
    {
      id: 'security',
      title: '🔐 Security & Authentication Policies',
      icon: '🔐',
      colorTheme: 'rose',
      description: 'Password complexity policies, failed login attempt thresholds, roles, permissions, and MFA.',
      summaryItems: ['Password Policy', 'Max 5 Failed Logins', 'Roles & Permissions', 'MFA'],
      settingsCount: 4,
    },
    {
      id: 'application',
      title: '⚙️ Application, Themes & Wallpapers',
      icon: '⚙️',
      colorTheme: 'purple',
      description: 'Custom theme color schemes, font sizing, wallpaper gradients, SMTP email, and default pagination.',
      summaryItems: ['Theme Color (Blue)', 'Font Sizing', 'Header Gradients', 'Default Page Size (10)'],
      settingsCount: 4,
    },
    {
      id: 'finance',
      title: '💰 Finance & Fee Structure',
      icon: '💰',
      colorTheme: 'amber',
      description: 'Tuition fee types, payment gateway settings, fee categories, tax rules, and discount thresholds.',
      summaryItems: ['Fee Types', 'Payment Methods', 'Fee Categories', 'Tax / Discount'],
      settingsCount: 4,
    },
    {
      id: 'student',
      title: '🎓 Student & Registration Types',
      icon: '🎓',
      colorTheme: 'indigo',
      description: 'Student categorization, registration types, student statuses, and auto ID generator format.',
      summaryItems: ['Student Types', 'ID Format (STU/2026)', 'Registration Types', 'Statuses'],
      settingsCount: 4,
    },
    {
      id: 'hostel',
      title: '🏠 Hostel & Room Allocation',
      icon: '🏠',
      colorTheme: 'emerald',
      description: 'Hostel facility types, room types, room statuses, maintenance locks, and auto-allocation rules.',
      summaryItems: ['Hostel Types', 'Room Types', 'Room Statuses', 'Allocation Rules'],
      settingsCount: 4,
    },
    {
      id: 'events',
      title: '🎪 Campus Events & Venues',
      icon: '🎪',
      colorTheme: 'blue',
      description: 'Event categories, auditorium/venue types, event statuses, and advance venue booking windows.',
      summaryItems: ['Event Types', 'Venue Types', 'Event Statuses', 'Booking Windows'],
      settingsCount: 3,
    },
    {
      id: 'notifications',
      title: '🔔 Notifications & Templates',
      icon: '🔔',
      colorTheme: 'slate',
      description: 'Notification types, automated Email/SMS message templates, and dispatch alert rules.',
      summaryItems: ['Notification Types', 'Email Templates', 'SMS Dispatch', 'Alert Rules'],
      settingsCount: 3,
    },
  ];

  public readonly filteredCards = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const cat = this.selectedCategoryFilter();

    return this.masterCategoryCards.filter((c) => {
      const matchesCategory = cat === 'ALL' || c.id === cat;
      const matchesSearch =
        !q ||
        c.title.toLowerCase().includes(q) ||
        c.description.toLowerCase().includes(q) ||
        c.summaryItems.some((s) => s.toLowerCase().includes(q));

      return matchesCategory && matchesSearch;
    });
  });

  public ngOnInit(): void {
    this.fetchSystemSettings();
  }

  public fetchSystemSettings(): void {
    this.isLoading.set(true);
    this.settingsService.getAllSettings().subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.data) {
          const dict = res.data;
          if (dict['InstitutionName']) this.institutionName.set(dict['InstitutionName']);
          if (dict['LabBookingSlotDurationMinutes']) this.holdMinutes.set(parseInt(dict['LabBookingSlotDurationMinutes'], 10) || 15);
          else if (dict['LabBookingHoldMinutes']) this.holdMinutes.set(parseInt(dict['LabBookingHoldMinutes'], 10) || 15);
          if (dict['MaxLabBookingsPerStudentPerDay']) this.maxDailySlots.set(parseInt(dict['MaxLabBookingsPerStudentPerDay'], 10) || 2);
          else if (dict['MaxDailySlots']) this.maxDailySlots.set(parseInt(dict['MaxDailySlots'], 10) || 2);
          if (dict['RequireSeatSelection']) this.requireSeatSelection.set(dict['RequireSeatSelection'] === 'true');
          if (dict['ComputerLabSeatSelection']) this.computerLabSeatSelection.set(dict['ComputerLabSeatSelection'] === 'true');
          if (dict['ScienceLabSeatSelection']) this.scienceLabSeatSelection.set(dict['ScienceLabSeatSelection'] === 'true');
          if (dict['AcademicYear']) this.academicYear.set(dict['AcademicYear']);
          if (dict['AcademicYearStartDate']) this.academicYearStartDate.set(dict['AcademicYearStartDate']);
          if (dict['AcademicYearEndDate']) this.academicYearEndDate.set(dict['AcademicYearEndDate']);
          if (dict['AcademicYearsList']) {
            const list = dict['AcademicYearsList'].split(',').map((s) => s.trim()).filter(Boolean);
            if (list.length > 0) this.academicYearsList.set(list);
          }
          if (dict['Semester']) this.semester.set(dict['Semester']);
          if (dict['SemesterStartDate']) this.semesterStartDate.set(dict['SemesterStartDate']);
          if (dict['SemesterEndDate']) this.semesterEndDate.set(dict['SemesterEndDate']);
          if (dict['SemestersList']) {
            const list = dict['SemestersList'].split(',').map((s) => s.trim()).filter(Boolean);
            if (list.length > 0) this.semestersList.set(list);
          }
          if (dict['MinPasswordLength']) this.minPasswordLength.set(parseInt(dict['MinPasswordLength'], 10) || 8);
          if (dict['MaxFailedLogins']) this.maxFailedLogins.set(parseInt(dict['MaxFailedLogins'], 10) || 5);
          if (dict['RequirePasswordComplexity']) this.passwordComplexityTier.set(dict['RequirePasswordComplexity']);
          if (dict['PasswordExpiryDays']) this.passwordExpiryDays.set(parseInt(dict['PasswordExpiryDays'], 10) || 90);
          if (dict['PasswordReuseHistoryLimit']) this.passwordReuseHistoryLimit.set(parseInt(dict['PasswordReuseHistoryLimit'], 10) || 5);
          if (dict['OtpValidityMinutes']) this.otpValidityMinutes.set(parseInt(dict['OtpValidityMinutes'], 10) || 3);
          if (dict['MaxOtpResendAttempts']) this.maxOtpResendAttempts.set(parseInt(dict['MaxOtpResendAttempts'], 10) || 5);
          if (dict['ThemeColor']) this.themeColor.set(dict['ThemeColor']);
          if (dict['FontSize']) this.fontSize.set(dict['FontSize']);
          if (dict['DefaultPageSize']) this.defaultPageSize.set(parseInt(dict['DefaultPageSize'], 10) || 5);

          // Finance & Fee Structure
          if (dict['LateFeeGracePeriodDays']) this.lateFeeGracePeriodDays.set(parseInt(dict['LateFeeGracePeriodDays'], 10) || 7);
          if (dict['DefaultCurrency']) this.defaultCurrency.set(dict['DefaultCurrency']);
          if (dict['TaxPercentage']) this.taxPercentage.set(parseFloat(dict['TaxPercentage']) || 0);
          if (dict['EnableOnlinePaymentGateway']) this.enableOnlinePaymentGateway.set(dict['EnableOnlinePaymentGateway'] === 'true');

          // Student & Registration Types
          if (dict['StudentIdPrefixFormat']) this.studentIdPrefixFormat.set(dict['StudentIdPrefixFormat']);
          if (dict['MaxActiveRegistrationsPerStudent']) this.maxActiveRegistrationsPerStudent.set(parseInt(dict['MaxActiveRegistrationsPerStudent'], 10) || 10);
          if (dict['RequireEmailVerificationOnRegistration']) this.requireEmailVerificationOnRegistration.set(dict['RequireEmailVerificationOnRegistration'] === 'true');

          // Hostel & Room Allocation
          if (dict['HostelApplicationWindowDays']) this.hostelApplicationWindowDays.set(parseInt(dict['HostelApplicationWindowDays'], 10) || 30);
          if (dict['MaxRoomOccupancyCap']) this.maxRoomOccupancyCap.set(parseInt(dict['MaxRoomOccupancyCap'], 10) || 4);
          if (dict['AutoApproveHostelApplications']) this.autoApproveHostelApplications.set(dict['AutoApproveHostelApplications'] === 'true');

          // Campus Events & Venues
          if (dict['MaxAdvanceVenueBookingDays']) this.maxAdvanceVenueBookingDays.set(parseInt(dict['MaxAdvanceVenueBookingDays'], 10) || 60);
          if (dict['RequireAdminApprovalForVenueBooking']) this.requireAdminApprovalForVenueBooking.set(dict['RequireAdminApprovalForVenueBooking'] === 'true');
          if (dict['EventRegistrationCancellationDeadlineHours']) this.eventRegistrationCancellationDeadlineHours.set(parseInt(dict['EventRegistrationCancellationDeadlineHours'], 10) || 24);

          // Notifications & Templates
          if (dict['EnableEmailNotifications']) this.enableEmailNotifications.set(dict['EnableEmailNotifications'] === 'true');
          if (dict['EnableSmsNotifications']) this.enableSmsNotifications.set(dict['EnableSmsNotifications'] === 'true');
          if (dict['NotificationRetentionDays']) this.notificationRetentionDays.set(parseInt(dict['NotificationRetentionDays'], 10) || 90);
        }
      },
      error: () => this.isLoading.set(false),
    });
  }

  public onAcademicYearSelect(year: string): void {
    this.academicYear.set(year);
    this.autoCalculateDatesForAcademicYear(year);
  }

  public autoCalculateDatesForAcademicYear(yearStr: string): void {
    if (!yearStr) return;

    // Parse start year from format e.g. "2026/2027", "2026-2027", or "2026"
    const match = yearStr.match(/\d{4}/);
    if (match) {
      const startYear = parseInt(match[0], 10);
      const endYear = startYear + 1;

      const newStartDate = `${startYear}-09-01`;
      const newEndDate = `${endYear}-08-31`;

      this.academicYearStartDate.set(newStartDate);
      this.academicYearEndDate.set(newEndDate);

      // Auto-align active semester dates relative to academic year
      const currentSem = this.semester();
      if (currentSem.includes('2')) {
        this.semesterStartDate.set(`${endYear}-02-01`);
        this.semesterEndDate.set(`${endYear}-06-30`);
      } else if (currentSem.includes('Summer') || currentSem.includes('Special')) {
        this.semesterStartDate.set(`${endYear}-07-01`);
        this.semesterEndDate.set(`${endYear}-08-31`);
      } else {
        this.semesterStartDate.set(`${startYear}-09-01`);
        this.semesterEndDate.set(`${endYear}-01-31`);
      }
    }
  }

  public addAcademicYear(): void {
    const val = this.newAcademicYearInput().trim();
    if (!val) {
      this.toast.warning('⚠️ Please enter an Academic Year (e.g. 2027/2028).');
      return;
    }
    const current = this.academicYearsList();
    if (!current.includes(val)) {
      this.academicYearsList.set([...current, val]);
    }
    this.onAcademicYearSelect(val);
    this.newAcademicYearInput.set('');
    this.toast.success(`📅 Created & Auto-Linked Academic Year: ${val}`);
  }

  public addSemester(): void {
    const val = this.newSemesterInput().trim();
    if (!val) {
      this.toast.warning('⚠️ Please enter a Semester Name (e.g. Summer Trimester).');
      return;
    }
    const current = this.semestersList();
    if (current.includes(val)) {
      this.semester.set(val);
      this.newSemesterInput.set('');
      this.toast.info(`ℹ️ Semester "${val}" selected.`);
      return;
    }
    this.semestersList.set([...current, val]);
    this.semester.set(val);
    this.newSemesterInput.set('');
    this.toast.success(`🏛️ Created New Semester Term: ${val}`);
  }

  public onCategoryFilterChange(values: any[]): void {
    const val = values && values.length > 0 ? values[0] : 'ALL';
    this.selectedCategoryFilter.set(val);
  }

  public openCategoryModal(card: SystemSettingPanelCard): void {
    this.activeCategory.set(card);
    this.isModalOpen.set(true);
  }

  public closeModal(): void {
    this.isModalOpen.set(false);
    this.activeCategory.set(null);
  }

  public toggleCategoryDeactivation(categoryTitle: string): void {
    const current = this.activeStatusMap();
    const nextState = !current[categoryTitle];
    this.activeStatusMap.set({ ...current, [categoryTitle]: nextState });

    if (nextState) {
      this.toast.success(`⚡ Category Reactivated: ${categoryTitle} module parameters are active.`);
    } else {
      this.toast.warning(`⚙️ Category Deactivated: ${categoryTitle} module parameters placed in maintenance.`);
    }
  }

  public saveCategorySettings(): void {
    // Cross-Field Date Range Validation for Academic Year & Semester
    if (this.academicYearEndDate() && this.academicYearStartDate() && this.academicYearEndDate() < this.academicYearStartDate()) {
      this.toast.warning('⚠️ Cross-Field Date Error: Academic Year End Date must be after Start Date.');
      return;
    }

    if (this.semesterEndDate() && this.semesterStartDate() && this.semesterEndDate() < this.semesterStartDate()) {
      this.toast.warning('⚠️ Cross-Field Date Error: Active Semester End Date must be after Start Date.');
      return;
    }

    const payload: Record<string, string> = {
      InstitutionName: this.institutionName(),
      LabBookingSlotDurationMinutes: this.holdMinutes().toString(),
      LabBookingHoldMinutes: this.holdMinutes().toString(),
      MaxLabBookingsPerStudentPerDay: this.maxDailySlots().toString(),
      MaxDailySlots: this.maxDailySlots().toString(),
      RequireSeatSelection: this.requireSeatSelection().toString(),
      ComputerLabSeatSelection: this.computerLabSeatSelection().toString(),
      ScienceLabSeatSelection: this.scienceLabSeatSelection().toString(),
      AcademicYear: this.academicYear(),
      AcademicYearStartDate: this.academicYearStartDate(),
      AcademicYearEndDate: this.academicYearEndDate(),
      AcademicYearsList: this.academicYearsList().join(','),
      Semester: this.semester(),
      SemesterStartDate: this.semesterStartDate(),
      SemesterEndDate: this.semesterEndDate(),
      SemestersList: this.semestersList().join(','),
      MinPasswordLength: this.minPasswordLength().toString(),
      MaxFailedLogins: this.maxFailedLogins().toString(),
      RequirePasswordComplexity: this.passwordComplexityTier(),
      PasswordExpiryDays: this.passwordExpiryDays().toString(),
      PasswordReuseHistoryLimit: this.passwordReuseHistoryLimit().toString(),
      OtpValidityMinutes: this.otpValidityMinutes().toString(),
      MaxOtpResendAttempts: this.maxOtpResendAttempts().toString(),
      ThemeColor: this.themeColor(),
      FontSize: this.fontSize(),
      DefaultPageSize: this.defaultPageSize().toString(),

      // Finance & Fee Structure
      LateFeeGracePeriodDays: this.lateFeeGracePeriodDays().toString(),
      DefaultCurrency: this.defaultCurrency(),
      TaxPercentage: this.taxPercentage().toString(),
      EnableOnlinePaymentGateway: this.enableOnlinePaymentGateway().toString(),

      // Student & Registration Types
      StudentIdPrefixFormat: this.studentIdPrefixFormat(),
      MaxActiveRegistrationsPerStudent: this.maxActiveRegistrationsPerStudent().toString(),
      RequireEmailVerificationOnRegistration: this.requireEmailVerificationOnRegistration().toString(),

      // Hostel & Room Allocation
      HostelApplicationWindowDays: this.hostelApplicationWindowDays().toString(),
      MaxRoomOccupancyCap: this.maxRoomOccupancyCap().toString(),
      AutoApproveHostelApplications: this.autoApproveHostelApplications().toString(),

      // Campus Events & Venues
      MaxAdvanceVenueBookingDays: this.maxAdvanceVenueBookingDays().toString(),
      RequireAdminApprovalForVenueBooking: this.requireAdminApprovalForVenueBooking().toString(),
      EventRegistrationCancellationDeadlineHours: this.eventRegistrationCancellationDeadlineHours().toString(),

      // Notifications & Templates
      EnableEmailNotifications: this.enableEmailNotifications().toString(),
      EnableSmsNotifications: this.enableSmsNotifications().toString(),
      NotificationRetentionDays: this.notificationRetentionDays().toString(),
    };

    this.isLoading.set(true);
    this.themeService.setTheme(this.themeColor());
    this.settingsService.updateSettingsBatch(payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.closeModal();
        this.toast.success('System Settings updated successfully with live backend persistence!');
      },
      error: () => {
        this.isLoading.set(false);
        this.toast.warning('Warning: Unable to save system settings payload.');
      },
    });
  }
}
