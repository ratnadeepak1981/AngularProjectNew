import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminBillingService, FeeTypeItem } from '../../services/admin-billing';
import { ToastService } from '../../../../../core/services/toast.service';
import { ActionButtonComponent } from '../../../../../shared/components/action-button/action-button.component';
import { SelectDropdownComponent } from '../../../../../shared/components/select-dropdown/select-dropdown.component';
import { DropdownOption } from '../../../../../core/models/common/dropdown-option.model';

@Component({
  selector: 'app-assign-fee-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ActionButtonComponent, SelectDropdownComponent],
  templateUrl: './assign-fee-modal.component.html',
  styleUrl: './assign-fee-modal.component.css',
})
export class AssignFeeModalComponent implements OnChanges {
  private readonly billingService = inject(AdminBillingService);
  private readonly toast = inject(ToastService);

  @Input() isOpen = false;
  @Input() feeTypes: FeeTypeItem[] = [];
  @Input() students: any[] = [];
  @Input() faculties: any[] = [];

  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();
  @Output() triggerAlert = new EventEmitter<{ title: string; message: string; icon: string; variant: 'danger' | 'warning' | 'info' | 'success' }>();
  @Output() triggerConfirm = new EventEmitter<{ title: string; message: string; icon: string; variant: 'primary' | 'danger' | 'warning'; action: () => void }>();

  // Scope Mode: 'single' vs 'faculty'
  public readonly scopeMode = signal<'single' | 'faculty'>('single');

  // Single Student Search & Selection Signals
  public readonly studentSearchTerm = signal<string>('');
  public readonly selectedFacultyFilter = signal<string>('');
  public readonly searchedStudents = signal<any[]>([]);
  public readonly isSearchingStudents = signal<boolean>(false);

  public readonly selectedStudentId = signal<number>(0);
  public readonly selectedStudentLabel = signal<string>('');

  // Faculty Bulk Mode Signal
  public readonly selectedFacultyId = signal<number>(0);

  // Common Fee Assignment Signals
  public readonly selectedFeeTypeId = signal<number>(0);
  public readonly amount = signal<number>(100.0);
  public readonly dueDate = signal<string>('');
  public readonly description = signal<string>('');
  public readonly isSubmitting = signal<boolean>(false);

  public readonly facultyDropdownOptions = computed<DropdownOption[]>(() => {
    return (this.faculties || []).map((f: any) => {
      const facId = f.id || f.Id;
      const facName = (f.name || f.Name || '').toLowerCase().trim();
      const count = (this.students || []).filter((s: any) => {
        const studentFacId = s.facultyId ?? s.FacultyId;
        const studentFacName = (s.facultyName || s.FacultyName || '').toLowerCase().trim();
        const matches = (studentFacId != null && studentFacId === facId) || (facName !== '' && studentFacName === facName);
        const active = s.isActive !== false;
        return matches && active;
      }).length;

      return {
        value: facId,
        label: f.name || f.Name,
        icon: '🏛️',
        description: count > 0 ? `${count} active enrolled student(s)` : 'No active students enrolled',
        count: count,
        countUnit: 'Student',
      };
    });
  });

  public readonly selectedFacultyStudentCount = computed<number>(() => {
    const facId = this.selectedFacultyId();
    if (!facId) return 0;
    const fac = (this.faculties || []).find((f: any) => (f.id || f.Id) === facId);
    const facName = (fac ? (fac.name || fac.Name || '') : '').toLowerCase().trim();
    return (this.students || []).filter((s: any) => {
      const studentFacId = s.facultyId ?? s.FacultyId;
      const studentFacName = (s.facultyName || s.FacultyName || '').toLowerCase().trim();
      const matches = (studentFacId != null && studentFacId === facId) || (facName !== '' && studentFacName === facName);
      const active = s.isActive !== false;
      return matches && active;
    }).length;
  });

  public readonly selectedFacultyName = computed<string>(() => {
    const facId = this.selectedFacultyId();
    if (!facId) return 'Faculty';
    const fac = (this.faculties || []).find((f: any) => (f.id || f.Id) === facId);
    return fac ? (fac.name || fac.Name) : 'Faculty';
  });

  public readonly feeTypeDropdownOptions = computed<DropdownOption[]>(() => {
    return (this.feeTypes || [])
      .filter((t: any) => t.isActive !== false)
      .map((t: any) => ({
        value: t.id || t.Id,
        label: t.name || t.Name,
        icon: '💳',
        description: 'Active System Fee Type',
      }));
  });

