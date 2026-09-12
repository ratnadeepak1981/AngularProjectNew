import { Component, EventEmitter, Input, Output, booleanAttribute } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppTooltipDirective } from '../../directives/tooltip.directive';
import { AppHighlightDirective } from '../../directives/highlight.directive';

export type ButtonVariant = 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'outline' | 'ghost' | 'flat';
export type ButtonSize = 'sm' | 'md' | 'lg';
export type IconPosition = 'left' | 'right';

@Component({
  selector: 'app-action-button',
  standalone: true,
  imports: [CommonModule, AppTooltipDirective, AppHighlightDirective],
  templateUrl: './action-button.component.html',
  styleUrl: './action-button.component.css',
})
export class ActionButtonComponent {
  @Input() label: string = '';
  @Input() icon: string = '';
  @Input() imageSrc: string = '';
  @Input() iconPosition: IconPosition = 'left';
  @Input() variant: ButtonVariant = 'primary';
  @Input() size: ButtonSize = 'md';
  @Input() tooltip: string = '';
  @Input() tooltipPosition: 'top' | 'bottom' | 'left' | 'right' = 'top';
  @Input({ transform: booleanAttribute }) disabled: boolean = false;
  @Input({ transform: booleanAttribute }) loading: boolean = false;
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input({ transform: booleanAttribute }) fullWidth: boolean = false;

  @Output() btnClick = new EventEmitter<MouseEvent>();
  @Output('click') clickEmitter = new EventEmitter<MouseEvent>();

  onClick(event: MouseEvent): void {
    if (this.disabled || this.loading) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }
    this.btnClick.emit(event);
    this.clickEmitter.emit(event);
  }

  get buttonClasses(): string {
    const classes = [
      'inline-flex',
      'items-center',
      'justify-center',
      'gap-2',
      'font-bold',
      'rounded-xl',
      'transition-all',
      'duration-200',
      'ease-out',
      'cursor-pointer',
      'select-none',
      'border',
      'focus:outline-none',
      'focus:ring-2',
      'focus:ring-offset-1',
      'group',
    ];

    if (this.fullWidth) {
      classes.push('w-full');
    }

    if (this.disabled || this.loading) {
      classes.push('opacity-60', 'cursor-not-allowed', 'pointer-events-none');
    } else {
      classes.push('active:scale-95', 'hover:-translate-y-0.5', 'hover:shadow-md');
    }

    // Size variations
    switch (this.size) {
      case 'sm':
        classes.push('px-3 py-1.5 text-xs');
        break;
      case 'lg':
        classes.push('px-5 py-2.5 text-sm sm:text-base');
        break;
      case 'md':
      default:
        classes.push('px-4 py-2 text-xs sm:text-sm');
        break;
    }

    // Premium Solid & Gradient Color Variants with Dynamic Hover Shifts
    switch (this.variant) {
      case 'secondary':
        classes.push(
          'bg-gradient-to-r',
          'from-slate-700',
          'to-slate-800',
          'hover:from-slate-600',
          'hover:to-slate-700',
          'text-white',
          'border-slate-600/80',
          'shadow-sm',
          'shadow-slate-900/20',
          'focus:ring-slate-500'
        );
        break;
      case 'success':
        classes.push(
          'bg-gradient-to-r',
          'from-emerald-600',
          'to-teal-600',
          'hover:from-emerald-500',
          'hover:to-teal-500',
          'text-white',
          'border-emerald-500/80',
          'shadow-sm',
          'shadow-emerald-600/30',
          'hover:shadow-emerald-500/40',
          'focus:ring-emerald-500'
        );
        break;
      case 'danger':
        classes.push(
          'bg-gradient-to-r',
          'from-rose-600',
          'to-red-600',
          'hover:from-rose-500',
          'hover:to-red-500',
          'text-white',
          'border-rose-500/80',
          'shadow-sm',
          'shadow-rose-600/30',
          'hover:shadow-rose-500/40',
          'focus:ring-rose-500'
        );
        break;
      case 'warning':
        classes.push(
          'bg-gradient-to-r',
          'from-amber-500',
          'to-orange-600',
          'hover:from-amber-400',
          'hover:to-orange-500',
          'text-white',
          'border-amber-500/80',
          'shadow-sm',
          'shadow-amber-500/30',
          'hover:shadow-amber-500/40',
          'focus:ring-amber-500'
        );
        break;
      case 'outline':
        classes.push(
          'bg-white/80',
          'dark:bg-slate-900/80',
          'hover:bg-slate-100',
          'dark:hover:bg-slate-800',
          'text-slate-700',
          'dark:text-slate-200',
          'border-slate-300',
          'dark:border-slate-700',
          'shadow-xs',
          'focus:ring-slate-400'
        );
        break;
      case 'ghost':
        classes.push(
          'bg-transparent',
          'hover:bg-slate-100',
          'dark:hover:bg-slate-800',
          'text-slate-600',
          'dark:text-slate-300',
          'border-transparent',
          'focus:ring-slate-400'
        );
        break;
      case 'flat':
        classes.push(
          'bg-slate-800',
          'hover:bg-slate-700',
          'text-white',
          'border-slate-700',
          'focus:ring-blue-500'
        );
        break;
      case 'primary':
      default:
        classes.push(
          'theme-btn-primary',
          'shadow-sm'
        );
        break;
    }

    return classes.join(' ');
  }

  get iconBadgeClasses(): string {
    if (this.variant === 'outline' || this.variant === 'ghost') {
      return 'bg-slate-200/70 dark:bg-slate-800/80 text-slate-700 dark:text-slate-200 p-1 rounded-md shrink-0 transition-transform group-hover:scale-110';
    }
    return 'bg-white/20 dark:bg-white/15 text-white p-1 rounded-md shrink-0 transition-transform group-hover:scale-110';
  }
}
