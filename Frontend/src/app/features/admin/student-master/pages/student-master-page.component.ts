import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { StudentMasterService } from '../services/student-master.service';
import { ToastService } from '../../../../core/services/toast.service';
import { StudentMaster } from '../../../../core/models/student/student-master.model';
import { StudentProfile } from '../../../../core/models/auth/student-profile.model';
import { CsvMasterRow } from '../../../../core/models/student/csv-master-row.model';
import { PaginationComponent } from '../../../../shared/components/tables-utilities/pagination/pagination.component';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { AlertModalComponent } from '../../../../shared/components/dialogs/alert-modal/alert-modal.component';
import { TabComponent, TabItem } from '../../../../shared/components/tab-component/tab.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../shared/components/data-table/models/table-column.model';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';

@Component({
  selector: 'app-student-master-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ConfirmModalComponent,
    AlertModalComponent,
    TabComponent,
    PageHeaderComponent,
    DataTableComponent,
    ActionButtonComponent,
  ],
  templateUrl: './student-master-page.component.html',
  styleUrl: './student-master-page.component.css',
})
export class StudentMasterPageComponent implements OnInit {
  private readonly studentMasterService = inject(StudentMasterService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  // Tab State
  public readonly activeTab = signal<'intake' | 'accounts'>('intake');

  // Reusable 3D Admin Tabs Model
  public readonly adminTabs = computed<TabItem[]>(() => [
    {
      id: 'accounts',
      label: 'Registered Student Accounts Directory',
      icon: '👥',
      count: this.accountsTotalRecords(),
    },
    {
      id: 'intake',
      label: 'Student Master Intake List',
      icon: '📋',
      count: this.totalRecords(),
    },
  ]);

  // Common Data Table Column Configurations
  public readonly intakeColumns: TableColumn<any>[] = [
    { key: 'indexNumber', header: 'Index Number', sortable: true, filterable: true },
    { key: 'fullName', header: 'Full Name', sortable: true, filterable: true },
    { key: 'facultyName', header: 'Faculty', sortable: true, filterable: true },
    {
      key: 'registrationStatusText',
      header: 'Registration Status',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        'Account Registered': {
          label: 'Account Registered',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-700',
        },
        'Pending Registration': {
          label: 'Pending Registration',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-700',
        },
      },
    },
  ];

