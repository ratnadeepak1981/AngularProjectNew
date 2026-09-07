import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { LabSeat } from '../../../../core/models/lab/lab-seat.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-lab-grid-matrix',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    StatusBadgeComponent,
    ActionButtonComponent,
    ConfirmModalComponent,
  ],
  templateUrl: './lab-grid-matrix.component.html',
  styleUrl: './lab-grid-matrix.component.css',
})
export class LabGridMatrixComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  @Input() labId: number = 1;
  @Input() labName: string = 'Campus Laboratory';
  @Input() totalRows: number = 4;
  @Input() totalCols: number = 3;
  @Input() seats: LabSeat[] = [];
  @Input() isModal: boolean = false;
  @Input() isStudent: boolean = false;
  @Input() showBookingStatus: boolean = true;

  @Output() seatAdded = new EventEmitter<{ seatNumber: string; row: number; col: number }>();
  @Output() seatRemoved = new EventEmitter<number>();
  @Output() closeModal = new EventEmitter<void>();
  @Output() seatSelected = new EventEmitter<LabSeat>();

  // Reactive 2D Form Matrix Structure: Root FormGroup -> rows (FormArray) -> cells (FormArray)
  public readonly matrixForm: FormGroup = this.fb.group({
    rows: this.fb.array<AbstractControl>([]),
  });

  // Confirmation Modal Signals
  public readonly isAddConfirmOpen = signal(false);
  public readonly selectedCell = signal<{ row: number; col: number } | null>(null);
  public readonly pendingSeatNumber = signal<string>('');

  public readonly isRemoveConfirmOpen = signal(false);
  public readonly selectedSeatToRemove = signal<LabSeat | null>(null);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['seats'] || changes['totalRows'] || changes['totalCols']) {
      this.rebuildMatrixFormArray();
    }
  }

  /**
   * Rebuilds the 2D FormArray matrix structure from totalRows x totalCols inputs
   */
  private rebuildMatrixFormArray(): void {
    const rowsArray = new FormArray<AbstractControl>([]);
    const rows = Math.max(1, this.totalRows);
    const cols = Math.max(1, this.totalCols);

    for (let r = 1; r <= rows; r++) {
      const colsArray = new FormArray<AbstractControl>([]);
      for (let c = 1; c <= cols; c++) {
        const existingSeat = this.seats.find((s) => s.rowIndex === r && s.columnIndex === c);
        const cellGroup = this.fb.group({
          rowIndex: [r],
          columnIndex: [c],
          seat: [existingSeat || null],
        });
        colsArray.push(cellGroup);
      }
      rowsArray.push(colsArray);
    }

    this.matrixForm.setControl('rows', rowsArray);
  }

  get matrixRows(): FormArray {
    return this.matrixForm.get('rows') as FormArray;
  }

  getRowCells(rowIndex: number): FormArray {
    return this.matrixRows.at(rowIndex - 1) as FormArray;
  }

  getCellGroup(row: number, col: number): FormGroup | null {
    const rowArray = this.getRowCells(row);
    if (!rowArray) return null;
    return rowArray.at(col - 1) as FormGroup;
  }

  /**
   * Helper array for 1-indexed row loop (1..totalRows)
   */
  get rowsArray(): number[] {
    return Array.from({ length: Math.max(1, this.totalRows) }, (_, i) => i + 1);
  }

  /**
   * Helper array for 1-indexed col loop (1..totalCols)
   */
  get colsArray(): number[] {
    return Array.from({ length: Math.max(1, this.totalCols) }, (_, i) => i + 1);
  }

  /**
   * Get seat object located at 1-indexed (row, col) from the 2D FormArray matrix
   */
  getSeatAt(row: number, col: number): LabSeat | undefined {
    const cellGroup = this.getCellGroup(row, col);
    if (!cellGroup) return undefined;
    return cellGroup.get('seat')?.value || undefined;
  }

  /**
   * Cell Click Handler for 1-indexed Address (row, col)
   */
  onCellClick(row: number, col: number): void {
    if (this.isStudent) return; // In Student mode, blank cell creation is disabled

    const existingSeat = this.getSeatAt(row, col);
    if (existingSeat) {
      // Seat already exists at this address
      return;
    }

    // Open confirmation modal for blank cell placement
    this.selectedCell.set({ row, col });
    this.pendingSeatNumber.set(`LAB${this.labId}-PC-R${row}C${col}`);
    this.isAddConfirmOpen.set(true);
  }

  onStudentSeatClick(seat: LabSeat): void {
    if (this.isStudent && seat.status === 'Available' && !seat.isBroken) {
      this.seatSelected.emit(seat);
    }
  }

  confirmAddSeat(): void {
    const cell = this.selectedCell();
    const seatNum = this.pendingSeatNumber().trim();
    if (cell && seatNum) {
      this.seatAdded.emit({
        seatNumber: seatNum,
        row: cell.row,
        col: cell.col,
      });
    }
    this.closeAddModal();
  }

  closeAddModal(): void {
    this.isAddConfirmOpen.set(false);
    this.selectedCell.set(null);
  }

  promptRemoveSeat(seat: LabSeat, event: Event): void {
    event.stopPropagation();
    this.selectedSeatToRemove.set(seat);
    this.isRemoveConfirmOpen.set(true);
  }

  confirmRemoveSeat(): void {
    const seat = this.selectedSeatToRemove();
    if (seat) {
      this.seatRemoved.emit(seat.id);
    }
    this.closeRemoveModal();
  }

  closeRemoveModal(): void {
    this.isRemoveConfirmOpen.set(false);
    this.selectedSeatToRemove.set(null);
  }
}
