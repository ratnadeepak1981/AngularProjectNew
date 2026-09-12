import {
  Component,
  Input,
  Output,
  EventEmitter,
  ContentChild,
  TemplateRef,
  OnChanges,
  SimpleChanges,
  signal,
  computed,
  inject,
  effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableColumn } from './models/table-column.model';
import { TableSortState } from './models/table-sort.model';
import { PaginationComponent } from '../tables-utilities/pagination/pagination.component';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import { SystemSettingsService } from '../../../core/services/system-settings.service';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginationComponent, StatusBadgeComponent],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.css',
})
export class DataTableComponent implements OnChanges {
  @Input() columns: TableColumn<any>[] = [];
  @Input() data: any[] = [];
  @Input() totalRecords: number = 0;
  @Input() currentPage: number = 1;
  @Input() pageSize: number = 5;
  @Input() isLoading: boolean = false;
  @Input() sortColumn: string = '';
  @Input() sortDirection: 'asc' | 'desc' = 'asc';
  @Input() sortIconStyle: 'svg-arrows' | 'unicode-arrows' | 'minimal' = 'svg-arrows';
  @Input() emptyMessage: string = 'No records found matching criteria.';
  @Input() serverSide: boolean = false;
  @Input() showSearch: boolean = false;
  @Input() searchPlaceholder: string = 'Search records...';

  @ContentChild('cellActions') cellActionsTemplate?: TemplateRef<any>;
  @ContentChild('cellCustom') cellCustomTemplate?: TemplateRef<any>;

  @Output() sortChange = new EventEmitter<TableSortState>();
  @Output() filterChange = new EventEmitter<Record<string, string[]>>();
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  // Global Search Signal
  public readonly globalSearchTerm = signal<string>('');

  // Filter Popover & Sorting Internal Signals
  public readonly activeFilterMenu = signal<string | null>(null);
  public readonly activeFilters = signal<Record<string, string[]>>({});
  public readonly tempSelectedFilters = signal<Record<string, string[]>>({});
  public readonly columnSearchTerms = signal<Record<string, string>>({});

  public readonly inputDataSignal = signal<any[]>([]);
  public readonly currentSortColumn = signal<string>('');
  public readonly currentSortDirection = signal<'asc' | 'desc'>('asc');
  public readonly clientCurrentPage = signal<number>(1);
  public readonly clientPageSize = signal<number>(5);

  private readonly systemSettings = inject(SystemSettingsService, { optional: true });
  private hasUserChangedPageSize = false;

  constructor() {
    if (this.systemSettings) {
      effect(() => {
        const sysSize = this.systemSettings!.defaultPageSize();
        // If user hasn't manually overridden the page size on this table and pageSize input is default/not provided
        if (!this.hasUserChangedPageSize && (!this.pageSize || this.pageSize === 5) && sysSize > 0) {
          this.clientPageSize.set(sysSize);
        }
      });
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data']) {
      this.inputDataSignal.set(this.data || []);
    }
    if (changes['sortColumn']) {
      this.currentSortColumn.set(this.sortColumn || '');
    }
    if (changes['sortDirection']) {
      this.currentSortDirection.set(this.sortDirection || 'asc');
    }
    if (changes['currentPage']) {
      this.clientCurrentPage.set(this.currentPage || 1);
    }
    if (changes['pageSize']) {
      if (this.pageSize && this.pageSize > 0) {
        this.clientPageSize.set(this.pageSize);
      } else {
        const sysSize = this.systemSettings?.defaultPageSize() || 5;
        this.clientPageSize.set(sysSize);
      }
    }
    if (changes['totalRecords'] || changes['data'] || changes['pageSize']) {
      const total = this.serverSide ? (this.totalRecords || 0) : (this.data?.length || 0);
      const size = this.clientPageSize() || 5;
      const maxPage = Math.max(1, Math.ceil(total / size));
      if (this.clientCurrentPage() > maxPage) {
        this.clientCurrentPage.set(maxPage);
        this.pageChange.emit(maxPage);
      }
    }
  }

  public readonly activeFilterCount = computed(() => {
    return Object.keys(this.activeFilters()).filter(
      (k) => (this.activeFilters()[k] || []).length > 0
    ).length;
  });

