import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="min-h-[80vh] flex items-center justify-center p-6 text-center animate-fade-in">
      <div class="max-w-md w-full bg-white dark:bg-slate-900 rounded-3xl p-8 sm:p-10 shadow-xl border border-slate-200 dark:border-slate-800 space-y-6">
        <div class="w-24 h-24 mx-auto rounded-3xl bg-blue-50 dark:bg-blue-950/50 border border-blue-200 dark:border-blue-800 flex items-center justify-center text-5xl select-none animate-bounce">
          🧭
        </div>

        <div class="space-y-2">
          <span class="inline-block px-3 py-1 rounded-full text-2xs font-mono font-bold uppercase tracking-wider bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700">
            HTTP 404 • Resource Not Found
          </span>
          <h1 class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
            Lost on Campus?
          </h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed max-w-sm mx-auto">
            The page or service resource you are looking for has been moved, renamed, or does not exist on the portal.
          </p>
        </div>

        <div class="pt-4 flex flex-col sm:flex-row items-center justify-center gap-3">
          <button
            type="button"
            (click)="navigateDashboard()"
            class="w-full sm:w-auto px-5 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl shadow-md transition-all active:scale-95 cursor-pointer flex items-center justify-center gap-2"
          >
            <span>🏛️</span>
            <span>Return to Dashboard</span>
          </button>
          <button
            type="button"
            (click)="goBack()"
            class="w-full sm:w-auto px-4 py-2.5 bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-xs font-semibold rounded-xl transition-all active:scale-95 cursor-pointer"
          >
            ← Go Back
          </button>
        </div>
      </div>
    </div>
  `
})
export class NotFoundPageComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  navigateDashboard(): void {
    if (this.authService.isAuthenticated()) {
      if (this.authService.isAdmin()) {
        this.router.navigate(['/admin/dashboard']);
      } else {
        this.router.navigate(['/student/dashboard']);
      }
    } else {
      this.router.navigate(['/auth/login']);
    }
  }

  goBack(): void {
    window.history.back();
  }
}
