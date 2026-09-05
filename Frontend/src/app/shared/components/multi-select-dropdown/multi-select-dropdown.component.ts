import { Component, Input, Output, EventEmitter, ElementRef, HostListener, forwardRef, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MultiSelectOption } from './models/multi-select-option.model';

@Component({
  selector: 'app-multi-select-dropdown',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './multi-select-dropdown.component.html',
  styleUrls: ['./multi-select-dropdown.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MultiSelectDropdownComponent),
      multi: true,
    },
  ],
})
export class MultiSelectDropdownComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = 'Select options...';
  @Input() options: MultiSelectOption[] = [];
  @Input() showSearch = true;
  @Input() disabled = false;

  @Output() selectionChange = new EventEmitter<(string | number)[]>();

  isOpen = signal<boolean>(false);
  selectedItems = signal<(string | number)[]>([]);
  searchQuery = '';

  private onChange: (value: any) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private elementRef: ElementRef) {}

  filteredOptions = computed(() => {
    if (!this.searchQuery.trim()) return this.options;
    const q = this.searchQuery.toLowerCase();
    return this.options.filter((o) => o.label.toLowerCase().includes(q));
  });

  displayText = computed(() => {
    const selected = this.selectedItems();
    if (selected.length === 0) return this.placeholder;
    const labels = this.options.filter((o) => selected.includes(o.id)).map((o) => o.label);
    if (labels.length <= 2) return labels.join(', ');
    return `${labels[0]}, ${labels[1]} +${labels.length - 2} more`;
  });

  toggleDropdown(): void {
    if (this.disabled) return;
    this.isOpen.update((v) => !v);
  }

  isSelected(id: string | number): boolean {
    return this.selectedItems().includes(id);
  }

  toggleOption(id: string | number): void {
    const current = [...this.selectedItems()];
    const index = current.indexOf(id);
    if (index > -1) {
      current.splice(index, 1);
    } else {
      current.push(id);
    }
    this.updateValue(current);
  }

  selectAll(): void {
    const allIds = this.options.map((o) => o.id);
    this.updateValue(allIds);
  }

  clearAll(): void {
    this.updateValue([]);
  }

  private updateValue(val: (string | number)[]): void {
    this.selectedItems.set(val);
    this.onChange(val);
    this.onTouched();
    this.selectionChange.emit(val);
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  writeValue(value: (string | number)[]): void {
    this.selectedItems.set(value || []);
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
export type { MultiSelectOption };
