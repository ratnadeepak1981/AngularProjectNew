import { Component, Input, booleanAttribute } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'auto' | 'success' | 'warning' | 'danger' | 'info' | 'neutral' | 'purple';
export type BadgeSize = 'sm' | 'md';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.css',
})
export class StatusBadgeComponent {
  @Input() status: string = '';
  @Input() count?: number | null;
  @Input() countUnit: string = '';
  @Input() variant: BadgeVariant = 'auto';
  @Input({ transform: booleanAttribute }) pulse: boolean = true;
  @Input() size: BadgeSize = 'md';
  @Input() icon: string = '';

  get displayStatus(): string {
    if (this.count !== undefined && this.count !== null) {
      const c = this.count;
      const unit = this.countUnit || '';
      if (unit) {
        return `${c} ${c === 1 ? unit : unit + 's'}`;
      }
      return `${c}`;
    }
    return this.status;
  }

  get resolvedVariant(): BadgeVariant {
    if (this.variant && this.variant !== 'auto') {
      return this.variant;
    }

    if (this.count !== undefined && this.count !== null) {
      return this.count > 0 ? 'success' : 'danger';
    }

    const s = (this.status || '').toLowerCase().trim();

    if (
      s.includes('approve') ||
      s.includes('verified') ||
      s.includes('active') ||
      s.includes('paid') ||
      s.includes('complete') ||
      s.includes('resolve') ||
      s.includes('pass')
    ) {
      return 'success';
    }

    if (
      s.includes('pending') ||
      s.includes('progress') ||
      s.includes('wait') ||
      s.includes('hold') ||
      s.includes('review')
    ) {
      return 'warning';
    }

    if (
      s.includes('reject') ||
      s.includes('deactivat') ||
      s.includes('overdue') ||
      s.includes('unpaid') ||
      s.includes('cancel') ||
      s.includes('fail') ||
      s.includes('close')
    ) {
      return 'danger';
    }

    if (
      s.includes('assign') ||
      s.includes('submit') ||
      s.includes('info') ||
      s.includes('process')
    ) {
      return 'info';
    }

    return 'neutral';
  }

  get showPulseDot(): boolean {
    return this.pulse && this.resolvedVariant === 'warning';
  }

  get badgeClasses(): string {
    const classes = [
      'inline-flex',
      'items-center',
      'gap-1.5',
      'font-semibold',
      'rounded-full',
      'border',
      'transition-all',
      'duration-200',
      'select-none',
      'whitespace-nowrap',
    ];

    if (this.size === 'sm') {
      classes.push('px-2.5', 'py-0.5', 'text-[11px]');
    } else {
      classes.push('px-3', 'py-1', 'text-xs');
    }

    switch (this.resolvedVariant) {
      case 'success':
        classes.push(
          'bg-emerald-50',
          'dark:bg-emerald-950/40',
          'text-emerald-700',
          'dark:text-emerald-300',
          'border-emerald-200',
          'dark:border-emerald-800/60'
        );
        break;
      case 'warning':
        classes.push(
          'bg-amber-50',
          'dark:bg-amber-950/40',
          'text-amber-700',
          'dark:text-amber-300',
          'border-amber-200',
          'dark:border-amber-800/60'
        );
        break;
      case 'danger':
        classes.push(
          'bg-rose-50',
          'dark:bg-rose-950/40',
          'text-rose-700',
          'dark:text-rose-300',
          'border-rose-200',
          'dark:border-rose-800/60'
        );
        break;
      case 'info':
        classes.push(
          'bg-sky-50',
          'dark:bg-sky-950/40',
          'text-sky-700',
          'dark:text-sky-300',
          'border-sky-200',
          'dark:border-sky-800/60'
        );
        break;
      case 'purple':
        classes.push(
          'bg-purple-50',
          'dark:bg-purple-950/40',
          'text-purple-700',
          'dark:text-purple-300',
          'border-purple-200',
          'dark:border-purple-800/60'
        );
        break;
      case 'neutral':
      default:
        classes.push(
          'bg-slate-100',
          'dark:bg-slate-800/60',
          'text-slate-700',
          'dark:text-slate-300',
          'border-slate-200',
          'dark:border-slate-700'
        );
        break;
    }

    return classes.join(' ');
  }

  get dotClasses(): string {
    switch (this.resolvedVariant) {
      case 'success':
        return 'bg-emerald-500';
      case 'warning':
        return 'bg-amber-500';
      case 'danger':
        return 'bg-rose-500';
      case 'info':
        return 'bg-sky-500';
      case 'purple':
        return 'bg-purple-500';
      case 'neutral':
      default:
        return 'bg-slate-400';
    }
  }
}
