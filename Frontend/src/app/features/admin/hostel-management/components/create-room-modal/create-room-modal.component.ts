import { Component, EventEmitter, Input, Output, signal, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HostelManagementService, HostelBuilding, HostelRoom } from '../../services/hostel-management.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { ActionButtonComponent } from '../../../../../shared/components/action-button/action-button.component';

@Component({
  selector: 'app-create-room-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ActionButtonComponent],
  templateUrl: './create-room-modal.component.html',
  styleUrl: './create-room-modal.component.css',
})
export class CreateRoomModalComponent implements OnChanges {
  private readonly hostelService = inject(HostelManagementService);
  private readonly toast = inject(ToastService);

  @Input() isOpen = false;
  @Input() hostels: HostelBuilding[] = [];
  @Input() initialHostelId = 0;
  @Input() roomToEdit: HostelRoom | null = null;

  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  public readonly selectedHostelId = signal<number>(0);
  public readonly roomNumber = signal<string>('');
  public readonly maxCapacity = signal<number>(2);
  public readonly isSubmitting = signal<boolean>(false);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && !this.roomToEdit) {
      this.roomNumber.set('');
      this.maxCapacity.set(2);
    }
    if (changes['roomToEdit'] && this.roomToEdit) {
      this.roomNumber.set(this.roomToEdit.roomNumber || '');
      this.maxCapacity.set(this.roomToEdit.maxCapacity || 2);
    }
    if (changes['initialHostelId'] && this.initialHostelId) {
      this.selectedHostelId.set(this.initialHostelId);
    }
  }

  onClose(): void {
    this.close.emit();
  }

  onSubmit(): void {
    if (this.isSubmitting()) return;

    const roomNum = this.roomNumber().trim();
    const capacity = this.maxCapacity();
    if (!roomNum || capacity <= 0) {
      this.toast.error('Please enter valid room details and capacity.');
      return;
    }

    this.isSubmitting.set(true);

    if (this.roomToEdit) {
      this.hostelService.updateRoom(this.roomToEdit.id, roomNum, capacity).subscribe({
        next: () => {
          this.toast.success(`Room "${roomNum}" updated successfully.`);
          this.isSubmitting.set(false);
          this.onClose();
          this.saved.emit();
        },
        error: (err: any) => {
          this.toast.error(err?.error?.message || 'Failed to update room.');
          this.isSubmitting.set(false);
        },
      });
    } else {
      const hostelId = this.selectedHostelId() || (this.hostels[0]?.id || 0);
      this.hostelService.createRoom(hostelId, roomNum, capacity).subscribe({
        next: () => {
          this.toast.success(`Room "${roomNum}" created successfully.`);
          this.isSubmitting.set(false);
          this.onClose();
          this.saved.emit();
        },
        error: (err: any) => {
          this.toast.error(err?.error?.message || 'Failed to create room.');
          this.isSubmitting.set(false);
        },
      });
    }
  }
}
