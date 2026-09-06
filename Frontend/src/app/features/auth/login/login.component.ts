import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { ActionButtonComponent } from '../../../shared/components/action-button/action-button.component';
import { HttpContext } from '@angular/common/http';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../core/interceptors/error-interceptor';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, ActionButtonComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly apiService = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  // Reactive State Signals
  public readonly isLoading = signal(false);
  public readonly errorMessage = signal<string | null>(null);
  public readonly resetErrorMessage = signal<string | null>(null);
  public readonly isForgotPasswordOpen = signal(false);
  public readonly resetStep = signal(1);
  public readonly isResetLoading = signal(false);

  // Password Visibility Toggle Signals
  public readonly showPassword = signal(false);
  public readonly showResetPassword = signal(false);

  toggleShowPassword(): void {
    this.showPassword.update((val) => !val);
  }

  toggleShowResetPassword(): void {
    this.showResetPassword.update((val) => !val);
  }

  navigateToRegister(): void {
    this.router.navigate(['/auth/register']);
  }

  // Strongly Typed Forms
  public readonly loginForm = this.fb.group({
    email: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: this.fb.control('', { nonNullable: true, validators: [Validators.required] }),
  });

  public readonly resetForm = this.fb.group({
    email: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    token: this.fb.control('', { nonNullable: true }),
    newPassword: this.fb.control('', { nonNullable: true, validators: [Validators.minLength(3)] }),
    confirmPassword: this.fb.control('', { nonNullable: true }),
  });

  onLogin(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      const emailCtrl = this.loginForm.get('email');
      const passCtrl = this.loginForm.get('password');
      
      if (emailCtrl?.invalid) {
        this.toast.warning('Please enter a valid registered email address.');
      } else if (passCtrl?.invalid) {
        this.toast.warning('Please enter your password.');
      }
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.loginForm.getRawValue()).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        const data = res.data;

        if (data?.mustChangePassword) {
          const reason = data.forceChangeReason || (data.passwordExpired ? 'ExpiredPassword' : 'TemporaryPassword');
          this.toast.info('Password update is required before continuing.');
          this.router.navigate(['/auth/forgot-password'], { queryParams: { mode: reason } });
          return;
        }

        this.toast.success('Authentication Successful! Redirecting...');
        
        const role = data?.role;
        setTimeout(() => {
          if (role === 'Admin') {
            this.router.navigate(['/admin/dashboard']);
          } else {
            this.router.navigate(['/student/dashboard']);
          }
        }, 500);
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.message || err.error?.Message || err.message || 'Invalid email address or password.';
        this.errorMessage.set(msg);
      },
    });
  }

  openForgotPassword(): void {
    this.router.navigate(['/auth/forgot-password']);
  }

  closeForgotPassword(): void {
    this.isForgotPasswordOpen.set(false);
    this.resetErrorMessage.set(null);
  }

  onRequestResetToken(): void {
    this.resetErrorMessage.set(null);
    const email = this.resetForm.get('email')?.value;
    if (!email) {
      this.toast.warning('Please enter your registered email address.');
      return;
    }

    this.isResetLoading.set(true);
    this.authService.requestPasswordReset(email).subscribe({
      next: () => {
        this.isResetLoading.set(false);
        this.toast.success('Reset code dispatched via SMS simulation! Please check SMS preview.');
        this.resetStep.set(2);
      },
      error: (err) => {
        this.isResetLoading.set(false);
        const msg = err.error?.message || err.error?.Message || 'Failed to dispatch reset code.';
        this.resetErrorMessage.set(msg);
      },
    });
  }

  onSubmitPasswordReset(): void {
    this.resetErrorMessage.set(null);
    const { token, newPassword, confirmPassword } = this.resetForm.value;
    if (!token || !newPassword) {
      this.resetErrorMessage.set('Please enter the OTP token code and your new password.');
      return;
    }

    if (newPassword !== confirmPassword) {
      this.resetErrorMessage.set('New Password and Confirm Password do not match.');
      return;
    }

    this.isResetLoading.set(true);
    this.authService.resetPassword({ token, newPassword }).subscribe({
      next: () => {
        this.isResetLoading.set(false);
        this.resetErrorMessage.set(null);
        this.toast.success('Password updated successfully! You can now sign in.');
        this.closeForgotPassword();
      },
      error: (err) => {
        this.isResetLoading.set(false);
        const errorMsg = err.error?.message || err.error?.Message || err.error || 'Failed to update password.';
        this.resetErrorMessage.set(errorMsg);
      },
    });
  }
}
