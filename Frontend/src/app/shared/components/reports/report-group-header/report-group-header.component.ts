import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-report-group-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-group-header.component.html',
  styleUrls: ['./report-group-header.component.css'],
})
export class ReportGroupHeaderComponent {
  @Input() title = '';
  @Input() badge = '';
  @Input() count?: number;
  @Input() isExpanded = true;
  @Input() submetrics: { label: string; value: string | number }[] = [];

  @Output() toggle = new EventEmitter<void>();
}
