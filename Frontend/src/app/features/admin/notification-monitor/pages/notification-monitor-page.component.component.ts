import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationMonitorService } from '../services/notification-monitor.service';
import { ToastService } from '../../../../core/services/toast.service';
import { SystemSettingsService } from '../../../../core/services/system-settings.service';
import { Notification } from '../../../../core/models/system/notification.model';
import { StudentProfile } from '../../../../core/models/auth/student-profile.model';
import { Faculty } from '../../../../core/models/faculty/faculty.model';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TabComponent, TabItem } from '../../../../shared/components/tab-component/tab.component';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { TableColumn } from '../../../../shared/components/data-table/models/table-column.model';

@Component({
  selector: 'app-notification-monitor-page.component',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    DataTableComponent,
    TabComponent,
    ConfirmModalComponent,
  ],
  templateUrl: './notification-monitor-page.component.component.html',
  styleUrl: './notification-monitor-page.component.component.css',
})
export class NotificationMonitorPageComponentComponent implements OnInit {
  private readonly monitorService = inject(NotificationMonitorService);
  private readonly settingsService = inject(SystemSettingsService);
  private readonly toast = inject(ToastService);

  // Audit Table Signals
  public readonly auditNotifications = signal<Notification[]>([]);
  public readonly isLoading = signal<boolean>(false);
  public readonly searchTerm = signal<string>('');
  public readonly currentPage = signal<number>(1);
  public readonly pageSize = signal<number>(5);
  public readonly sortColumn = signal<string>('');
  public readonly sortDirection = signal<'asc' | 'desc'>('asc');

  // Directory Data Signals
  public readonly studentProfiles = signal<StudentProfile[]>([]);
  public readonly faculties = signal<Faculty[]>([]);

  // Dispatch Modal Signals
  public readonly isDispatchModalOpen = signal<boolean>(false);
  public readonly isDispatching = signal<boolean>(false);
  public readonly activeDispatchTab = signal<'single' | 'faculty'>('single');
  public readonly selectedStudentId = signal<number>(0);
  public readonly selectedFacultyId = signal<number>(0);
  public readonly studentSearchTerm = signal<string>('');
  public readonly notificationType = signal<string>('SYSTEMNOTICE');
  public readonly notificationMessage = signal<string>('');

  // Confirmation Modal Signals
  public readonly isConfirmModalOpen = signal<boolean>(false);

  // 3D Tab Items for Modal Dispatch Modes
  public readonly modalModeTabs = computed<TabItem[]>(() => [
    {
      id: 'single',
      label: 'Single Student Dispatch',
      icon: '👤',
      count: this.searchedStudents().length,
    },
    {
      id: 'faculty',
      label: 'Faculty Bulk Broadcast',
      icon: '🏛️',
      count: this.faculties().length,
    },
  ]);

  // Live Typeahead Autocomplete Search Results (Matching Fee Assignment Screen)
  public readonly searchedStudents = computed(() => {
    const term = this.studentSearchTerm().trim().toLowerCase();
    const list = this.studentProfiles();
    if (!term) return [];
    return list
      .filter(
        (s) =>
          (s.indexNumber || '').toLowerCase().includes(term) ||
          (s.name || '').toLowerCase().includes(term) ||
          (s.email || '').toLowerCase().includes(term)
      )
      .slice(0, 10);
  });

  // Selected Student Object & Badge Label
  public readonly selectedStudent = computed(() => {
    const id = this.selectedStudentId();
    if (!id || id <= 0) return null;
    return this.studentProfiles().find((s) => s.id === id) || null;
  });

  public readonly selectedStudentLabel = computed(() => {
    const s = this.selectedStudent();
    if (!s) return '';
    return `${s.indexNumber || 'N/A'} — ${s.name} (${s.email || ''})`;
  });

  // Selected Faculty Object
  public readonly selectedFaculty = computed(() => {
    const id = this.selectedFacultyId();
    if (!id || id <= 0) return null;
    return this.faculties().find((f) => f.id === id) || null;
  });

