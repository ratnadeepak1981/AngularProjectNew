import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Lab } from '../../../../../core/models/lab/lab.model';
import { LabTimeSlot } from '../../../../../core/models/lab/lab-time-slot.model';
import { StatusBadgeComponent } from '../../../../../shared/components/status-badge/status-badge.component';
import { ActionButtonComponent } from '../../../../../shared/components/action-button/action-button.component';
import { LabManagementService } from '../../services/lab-management.service';
import { ToastService } from '../../../../../core/services/toast.service';

@Component({
  selector: 'app-lab-details',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent, ActionButtonComponent],
  templateUrl: './lab-details.component.html',
  styleUrl: './lab-details.component.css',
})
export class LabDetailsComponent implements OnChanges {
  private readonly labService = inject(LabManagementService);
  private readonly toast = inject(ToastService);

  @Input() lab: Lab | null = null;
  @Input() isModal: boolean = false;
  @Output() inspectLayout = new EventEmitter<Lab>();
  @Output() closeModal = new EventEmitter<void>();
  @Output() createLabSubmitted = new EventEmitter<{ name: string; labType: string; capacity: number; totalRows: number; totalColumns: number }>();

  // Create Lab Modal Signal
  public readonly isCreateModalOpen = signal(false);
  public readonly newLabName = signal('');
  public readonly newLabType = signal<'Computer' | 'Science'>('Computer');
  public readonly newLabCapacity = signal(24);
  public readonly newLabRows = signal(4);
  public readonly newLabCols = signal(3);

  // Time Slot Management Signals
  public readonly timeSlots = signal<LabTimeSlot[]>([]);
  public readonly isLoadingSlots = signal<boolean>(false);
  public readonly showAddSlotForm = signal<boolean>(false);
  public readonly newSlotStartTime = signal<string>('09:00');
  public readonly newSlotEndTime = signal<string>('09:15');
  public readonly newSlotDisplayOrder = signal<number>(1);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['lab'] && this.lab?.id) {
      this.loadTimeSlots();
    }
  }

  loadTimeSlots(): void {
    if (!this.lab?.id) return;
    this.isLoadingSlots.set(true);
    this.labService.getTimeSlots(this.lab.id).subscribe({
      next: (slots) => {
        this.timeSlots.set(slots);
        this.isLoadingSlots.set(false);
      },
      error: () => this.isLoadingSlots.set(false),
    });
  }

  toggleAddSlotForm(): void {
    this.showAddSlotForm.update((v) => !v);
  }

  submitCreateSlot(): void {
    if (!this.lab?.id) return;
    const startTime = this.newSlotStartTime().trim();
    const endTime = this.newSlotEndTime().trim();
    if (!startTime || !endTime) {
      this.toast.warning('Please specify both Start Time and End Time.');
      return;
    }

    this.labService
      .createTimeSlot(this.lab.id, {
        startTime,
        endTime,
        isActive: true,
        displayOrder: this.newSlotDisplayOrder(),
      })
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.toast.success(`Time Slot (${startTime} - ${endTime}) added successfully!`);
            this.showAddSlotForm.set(false);
            this.loadTimeSlots();
          } else {
            this.toast.error(res.message || 'Failed to create time slot.');
          }
        },
      });
  }

  toggleSlotActive(slot: LabTimeSlot): void {
    const nextState = !slot.isActive;
    this.labService.toggleTimeSlotActive(slot.id, nextState).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success(`Time slot ${slot.startTime} - ${slot.endTime} ${nextState ? 'activated' : 'deactivated'}.`);
          this.loadTimeSlots();
        } else {
          this.toast.error(res.message || 'Failed to toggle time slot status.');
        }
      },
    });
  }

  deleteSlot(slot: LabTimeSlot): void {
    this.labService.deleteTimeSlot(slot.id).subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success(`Time slot ${slot.startTime} - ${slot.endTime} deleted successfully.`);
          this.loadTimeSlots();
        } else {
          this.toast.error(res.message || 'Cannot delete time slot referenced by bookings.');
        }
      },
    });
  }

  openCreateModal(): void {
    this.newLabName.set('');
    this.newLabType.set('Computer');
    this.newLabRows.set(4);
    this.newLabCols.set(6);
    this.newLabCapacity.set(24);
    this.isCreateModalOpen.set(true);
  }

  onRowsColsChange(rows: number, cols: number): void {
    this.newLabRows.set(rows);
    this.newLabCols.set(cols);
    if (this.newLabType() === 'Computer') {
      this.newLabCapacity.set((rows || 0) * (cols || 0));
    }
  }

  onLabTypeChange(type: 'Computer' | 'Science'): void {
    this.newLabType.set(type);
    if (type === 'Computer') {
      this.newLabCapacity.set(this.newLabRows() * this.newLabCols());
    }
  }

  closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
  }

  submitCreate(): void {
    const name = this.newLabName().trim();
    if (!name || this.newLabCapacity() <= 0) return;

    this.createLabSubmitted.emit({
      name,
      labType: this.newLabType(),
      capacity: this.newLabCapacity(),
      totalRows: this.newLabRows(),
      totalColumns: this.newLabCols(),
    });

    this.closeCreateModal();
  }
}