  public readonly accountColumns: TableColumn<any>[] = [
    { key: 'indexNumber', header: 'Index Number', sortable: true, filterable: true },
    { key: 'name', header: 'Full Name', sortable: true, filterable: true },
    { key: 'email', header: 'Email Address', sortable: true, filterable: true },
    { key: 'facultyName', header: 'Faculty', sortable: true, filterable: true },
    {
      key: 'emailVerifiedText',
      header: 'Email Verified',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        Verified: {
          label: 'Verified',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-700',
        },
        'Pending Verification': {
          label: 'Pending Verification',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-bold bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-700',
        },
      },
    },
    {
      key: 'statusText',
      header: 'Account Status',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        Active: {
          label: 'Active',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-700',
        },
        Deactivated: {
          label: 'Deactivated',
          class: 'px-2.5 py-1 rounded-full text-[11px] font-bold bg-rose-100 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-700',
        },
      },
    },
    { key: 'actions', header: 'Actions', sortable: false, filterable: false, type: 'actions', align: 'center' },
  ];

  // Student Master Intake Directory State Signals
  public readonly masterRecords = signal<StudentMaster[]>([]);
  public readonly facultyMap = signal<Map<number, string>>(new Map());
  public readonly isLoading = signal<boolean>(false);
  public readonly searchTerm = signal<string>('');
  public readonly currentPage = signal<number>(1);
  public readonly pageSize = signal<number>(5);
  public readonly totalPages = signal<number>(1);
  public readonly totalRecords = signal<number>(0);

  // Registered Student Accounts State Signals
  public readonly studentAccounts = signal<StudentProfile[]>([]);
  public readonly isAccountsLoading = signal<boolean>(false);
  public readonly accountsSearchTerm = signal<string>('');
  public readonly accountsCurrentPage = signal<number>(1);
  public readonly accountsPageSize = signal<number>(5);
  public readonly accountsTotalPages = signal<number>(1);
  public readonly accountsTotalRecords = signal<number>(0);

  // Registered Student Accounts Excel Column Filter & Sort
  public readonly accSortColumn = signal<string | null>(null);
  public readonly accSortDirection = signal<'asc' | 'desc' | null>(null);
  public readonly accActiveFilterMenu = signal<string | null>(null);
  public readonly accColumnSearchTerms = signal<Record<string, string>>({});
  public readonly accColumnFilters = signal<Record<string, any[]>>({});
  public readonly accTempFilterSelections = signal<Record<string, any[]>>({});

  // Active Filter Count for Student Accounts
  public readonly accActiveFilterCount = computed(() => {
    const filters = this.accColumnFilters();
    let count = 0;
    for (const key of Object.keys(filters)) {
      if (filters[key] && filters[key].length > 0) count++;
    }
    return count;
  });

  // Display Student Accounts with Filtering & Sorting
  public readonly displayStudentAccounts = computed(() => {
    let list = [...this.studentAccounts()];
    const filters = this.accColumnFilters();

    for (const col of Object.keys(filters)) {
      const allowed = filters[col];
      if (allowed && allowed.length > 0) {
        list = list.filter((item: any) => {
          let itemVal = item[col];
          if (col === 'emailVerified') {
            itemVal = item.emailVerified ? 'Verified' : 'Pending Verification';
          } else if (col === 'isActive') {
            itemVal = item.isActive ? 'Active' : 'Deactivated';
          }
          return allowed.includes(itemVal);
        });
      }
    }

    const sortCol = this.accSortColumn();
    const sortDir = this.accSortDirection();
    if (sortCol && sortDir) {
      list.sort((a: any, b: any) => {
        let valA = a[sortCol];
        let valB = b[sortCol];

        if (sortCol === 'emailVerified') {
          valA = a.emailVerified ? 'Verified' : 'Pending Verification';
          valB = b.emailVerified ? 'Verified' : 'Pending Verification';
        } else if (sortCol === 'isActive') {
          valA = a.isActive ? 'Active' : 'Deactivated';
          valB = b.isActive ? 'Active' : 'Deactivated';
        }

        const comp = String(valA ?? '').localeCompare(String(valB ?? ''), undefined, { numeric: true });
        return sortDir === 'asc' ? comp : -comp;
      });
    }

    return list;
  });

  // Reusable Dialogs State
  public readonly isConfirmModalOpen = signal<boolean>(false);
  public readonly isAlertModalOpen = signal<boolean>(false);
  public readonly selectedStudentForDeactivate = signal<StudentProfile | null>(null);
  public readonly isSafetyCheckLoading = signal<boolean>(false);
  public readonly safetyCheckReasons = signal<string[]>([]);
  public readonly isDeactivating = signal<boolean>(false);

  // Excel-Style Column Filter & Sort State Signals
  public readonly sortColumn = signal<string | null>(null);
  public readonly sortDirection = signal<'asc' | 'desc' | null>(null);
  public readonly activeFilterMenu = signal<string | null>(null);
  public readonly columnSearchTerms = signal<Record<string, string>>({});
  public readonly columnFilters = signal<Record<string, any[]>>({});
  public readonly tempFilterSelections = signal<Record<string, any[]>>({});

  // Active Filter Count
  public readonly activeFilterCount = computed(() => {
    const filters = this.columnFilters();
    let count = 0;
    for (const key of Object.keys(filters)) {
      if (filters[key] && filters[key].length > 0) count++;
    }
    return count;
  });

  // Filtered & Sorted Display Records
  public readonly displayRecords = computed(() => {
    let list = [...this.masterRecords()];
    const filters = this.columnFilters();

    for (const col of Object.keys(filters)) {
      const allowed = filters[col];
      if (allowed && allowed.length > 0) {
        list = list.filter((item: any) => {
          let itemVal = item[col];
          if (col === 'facultyId') {
            itemVal = this.getFacultyName(item.facultyId);
          } else if (col === 'isUsed') {
            itemVal = item.isUsed ? 'Account Registered' : 'Pending Registration';
          }
          return allowed.includes(itemVal);
        });
      }
    }

    const sortCol = this.sortColumn();
    const sortDir = this.sortDirection();
    if (sortCol && sortDir) {
      list.sort((a: any, b: any) => {
        let valA = a[sortCol];
        let valB = b[sortCol];

        if (sortCol === 'facultyId') {
          valA = this.getFacultyName(a.facultyId);
          valB = this.getFacultyName(b.facultyId);
        } else if (sortCol === 'isUsed') {
          valA = a.isUsed ? 'Account Registered' : 'Pending Registration';
          valB = b.isUsed ? 'Account Registered' : 'Pending Registration';
        }

        const comp = String(valA ?? '').localeCompare(String(valB ?? ''), undefined, { numeric: true });
        return sortDir === 'asc' ? comp : -comp;
      });
    }

    return list;
  });

  public readonly displayIntakeRecords = computed(() => {
    return this.displayRecords().map((r) => ({
      ...r,
      facultyName: this.getFacultyName(r.facultyId),
      registrationStatusText: r.isUsed ? 'Account Registered' : 'Pending Registration',
    }));
  });

  public readonly displayFormattedStudentAccounts = computed(() => {
    return this.displayStudentAccounts().map((acc) => ({
      ...acc,
      emailVerifiedText: acc.emailVerified ? 'Verified' : 'Pending Verification',
      statusText: acc.isActive ? 'Active' : 'Deactivated',
    }));
  });

  onIntakeSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.sortColumn.set(event.column);
    this.sortDirection.set(event.direction);
  }

  onIntakeFilterChange(filters: Record<string, string[]>): void {
    this.columnFilters.set(filters);
  }

  onAccSortChange(event: { column: string; direction: 'asc' | 'desc' }): void {
    this.accSortColumn.set(event.column);
    this.accSortDirection.set(event.direction);
  }

  onAccFilterChange(filters: Record<string, string[]>): void {
    this.accColumnFilters.set(filters);
  }

  // CSV Import Modal & Validation State
  public readonly isImportModalOpen = signal<boolean>(false);
  public readonly selectedFile = signal<File | null>(null);
  public readonly isParsingCsv = signal<boolean>(false);
  public readonly isUploading = signal<boolean>(false);
  public readonly parsedCsvRows = signal<CsvMasterRow[]>([]);
  public readonly csvHeaderError = signal<string | null>(null);
  public readonly validRowCount = signal<number>(0);
  public readonly errorRowCount = signal<number>(0);

  ngOnInit(): void {
    this.loadFaculties();
    if (this.router.url.includes('/admin/students')) {
      this.activeTab.set('accounts');
      this.loadStudentAccounts();
    } else {
      this.activeTab.set('intake');
      this.loadMasterList();
    }
  }

  setActiveTab(tab: 'intake' | 'accounts'): void {
    this.activeTab.set(tab);
    if (tab === 'accounts' && this.studentAccounts().length === 0) {
      this.loadStudentAccounts();
    } else if (tab === 'intake' && this.masterRecords().length === 0) {
      this.loadMasterList();
    }
  }

  loadStudentAccounts(): void {
    this.isAccountsLoading.set(true);

    this.studentMasterService
      .loadStudentAccounts(this.accountsCurrentPage(), this.accountsPageSize(), this.accountsSearchTerm())
      .subscribe({
        next: (res) => {
          this.isAccountsLoading.set(false);
          const payload = res.data || (res as any);
          const items = payload?.items || (Array.isArray(payload) ? payload : []);
          this.studentAccounts.set(items);
          this.accountsTotalPages.set(payload?.totalPages || 1);
          this.accountsTotalRecords.set(payload?.totalRecords || items.length);
        },
        error: (err) => {
          this.isAccountsLoading.set(false);
          this.toast.error(err.error?.message || 'Failed to load registered student accounts directory.');
        },
      });
  }

  onAccountsSearchChange(): void {
    this.accountsCurrentPage.set(1);
    this.loadStudentAccounts();
  }

  changeAccountsPage(newPage: number): void {
    if (newPage >= 1 && newPage <= this.accountsTotalPages()) {
      this.accountsCurrentPage.set(newPage);
      this.loadStudentAccounts();
    }
  }

  onAccountsPageSizeChange(newSize: number): void {
    this.accountsPageSize.set(newSize);
    this.accountsCurrentPage.set(1);
    this.loadStudentAccounts();
  }

  // Registered Student Accounts Column Filter & Sort
  toggleAccColumnFilter(col: string, event?: Event): void {
    if (event) event.stopPropagation();
    if (this.accActiveFilterMenu() === col) {
      this.closeAccColumnFilter();
      return;
    }

    const currentActive = this.accColumnFilters()[col];
    const distinct = this.getAccDistinctColumnValues(col);
    const initialSelected = currentActive && currentActive.length > 0 ? [...currentActive] : [...distinct];

    this.accTempFilterSelections.update((prev) => ({
      ...prev,
      [col]: initialSelected,
    }));

    this.accActiveFilterMenu.set(col);
  }

  closeAccColumnFilter(): void {
    this.accActiveFilterMenu.set(null);
  }

  setAccColumnSort(col: string, dir: 'asc' | 'desc'): void {
    this.accSortColumn.set(col);
    this.accSortDirection.set(dir);
    this.closeAccColumnFilter();
  }

  toggleAccSort(col: string): void {
    if (this.accSortColumn() !== col) {
      this.accSortColumn.set(col);
      this.accSortDirection.set('asc');
    } else if (this.accSortDirection() === 'asc') {
      this.accSortDirection.set('desc');
    } else if (this.accSortDirection() === 'desc') {
      this.accSortColumn.set(null);
      this.accSortDirection.set(null);
    }
  }

  clearAccColumnSort(): void {
    this.accSortColumn.set(null);
    this.accSortDirection.set(null);
    this.closeAccColumnFilter();
  }

  onAccColSearch(col: string, term: string): void {
    this.accColumnSearchTerms.update((prev) => ({
      ...prev,
      [col]: term,
    }));
  }

  getAccDistinctColumnValues(col: string): any[] {
    const raw = this.studentAccounts();
    const set = new Set<string>();

    raw.forEach((r: any) => {
      let val = r[col];
      if (col === 'emailVerified') {
        val = r.emailVerified ? 'Verified' : 'Pending Verification';
      } else if (col === 'isActive') {
        val = r.isActive ? 'Active' : 'Deactivated';
      }
      if (val !== undefined && val !== null && String(val).trim() !== '') {
        set.add(String(val).trim());
      }
    });

    return Array.from(set).sort((a, b) => a.localeCompare(b, undefined, { numeric: true }));
  }

  getAccFilteredDistinctVals(col: string): any[] {
    const all = this.getAccDistinctColumnValues(col);
    const search = (this.accColumnSearchTerms()[col] || '').toLowerCase().trim();
    if (!search) return all;
    return all.filter((v) => String(v).toLowerCase().includes(search));
  }

  isAccValSelected(col: string, val: any): boolean {
    const temp = this.accTempFilterSelections()[col];
    if (!temp) return true;
    return temp.includes(val);
  }

  toggleAccValSelection(col: string, val: any): void {
    const temp = this.accTempFilterSelections()[col] || this.getAccDistinctColumnValues(col);
    let updated: any[];
    if (temp.includes(val)) {
      updated = temp.filter((x: any) => x !== val);
    } else {
      updated = [...temp, val];
    }
    this.accTempFilterSelections.update((prev) => ({
      ...prev,
      [col]: updated,
    }));
  }

  isAccAllValSelected(col: string): boolean {
    const temp = this.accTempFilterSelections()[col];
    const distinct = this.getAccDistinctColumnValues(col);
    if (!temp) return true;
    return distinct.length > 0 && temp.length === distinct.length;
  }

  toggleAccSelectAllVals(col: string): void {
    const distinct = this.getAccDistinctColumnValues(col);
    if (this.isAccAllValSelected(col)) {
      this.accTempFilterSelections.update((prev) => ({
        ...prev,
        [col]: [],
      }));
    } else {
      this.accTempFilterSelections.update((prev) => ({
        ...prev,
        [col]: [...distinct],
      }));
    }
  }

  applyAccColumnFilter(col: string): void {
    const selected = this.accTempFilterSelections()[col] || [];
    const distinct = this.getAccDistinctColumnValues(col);

    if (selected.length === distinct.length) {
      this.clearAccColumnFilter(col);
      return;
    }

    this.accColumnFilters.update((prev) => ({
      ...prev,
      [col]: selected,
    }));
    this.closeAccColumnFilter();
  }

  clearAccColumnFilter(col: string): void {
    this.accColumnFilters.update((prev) => {
      const next = { ...prev };
      delete next[col];
      return next;
    });
    this.accTempFilterSelections.update((prev) => {
      const next = { ...prev };
      delete next[col];
      return next;
    });
    this.closeAccColumnFilter();
  }

  clearAllAccColumnFilters(): void {
    this.accColumnFilters.set({});
    this.accTempFilterSelections.set({});
    this.accColumnSearchTerms.set({});
    this.closeAccColumnFilter();
  }

  isAccColumnFiltered(col: string): boolean {
    const filter = this.accColumnFilters()[col];
    return !!(filter && filter.length > 0);
  }

  // Safety Deactivation Modal Handlers
  openSafetyModal(student: StudentProfile): void {
    this.selectedStudentForDeactivate.set(student);
    this.isSafetyCheckLoading.set(true);
    this.safetyCheckReasons.set([]);

    this.studentMasterService.checkDeactivateSafety(student.id).subscribe({
      next: (res) => {
        this.isSafetyCheckLoading.set(false);
        const data = res.data || (res as any);
        const canDeactivate = data?.canDeactivate ?? data?.CanDeactivate ?? true;
        const reasons = data?.reasons || data?.Reasons || [];
        this.safetyCheckReasons.set(reasons);

        if (canDeactivate) {
          this.isConfirmModalOpen.set(true);
        } else {
          this.isAlertModalOpen.set(true);
        }
      },
      error: () => {
        this.isSafetyCheckLoading.set(false);
        this.safetyCheckReasons.set([
          'Failed to perform cross-module integrity check. Active records may exist.',
        ]);
        this.isAlertModalOpen.set(true);
      },
    });
  }

  closeConfirmModal(): void {
    this.isConfirmModalOpen.set(false);
    this.selectedStudentForDeactivate.set(null);
  }

  closeAlertModal(): void {
    this.isAlertModalOpen.set(false);
    this.selectedStudentForDeactivate.set(null);
    this.safetyCheckReasons.set([]);
  }

  confirmDeactivation(): void {
    const student = this.selectedStudentForDeactivate();
    if (!student) return;

    this.isDeactivating.set(true);
    this.studentMasterService.deactivateAccount(student.id).subscribe({
      next: () => {
        this.isDeactivating.set(false);
        this.toast.success(`Student account for "${student.name}" was soft-deactivated.`);
        this.closeConfirmModal();
        this.loadStudentAccounts();
      },
      error: (err) => {
        this.isDeactivating.set(false);
        this.toast.error(err.error?.message || 'Failed to deactivate student account.');
      },
    });
  }

  reactivateAccount(student: StudentProfile): void {
    this.studentMasterService.reactivateAccount(student.id).subscribe({
      next: () => {
        this.toast.success(`Student account for "${student.name}" was reactivated successfully.`);
        this.loadStudentAccounts();
      },
      error: (err) => {
        this.toast.error(err.error?.message || 'Failed to reactivate student account.');
      },
    });
  }

  loadFaculties(): void {
    this.studentMasterService.loadFaculties().subscribe({
      next: (map) => {
        this.facultyMap.set(map);
      },
    });
  }

  getFacultyName(facultyId: number): string {
    return this.facultyMap().get(facultyId) || `Faculty #${facultyId}`;
  }

  loadMasterList(): void {
    this.isLoading.set(true);

    this.studentMasterService
      .loadMasterList(this.currentPage(), this.pageSize(), this.searchTerm())
      .subscribe({
        next: (res) => {
          this.isLoading.set(false);
          const payload = res.data || (res as any);
          const items = payload?.items || (Array.isArray(payload) ? payload : []);
          this.masterRecords.set(items);
          this.totalPages.set(payload?.totalPages || 1);
          this.totalRecords.set(payload?.totalRecords || items.length);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.toast.error(err.error?.message || 'Failed to load student master intake directory.');
        },
      });
  }

  onSearchChange(): void {
    this.currentPage.set(1);
    this.loadMasterList();
  }

  changePage(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages()) {
      this.currentPage.set(newPage);
      this.loadMasterList();
    }
  }

  onPageSizeChange(newSize: number): void {
    this.pageSize.set(newSize);
    this.currentPage.set(1);
    this.loadMasterList();
  }

  // Excel Column Filter & Sort Actions
  toggleColumnFilter(col: string, event?: Event): void {
    if (event) event.stopPropagation();
    if (this.activeFilterMenu() === col) {
      this.closeColumnFilter();
      return;
    }

    const currentActive = this.columnFilters()[col];
    const distinct = this.getDistinctColumnValues(col);
    const initialSelected = currentActive && currentActive.length > 0 ? [...currentActive] : [...distinct];

    this.tempFilterSelections.update((prev) => ({
      ...prev,
      [col]: initialSelected,
    }));

    this.activeFilterMenu.set(col);
  }

  closeColumnFilter(): void {
    this.activeFilterMenu.set(null);
  }

  setColumnSort(col: string, dir: 'asc' | 'desc' | null): void {
    this.sortColumn.set(dir ? col : null);
    this.sortDirection.set(dir);
    this.closeColumnFilter();
  }

  toggleSort(col: string): void {
    let nextDir: 'asc' | 'desc' | null = 'asc';
    if (this.sortColumn() === col) {
      if (this.sortDirection() === 'asc') {
        nextDir = 'desc';
      } else if (this.sortDirection() === 'desc') {
        nextDir = null;
      }
    }
    this.sortColumn.set(nextDir ? col : null);
    this.sortDirection.set(nextDir);
  }

  isColumnFiltered(col: string): boolean {
    const f = this.columnFilters()[col];
    return Array.isArray(f) && f.length > 0;
  }

  getDistinctColumnValues(col: string): string[] {
    const records = this.masterRecords();
    if (!records || records.length === 0) return [];

    const set = new Set<string>();
    records.forEach((row) => {
      let val = '';
      if (col === 'indexNumber') val = row.indexNumber;
      else if (col === 'fullName') val = row.fullName;
      else if (col === 'facultyId') val = this.getFacultyName(row.facultyId);
      else if (col === 'isUsed') val = row.isUsed ? 'Account Registered' : 'Pending Registration';

      if (val) set.add(val);
    });

    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }

  getFilteredDistinctVals(col: string): string[] {
    const distinct = this.getDistinctColumnValues(col);
    const search = (this.columnSearchTerms()[col] || '').toLowerCase().trim();
    if (!search) return distinct;
    return distinct.filter((v) => v.toLowerCase().includes(search));
  }

  isValSelected(col: string, val: string): boolean {
    const selected = this.tempFilterSelections()[col] || [];
    return selected.includes(val);
  }

  toggleValSelection(col: string, val: string): void {
    const current = this.tempFilterSelections()[col] || [];
    let updated: string[];
    if (current.includes(val)) {
      updated = current.filter((v) => v !== val);
    } else {
      updated = [...current, val];
    }
    this.tempFilterSelections.update((prev) => ({
      ...prev,
      [col]: updated,
    }));
  }

  isAllValSelected(col: string): boolean {
    const distinct = this.getFilteredDistinctVals(col);
    const selected = this.tempFilterSelections()[col] || [];
    return distinct.length > 0 && distinct.every((v) => selected.includes(v));
  }

  toggleSelectAllVals(col: string): void {
    const distinct = this.getDistinctColumnValues(col);
    const isAll = this.isAllValSelected(col);
    this.tempFilterSelections.update((prev) => ({
      ...prev,
      [col]: isAll ? [] : [...distinct],
    }));
  }

  onColSearch(col: string, term: string): void {
    this.columnSearchTerms.update((prev) => ({
      ...prev,
      [col]: term,
    }));
  }

  applyColumnFilter(col: string): void {
    const selected = this.tempFilterSelections()[col] || [];
    const distinct = this.getDistinctColumnValues(col);
    const isUnrestricted = selected.length === 0 || selected.length === distinct.length;

    this.columnFilters.update((prev) => {
      const copy = { ...prev };
      if (isUnrestricted) {
        delete copy[col];
      } else {
        copy[col] = selected;
      }
      return copy;
    });

    this.closeColumnFilter();
  }

  clearColumnFilter(col: string): void {
    this.columnFilters.update((prev) => {
      const copy = { ...prev };
      delete copy[col];
      return copy;
    });
    this.tempFilterSelections.update((prev) => {
      const copy = { ...prev };
      delete copy[col];
      return copy;
    });
    this.columnSearchTerms.update((prev) => {
      const copy = { ...prev };
      delete copy[col];
      return copy;
    });
    this.closeColumnFilter();
  }

  clearAllColumnFilters(): void {
    this.columnFilters.set({});
    this.tempFilterSelections.set({});
    this.columnSearchTerms.set({});
    this.sortColumn.set(null);
    this.sortDirection.set(null);
    this.closeColumnFilter();
  }

  // Modal Open/Close
  openImportModal(): void {
    this.selectedFile.set(null);
    this.parsedCsvRows.set([]);
    this.csvHeaderError.set(null);
    this.validRowCount.set(0);
    this.errorRowCount.set(0);
    this.isImportModalOpen.set(true);
  }

  closeImportModal(): void {
    this.isImportModalOpen.set(false);
    this.selectedFile.set(null);
    this.parsedCsvRows.set([]);
  }

  // Client-Side CSV Stream Parser & Validation Engine
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.toast.warning('Please select a valid .csv file format.');
      input.value = '';
      return;
    }

    this.selectedFile.set(file);
    this.isParsingCsv.set(true);
    this.csvHeaderError.set(null);
    this.parsedCsvRows.set([]);

    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      this.studentMasterService.loadAllMasterRecords().subscribe({
        next: (allRecords) => {
          const combinedRecords = allRecords.length > 0 ? allRecords : this.masterRecords();
          const existingDbIndices = new Set<string>(
            combinedRecords
              .map((r: any) => (r.indexNumber || r.IndexNumber || '').toUpperCase().trim())
              .filter(Boolean)
          );
          const result = this.studentMasterService.validateCsvContent(text, existingDbIndices);
          this.parsedCsvRows.set(result.rows);
          this.validRowCount.set(result.validCount);
          this.errorRowCount.set(result.errorCount);
          this.csvHeaderError.set(result.headerError);
          this.isParsingCsv.set(false);
        },
        error: () => {
          const existingDbIndices = new Set<string>(
            this.masterRecords()
              .map((r: any) => (r.indexNumber || r.IndexNumber || '').toUpperCase().trim())
              .filter(Boolean)
          );
          const result = this.studentMasterService.validateCsvContent(text, existingDbIndices);
          this.parsedCsvRows.set(result.rows);
          this.validRowCount.set(result.validCount);
          this.errorRowCount.set(result.errorCount);
          this.csvHeaderError.set(result.headerError);
          this.isParsingCsv.set(false);
        },
      });
    };

    reader.onerror = () => {
      this.isParsingCsv.set(false);
      this.toast.error('Unable to read selected CSV file.');
    };

    reader.readAsText(file);
  }

  // Upload to Backend
  submitImportCSV(): void {
    const file = this.selectedFile();
    if (!file) {
      this.toast.error('Please select a CSV file to upload.');
      return;
    }

    if (this.errorRowCount() > 0) {
      this.toast.warning(
        `Please resolve the ${this.errorRowCount()} validation error(s) in your CSV before importing.`
      );
      return;
    }

    if (this.validRowCount() === 0) {
      this.toast.error('No valid records found in the CSV file.');
      return;
    }

    this.isUploading.set(true);

    this.studentMasterService.uploadMasterCsv(file).subscribe({
      next: (res) => {
        this.isUploading.set(false);
        const count = res.data ?? 0;
        if (count > 0) {
          this.toast.success(`Successfully imported ${count} new student master record(s)!`);
        } else {
          this.toast.info('All records in the CSV file already exist in the database master list.');
        }
        this.closeImportModal();
        this.loadMasterList();
      },
      error: (err) => {
        this.isUploading.set(false);
        this.toast.error(err.error?.message || 'Failed to upload and import CSV master file.');
      },
    });
  }

  // Admin Reset Student Password State & Methods
  public readonly isResetPasswordModalOpen = signal<boolean>(false);
  public readonly selectedStudentForReset = signal<StudentProfile | null>(null);
  public readonly isResettingPassword = signal<boolean>(false);

  openResetPasswordModal(student: StudentProfile): void {
    this.selectedStudentForReset.set(student);
    this.isResetPasswordModalOpen.set(true);
  }

  closeResetPasswordModal(): void {
    this.isResetPasswordModalOpen.set(false);
    this.selectedStudentForReset.set(null);
  }

  confirmResetPassword(): void {
    const student = this.selectedStudentForReset();
    if (!student) return;

    this.isResettingPassword.set(true);
    this.studentMasterService.resetStudentPassword(student.id).subscribe({
      next: (res) => {
        this.isResettingPassword.set(false);
        this.closeResetPasswordModal();
        this.toast.success(`Password reset link successfully dispatched to ${student.name}'s registered email.`);
      },
      error: (err) => {
        this.isResettingPassword.set(false);
        this.toast.error(err.error?.message || 'Failed to reset student password.');
      },
    });
  }
}
