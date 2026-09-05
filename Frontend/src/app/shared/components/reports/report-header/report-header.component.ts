import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-report-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-header.component.html',
  styleUrls: ['./report-header.component.css'],
})
export class ReportHeaderComponent {
  @Input() title = 'Institutional Report';
  @Input() subtitle = '';
  @Input() institutionName = 'University of Knowledge (UOK)';
  @Input() academicTerm = '';
  @Input() generatedBy = 'Administrator';
  @Input() generatedDate: Date | string = new Date();
  @Input() filterChips: string[] = [];
}
