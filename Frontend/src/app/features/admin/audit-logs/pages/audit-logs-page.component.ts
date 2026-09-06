import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AuditLogService } from '../../../../core/services/audit-log.service';
import { SystemSettingsService } from '../../../../core/services/system-settings.service';
import { ToastService } from '../../../../core/services/toast.service';
import {
  AuditLog,
  AuditLogFilter,
} from '../../../../core/models/audit-log/audit-log.model';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import {
  TabComponent,
  TabItem,
} from '../../../../shared/components/tab-component/tab.component';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../shared/components/data-table/models/table-column.model';
import { TableSortState } from '../../../../shared/components/data-table/models/table-sort.model';
import { DropdownOption } from '../../../../core/models/common/dropdown-option.model';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { AuditDiffModalComponent } from './components/audit-diff-modal.component';
import { DatePresetUtil } from '../../../../core/utils/date-preset.util';

@Component({
  selector: 'app-audit-logs-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PageHeaderComponent,
    TabComponent,
    DataTableComponent,
    ActionButtonComponent,
    StatusBadgeComponent,
    AuditDiffModalComponent,
  ],
  templateUrl: './audit-logs-page.component.html',
  styleUrl: './audit-logs-page.component.css',
})
export class AuditLogsPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auditService = inject(AuditLogService);
  private readonly settingsService = inject(SystemSettingsService);
  private readonly toast = inject(ToastService);

  // Reactive State Signals
  public readonly isLoading = signal<boolean>(false);
  public readonly isAcknowledging = signal<boolean>(false);
  public readonly unreviewedSecurityCount = signal<number>(0);
  public readonly auditLogs = signal<AuditLog[]>([]);
  public readonly totalRecords = signal<number>(0);
  public readonly currentPage = signal<number>(1);
  public readonly pageSize = signal<number>(5);
  public readonly currentSortBy = signal<string>('Timestamp');
  public readonly currentSortDir = signal<'asc' | 'desc'>('desc');

  // Active Tab Filter Signal
  public readonly activeTabId = signal<string>('all');

  // Modal Signals
  public readonly isDiffModalOpen = signal<boolean>(false);
  public readonly selectedLog = signal<AuditLog | null>(null);

  // Dynamic Tabs Configuration: 1. All Activity, 2. Security Alerts
  public readonly tabs = computed<TabItem[]>(() => {
    const unreviewed = this.unreviewedSecurityCount();
    return [
      { id: 'all', label: 'All Activity', icon: '📋' },
      {
        id: 'security',
        label: unreviewed > 0 ? `Security Alerts (${unreviewed})` : 'Security Alerts',
        icon: unreviewed > 0 ? '🚨' : '🛡️',
      },
    ];
  });

  // Displayed Logs
  public readonly displayedLogs = computed<AuditLog[]>(() => this.auditLogs());

  // Dropdown Options
  public readonly moduleOptions: DropdownOption[] = [
    { label: 'All Modules', value: '' },
    { label: 'Auth & Security', value: 'Auth' },
    { label: 'Student Accounts', value: 'Students' },
    { label: 'Student Master', value: 'StudentMaster' },
    { label: 'Hostel Management', value: 'Hostels' },
    { label: 'Lab Reservations', value: 'Labs' },
    { label: 'Events & Venues', value: 'Events' },
    { label: 'Grievance Complaints', value: 'Complaints' },
    { label: 'Certificates', value: 'Certificates' },
    { label: 'Billing & Fees', value: 'Billing' },
    { label: 'System Settings', value: 'SystemSettings' },
  ];

  public readonly statusOptions: DropdownOption[] = [
    { label: 'All Outcomes', value: '' },
    { label: 'Success Only (✓)', value: 'true' },
    { label: 'All Failures (✕)', value: 'false' },
    { label: 'Unreviewed Security Alerts (🚨)', value: 'unreviewed' },
  ];

  // Quick Date Range Preset Options (Reusable for Reports & Audits)
  public readonly datePresetOptions: DropdownOption[] = DatePresetUtil.getPresetOptions();

  // Reactive Filter Form
  public readonly filterForm: FormGroup = this.fb.group({
    searchTerm: [''],
    datePreset: ['all'],
    fromDate: [''],
    toDate: [''],
    module: [''],
    status: [''],
  });

  // Data Table Column Definitions
  public readonly columns: TableColumn<AuditLog>[] = [
    {
      key: 'id',
      header: 'Audit ID',
      sortable: true,
      filterable: false,
      type: 'custom',
    },
    {
      key: 'timestamp',
      header: 'Timestamp (UTC)',
      sortable: true,
      filterable: false,
      type: 'custom',
    },
    {
      key: 'userDisplayName',
      header: 'User / Initiator',
      sortable: true,
      filterable: true,
      type: 'custom',
    },
    {
      key: 'module',
      header: 'Module',
      sortable: true,
      filterable: true,
      type: 'custom',
    },
    {
      key: 'action',
      header: 'Action Type',
      sortable: true,
      filterable: true,
      type: 'custom',
    },
    {
      key: 'entityId',
      header: 'Entity Ref',
      sortable: true,
      filterable: false,
      type: 'custom',
    },
    {
      key: 'isSuccess',
      header: 'Outcome',
      sortable: true,
      filterable: true,
      type: 'custom',
    },
    {
      key: 'isReviewed',
      header: 'Review & Triage',
      sortable: true,
      filterable: true,
      type: 'custom',
    },
    {
      key: 'ipAddress',
      header: 'Client IP',
      sortable: true,
      filterable: false,
      type: 'custom',
    },
    {
      key: 'actions',
      header: 'Diff & Details',
      sortable: false,
      filterable: false,
      type: 'actions',
    },
  ];

  ngOnInit(): void {
    this.loadUnreviewedSecurityCount();
    // Read Default Page Size from System Settings
    this.settingsService.getAllSettings().subscribe({
      next: (res) => {
        const dict = (res as any)?.data || res;
        if (dict) {
          const rawSize = dict['DefaultPageSize'] || dict['PageSize'] || dict['AdminDefaultPageSize'];
          if (rawSize) {
            const parsed = parseInt(rawSize, 10);
            if (!isNaN(parsed) && parsed > 0) {
              this.pageSize.set(parsed);
            }
          }
        }
        this.loadAuditLogs();
      },
      error: () => {
        // Fallback to default page size 5 if settings fetch fails
        this.pageSize.set(5);
        this.loadAuditLogs();
      },
    });
  }

  public loadUnreviewedSecurityCount(): void {
    this.auditService.getAuditLogs({ isSuccess: false, isReviewed: false, pageNumber: 1, pageSize: 1 }).subscribe({
      next: (res) => {
        this.unreviewedSecurityCount.set(res.data?.totalCount || 0);
      },
      error: () => this.unreviewedSecurityCount.set(0),
    });
  }

  public loadAuditLogs(): void {
    this.isLoading.set(true);

    const formValues = this.filterForm.value;
    const tab = this.activeTabId();

    let moduleFilter = formValues.module || '';
    let isSuccessFilter: boolean | undefined = undefined;
    let isReviewedFilter: boolean | undefined = undefined;

    if (formValues.status === 'true') {
      isSuccessFilter = true;
    } else if (formValues.status === 'false') {
      isSuccessFilter = false;
    } else if (formValues.status === 'unreviewed') {
      isSuccessFilter = false;
      isReviewedFilter = false;
    }

    // Apply Tab-level context: Security Alerts tab filters for failures/incidents
    if (tab === 'security') {
      isSuccessFilter = false;
    }

    const filter: AuditLogFilter = {
      searchTerm: formValues.searchTerm || undefined,
      fromDate: formValues.fromDate || undefined,
      toDate: formValues.toDate || undefined,
      module: moduleFilter || undefined,
      isSuccess: isSuccessFilter,
      isReviewed: isReviewedFilter,
      sortBy: this.currentSortBy(),
      sortDirection: this.currentSortDir(),
      pageNumber: this.currentPage(),
      pageSize: this.pageSize(),
    };

    this.auditService.getAuditLogs(filter).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        const data = res.data;
        if (data) {
          this.auditLogs.set(data.items || []);
          this.totalRecords.set(data.totalCount || 0);
        } else {
          this.auditLogs.set([]);
          this.totalRecords.set(0);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.toast.error(
          err.error?.message || 'Failed to load audit logs from server.'
        );
      },
    });
  }

  public acknowledgeLog(row: AuditLog): void {
    if (!row.id) return;
    this.isAcknowledging.set(true);
    this.auditService.acknowledgeLog(row.id).subscribe({
      next: () => {
        this.isAcknowledging.set(false);
        this.toast.success(`Security alert #${row.id} acknowledged and marked as reviewed.`);
        this.loadAuditLogs();
        this.loadUnreviewedSecurityCount();
      },
      error: (err) => {
        this.isAcknowledging.set(false);
        this.toast.error(err.error?.message || 'Failed to acknowledge security incident.');
      },
    });
  }

  public acknowledgeAllSecurity(): void {
    this.isAcknowledging.set(true);
    this.auditService.acknowledgeAll().subscribe({
      next: (res) => {
        this.isAcknowledging.set(false);
        const count = res.data ?? 0;
        this.toast.success(`Successfully acknowledged ${count} pending security alert(s).`);
        this.loadAuditLogs();
        this.loadUnreviewedSecurityCount();
      },
      error: (err) => {
        this.isAcknowledging.set(false);
        this.toast.error(err.error?.message || 'Failed to acknowledge security incidents.');
      },
    });
  }

  // Tab change handler
  public onTabChange(tabId: string): void {
    this.activeTabId.set(tabId);
    this.currentPage.set(1);
    this.loadAuditLogs();
  }

  // Quick Date Preset handler
  public onDatePresetChange(preset: string): void {
    this.filterForm.get('datePreset')?.setValue(preset);

    if (preset !== 'custom') {
      const { fromDate, toDate } = DatePresetUtil.calculateDateRange(preset);
      this.filterForm.patchValue({
        fromDate,
        toDate,
      });
      this.applyFilters();
    }
  }

  // Filter form handlers
  public applyFilters(): void {
    this.currentPage.set(1);
    this.loadAuditLogs();
  }

  public resetFilters(): void {
    this.filterForm.reset({
      searchTerm: '',
      datePreset: 'all',
      fromDate: '',
      toDate: '',
      module: '',
      status: '',
    });
    this.currentPage.set(1);
    this.loadAuditLogs();
    this.toast.info('Audit filters reset to default view.');
  }

  // Pagination & Sorting handlers
  public onPageChange(page: number): void {
    this.currentPage.set(page);
    this.loadAuditLogs();
  }

  public onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1);
    this.loadAuditLogs();
  }

  public onSortChange(sort: TableSortState): void {
    this.currentSortBy.set(sort.column || 'Timestamp');
    this.currentSortDir.set(sort.direction || 'desc');
    this.loadAuditLogs();
  }

  // Check if row has a correlated sibling in current dataset
  public isRowCorrelated(row: AuditLog): boolean {
    return !!this.auditService.computeCorrelatedDiff(row, this.auditLogs())?.hasCorrelation;
  }

  // Diff Modal trigger

  // Determine if record has a meaningful before/after diff
  public hasMeaningfulDiff(row: AuditLog): boolean {
    if (!row) return false;
    const action = (row.action || '').toLowerCase();
    if (
      action.includes('login') ||
      action.includes('logout') ||
      action.includes('otp') ||
      action.includes('token')
    ) {
      return false;
    }
    return this.isMeaningfulJson(row.beforeValuesJson) && this.isMeaningfulJson(row.afterValuesJson);
  }

  public isMeaningfulJson(jsonStr?: string | null): boolean {
    if (!jsonStr) return false;
    const trimmed = jsonStr.trim();
    if (trimmed === '' || trimmed === '{}' || trimmed === '[]' || trimmed === 'null') {
      return false;
    }
    try {
      const parsed = JSON.parse(trimmed);
      if (parsed === null || parsed === undefined) return false;
      if (typeof parsed === 'object') {
        return Object.keys(parsed).length > 0;
      }
      return true;
    } catch {
      return false;
    }
  }

  public openDiffModal(log: AuditLog): void {
    this.selectedLog.set(log);
    this.isDiffModalOpen.set(true);
  }

  public closeDiffModal(): void {
    this.isDiffModalOpen.set(false);
    this.selectedLog.set(null);
  }

  // Export to CSV
  public exportToCsv(): void {
    const data = this.auditLogs();
    if (!data || data.length === 0) {
      this.toast.warning('No audit logs available in current view to export.');
      return;
    }

    const headers = [
      'Audit ID',
      'Timestamp (UTC)',
      'User / Email',
      'Module',
      'Action',
      'Entity Ref',
      'Outcome',
      'Client IP',
      'Trace ID',
      'Description',
    ];

    const rows = data.map((item) => [
      item.id,
      item.timestamp,
      `"${(item.userDisplayName || '').replace(/"/g, '""')}"`,
      `"${item.module}"`,
      `"${item.action}"`,
      `"${item.entityId || ''}"`,
      item.isSuccess ? 'SUCCESS' : 'FAILED',
      `"${item.ipAddress || ''}"`,
      `"${item.traceId || ''}"`,
      `"${(item.description || '').replace(/"/g, '""')}"`,
    ]);

    const csvContent =
      'data:text/csv;charset=utf-8,' +
      [headers.join(','), ...rows.map((e) => e.join(','))].join('\n');

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement('a');
    link.setAttribute('href', encodedUri);
    link.setAttribute(
      'download',
      `AuditTrail_Export_${new Date().toISOString().slice(0, 10)}.csv`
    );
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    this.toast.success('Audit trail CSV export generated successfully.');
  }
}
