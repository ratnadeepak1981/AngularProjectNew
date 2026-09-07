import { ErrorHandler, Injectable } from '@angular/core';
import { ToastService } from '../services/toast.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {

  constructor(
    private readonly toastService: ToastService
  ) {}

  handleError(error: unknown): void {
    console.error('Unhandled application error:', error);

    this.toastService.critical(
      'An unexpected error occurred. Please try again.',
      undefined,
      'Application Error'
    );
  }
}