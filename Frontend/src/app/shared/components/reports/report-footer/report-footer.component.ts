import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GrandTotalMetric } from '../models/report-band.model';

@Component({
  selector: 'app-report-footer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-footer.component.html',
  styleUrls: ['./report-footer.component.css'],
})
export class ReportFooterComponent {
  @Input() grandTotals: GrandTotalMetric[] = [];
  @Input() showSignOff = true;
  @Input() preparedBy = 'System Administrator';
  @Input() institutionName = 'University of Knowledge (UOK)';
  @Input() printedDateTime: Date | string = new Date();
  @Input() systemInfo = 'Campus Services Portal v2.0 • ISO-9001 Compliant';
}
export type { GrandTotalMetric };
