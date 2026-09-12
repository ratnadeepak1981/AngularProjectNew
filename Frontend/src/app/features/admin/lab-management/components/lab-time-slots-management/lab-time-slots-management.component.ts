import { Component, OnInit, inject, signal, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { DataTableComponent } from '../../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../../shared/components/data-table/models/table-column.model';
import { ActionButtonComponent } from '../../../../../shared/components/action-button/action-button.component';
import { ConfirmModalComponent } from '../../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { TimePickerComponent } from '../../../../../shared/components/time-picker/time-picker.component';

import { Lab } from '../../../../../core/models/lab/lab.model';
import { LabTimeSlot, CreateLabTimeSlotDto } from '../../../../../core/models/lab/lab-time-slot.model';
import { LabManagementService } from '../../services/lab-management.service';
import { ToastService } from '../../../../../core/services/toast.service';

@Component({
  selector: 'app-lab-time-slots-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DataTableComponent,
    ActionButtonComponent,
    ConfirmModalComponent,
    TimePickerComponent,
  ],
  templateUrl: './lab-time-slots-management.component.html',
  styleUrl: './lab-time-slots-management.component.css',
})
export class LabTimeSlotsManagementComponent implements OnInit {
  private readonly labService = inject(LabManagementService);
  private readonly toast = inject(ToastService);

  public readonly labs = input<Lab[]>([]);

  // State Signals
  public readonly allSlots = signal<LabTimeSlot[]>([]);
  public readonly isLoading = signal<boolean>(false);
  public readonly pageSize = signal<number>(5);

  // Table Column Configuration (Matching Image 1 / Booking History Table Structure)
  public readonly columns: TableColumn<LabTimeSlot>[] = [
    { key: 'displayOrder', header: 'ORDER', sortable: true, filterable: true, type: 'text', align: 'left', width: '90px' },
    { key: 'labName', header: 'LABORATORY FACILITY', sortable: true, filterable: true, type: 'text', align: 'left' },
    { key: 'startTime', header: 'START TIME', sortable: true, filterable: true, type: 'text', align: 'left' },
    { key: 'endTime', header: 'END TIME', sortable: true, filterable: true, type: 'text', align: 'left' },
    {
      key: 'isActive',
      header: 'AVAILABILITY STATUS',
      sortable: true,
      filterable: true,
      type: 'badge',
      align: 'left',
      badgeMap: {
        true: {
          label: '✓ Active',
          class: 'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-50 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800',
        },
        false: {
          label: '✕ Inactive',
          class: 'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-rose-50 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800',
        },
      },
      format: (val: any) => (val === true || val === 'true' ? 'true' : 'false'),
    },
    { key: 'actions', header: 'MANAGEMENT ACTIONS', sortable: false, filterable: false, type: 'actions', align: 'right' },
  ];

  // Create Modal Signals
  public readonly isCreateModalOpen = signal<boolean>(false);
  public readonly newSlotLabId = signal<number>(0);
  public readonly newSlotStartTime = signal<string>('09:00 AM');
  public readonly newSlotEndTime = signal<string>('11:00 AM');
  public readonly newSlotDisplayOrder = signal<number>(1);
  public readonly isSubmitting = signal<boolean>(false);

  // Confirm Modal Dialog State
  public readonly isConfirmModalOpen = signal<boolean>(false);
  public readonly modalTitle = signal<string>('Confirm Action');
  public readonly modalMessage = signal<string>('');
  public readonly modalIcon = signal<string>('⚙️');
  public readonly modalIconVariant = signal<'danger' | 'warning' | 'primary' | 'info'>('warning');
  public readonly modalConfirmText = signal<string>('Confirm');
  public readonly modalConfirmIcon = signal<string>('✅');
  public readonly modalConfirmVariant = signal<'danger' | 'primary' | 'warning'>('primary');
  
  public modalActionType: 'toggle' | 'delete' = 'toggle';
  public targetSlot: LabTimeSlot | null = null;

  ngOnInit(): void {
    this.loadAllTimeSlots();
  }

  public loadAllTimeSlots(): void {
    this.isLoading.set(true);
    const labsList = this.labs();

    if (labsList.length === 0) {
      this.labService.getLabs().subscribe({
        next: (fetchedLabs) => {
          this.fetchSlotsForLabs(fetchedLabs);
        },
        error: () => this.isLoading.set(false),
      });
    } else {
      this.fetchSlotsForLabs(labsList);
    }
  }

  private fetchSlotsForLabs(labsList: Lab[]): void {
    if (labsList.length === 0) {
      this.allSlots.set([]);
      this.isLoading.set(false);
      return;
    }

    const aggregated: LabTimeSlot[] = [];
    let completed = 0;

    for (const lab of labsList) {
      this.labService.getTimeSlots(lab.id).subscribe({
        next: (slots) => {
          for (const s of slots) {
            aggregated.push({
              ...s,
              labName: lab.name,
            });
          }
          completed++;
          if (completed === labsList.length) {
            this.allSlots.set(aggregated);
            this.isLoading.set(false);
          }
        },
        error: () => {
          completed++;
          if (completed === labsList.length) {
            this.allSlots.set(aggregated);
            this.isLoading.set(false);
          }
        },
      });
    }
  }

