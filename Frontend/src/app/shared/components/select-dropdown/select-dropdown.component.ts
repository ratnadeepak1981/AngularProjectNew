import { Component, HostListener, computed, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DropdownOption } from '../../../core/models/common/dropdown-option.model';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';

@Component({
  selector: 'app-select-dropdown',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './select-dropdown.component.html',
})
export class SelectDropdownComponent {
  public readonly label = input<string>('');
  public readonly options = input<DropdownOption[]>([]);
  public readonly selectedValue = input<any>(null);
  public readonly placeholder = input<string>('-- Select Option --');
  public readonly icon = input<string>('📌');
  public readonly required = input<boolean>(false);
  public readonly searchable = input<boolean>(false);

  public readonly selectionChange = output<any>();

  public readonly isOpen = signal<boolean>(false);
  public readonly searchTerm = signal<string>('');

  public readonly selectedOption = computed(() => {
    const val = this.selectedValue();
    return this.options().find((o) => o.value === val) || null;
  });

  public readonly isSearchEnabled = computed(() => {
    return this.searchable() || this.options().length > 8;
  });

  public readonly filteredOptions = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const all = this.options();
    if (!term) return all;
    return all.filter(
      (opt) =>
        opt.label?.toLowerCase().includes(term) ||
        opt.description?.toLowerCase().includes(term) ||
        String(opt.value)?.toLowerCase().includes(term)
    );
  });

  public toggleOpen(event?: Event): void {
    if (event) event.stopPropagation();
    const nextState = !this.isOpen();
    this.isOpen.set(nextState);
    if (!nextState) {
      this.searchTerm.set('');
    }
  }

  public onSearchInput(term: string): void {
    this.searchTerm.set(term);
  }

  public selectOption(opt: DropdownOption, event?: Event): void {
    if (event) event.stopPropagation();
    this.selectionChange.emit(opt.value);
    this.isOpen.set(false);
    this.searchTerm.set('');
  }

  @HostListener('document:click')
  public closeOnOutsideClick(): void {
    this.isOpen.set(false);
    this.searchTerm.set('');
  }
}
