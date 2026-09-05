import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportOrientation, ReportPaperSize } from '../models/report-page-setup.model';

@Component({
  selector: 'app-report-page-setup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report-page-setup.component.html',
  styleUrls: ['./report-page-setup.component.css'],
})
export class ReportPageSetupComponent {
  @Input() orientation: ReportOrientation = 'portrait';
  @Input() paperSize: ReportPaperSize = 'a4';
  @Input() zoom = 100;
  @Input() isFullscreen = false;

  @Output() orientationChange = new EventEmitter<ReportOrientation>();
  @Output() paperSizeChange = new EventEmitter<ReportPaperSize>();
  @Output() zoomChange = new EventEmitter<number>();
  @Output() toggleFullscreen = new EventEmitter<void>();

  @Output() print = new EventEmitter<void>();
  @Output() exportPdf = new EventEmitter<void>();
  @Output() exportExcel = new EventEmitter<void>();
  @Output() exportWord = new EventEmitter<void>();
  @Output() exportCsv = new EventEmitter<void>();

  setOrientation(o: ReportOrientation): void {
    this.orientation = o;
    this.orientationChange.emit(o);
  }

  onPaperSizeChange(s: ReportPaperSize): void {
    this.paperSizeChange.emit(s);
  }

  onZoomChange(z: number): void {
    this.zoomChange.emit(z);
  }
}
export type { ReportOrientation, ReportPaperSize };