  // Open Create Modal
  public openCreateModal(): void {
    const labsList = this.labs();
    if (labsList.length > 0) {
      this.newSlotLabId.set(labsList[0].id);
    } else {
      this.newSlotLabId.set(1);
    }
    this.newSlotStartTime.set('09:00 AM');
    this.newSlotEndTime.set('11:00 AM');
    this.newSlotDisplayOrder.set(this.allSlots().length + 1);
    this.isCreateModalOpen.set(true);
  }

  public closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
  }

  public submitCreateSlot(): void {
    if (this.isSubmitting()) return;

    const labId = this.newSlotLabId();
    const startTime = this.newSlotStartTime().trim();
    const endTime = this.newSlotEndTime().trim();
    const order = this.newSlotDisplayOrder();

    if (!labId || !startTime || !endTime) {
      this.toast.warning('Please enter valid Start Time and End Time.');
      return;
    }

    const dto: CreateLabTimeSlotDto = {
      labId,
      startTime,
      endTime,
      displayOrder: order,
    };

    this.isSubmitting.set(true);
    this.labService.createTimeSlot(labId, dto).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.success) {
          this.toast.success(`Time slot '${startTime} - ${endTime}' created successfully!`);
          this.closeCreateModal();
          this.loadAllTimeSlots();
        } else {
          this.toast.error(res.message || 'Failed to create time slot.');
        }
      },
      error: () => {
        this.isSubmitting.set(false);
        this.toast.error('Failed to create time slot.');
      },
    });
  }

  // Prompt Toggle Active Confirmation Modal
  public promptToggleActive(slot: LabTimeSlot): void {
    this.targetSlot = slot;
    this.modalActionType = 'toggle';
    const action = slot.isActive ? 'Deactivate' : 'Activate';
    
    this.modalTitle.set(`${action} Time Slot`);
    this.modalMessage.set(
      `Are you sure you want to ${action.toLowerCase()} time slot '${slot.startTime} - ${slot.endTime}' for ${slot.labName || 'Lab ID: ' + slot.labId}?`
    );
    this.modalIcon.set(slot.isActive ? '⚙️' : '⚡');
    this.modalIconVariant.set(slot.isActive ? 'warning' : 'primary');
    this.modalConfirmText.set(`${action} Time Slot`);
    this.modalConfirmIcon.set('✅');
    this.modalConfirmVariant.set(slot.isActive ? 'warning' : 'primary');
    this.isConfirmModalOpen.set(true);
  }

  // Prompt Delete Confirmation Modal
  public promptDelete(slot: LabTimeSlot): void {
    this.targetSlot = slot;
    this.modalActionType = 'delete';
    
    this.modalTitle.set('Delete Time Slot');
    this.modalMessage.set(
      `Are you sure you want to permanently delete time slot '${slot.startTime} - ${slot.endTime}' for ${slot.labName || 'Lab ID: ' + slot.labId}? This action cannot be undone.`
    );
    this.modalIcon.set('🗑️');
    this.modalIconVariant.set('danger');
    this.modalConfirmText.set('Delete Time Slot');
    this.modalConfirmIcon.set('🗑️');
    this.modalConfirmVariant.set('danger');
    this.isConfirmModalOpen.set(true);
  }

  public onModalConfirm(): void {
    if (!this.targetSlot || this.isSubmitting()) return;
    this.isConfirmModalOpen.set(false);

    if (this.modalActionType === 'toggle') {
      this.executeToggleActive(this.targetSlot);
    } else if (this.modalActionType === 'delete') {
      this.executeDelete(this.targetSlot);
    }
  }

  public onModalCancel(): void {
    this.isConfirmModalOpen.set(false);
    this.targetSlot = null;
  }

  private executeToggleActive(slot: LabTimeSlot): void {
    const nextState = !slot.isActive;
    this.isSubmitting.set(true);

    this.labService.toggleTimeSlotActive(slot.id, nextState).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.success) {
          this.toast.success(
            `Time slot '${slot.startTime} - ${slot.endTime}' is now ${nextState ? 'Active' : 'Inactive'}.`
          );
          this.loadAllTimeSlots();
        } else {
          this.toast.error(res.message || 'Failed to update time slot status.');
        }
      },
      error: () => {
        this.isSubmitting.set(false);
        this.toast.error('Failed to update time slot status.');
      },
    });
  }

  private executeDelete(slot: LabTimeSlot): void {
    this.isSubmitting.set(true);

    this.labService.deleteTimeSlot(slot.id).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.success) {
          this.toast.success(`Time slot '${slot.startTime} - ${slot.endTime}' deleted successfully.`);
          this.loadAllTimeSlots();
        } else {
          this.toast.error(res.message || 'Failed to delete time slot.');
        }
      },
      error: () => {
        this.isSubmitting.set(false);
        this.toast.error('Failed to delete time slot.');
      },
    });
  }
}
