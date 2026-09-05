import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-report-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report-pagination.component.html',
  styleUrls: ['./report-pagination.component.css'],
})
export class ReportPaginationComponent {
  @Input() pageNumber = 1;
  @Input() pageSize = 25;
  @Input() totalCount = 0;
  @Input() totalPages = 1;
  @Input() hasPreviousPage = false;
  @Input() hasNextPage = false;

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get startRecord(): number {
    if (this.totalCount === 0) return 0;
    return (this.pageNumber - 1) * this.pageSize + 1;
  }

  get endRecord(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalCount);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) return;
    this.pageChange.emit(page);
  }

  onPageSizeChange(size: number): void {
    this.pageSizeChange.emit(Number(size));
  }

  onPageInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = parseInt(input.value, 10);
    if (!isNaN(val) && val >= 1 && val <= this.totalPages) {
      this.goToPage(val);
    } else {
      input.value = this.pageNumber.toString();
    }
  }
}
