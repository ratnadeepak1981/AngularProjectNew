import { ErrorHandler, Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastService } from '../services/toast.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler 
{
  constructor(private readonly toastService: ToastService) {}

  handleError(error: unknown): void 
  {
    // Keep developer console tracking active
    console.error('Unhandled application error:', error);

    // GLOBAL HTTP STATUS CODE HANDLER LAYER
    if (error instanceof HttpErrorResponse) 
    {
      let friendlyMessage = error.error?.message || 'A network communication problem occurred.';
      
      switch (error.status) 
      {
        case 400: // Bad Request (Validation failures)
          // 🌟 FIXED: Removed the third argument to match your warning signature (message, title)
          this.toastService.warning(
            friendlyMessage || 'Please check your form inputs for validation errors.',
            'Invalid Input'
          );
          return;

        case 401: // Unauthorized (Expired or missing JWT tracking tokens)
          // 🌟 FIXED: Only passing 2 arguments
          this.toastService.warning(
            'Your login session has expired. Please log in again to continue.',
            'Session Expired'
          );
          return;

        case 403: // Forbidden (Role check validation failure)
          // Matches your critical signature style: (message, actionText, title)
          this.toastService.critical(
            'Access Denied. You do not have administrative privileges to view this module.',
            undefined,
            'Security Restriction'
          );
          return;

        case 404: // Not Found (Resource missing)
          // 🌟 FIXED: Only passing 2 arguments
          this.toastService.warning(
            friendlyMessage || 'The requested campus data ledger record could not be found.',
            'Not Found'
          );
          return;

        case 409: // Conflict (Duplicate entries captured by your updated C# middleware)
          // 🌟 FIXED: Only passing 2 arguments
          this.toastService.warning(
            friendlyMessage || 'A data validation conflict occurred. Duplicate entry detected.',
            'Data Conflict'
          );
          return;

        case 500: // Internal Server Error
          this.toastService.critical(
            'The application server encountered a critical internal failure.',
            undefined,
            'System Crash'
          );
          return;
          
        case 503: // Service Unavailable (API Offline)
          this.toastService.critical(
            'The campus database engine is temporarily offline for maintenance. Try again later.',
            undefined,
            'Server Offline'
          );
          return;
      }
    }

    // FALLBACK STRATEGY FOR FRONTEND JAVASCRIPT/TYPESCRIPT EXCEPTION CRASHES
    this.toastService.critical(
      'An unexpected frontend application error occurred. Please refresh your browser page.',
      undefined,
      'Application Error'
    );
  }
}
