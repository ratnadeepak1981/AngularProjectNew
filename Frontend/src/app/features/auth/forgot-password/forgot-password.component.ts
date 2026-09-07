import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { AlertModalComponent } from '../../../shared/components/dialogs/alert-modal/alert-modal.component';
import { PasswordChangeComponent, PasswordChangeMode } from '../../../shared/components/password-change/password-change.component';
import { ActionButtonComponent } from '../../../shared/components/action-button/action-button.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    AlertModalComponent,
    PasswordChangeComponent,
    ActionButtonComponent,
  ],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css',
})
export class ForgotPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly apiService = inject(ApiService);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Workflow Mode: 'ForgotPassword' | 'TemporaryPassword' | 'ExpiredPassword'
  public readonly mode = signal<PasswordChangeMode>('ForgotPassword');

  // Workflow Steps: 'email' (Step 1) | 'otp' (Step 2) | 'reset' (Step 3)
  public readonly step = signal<'email' | 'otp' | 'reset'>('email');

  // Forms
  public readonly requestOtpForm = this.fb.group({
    email: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  });

  public readonly verifyOtpForm = this.fb.group({
    otpCode: this.fb.control('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(6), Validators.maxLength(6)],
    }),
  });

  public readonly resetTicket = signal<string>('');
  public readonly userEmail = signal<string>('');
  public readonly errorMessage = signal<string | null>(null);
  public readonly isSubmitting = signal<boolean>(false);

  // System Settings OTP Expiration Countdown Timer
  public readonly otpValidityMinutes = signal<number>(3);
  public readonly countdownSeconds = signal<number>(180);
  private countdownTimer: any = null;

  public readonly isOtpExpired = computed<boolean>(() => this.countdownSeconds() <= 0);
  public readonly formattedCountdown = computed<string>(() => {
    const total = this.countdownSeconds();
    const mins = Math.floor(total / 60);
    const secs = total % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  });

  // Reusable Alert Modal Signals
  public readonly isAlertOpen = signal<boolean>(false);
  public readonly alertTitle = signal<string>('Notice');
  public readonly alertMessage = signal<string>('');
  public readonly alertIcon = signal<string>('📱');
  public readonly alertVariant = signal<'danger' | 'warning' | 'info' | 'success'>('info');

  ngOnInit(): void {
    this.loadOtpPolicy();
    const qMode = this.route.snapshot.queryParamMap.get('mode');
    const qToken = this.route.snapshot.queryParamMap.get('token');
    const storedReason = this.authService.forceChangeReason();

    if (qToken) {
      this.mode.set('ForgotPassword');
      this.resetTicket.set(qToken.trim());
      this.step.set('reset');
    } else if (qMode === 'ExpiredPassword' || storedReason === 'ExpiredPassword') {
      this.mode.set('ExpiredPassword');
      this.step.set('reset');
    } else if (qMode === 'TemporaryPassword' || storedReason === 'TemporaryPassword') {
      this.mode.set('TemporaryPassword');
      this.step.set('reset');
    } else {
      this.mode.set('ForgotPassword');
      this.step.set('email');
    }
  }

  private loadOtpPolicy(): void {
    this.authService.getPasswordPolicy().subscribe({
      next: (res) => {
        const data = res?.data || res;
        const mins = data?.otpValidityMinutes ?? data?.OtpValidityMinutes ?? 3;
        if (mins > 0) {
          this.otpValidityMinutes.set(mins);
          this.countdownSeconds.set(mins * 60);
        }
      },
      error: () => {},
    });
  }

  private startCountdown(): void {
    this.stopCountdown();
    const totalSecs = this.otpValidityMinutes() * 60;
    this.countdownSeconds.set(totalSecs);
    this.countdownTimer = setInterval(() => {
      const current = this.countdownSeconds();
      if (current <= 1) {
        this.countdownSeconds.set(0);
        this.stopCountdown();
      } else {
        this.countdownSeconds.set(current - 1);
      }
    }, 1000);
  }

  private stopCountdown(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }

  // Step 1: Send OTP
  onRequestOtp(): void {
    this.errorMessage.set(null);
    if (this.requestOtpForm.invalid) {
      this.toast.error('Please enter a valid registered email address.');
      return;
    }

    const email = this.requestOtpForm.getRawValue().email.trim();
    this.userEmail.set(email);
    this.isSubmitting.set(true);

    this.authService.requestPasswordReset(email).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.step.set('otp');
        this.startCountdown();
        this.toast.success(`6-digit OTP code dispatched successfully! Valid for ${this.otpValidityMinutes()} minutes.`);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.toast.error(err?.error?.message || 'Failed to dispatch OTP. Please check email address.');
      },
    });
  }

  // Step 2: Verify 6-digit OTP
  onVerifyOtp(): void {
    this.errorMessage.set(null);
    if (this.isOtpExpired()) {
      this.errorMessage.set('The verification OTP has expired. Please click "Resend OTP Code".');
      return;
    }

    if (this.verifyOtpForm.invalid) {
      this.verifyOtpForm.markAllAsTouched();
      this.errorMessage.set('Please enter the valid 6-digit OTP code.');
      return;
    }

    const email = this.userEmail();
    const otp = this.verifyOtpForm.getRawValue().otpCode.trim();

    this.isSubmitting.set(true);
    this.authService.verifyResetOtp(email, otp).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        this.stopCountdown();
        const data = res.data || res;
        const ticket = data?.resetTicket || data?.ResetTicket || '';
        this.resetTicket.set(ticket);
        this.step.set('reset');
        this.toast.success('Identity verified! You may now set your new password.');
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err?.error?.message || err?.error?.Message || err?.message || 'Invalid or expired OTP code.';
        this.errorMessage.set(msg);
      },
    });
  }

  // Step 3 / Direct Forced Password Change Success
  onResetSuccess(): void {
    if (this.mode() === 'ForgotPassword') {
      this.alertTitle.set('Password Reset Complete');
      this.alertMessage.set('Your password has been successfully reset. You may now sign in with your new credentials.');
      this.alertIcon.set('✓');
      this.alertVariant.set('success');
      this.isAlertOpen.set(true);
    } else {
      this.toast.success('Password updated successfully! Redirecting to dashboard...');
      const role = this.authService.role();
      setTimeout(() => {
        if (role === 'Admin') {
          this.router.navigate(['/admin/dashboard']);
        } else {
          this.router.navigate(['/student/dashboard']);
        }
      }, 600);
    }
  }

  closeSuccessAlert(): void {
    this.isAlertOpen.set(false);
    this.router.navigate(['/auth/login']);
  }

  onCancelForced(): void {
    this.stopCountdown();
    this.authService.logout();
  }

  onCancelToLogin(): void {
    this.stopCountdown();
    this.router.navigate(['/auth/login']);
  }
}
