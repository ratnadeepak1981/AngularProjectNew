import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Lab } from '../../../../core/models/lab/lab.model';
import { DatePickerComponent } from '../../../../shared/components/date-picker/date-picker.component';

@Component({
  selector: 'app-booking-selectors',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePickerComponent],
  templateUrl: './booking-selectors.component.html',
  styleUrl: './booking-selectors.component.css',
})
export class BookingSelectorsComponent {
  public readonly labs = input<Lab[]>([]);
  public readonly selectedLabId = input<number>(0);
  public readonly selectedDate = input<string>('');
  public readonly selectedTimeSlot = input<string>('');
  public readonly timeSlotsInput = input<string[] | undefined>(undefined, { alias: 'timeSlots' });
  public readonly isLoading = input<boolean>(false);
  public readonly allowPastDates = input<boolean>(false);

  public readonly minTodayDate = computed(() =>
    this.allowPastDates() ? '' : new Date().toISOString().split('T')[0]
  );

  public readonly timeSlots = computed<string[]>(() => {
    return this.timeSlotsInput() || [];
  });

  public readonly labChange = output<number>();
  public readonly dateChange = output<string>();
  public readonly timeSlotChange = output<string>();
  public readonly refresh = output<void>();

  public onLabSelect(event: Event): void {
    const val = parseInt((event.target as HTMLSelectElement).value, 10) || 0;
    this.labChange.emit(val);
  }

  public onDateSelect(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    const minDate = this.minTodayDate();
    if (minDate && val && val < minDate) {
      (event.target as HTMLInputElement).value = minDate;
      this.dateChange.emit(minDate);
      return;
    }
    this.dateChange.emit(val);
  }

  public onTimeSlotSelect(event: Event): void {
    const val = (event.target as HTMLSelectElement).value;
    this.timeSlotChange.emit(val);
  }

  public onRefreshClick(): void {
    this.refresh.emit();
  }
}
