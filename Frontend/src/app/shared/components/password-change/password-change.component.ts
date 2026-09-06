import { Component, EventEmitter, Input, OnInit, Output, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { PasswordPolicy } from '../../../core/models/auth/password-policy.model';
import { ActionButtonComponent } from '../action-button/action-button.component';

export type PasswordChangeMode = 'NormalChange' | 'ForgotPassword' | 'TemporaryPassword' | 'ExpiredPassword';

@Component({
  selector: 'app-password-change',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ActionButtonComponent],
  templateUrl: './password-change.component.html',
  styleUrl: './password-change.component.css',
})
export class PasswordChangeComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);

  @Input() mode: PasswordChangeMode = 'NormalChange';
  @Input() resetToken?: string;
  @Input() email?: string;
  @Input() showCancelButton = false;

  @Output() success = new EventEmitter<any>();
  @Output() cancel = new EventEmitter<void>();

  // Visibility Toggles
  public readonly showCurrentPassword = signal<boolean>(false);
  public readonly showNewPassword = signal<boolean>(false);
  public readonly showConfirmPassword = signal<boolean>(false);

  // Policy Signal
  public readonly policy = signal<PasswordPolicy>({
    minLength: 8,
    complexityTier: 'strong',
    expiryDays: 90,
    reuseHistoryLimit: 5,
    otpValidityMinutes: 3,
    maxFailedLogins: 5,
    lockoutDurationMinutes: 15,
  });

  public readonly isSubmitting = signal<boolean>(false);
  public readonly errorMessage = signal<string | null>(null);

  // Strongly-typed reactive form
  public readonly form: FormGroup = this.fb.group({
    currentPassword: [''],
    newPassword: ['', [Validators.required]],
    confirmPassword: ['', [Validators.required]],
  });

  // Mode helpers
  public readonly requiresCurrentPassword = computed(() => {
    return this.mode === 'NormalChange' || this.mode === 'TemporaryPassword' || this.mode === 'ExpiredPassword';
  });

  public readonly currentPasswordLabel = computed(() => {
    switch (this.mode) {
      case 'TemporaryPassword':
        return 'Temporary Password';
      case 'ExpiredPassword':
        return 'Expired / Current Password';
      default:
        return 'Current Password';
    }
  });

  public readonly currentPasswordPlaceholder = computed(() => {
    switch (this.mode) {
      case 'TemporaryPassword':
        return 'Enter the temporary password / OTP received';
      case 'ExpiredPassword':
        return 'Enter your existing (expired) password';
      default:
        return 'Enter your current account password';
    }
  });

  // Dynamic Password Validation Checks based on live System Settings
  public readonly currentNewPasswordValue = signal<string>('');
  public readonly currentConfirmPasswordValue = signal<string>('');
  public readonly currentOldPasswordValue = signal<string>('');

  public readonly hasMinLength = computed(() => {
    const val = this.currentNewPasswordValue();
    return val.length >= this.policy().minLength;
  });

  public readonly hasUppercase = computed(() => {
    return /[A-Z]/.test(this.currentNewPasswordValue());
  });

  public readonly hasLowercase = computed(() => {
    return /[a-z]/.test(this.currentNewPasswordValue());
  });

  public readonly hasNumber = computed(() => {
    return /[0-9]/.test(this.currentNewPasswordValue());
  });

  public readonly hasSpecialChar = computed(() => {
    return /[^A-Za-z0-9]/.test(this.currentNewPasswordValue());
  });

  public readonly passwordsMatch = computed(() => {
    const newPass = this.currentNewPasswordValue();
    const confirmPass = this.currentConfirmPasswordValue();
    return !!newPass && !!confirmPass && newPass === confirmPass;
  });

  public readonly isPolicySatisfied = computed(() => {
    const p = this.policy();
    const val = this.currentNewPasswordValue();
    if (!val || val.length < p.minLength) return false;

    if (p.complexityTier === 'basic') return true;

    const hasUp = this.hasUppercase();
    const hasLow = this.hasLowercase();
    const hasNum = this.hasNumber();
    const hasSpec = this.hasSpecialChar();

    if (p.complexityTier === 'medium') {
      return hasUp && hasLow && hasNum;
    }

    if (p.complexityTier === 'strict') {
      return val.length >= Math.max(12, p.minLength) && hasUp && hasLow && hasNum && hasSpec;
    }

    // Default 'strong'
    return hasUp && hasLow && hasNum && hasSpec;
  });

  public readonly isFormValid = computed(() => {
    if (this.requiresCurrentPassword() && !this.currentOldPasswordValue().trim()) {
      return false;
    }
    if (!this.currentNewPasswordValue().trim() || !this.currentConfirmPasswordValue().trim()) {
      return false;
    }
    return this.isPolicySatisfied() && this.passwordsMatch();
  });

  ngOnInit(): void {
    this.loadSystemPolicy();

    // Listen to value changes for live visual checklist and reactive validation
    this.form.get('newPassword')?.valueChanges.subscribe((val) => {
      this.currentNewPasswordValue.set(val || '');
    });

    this.form.get('confirmPassword')?.valueChanges.subscribe((val) => {
      this.currentConfirmPasswordValue.set(val || '');
    });

    this.form.get('currentPassword')?.valueChanges.subscribe((val) => {
      this.currentOldPasswordValue.set(val || '');
    });

    // Dynamically require currentPassword validator if needed
    if (this.requiresCurrentPassword()) {
      this.form.get('currentPassword')?.setValidators([Validators.required]);
      this.form.get('currentPassword')?.updateValueAndValidity();
    }
  }

  private loadSystemPolicy(): void {
    this.authService.getPasswordPolicy().subscribe({
      next: (res) => {
        const data = res?.data || res;
        if (data) {
          const mappedPolicy: PasswordPolicy = {
            minLength: data.minLength ?? data.MinLength ?? 8,
            complexityTier: (data.complexityTier ?? data.ComplexityTier ?? 'strong').toLowerCase() as any,
            expiryDays: data.expiryDays ?? data.ExpiryDays ?? 90,
            reuseHistoryLimit: data.reuseHistoryLimit ?? data.ReuseHistoryLimit ?? 5,
            otpValidityMinutes: data.otpValidityMinutes ?? data.OtpValidityMinutes ?? 3,
            maxFailedLogins: data.maxFailedLogins ?? data.MaxFailedLogins ?? 5,
            lockoutDurationMinutes: data.lockoutDurationMinutes ?? data.LockoutDurationMinutes ?? 15,
          };
          this.policy.set(mappedPolicy);
          this.form.get('newPassword')?.setValidators([Validators.required, Validators.minLength(mappedPolicy.minLength)]);
          this.form.get('newPassword')?.updateValueAndValidity();
        }
      },
      error: () => {
        // Keeps default secure policy if offline/failed
      },
    });
  }

  public toggleShowCurrent(): void {
    this.showCurrentPassword.update((v) => !v);
  }

  public toggleShowNew(): void {
    this.showNewPassword.update((v) => !v);
  }

  public toggleShowConfirm(): void {
    this.showConfirmPassword.update((v) => !v);
  }

  public onSubmit(): void {
    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Please fill in all required password fields.');
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.form.getRawValue();

    if (newPassword !== confirmPassword) {
      this.errorMessage.set('New password and confirmation password do not match.');
      return;
    }

    if (!this.isPolicySatisfied()) {
      this.errorMessage.set(`Password does not meet the ${this.policy().complexityTier} security policy requirements.`);
      return;
    }

    this.isSubmitting.set(true);

    if (this.mode === 'ForgotPassword') {
      const token = this.resetToken || '';
      if (!token) {
        this.isSubmitting.set(false);
        this.errorMessage.set('Missing password reset session ticket. Please restart the forgot password process.');
        return;
      }

      this.authService.resetPassword({ token, newPassword, confirmPassword }).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          this.toast.success('Password reset successfully! You can now log in.');
          this.success.emit(res);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          const msg = err?.error?.message || err?.error?.Message || err?.message || 'Failed to reset password.';
          this.errorMessage.set(msg);
        },
      });
    } else {
      // NormalChange, TemporaryPassword, ExpiredPassword
      this.authService.changePassword({ currentPassword, newPassword, confirmPassword }).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          this.authService.clearMustChangePassword();
          this.toast.success('Password changed successfully.');
          this.success.emit(res);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          const msg = err?.error?.message || err?.error?.Message || err?.message || 'Failed to update password.';
          this.errorMessage.set(msg);
        },
      });
    }
  }

  public onCancel(): void {
    this.cancel.emit();
  }
}