  // Processed Data Signal (Automatic Client-Side Sorting & Filtering)
  public readonly processedData = computed(() => {
    const raw = this.inputDataSignal();
    if (this.serverSide) {
      return raw;
    }

    let list = [...raw];

    // 1. Global Multi-Column Search Filter
    const search = this.globalSearchTerm().toLowerCase().trim();
    if (search) {
      list = list.filter((row) => {
        return this.columns.some((col) => {
          if (col.type === 'actions' || col.type === 'custom') return false;
          const val = row[col.key];
          if (val === undefined || val === null) return false;
          return String(val).toLowerCase().includes(search);
        });
      });
    }

    // 2. Column Funnel Filters
    const colFilters = this.activeFilters();
    Object.entries(colFilters).forEach(([colKey, allowedVals]) => {
      if (allowedVals && allowedVals.length > 0) {
        list = list.filter((row) => {
          const val = row[colKey];
          const strVal = val !== undefined && val !== null ? String(val) : '';
          return allowedVals.includes(strVal);
        });
      }
    });

    // 2. Column Sorting
    const sCol = this.currentSortColumn();
    const sDir = this.currentSortDirection();
    if (sCol) {
      list.sort((a, b) => {
        let vA: any = a[sCol];
        let vB: any = b[sCol];

        if (vA === undefined || vA === null) vA = '';
        if (vB === undefined || vB === null) vB = '';

        if (typeof vA === 'number' && typeof vB === 'number') {
          return sDir === 'asc' ? vA - vB : vB - vA;
        }

        const dateA = Date.parse(vA);
        const dateB = Date.parse(vB);
        if (!isNaN(dateA) && !isNaN(dateB) && typeof vA === 'string' && (vA.includes('-') || vA.includes('/') || vA.includes(':'))) {
          return sDir === 'asc' ? dateA - dateB : dateB - dateA;
        }

        const strA = String(vA).toLowerCase();
        const strB = String(vB).toLowerCase();
        return sDir === 'asc' ? strA.localeCompare(strB) : strB.localeCompare(strA);
      });
    }

    return list;
  });

  public readonly displayTotalRecords = computed(() => {
    const raw = this.inputDataSignal();
    if (this.serverSide || this.totalRecords > 0) {
      return this.totalRecords || raw.length;
    }
    return this.processedData().length;
  });

  public readonly pagedData = computed(() => {
    const list = this.processedData();
    const size = this.clientPageSize() || 5;

    // Defensive Safeguard: In true serverSide mode, the server returns at most 'size' items.
    // If serverSide is true BUT the list passed has more items than 'size' (e.g. an unpaginated payload),
    // we defensively slice it so the UI table never overflows beyond the selected page size.
    if (this.serverSide && list.length <= size) {
      return list;
    }

    const maxPage = Math.max(1, Math.ceil(list.length / size));
    const page = Math.min(this.clientCurrentPage(), maxPage);
    const start = (page - 1) * size;
    return list.slice(start, start + size);
  });

  // Sorting Handlers
  toggleSort(columnKey: string): void {
    const col = this.columns.find((c) => c.key === columnKey);
    if (!col || col.sortable === false) return;

    let nextDir: 'asc' | 'desc' = 'asc';
    if (this.currentSortColumn() === columnKey) {
      nextDir = this.currentSortDirection() === 'asc' ? 'desc' : 'asc';
    }

    this.currentSortColumn.set(columnKey);
    this.currentSortDirection.set(nextDir);
    this.sortChange.emit({ column: columnKey, direction: nextDir });
  }

  setSort(columnKey: string, direction: 'asc' | 'desc'): void {
    this.currentSortColumn.set(columnKey);
    this.currentSortDirection.set(direction);
    this.sortChange.emit({ column: columnKey, direction });
  }

  // Pagination Handlers
  onPageChange(newPage: number): void {
    this.clientCurrentPage.set(newPage);
    this.pageChange.emit(newPage);
  }

  onGlobalSearchChange(term: string): void {
    this.globalSearchTerm.set(term);
    this.clientCurrentPage.set(1);
  }

  onPageSizeChange(newSize: number): void {
    this.hasUserChangedPageSize = true;
    this.clientPageSize.set(newSize);
    this.clientCurrentPage.set(1);
    this.pageSizeChange.emit(newSize);
  }

  // Filter Popover Menu Controls
  toggleColumnFilter(columnKey: string, event: MouseEvent): void {
    event.stopPropagation();
    if (this.activeFilterMenu() === columnKey) {
      this.activeFilterMenu.set(null);
    } else {
      const currentApplied = this.activeFilters()[columnKey] || [];
      const distinct = this.getDistinctValues(columnKey);
      const initialSelected = currentApplied.length > 0 ? [...currentApplied] : [...distinct];

      this.tempSelectedFilters.set({
        ...this.tempSelectedFilters(),
        [columnKey]: initialSelected,
      });

      this.activeFilterMenu.set(columnKey);
    }
  }

