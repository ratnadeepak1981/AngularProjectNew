import {
  ApplicationConfig,
  ErrorHandler,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt-interceptor';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { GlobalErrorHandler } from './core/errors/global-error.handler';

export const appConfig: ApplicationConfig = {
  providers: [
    // Framework Engine Global Observers
    provideBrowserGlobalErrorListeners(),

    // Custom Error Boundaries Handler mapping
    {
      provide: ErrorHandler,
      useClass: GlobalErrorHandler
    },

    // Global Component Router configurations
    provideRouter(
      routes,
      withComponentInputBinding()
    ),

    // Unified HTTP Network Interceptor pipelines
    provideHttpClient(
      withInterceptors([
        jwtInterceptor,
        errorInterceptor
      ])
    )
  ]
};
