import { HttpInterceptorFn, HttpErrorResponse, HttpContextToken } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, finalize, switchMap, take, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../services/auth.service';
import { ErrorDiagnostics } from '../models/system/error-diagnostics.model';

export const SKIP_GLOBAL_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // If caller requested suppression of global toast (e.g. handled by dedicated custom modal/dialog)
      if (req.context.get(SKIP_GLOBAL_ERROR_TOAST)) {
        return throwError(() => error);
      }

      const isAuthEndpoint =
        req.url.includes('/auth/login') ||
        req.url.includes('/auth/refresh-token');

      // Handle 401 Unauthorized with silent token refreshing
      if (error.status === 401) {
        if (isAuthEndpoint) {
          if (req.url.includes('/auth/refresh-token')) {
            toast.error('Your active session has expired. Please sign in again.');
            authService.logout();
          } else {
            let clientMsg = 'Invalid credentials provided.';
            if (error.error) {
              if (typeof error.error === 'string') clientMsg = error.error;
              else if (error.error.message || error.error.Message) {
                clientMsg = error.error.message || error.error.Message;
              }
            }
            toast.error(clientMsg);
          }
          return throwError(() => error);
        }

        const storedRefreshToken = authService.refreshToken();
        if (storedRefreshToken) {
          if (!isRefreshing) {
            isRefreshing = true;
            refreshTokenSubject.next(null);

            return authService.refreshTokenSession().pipe(
              switchMap((response) => {
                const payload = response.data || (response as any);
                const newToken = payload?.token;
                if (!newToken) {
                  throw new Error('No new access token received during refresh.');
                }
                refreshTokenSubject.next(newToken);
                return next(
                  req.clone({
                    setHeaders: {
                      Authorization: `Bearer ${newToken}`,
                    },
                  })
                );
              }),
              catchError((refreshError) => {
                toast.error('Your active session has expired. Please sign in again.');
                authService.logout();
                return throwError(() => refreshError);
              }),
              finalize(() => {
                isRefreshing = false;
              })
            );
          } else {
            // Wait until the ongoing refresh finishes, then replay the request
            return refreshTokenSubject.pipe(
              filter((token) => token !== null),
              take(1),
              switchMap((token) => {
                return next(
                  req.clone({
                    setHeaders: {
                      Authorization: `Bearer ${token}`,
                    },
                  })
                );
              })
            );
          }
        } else {
          if (authService.isAuthenticated()) {
            toast.error('Your active session has expired. Please sign in again.');
            authService.logout();
          }
          return throwError(() => error);
        }
      }

      // Format diagnostic messages for all other error codes
      let clientMsg = 'An unexpected error occurred.';
      const technicalErrors: string[] = [];

      if (error.error) {
        if (typeof error.error === 'string') {
          clientMsg = error.error;
        } else if (error.error.message || error.error.Message) {
          clientMsg = error.error.message || error.error.Message;
        }

        if (Array.isArray(error.error.errors)) {
          technicalErrors.push(...error.error.errors);
        }
      }

      const diagnostics: ErrorDiagnostics = {
        statusCode: error.status,
        endpoint: `${req.method} ${req.urlWithParams}`,
        timestamp: new Date().toISOString(),
        technicalMessage: error.message,
        errors: technicalErrors.length > 0 ? technicalErrors : undefined,
      };

      if (error.status === 409) {
        toast.warning(clientMsg || 'A booking or duplicate registration conflict occurred. Refreshing active state...', 'Resource Conflict (409)');
      } else if (error.status === 400 || error.status === 422) {
        toast.warning(clientMsg || 'Please verify form inputs and try again.', 'Validation Notice');
      } else if (error.status === 403) {
        toast.error(clientMsg || 'Access Denied: You do not have permission for this resource.');
      } else if (error.status === 404) {
        toast.error(clientMsg || 'The requested campus resource was not found.', diagnostics, 'Resource Not Found');
      } else if (error.status === 0) {
        toast.critical(
          'Unable to reach Campus API backend server. Please verify network or server status.',
          diagnostics,
          'Backend Network Disconnected'
        );
      } else if (error.status >= 500) {
        toast.critical(
          clientMsg || 'A critical system crash occurred while processing your request.',
          diagnostics,
          'Internal System Exception'
        );
      } else {
        toast.error(clientMsg, diagnostics);
      }

      return throwError(() => error);
    })
  );
};