  closeColumnFilter(): void {
    this.activeFilterMenu.set(null);
  }

  isColumnFiltered(columnKey: string): boolean {
    return (this.activeFilters()[columnKey] || []).length > 0;
  }

  getDistinctValues(columnKey: string): string[] {
    const raw = this.inputDataSignal();
    if (!raw) return [];
    const set = new Set<string>();
    raw.forEach((row) => {
      const val = row[columnKey];
      if (val !== undefined && val !== null) {
        set.add(String(val));
      }
    });
    return Array.from(set).sort();
  }

  getFilteredDistinctVals(columnKey: string): string[] {
    const all = this.getDistinctValues(columnKey);
    const search = (this.columnSearchTerms()[columnKey] || '').toLowerCase().trim();
    if (!search) return all;
    return all.filter((v) => v.toLowerCase().includes(search));
  }

  onColSearch(columnKey: string, search: string): void {
    this.columnSearchTerms.set({
      ...this.columnSearchTerms(),
      [columnKey]: search,
    });
  }

  isValSelected(columnKey: string, val: string): boolean {
    const list = this.tempSelectedFilters()[columnKey] || [];
    return list.includes(val);
  }

  toggleValSelection(columnKey: string, val: string): void {
    const list = [...(this.tempSelectedFilters()[columnKey] || [])];
    const idx = list.indexOf(val);
    if (idx > -1) {
      list.splice(idx, 1);
    } else {
      list.push(val);
    }
    this.tempSelectedFilters.set({
      ...this.tempSelectedFilters(),
      [columnKey]: list,
    });
  }

  isAllValSelected(columnKey: string): boolean {
    const filtered = this.getFilteredDistinctVals(columnKey);
    const selected = this.tempSelectedFilters()[columnKey] || [];
    return filtered.length > 0 && filtered.every((v) => selected.includes(v));
  }

  toggleSelectAllVals(columnKey: string): void {
    const filtered = this.getFilteredDistinctVals(columnKey);
    const currentSelected = [...(this.tempSelectedFilters()[columnKey] || [])];
    const allSelected = this.isAllValSelected(columnKey);

    let updated: string[];
    if (allSelected) {
      updated = currentSelected.filter((v) => !filtered.includes(v));
    } else {
      updated = Array.from(new Set([...currentSelected, ...filtered]));
    }

    this.tempSelectedFilters.set({
      ...this.tempSelectedFilters(),
      [columnKey]: updated,
    });
  }

  applyColumnFilter(columnKey: string): void {
    const selected = this.tempSelectedFilters()[columnKey] || [];
    const allVals = this.getDistinctValues(columnKey);

    const isFiltered = selected.length > 0 && selected.length < allVals.length;

    const newActive = { ...this.activeFilters() };
    if (isFiltered) {
      newActive[columnKey] = selected;
    } else {
      delete newActive[columnKey];
    }

    this.clientCurrentPage.set(1);
    this.activeFilters.set(newActive);
    this.filterChange.emit(newActive);
    this.closeColumnFilter();
  }

  clearColumnFilter(columnKey: string): void {
    const newActive = { ...this.activeFilters() };
    delete newActive[columnKey];
    this.activeFilters.set(newActive);

    const newSearch = { ...this.columnSearchTerms() };
    delete newSearch[columnKey];
    this.columnSearchTerms.set(newSearch);

    this.clientCurrentPage.set(1);
    this.filterChange.emit(newActive);
    this.closeColumnFilter();
  }

  clearAllColumnFilters(): void {
    this.activeFilters.set({});
    this.columnSearchTerms.set({});
    this.tempSelectedFilters.set({});
    this.clientCurrentPage.set(1);
    this.filterChange.emit({});
    this.closeColumnFilter();
  }

  getCellValue(row: any, col: TableColumn<any>): any {
    const raw = row[col.key];
    if (col.format) {
      return col.format(raw, row);
    }
    return raw;
  }

  getBadgeConfig(row: any, col: TableColumn<any>): { label: string; class: string } | null {
    if (!col.badgeMap) return null;
    const raw = this.getCellValue(row, col);
    const key = raw !== undefined && raw !== null ? String(raw) : '';
    return col.badgeMap[key] || null;
  }

  getStringValue(val: any): string {
    return val !== undefined && val !== null ? String(val) : '';
  }
}
