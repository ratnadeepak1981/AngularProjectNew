import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GroupSubtotalItem } from '../models/report-band.model';

@Component({
  selector: 'app-report-group-footer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-group-footer.component.html',
  styleUrls: ['./report-group-footer.component.css'],
})
export class ReportGroupFooterComponent {
  @Input() label = '';
  @Input() subtotals: GroupSubtotalItem[] = [];
}
export type { GroupSubtotalItem };