  // Table Column Definitions
  public readonly tableColumns: TableColumn<any>[] = [
    { key: 'targetStudentText', header: 'Target Student Index No', sortable: true, filterable: true },
    {
      key: 'type',
      header: 'Notification Type',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        FEEPAYMENTSETTLED: {
          label: 'FEEPAYMENTSETTLED',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-extrabold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-700',
        },
        NEWFEEASSIGNED: {
          label: 'NEWFEEASSIGNED',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-extrabold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-700',
        },
        HOSTELALLOCATED: {
          label: 'HOSTELALLOCATED',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-extrabold bg-blue-100 dark:bg-blue-950/60 text-blue-800 dark:text-blue-300 border border-blue-300 dark:border-blue-700',
        },
        EVENTREGISTRATION: {
          label: 'EVENTREGISTRATION',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-extrabold bg-purple-100 dark:bg-purple-950/60 text-purple-800 dark:text-purple-300 border border-purple-300 dark:border-purple-700',
        },
        SYSTEMNOTICE: {
          label: 'SYSTEMNOTICE',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-extrabold bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-700',
        },
        SECURITYALERT: {
          label: 'SECURITYALERT',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-extrabold bg-rose-100 dark:bg-rose-950/60 text-rose-800 dark:text-rose-300 border border-rose-300 dark:border-rose-700',
        },
      },
    },
    { key: 'message', header: 'Message Content', sortable: true, filterable: true },
    {
      key: 'deliveryStatusText',
      header: 'Delivery Status',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        'UNREAD / DISPATCHED': {
          label: 'UNREAD / DISPATCHED',
          class: 'px-2.5 py-1 rounded-full text-[10px] font-extrabold bg-amber-100 dark:bg-amber-950/70 text-amber-900 dark:text-amber-300 border border-amber-300 dark:border-amber-700',
        },
        READ: {
          label: 'READ',
          class: 'px-2.5 py-1 rounded-full text-[10px] font-extrabold bg-emerald-100 dark:bg-emerald-950/70 text-emerald-900 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-700',
        },
      },
    },
    { key: 'formattedDate', header: 'Dispatched Date & Time', sortable: true, filterable: true },
  ];

  // Computed Formatted Table Records
  public readonly displayAuditRecords = computed(() => {
    let list = this.auditNotifications().map((n) => ({
      ...n,
      targetStudentText: `${n.indexNumber}`,
      deliveryStatusText: n.isRead ? 'READ' : 'UNREAD / DISPATCHED',
      formattedDate: this.formatDate(n.createdAt),
    }));

    const search = this.searchTerm().trim().toLowerCase();
    if (search) {
      list = list.filter(
        (n) =>
          n.targetStudentText.toLowerCase().includes(search) ||
          (n.type || '').toLowerCase().includes(search) ||
          n.message.toLowerCase().includes(search) ||
          n.deliveryStatusText.toLowerCase().includes(search)
      );
    }

    return list;
  });

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
    this.loadAuditLog();
    this.loadDirectories();
  }

  loadAuditLog(): void {
    this.isLoading.set(true);
    this.monitorService.getAdminAuditLog().subscribe({
      next: (data: any) => {
        this.isLoading.set(false);
        const list = Array.isArray(data) ? data : (data as any)?.data || [];
        this.auditNotifications.set(list);
      },
      error: (err: any) => {
        this.isLoading.set(false);
        this.toast.error(err.error?.message || 'Failed to load notifications dispatch audit monitor.');
      },
    });
  }

  loadDirectories(): void {
    this.monitorService.getStudentsDirectory().subscribe({
      next: (res: any) => {
        const payload = res.data || res;
        const items: StudentProfile[] = Array.isArray(payload)
          ? payload
          : payload?.items || [];
        this.studentProfiles.set(items);
      },
    });

    this.monitorService.getFaculties().subscribe({
      next: (res) => {
        const list = res.data || (Array.isArray(res) ? res : []);
        this.faculties.set(list);
      },
    });
  }

  onSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.sortColumn.set(event.column);
    this.sortDirection.set(event.direction);
  }

  // Typeahead Student Selection Actions
  selectStudent(student: StudentProfile): void {
    this.selectedStudentId.set(student.id);
    this.studentSearchTerm.set('');
  }

  clearSelectedStudent(): void {
    this.selectedStudentId.set(0);
    this.studentSearchTerm.set('');
  }

  // Modal Actions
  openDispatchModal(): void {
    this.selectedStudentId.set(0);
    this.selectedFacultyId.set(0);
    this.studentSearchTerm.set('');
    this.notificationType.set('SYSTEMNOTICE');
    this.notificationMessage.set('');
    this.isDispatchModalOpen.set(true);
  }

  closeDispatchModal(): void {
    this.isDispatchModalOpen.set(false);
    this.isConfirmModalOpen.set(false);
  }

  setActiveDispatchTab(tabId: 'single' | 'faculty'): void {
    this.activeDispatchTab.set(tabId);
  }

  // Trigger Confirmation Modal
  requestDispatchConfirmation(): void {
    const msg = this.notificationMessage().trim();
    if (!msg) {
      this.toast.warning('Please enter a notification message body payload.');
      return;
    }

    if (this.activeDispatchTab() === 'single' && this.selectedStudentId() <= 0) {
      this.toast.warning('Please search and select a target student recipient.');
      return;
    }

    if (this.activeDispatchTab() === 'faculty' && this.selectedFacultyId() <= 0) {
      this.toast.warning('Please select a target academic faculty.');
      return;
    }

    this.isConfirmModalOpen.set(true);
  }

  // Execute Dispatch Payload
  confirmAndSubmitDispatch(): void {
    this.isDispatching.set(true);
    const mode = this.activeDispatchTab();

    const payload = {
      dispatchMode: mode,
      studentId: mode === 'single' ? Number(this.selectedStudentId()) : undefined,
      facultyId: mode === 'faculty' ? Number(this.selectedFacultyId()) : undefined,
      type: this.notificationType(),
      message: this.notificationMessage().trim(),
    };

    this.monitorService.sendInternalNotification(payload).subscribe({
      next: () => {
        this.isDispatching.set(false);
        const targetText =
          mode === 'single'
            ? `Student "${this.selectedStudent()?.name || 'Selected Student'}"`
            : `all students in "${this.selectedFaculty()?.name || 'Selected Faculty'}"`;

        this.toast.success(`Internal notification alert dispatched to ${targetText}!`);
        this.closeDispatchModal();
        this.loadAuditLog();
      },
      error: (err: any) => {
        this.isDispatching.set(false);
        this.toast.error(err.error?.message || 'Failed to dispatch internal notification alert.');
      },
    });
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    try {
      return new Date(dateStr).toLocaleString('en-US', {
        month: 'numeric',
        day: 'numeric',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        second: '2-digit',
        hour12: true,
      });
    } catch {
      return dateStr;
    }
  }
}

export { NotificationMonitorPageComponentComponent as NotificationMonitorPageComponent };

