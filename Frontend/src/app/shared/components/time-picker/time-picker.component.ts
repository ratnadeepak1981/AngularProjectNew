import { Component, OnChanges, SimpleChanges, computed, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-time-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './time-picker.component.html',
})
export class TimePickerComponent implements OnChanges {
  public readonly label = input<string>('Select Time');
  public readonly selectedTime = input<string>('09:00 AM');
  public readonly minuteStep = input<number>(15);
  public readonly disabled = input<boolean>(false);
  public readonly showLabel = input<boolean>(true);
  public readonly icon = input<string>('⏰');

  public readonly timeChange = output<string>();

  public readonly hour = signal<string>('09');
  public readonly minute = signal<string>('00');
  public readonly period = signal<string>('AM');

  public readonly hoursList: string[] = ['01', '02', '03', '04', '05', '06', '07', '08', '09', '10', '11', '12'];
  public readonly periodsList: string[] = ['AM', 'PM'];

  public readonly minutesList = computed<string[]>(() => {
    const step = Math.max(1, Math.min(60, this.minuteStep() || 15));
    const mins: string[] = [];
    for (let i = 0; i < 60; i += step) {
      mins.push(i < 10 ? `0${i}` : `${i}`);
    }
    return mins;
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedTime'] && changes['selectedTime'].currentValue) {
      this.syncFromInput(changes['selectedTime'].currentValue);
    }
  }

  private syncFromInput(val: string): void {
    const parsed = this.parseTime(val);
    this.hour.set(parsed.hour);
    this.minute.set(parsed.minute);
    this.period.set(parsed.period);
  }

  public onHourChange(h: string): void {
    this.hour.set(h);
    this.emitTime();
  }

  public onMinuteChange(m: string): void {
    this.minute.set(m);
    this.emitTime();
  }

  public onPeriodChange(p: string): void {
    this.period.set(p);
    this.emitTime();
  }

  private emitTime(): void {
    const formatted = `${this.hour()}:${this.minute()} ${this.period()}`;
    this.timeChange.emit(formatted);
  }

  private parseTime(timeStr: string): { hour: string; minute: string; period: string } {
    if (!timeStr) return { hour: '09', minute: '00', period: 'AM' };

    const match12 = timeStr.trim().match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
    if (match12) {
      let h = parseInt(match12[1], 10);
      if (h < 1 || h > 12) h = 9;
      const hStr = h < 10 ? `0${h}` : `${h}`;
      const mStr = match12[2];
      const pStr = match12[3].toUpperCase();
      return { hour: hStr, minute: mStr, period: pStr };
    }

    const match24 = timeStr.trim().match(/^(\d{1,2}):(\d{2})/);
    if (match24) {
      let h = parseInt(match24[1], 10);
      const m = match24[2];
      const period = h >= 12 ? 'PM' : 'AM';
      h = h % 12;
      if (h === 0) h = 12;
      const hStr = h < 10 ? `0${h}` : `${h}`;
      return { hour: hStr, minute: m, period };
    }

    return { hour: '09', minute: '00', period: 'AM' };
  }
}
