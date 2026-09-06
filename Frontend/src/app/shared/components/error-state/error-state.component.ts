import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col items-center justify-center p-8 sm:p-12 text-center rounded-2xl border border-rose-200 dark:border-rose-900/50 bg-rose-50/50 dark:bg-rose-950/20 animate-fade-in my-3">
      <div class="w-16 h-16 mb-4 rounded-2xl bg-white dark:bg-slate-800 shadow-sm border border-rose-200 dark:border-rose-800 flex items-center justify-center text-3xl select-none text-rose-500">
        ⚠️
      </div>
      <h3 class="text-base font-bold text-rose-900 dark:text-rose-200 mb-1.5">
        {{ title }}
      </h3>
      <p class="text-xs text-rose-700/80 dark:text-rose-300/80 max-w-md mb-5 leading-relaxed">
        {{ message }}
      </p>
      @if (retryLabel) {
        <button
          type="button"
          (click)="retry.emit()"
          class="inline-flex items-center gap-2 px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white text-xs font-semibold rounded-xl shadow-sm transition-all active:scale-95 cursor-pointer"
        >
          <span>🔄</span>
          <span>{{ retryLabel }}</span>
        </button>
      }
    </div>
  `
})
export class ErrorStateComponent {
  @Input() title: string = 'Unable to Load Data';
  @Input() message: string = 'An unexpected error occurred while communicating with the campus service. Please try again.';
  @Input() retryLabel: string = 'Try Again';

  @Output() retry = new EventEmitter<void>();
}
