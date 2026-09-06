import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  // If account requires forced password change, restrict access exclusively to password change screen
  if (authService.mustChangePassword()) {
    if (state.url.includes('/auth/forgot-password') || state.url.includes('/auth/force-password-change')) {
      return true;
    }
    const reason = authService.forceChangeReason() || 'TemporaryPassword';
    router.navigate(['/auth/forgot-password'], { queryParams: { mode: reason } });
    return false;
  }

  return true;
};
