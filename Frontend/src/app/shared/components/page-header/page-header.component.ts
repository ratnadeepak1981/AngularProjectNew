import { Component, OnInit, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { SystemSettingsService } from '../../../core/services/system-settings.service';
import { ApiService } from '../../../core/services/api.service';
import { ToastService } from '../../../core/services/toast.service';
import { OtpVerificationModalComponent } from '../modals/otp-verification-modal/otp-verification-modal.component';
import { ApiResponse } from '../../../core/models/common/api-response.model';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, OtpVerificationModalComponent],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.css',
})
export class PageHeaderComponent implements OnInit {
  public readonly authService = inject(AuthService);
  private readonly settingsService = inject(SystemSettingsService);
  private readonly apiService = inject(ApiService);
  private readonly toast = inject(ToastService);

  icon = input<string>('');
  title = input<string>('');
  description = input<string>('');
  badgeText = input<string>('');
  showIndexBadge = input<boolean>(true);

  public readonly academicYear = signal<string>('2025/2026');
  public readonly semester = signal<string>('Semester 1');

  // Phone OTP Verification modal state
  public readonly isOtpModalOpen = signal<boolean>(false);
  public readonly isVerifyingSms = signal<boolean>(false);
  public readonly isResendingSms = signal<boolean>(false);
  public readonly otpValidityMinutes = signal<number>(3);

  ngOnInit(): void {
    if (this.authService.isStudent()) {
      this.settingsService.getAllSettings().subscribe({
        next: (res) => {
          const dict = (res as any)?.data || res;
          if (dict) {
            if (dict['AcademicYear']) this.academicYear.set(dict['AcademicYear']);
            if (dict['Semester']) this.semester.set(dict['Semester']);
          }
        },
        error: () => {},
      });
    }
  }

  public get avatarInitials(): string {
    const name = this.authService.userProfile()?.name;
    if (!name) return 'ST';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return name.substring(0, 2).toUpperCase();
  }

  public get studentName(): string {
    return this.authService.userProfile()?.name || 'Student';
  }

  public get studentIndex(): string {
    return this.authService.userProfile()?.indexNumber || 'STU/2026/001';
  }

  public get studentFaculty(): string {
    return this.authService.userProfile()?.facultyName || 'Faculty of Computing & Technology';
  }

  public openDirectPhoneVerification(): void {
    const phone = this.authService.primaryMobileNumber();
    if (!phone) {
      this.toast.warning('No primary mobile number found on your profile. Please add one in profile settings.');
      return;
    }

    const profile = this.authService.userProfile();
    const req = {
      emailOrIndex: profile?.email || profile?.indexNumber || '',
      phoneNumber: phone,
      purpose: 'PrimaryMobileVerification'
    };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.sendPhoneOtp, req).subscribe({
      next: (res) => {
        if (res.data?.validityMinutes) {
          this.otpValidityMinutes.set(res.data.validityMinutes);
        }
        this.toast.info(`OTP security code sent to ${phone}.`);
      },
      error: () => {
        this.toast.info(`OTP security code sent to ${phone}.`);
      }
    });

    this.isOtpModalOpen.set(true);
  }

  public submitPhoneOtp(otpCode: string): void {
    const phone = this.authService.primaryMobileNumber();
    const profile = this.authService.userProfile();
    const req = {
      emailOrIndex: profile?.email || profile?.indexNumber || '',
      phoneNumber: phone,
      otpCode: otpCode.trim()
    };

    this.isVerifyingSms.set(true);
    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.verifyPhoneOtp, req).subscribe({
      next: () => {
        this.isVerifyingSms.set(false);
        this.isOtpModalOpen.set(false);

        const current = this.authService.userProfile();
        const updatedPhones = current?.phoneNumbers ? current.phoneNumbers.map(p => 
          (p.isPrimary || p.phoneType === 'Primary Mobile') ? { ...p, isVerified: true } : p
        ) : undefined;

        this.authService.updateStoredProfile({
          phoneVerified: true,
          phoneNumbers: updatedPhones
        });

        this.toast.success('Primary mobile verified successfully! All payment and campus services are now unlocked.');
      },
      error: (err) => {
        this.isVerifyingSms.set(false);
        const msg = err.error?.message || err.error?.Message || 'Invalid or expired OTP code.';
        this.toast.error(msg);
      }
    });
  }

  public resendPhoneOtp(): void {
    this.isResendingSms.set(true);
    const phone = this.authService.primaryMobileNumber();
    const profile = this.authService.userProfile();
    const req = {
      emailOrIndex: profile?.email || profile?.indexNumber || '',
      phoneNumber: phone,
      purpose: 'PrimaryMobileVerification'
    };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.sendPhoneOtp, req).subscribe({
      next: (res) => {
        this.isResendingSms.set(false);
        if (res.data?.validityMinutes) {
          this.otpValidityMinutes.set(res.data.validityMinutes);
        }
        this.toast.info('Fresh OTP sent to your primary mobile number.');
      },
      error: () => {
        this.isResendingSms.set(false);
        this.toast.info('Fresh OTP sent to your primary mobile number.');
      }
    });
  }
}
