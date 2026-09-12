import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.css',
})
export class PaginationComponent {
  // Configurable Inputs
  public readonly currentPage = input<number>(1);
  public readonly pageSize = input<number>(5);
  public readonly totalRecords = input<number>(0);
  public readonly pageSizeOptions = input<number[]>([5, 10, 20, 50, 100]);
  public readonly showPageSizeOptions = input<boolean>(true);
  public readonly showInfo = input<boolean>(true);

  // Event Outputs
  public readonly pageChange = output<number>();
  public readonly pageSizeChange = output<number>();

  // Dynamic Options including active page size
  public readonly effectivePageSizeOptions = computed<number[]>(() => {
    const rawOptions = this.pageSizeOptions() || [5, 10, 20, 50, 100];
    const current = this.pageSize();
    if (current && !rawOptions.includes(current)) {
      return Array.from(new Set([...rawOptions, current])).sort((a, b) => a - b);
    }
    return rawOptions;
  });

  // Reactive Computed Properties
  public readonly totalPages = computed(() => {
    const total = this.totalRecords();
    const size = this.pageSize() || 5;
    return Math.max(1, Math.ceil(total / size));
  });

  public readonly startRecord = computed(() => {
    if (this.totalRecords() === 0) return 0;
    return (this.currentPage() - 1) * this.pageSize() + 1;
  });

  public readonly endRecord = computed(() => {
    return Math.min(this.currentPage() * this.pageSize(), this.totalRecords());
  });

  public readonly isFirstPage = computed(() => this.currentPage() <= 1);
  public readonly isLastPage = computed(() => this.currentPage() >= this.totalPages());

  // Smart page numbers calculation with ellipsis
  public readonly displayedPages = computed<(number | string)[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();

    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    const pages: (number | string)[] = [];

    // Always include page 1
    pages.push(1);

    if (current > 3) {
      pages.push('...');
    }

    // Window around current page
    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    if (current < total - 2) {
      pages.push('...');
    }

    // Always include last page
    pages.push(total);

    return pages;
  });

  public goToFirst(): void {
    if (!this.isFirstPage()) {
      this.pageChange.emit(1);
    }
  }

  public goToPrevious(): void {
    if (!this.isFirstPage()) {
      this.pageChange.emit(this.currentPage() - 1);
    }
  }

  public goToNext(): void {
    if (!this.isLastPage()) {
      this.pageChange.emit(this.currentPage() + 1);
    }
  }

  public goToLast(): void {
    if (!this.isLastPage()) {
      this.pageChange.emit(this.totalPages());
    }
  }

  public goToPage(page: number | string): void {
    if (typeof page === 'number' && page >= 1 && page <= this.totalPages() && page !== this.currentPage()) {
      this.pageChange.emit(page);
    }
  }

  public onPageSizeSelect(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newSize = parseInt(select.value, 10);
    if (!isNaN(newSize) && newSize > 0) {
      this.pageSizeChange.emit(newSize);
    }
  }
}
