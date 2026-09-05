import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportPageSetupComponent } from '../report-page-setup/report-page-setup.component';
import { ReportOrientation, ReportPaperSize } from '../models/report-page-setup.model';
import { ReportExportService } from '../../../../core/services/report-export.service';
import { ExportColumn } from '../../../../core/models/reports/report-export.model';

@Component({
  selector: 'app-report-viewer',
  standalone: true,
  imports: [CommonModule, ReportPageSetupComponent],
  templateUrl: './report-viewer.component.html',
  styleUrls: ['./report-viewer.component.css'],
})
export class ReportViewerComponent {
  private readonly exportService = inject(ReportExportService);

  @Input() documentTitle = 'Institutional Report';
  @Input() exportColumns: ExportColumn[] = [];
  @Input() exportData: any[] = [];
  @Input() grandTotals?: Record<string, any>;

  @Input() orientation: ReportOrientation = 'portrait';
  @Input() paperSize: ReportPaperSize = 'a4';
  @Input() zoom = 100;
  @Input() isFullscreen = false;
  @Input() showToolbar = true;

  @Output() fullscreenChange = new EventEmitter<boolean>();
  @Output() exportPdfTriggered = new EventEmitter<void>();
  @Output() exportExcelTriggered = new EventEmitter<void>();
  @Output() exportWordTriggered = new EventEmitter<void>();
  @Output() exportCsvTriggered = new EventEmitter<void>();

  onToggleFullscreen(): void {
    this.isFullscreen = !this.isFullscreen;
    this.fullscreenChange.emit(this.isFullscreen);
  }

  onOrientationChange(o: ReportOrientation): void {
    this.orientation = o;
  }

  onPaperSizeChange(s: ReportPaperSize): void {
    this.paperSize = s;
  }

  onZoomChange(z: number): void {
    this.zoom = z;
  }

  onPrint(): void {
    this.exportService.printReport();
  }

  onExportPdf(): void {
    if (this.exportPdfTriggered.observed) {
      this.exportPdfTriggered.emit();
    } else {
      this.exportService.exportToPdf();
    }
  }

  onExportExcel(): void {
    if (this.exportExcelTriggered.observed) {
      this.exportExcelTriggered.emit();
    } else {
      this.exportService.exportToExcel(
        `Report_${this.documentTitle.replace(/\s+/g, '_')}`,
        this.documentTitle,
        this.exportColumns,
        this.exportData,
        this.grandTotals
      );
    }
  }

  onExportWord(): void {
    if (this.exportWordTriggered.observed) {
      this.exportWordTriggered.emit();
    } else {
      this.exportService.exportToWord(
        `Report_${this.documentTitle.replace(/\s+/g, '_')}`,
        this.documentTitle,
        this.exportColumns,
        this.exportData
      );
    }
  }

  onExportCsv(): void {
    if (this.exportCsvTriggered.observed) {
      this.exportCsvTriggered.emit();
    } else {
      this.exportService.exportToCsv(
        `Report_${this.documentTitle.replace(/\s+/g, '_')}`,
        this.exportColumns,
        this.exportData,
        this.documentTitle
      );
    }
  }
}
