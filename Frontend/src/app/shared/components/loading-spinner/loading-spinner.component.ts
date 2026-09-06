import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [ngClass]="containerClasses" class="inline-flex items-center justify-center">
      <svg
        [ngClass]="[spinnerSizeClass, spinnerColorClass]"
        class="animate-spin"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <circle
          class="opacity-25"
          cx="12"
          cy="12"
          r="10"
          stroke="currentColor"
          stroke-width="4"
        ></circle>
        <path
          class="opacity-75"
          fill="currentColor"
          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
        ></path>
      </svg>
      @if (label) {
        <span [ngClass]="labelClass" class="ml-2 font-medium">
          {{ label }}
        </span>
      }
    </div>
  `
})
export class LoadingSpinnerComponent {
  @Input() size: 'xs' | 'sm' | 'md' | 'lg' | 'xl' = 'md';
  @Input() color: 'primary' | 'white' | 'slate' | 'indigo' | 'rose' = 'primary';
  @Input() label?: string;
  @Input() containerClasses: string = '';

  get spinnerSizeClass(): string {
    switch (this.size) {
      case 'xs': return 'w-3.5 h-3.5';
      case 'sm': return 'w-4 h-4';
      case 'md': return 'w-6 h-6';
      case 'lg': return 'w-8 h-8';
      case 'xl': return 'w-12 h-12';
      default: return 'w-6 h-6';
    }
  }

  get spinnerColorClass(): string {
    switch (this.color) {
      case 'white': return 'text-white';
      case 'slate': return 'text-slate-500 dark:text-slate-400';
      case 'indigo': return 'text-indigo-600 dark:text-indigo-400';
      case 'rose': return 'text-rose-600 dark:text-rose-400';
      case 'primary':
      default:
        return 'text-blue-600 dark:text-blue-400';
    }
  }

  get labelClass(): string {
    switch (this.size) {
      case 'xs':
      case 'sm':
        return 'text-xs text-slate-600 dark:text-slate-400';
      case 'lg':
      case 'xl':
        return 'text-base text-slate-700 dark:text-slate-300';
      case 'md':
      default:
        return 'text-sm text-slate-600 dark:text-slate-400';
    }
  }
}
