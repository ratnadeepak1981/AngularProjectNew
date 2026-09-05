import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportDetailGridComponent } from '../report-detail-grid/report-detail-grid.component';
import { ReportColumn } from '../models/report-column.model';
import { ReportExportService } from '../../../../core/services/report-export.service';
import { ExportColumn } from '../../../../core/models/reports/report-export.model';

@Component({
  selector: 'app-report-subreport',
  standalone: true,
  imports: [CommonModule, ReportDetailGridComponent],
  templateUrl: './report-subreport.component.html',
  styleUrls: ['./report-subreport.component.css'],
})
export class ReportSubreportComponent {
  private readonly exportService = inject(ReportExportService);

  @Input() isOpen = false;
  @Input() title = 'Subreport Details';
  @Input() parentCategory = '';
  @Input() columns: ReportColumn[] = [];
  @Input() items: any[] = [];
  @Input() loading = false;

  @Output() close = new EventEmitter<void>();

  exportExcel(): void {
    const expCols: ExportColumn[] = this.columns.map((c) => ({
      header: c.header,
      field: c.field,
      align: c.align,
      formatter: c.formatter,
    }));
    this.exportService.exportToExcel(`Subreport_${this.parentCategory.replace(/\s+/g, '_')}`, this.title, expCols, this.items);
  }

  exportCsv(): void {
    const expCols: ExportColumn[] = this.columns.map((c) => ({
      header: c.header,
      field: c.field,
      align: c.align,
      formatter: c.formatter,
    }));
    this.exportService.exportToCsv(`Subreport_${this.parentCategory.replace(/\s+/g, '_')}`, expCols, this.items, this.title);
  }

  print(): void {
    this.exportService.printReport();
  }

  exportPdf(): void {
    this.exportService.exportToPdf();
  }
}