  public onFeeTypeSelected(id: number): void {
    this.selectedFeeTypeId.set(id);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.resetForm();
    }
  }

  resetForm(): void {
    this.scopeMode.set('single');
    this.studentSearchTerm.set('');
    this.selectedFacultyFilter.set('');
    this.searchedStudents.set([]);
    this.selectedStudentId.set(0);
    this.selectedStudentLabel.set('');
    this.selectedFacultyId.set(0);

    this.selectedFeeTypeId.set(0);
    this.amount.set(100.0);
    this.description.set('');

    const d = new Date();
    d.setDate(d.getDate() + 30);
    const dateStr = d.toISOString().split('T')[0];
    this.dueDate.set(dateStr);
  }

  onStudentSearchInput(term: string): void {
    this.studentSearchTerm.set(term);
    this.selectedStudentId.set(0);
    this.selectedStudentLabel.set('');

    if (!term || term.trim().length < 1) {
      this.searchedStudents.set([]);
      return;
    }

    this.isSearchingStudents.set(true);
    this.billingService.searchStudents(term.trim(), this.selectedFacultyFilter(), 1, 10).subscribe({
      next: (res) => {
        const payload = res?.data || res || {};
        const items = payload.items || payload.Items || (Array.isArray(payload) ? payload : []);
        this.searchedStudents.set(items);
        this.isSearchingStudents.set(false);
      },
      error: () => {
        this.searchedStudents.set([]);
        this.isSearchingStudents.set(false);
      },
    });
  }

  onFacultyFilterChange(facName: string): void {
    this.selectedFacultyFilter.set(facName);
    if (this.studentSearchTerm().trim()) {
      this.onStudentSearchInput(this.studentSearchTerm());
    }
  }

  selectStudent(s: any): void {
    const sId = s.id || s.Id;
    const name = s.fullName || s.name || s.FullName || 'Student';
    const idx = s.indexNumber || s.IndexNumber || 'N/A';

    this.selectedStudentId.set(sId);
    this.selectedStudentLabel.set(`${name} (${idx})`);
    this.studentSearchTerm.set(`${name} (${idx})`);
    this.searchedStudents.set([]);
  }

  clearSelectedStudent(): void {
    this.selectedStudentId.set(0);
    this.selectedStudentLabel.set('');
    this.studentSearchTerm.set('');
    this.searchedStudents.set([]);
  }

  onClose(): void {
    this.close.emit();
  }

  onSubmit(): void {
    const mode = this.scopeMode();
    const feeTypeId = this.selectedFeeTypeId();
    const amt = this.amount();
    const due = this.dueDate();

    if (!feeTypeId || feeTypeId === 0) {
      this.toast.error('Please select a fee type.');
      return;
    }
    if (amt <= 0) {
      this.triggerAlert.emit({
        title: 'Invalid Fee Amount',
        message: 'The assigned fee amount must be strictly greater than $0.00.',
        icon: '⚠️',
        variant: 'warning',
      });
      return;
    }
    if (!due) {
      this.toast.error('Please select a due date.');
      return;
    }

    const ft = this.feeTypes.find((f) => f.id === feeTypeId);
    const feeName = ft ? ft.name : 'Fee';
    const dueYear = new Date(due).getFullYear();

    if (mode === 'single') {
      const studentId = this.selectedStudentId();
      if (!studentId || studentId === 0) {
        this.toast.error('Please search and select a valid target student.');
        return;
      }

      const label = this.selectedStudentLabel() || 'Student';
      const payload = {
        studentId,
        facultyId: null,
        feeTypeId,
        amount: amt,
        billingPeriod: `${dueYear} Period`,
        description: this.description().trim() || `Standard fee assignment due by ${due}`,
        dueDate: new Date(due).toISOString(),
      };

      this.triggerConfirm.emit({
        title: 'Confirm Fee Assignment',
        message: `Are you sure you want to assign "${feeName}" ($${amt.toFixed(2)}) to ${label}?`,
        icon: '💳',
        variant: 'primary',
        action: () => {
          this.executeAssignment(payload, `Fee "${feeName}" assigned to ${label} successfully!`);
        },
      });
    } else {
      const facultyId = this.selectedFacultyId();
      if (!facultyId || facultyId === 0) {
        this.toast.error('Please select a target faculty for bulk cohort assignment.');
        return;
      }

      const fac = this.faculties.find((f) => (f.id || f.Id) === facultyId);
      const facName = fac ? (fac.name || fac.Name || 'Faculty') : 'Faculty';

      const payload = {
        studentId: null,
        facultyId,
        feeTypeId,
        amount: amt,
        billingPeriod: `${dueYear} Period`,
        description: this.description().trim() || `Faculty cohort fee run due by ${due}`,
        dueDate: new Date(due).toISOString(),
      };

      this.triggerConfirm.emit({
        title: 'Confirm Faculty Bulk Assignment Run',
        message: `Are you sure you want to execute a bulk run of "${feeName}" ($${amt.toFixed(2)}) for ALL students in ${facName}?`,
        icon: '🏛️',
        variant: 'warning',
        action: () => {
          this.executeAssignment(payload, `Bulk fee assignment run executed successfully for ${facName}!`);
        },
      });
    }
  }

  private executeAssignment(payload: any, successMsg: string): void {
    this.isSubmitting.set(true);
    this.billingService.assignFee(payload).subscribe({
      next: () => {
        this.toast.success(successMsg);
        this.isSubmitting.set(false);
        this.onClose();
        this.saved.emit();
      },
      error: (err: any) => {
        this.toast.error(err?.error?.message || 'Failed to execute fee assignment.');
        this.isSubmitting.set(false);
      },
    });
  }
}
