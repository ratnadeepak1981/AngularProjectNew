import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col items-center justify-center p-8 sm:p-12 text-center rounded-2xl border border-dashed border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/30 animate-fade-in my-3">
      <div class="w-16 h-16 mb-4 rounded-2xl bg-white dark:bg-slate-800 shadow-sm border border-slate-200/80 dark:border-slate-700/80 flex items-center justify-center text-3xl select-none">
        {{ icon }}
      </div>
      <h3 class="text-base font-bold text-slate-800 dark:text-slate-100 mb-1.5">
        {{ title }}
      </h3>
      <p class="text-xs text-slate-500 dark:text-slate-400 max-w-sm mb-5 leading-relaxed">
        {{ description }}
      </p>
      @if (actionText) {
        <button
          type="button"
          (click)="actionClick.emit()"
          class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold rounded-xl shadow-sm transition-all active:scale-95 cursor-pointer"
        >
          @if (actionIcon) {
            <span>{{ actionIcon }}</span>
          }
          <span>{{ actionText }}</span>
        </button>
      }
    </div>
  `
})
export class EmptyStateComponent {
  @Input() icon: string = '📭';
  @Input() title: string = 'No Records Found';
  @Input() description: string = 'There is currently no data to display in this view.';
  @Input() actionText?: string;
  @Input() actionIcon?: string;

  @Output() actionClick = new EventEmitter<void>();
}
