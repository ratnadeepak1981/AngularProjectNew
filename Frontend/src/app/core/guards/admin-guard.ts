import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  if (authService.isAuthenticated() && authService.isAdmin()) {
    return true;
  }

  toast.error('Access Denied. You do not have permission to view this resource.');
  
  if (authService.isAuthenticated()) {
    router.navigate(['/unauthorized']);
  } else {
    router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
  }
  
  return false;
};
