import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportColumn } from '../models/report-column.model';

@Component({
  selector: 'app-report-detail-grid',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report-detail-grid.component.html',
  styleUrls: ['./report-detail-grid.component.css'],
})
export class ReportDetailGridComponent {
  @Input() columns: ReportColumn[] = [];
  @Input() items: any[] = [];
  @Input() showRowNumbers = true;
  @Input() startIndex = 1;
  @Input() showDrilldown = false;
  @Input() drilldownButtonLabel = 'Details ➔';
  @Input() emptyMessage = 'No report records available for the selected criteria';
  @Input() sortBy = '';
  @Input() sortDirection: 'asc' | 'desc' = 'asc';

  @Output() sort = new EventEmitter<{ sortBy: string; sortDirection: 'asc' | 'desc' }>();
  @Output() drilldown = new EventEmitter<any>();

  // Excel-like Column Header Dropdown Menu State
  activeMenuCol: ReportColumn | null = null;
  columnSearchText = '';
  columnFilters: Record<string, Set<string>> = {};
  tempSelectedValues: Set<string> = new Set();

  get totalColumns(): number {
    return this.columns.length + (this.showRowNumbers ? 1 : 0) + (this.showDrilldown ? 1 : 0);
  }

  get totalActiveFiltersCount(): number {
    return Object.keys(this.columnFilters).filter((k) => this.columnFilters[k] && this.columnFilters[k].size > 0).length;
  }

  getRawValue(row: any, col: ReportColumn): string {
    if (!row) return '';
    const val = row[col.field];
    if (col.formatter) {
      const formatted = col.formatter(val, row);
      return formatted !== null && formatted !== undefined ? String(formatted) : '-';
    }
    if (val === null || val === undefined || val === '') return '(Blanks)';
    if (typeof val === 'boolean') return val ? 'Yes' : 'No';
    return String(val);
  }

  getDistinctValues(col: ReportColumn): string[] {
    if (!this.items || this.items.length === 0) return [];
    const values = new Set<string>();
    for (const row of this.items) {
      values.add(this.getRawValue(row, col));
    }
    return Array.from(values).sort((a, b) =>
      a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' })
    );
  }

  getFilteredDistinctValues(col: ReportColumn): string[] {
    const list = this.getDistinctValues(col);
    if (!this.columnSearchText || !this.columnSearchText.trim()) {
      return list;
    }
    const q = this.columnSearchText.toLowerCase().trim();
    return list.filter((v) => v.toLowerCase().includes(q));
  }

  get displayedItems(): any[] {
    if (!this.items || this.items.length === 0) return [];
    const activeCols = Object.keys(this.columnFilters).filter(
      (k) => this.columnFilters[k] && this.columnFilters[k].size > 0
    );
    if (activeCols.length === 0) {
      return this.items;
    }
    return this.items.filter((row) => {
      return activeCols.every((field) => {
        const col = this.columns.find((c) => c.field === field);
        if (!col) return true;
        const val = this.getRawValue(row, col);
        return this.columnFilters[field].has(val);
      });
    });
  }

  onSort(col: ReportColumn): void {
    if (!col.sortable) return;
    let nextDir: 'asc' | 'desc' = 'asc';
    if (this.sortBy === col.field) {
      nextDir = this.sortDirection === 'asc' ? 'desc' : 'asc';
    }
    this.sort.emit({ sortBy: col.field, sortDirection: nextDir });
  }

  toggleMenu(col: ReportColumn, event: MouseEvent): void {
    event.stopPropagation();
    if (this.activeMenuCol?.field === col.field) {
      this.closeMenu();
      return;
    }
    this.activeMenuCol = col;
    this.columnSearchText = '';
    const existing = this.columnFilters[col.field];
    if (existing && existing.size > 0) {
      this.tempSelectedValues = new Set(existing);
    } else {
      this.tempSelectedValues = new Set(this.getDistinctValues(col));
    }
  }

  closeMenu(): void {
    this.activeMenuCol = null;
    this.columnSearchText = '';
  }

  toggleValue(val: string): void {
    if (this.tempSelectedValues.has(val)) {
      this.tempSelectedValues.delete(val);
    } else {
      this.tempSelectedValues.add(val);
    }
  }

  toggleSelectAll(col: ReportColumn): void {
    const all = this.getDistinctValues(col);
    if (this.tempSelectedValues.size === all.length) {
      this.tempSelectedValues.clear();
    } else {
      this.tempSelectedValues = new Set(all);
    }
  }

  isAllSelected(col: ReportColumn): boolean {
    const all = this.getDistinctValues(col);
    return all.length > 0 && this.tempSelectedValues.size === all.length;
  }

  applyColumnFilter(col: ReportColumn): void {
    const all = this.getDistinctValues(col);
    if (this.tempSelectedValues.size === all.length || this.tempSelectedValues.size === 0) {
      delete this.columnFilters[col.field];
    } else {
      this.columnFilters[col.field] = new Set(this.tempSelectedValues);
    }
    this.closeMenu();
  }

  clearColumnFilter(col: ReportColumn): void {
    delete this.columnFilters[col.field];
    this.closeMenu();
  }

  clearAllColumnFilters(): void {
    this.columnFilters = {};
    this.closeMenu();
  }

  hasActiveFilter(col: ReportColumn): boolean {
    return !!this.columnFilters[col.field] && this.columnFilters[col.field].size > 0;
  }

  applySort(col: ReportColumn, direction: 'asc' | 'desc'): void {
    this.sort.emit({ sortBy: col.field, sortDirection: direction });
    this.closeMenu();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.activeMenuCol) {
      this.closeMenu();
    }
  }

  trackByFn(index: number, item: any): any {
    return item?.id || item?.studentId || item?.bookingId || item?.paymentId || item?.complaintId || item?.requestId || item?.registrationId || item?.notificationId || index;
  }
}
export type { ReportColumn };
