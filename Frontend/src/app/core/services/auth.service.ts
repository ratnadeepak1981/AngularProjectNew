import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { ToastService } from './toast.service';
import { SKIP_GLOBAL_ERROR_TOAST } from '../interceptors/error-interceptor';
import { LoginRequest } from '../models/auth/login-request.model';
import { StudentProfile } from '../models/auth/student-profile.model';
import { AuthResponse } from '../models/auth/auth-response.model';
import { ApiResponse } from '../models/common/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  private readonly tokenKey = 'portal_jwt_token';
  private readonly refreshTokenKey = 'portal_refresh_token';
  private readonly roleKey = 'portal_user_role';
  private readonly profileKey = 'portal_user_profile';
  private readonly mustChangePasswordKey = 'portal_must_change_password';
  private readonly forceReasonKey = 'portal_force_change_reason';

  // Reactive State Signals
  public readonly token = signal<string | null>(this.getStoredToken());
  public readonly refreshToken = signal<string | null>(this.getStoredRefreshToken());
  public readonly role = signal<'SuperAdmin' | 'Admin' | 'Student' | null>(this.getStoredRole());
  public readonly userProfile = signal<StudentProfile | null>(this.getStoredProfile());
  public readonly mustChangePassword = signal<boolean>(this.getStoredMustChangePassword());
  public readonly forceChangeReason = signal<string | null>(this.getStoredForceReason());

  public readonly isAuthenticated = computed(() => !!this.token());
  public readonly isSuperAdmin = computed(() => this.role() === 'SuperAdmin');
  public readonly isAdmin = computed(() => this.role() === 'Admin' || this.role() === 'SuperAdmin');
  public readonly isStudent = computed(() => this.role() === 'Student');

  private getStoredToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  private getStoredRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  private getStoredRole(): 'SuperAdmin' | 'Admin' | 'Student' | null {
    return localStorage.getItem(this.roleKey) as 'SuperAdmin' | 'Admin' | 'Student' | null;
  }

  private getStoredMustChangePassword(): boolean {
    return localStorage.getItem(this.mustChangePasswordKey) === 'true';
  }

  private getStoredForceReason(): string | null {
    return localStorage.getItem(this.forceReasonKey);
  }

  private getStoredProfile(): StudentProfile | null {
    const raw = localStorage.getItem(this.profileKey);
    try {
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }

  login(credentials: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    return this.api
      .post<ApiResponse<AuthResponse>>(
        this.api.routes.auth.login,
        credentials,
        { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
      )
      .pipe(
        tap((response) => {
          const payload = response.data || (response as any);
          if (payload && payload.token) {
            this.setSession(payload);
          }
        })
      );
  }

  refreshTokenSession(): Observable<ApiResponse<AuthResponse>> {
    const currentRefreshToken = this.refreshToken();
    if (!currentRefreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }

    return this.api
      .post<ApiResponse<AuthResponse>>(this.api.routes.auth.refreshToken, {
        refreshToken: currentRefreshToken,
      })
      .pipe(
        tap((response) => {
          const payload = response.data || (response as any);
          if (payload && payload.token) {
            this.setSession(payload);
          }
        })
      );
  }

  public updateStoredProfile(partialProfile: Partial<StudentProfile>): void {
    const current = this.userProfile();
    if (current) {
      const updated: StudentProfile = { ...current, ...partialProfile };
      localStorage.setItem(this.profileKey, JSON.stringify(updated));
      this.userProfile.set(updated);
    }
  }

  public clearMustChangePassword(): void {
    localStorage.removeItem(this.mustChangePasswordKey);
    localStorage.removeItem(this.forceReasonKey);
    this.mustChangePassword.set(false);
    this.forceChangeReason.set(null);
  }

  private setSession(auth: AuthResponse): void {
    localStorage.setItem(this.tokenKey, auth.token);
    this.token.set(auth.token);

    if (auth.refreshToken) {
      localStorage.setItem(this.refreshTokenKey, auth.refreshToken);
      this.refreshToken.set(auth.refreshToken);
    }

    if (auth.role) {
      const normalizedRole: 'SuperAdmin' | 'Admin' | 'Student' = 
        auth.role === 'SuperAdmin' ? 'SuperAdmin' : auth.role === 'Admin' ? 'Admin' : 'Student';
      localStorage.setItem(this.roleKey, normalizedRole);
      this.role.set(normalizedRole);
    }

    if (auth.mustChangePassword) {
      localStorage.setItem(this.mustChangePasswordKey, 'true');
      this.mustChangePassword.set(true);
      if (auth.forceChangeReason) {
        localStorage.setItem(this.forceReasonKey, auth.forceChangeReason);
        this.forceChangeReason.set(auth.forceChangeReason);
      }
    } else {
      localStorage.removeItem(this.mustChangePasswordKey);
      localStorage.removeItem(this.forceReasonKey);
      this.mustChangePassword.set(false);
      this.forceChangeReason.set(null);
    }

    if (auth.profile) {
      localStorage.setItem(this.profileKey, JSON.stringify(auth.profile));
      this.userProfile.set(auth.profile);
    } else {
      localStorage.removeItem(this.profileKey);
      this.userProfile.set(null);
    }
  }

  getPasswordPolicy(): Observable<ApiResponse<any>> {
    return this.api.get<ApiResponse<any>>(this.api.routes.password.policy, { _t: Date.now() });
  }

  requestPasswordReset(email: string): Observable<ApiResponse<any>> {
    return this.api.post<ApiResponse<any>>(
      this.api.routes.password.forgotPassword,
      { email },
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  verifyResetOtp(email: string, otpCode: string): Observable<ApiResponse<any>> {
    return this.api.post<ApiResponse<any>>(
      this.api.routes.password.verifyResetOtp,
      { email, otpCode },
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  resetPassword(payload: { token: string; newPassword: string; confirmPassword?: string }): Observable<ApiResponse<any>> {
    return this.api.post<ApiResponse<any>>(
      this.api.routes.password.resetPassword,
      payload,
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  changePassword(payload: { currentPassword: string; newPassword: string; confirmPassword: string }): Observable<ApiResponse<any>> {
    return this.api.post<ApiResponse<any>>(
      this.api.routes.password.changePassword,
      payload,
      { context: new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true) }
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.profileKey);
    localStorage.removeItem(this.mustChangePasswordKey);
    localStorage.removeItem(this.forceReasonKey);

    this.token.set(null);
    this.refreshToken.set(null);
    this.role.set(null);
    this.userProfile.set(null);
    this.mustChangePassword.set(false);
    this.forceChangeReason.set(null);

    this.toast.info('You have been logged out of the portal.');
    this.router.navigate(['/auth/login']);
  }
}
