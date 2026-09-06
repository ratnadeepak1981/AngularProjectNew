import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingSpinnerComponent } from '../loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-loading-state',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent],
  template: `
    <div class="flex flex-col items-center justify-center p-8 sm:p-12 text-center rounded-2xl border border-slate-200/80 dark:border-slate-800/80 bg-slate-50/50 dark:bg-slate-900/30 animate-fade-in my-3">
      <div class="w-16 h-16 mb-4 rounded-2xl bg-white dark:bg-slate-800 shadow-sm border border-slate-200/80 dark:border-slate-700/80 flex items-center justify-center">
        <app-loading-spinner [size]="spinnerSize" color="primary"></app-loading-spinner>
      </div>
      <h3 class="text-base font-bold text-slate-800 dark:text-slate-100 mb-1">
        {{ title }}
      </h3>
      @if (description) {
        <p class="text-xs text-slate-500 dark:text-slate-400 max-w-sm leading-relaxed">
          {{ description }}
        </p>
      }
    </div>
  `
})
export class LoadingStateComponent {
  @Input() title: string = 'Loading Data...';
  @Input() description: string = 'Please wait while we retrieve the latest information from the campus portal.';
  @Input() spinnerSize: 'sm' | 'md' | 'lg' | 'xl' = 'lg';
}
