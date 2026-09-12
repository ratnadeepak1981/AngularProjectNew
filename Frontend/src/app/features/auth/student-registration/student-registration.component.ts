import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { StudentMaster } from '../../../core/models/student/student-master.model';
import { Faculty } from '../../../core/models/faculty/faculty.model';
import { RegisterStudentRequest } from '../../../core/models/student/student-registration.model';
import { ApiResponse } from '../../../core/models/common/api-response.model';
import { VerifyEmailRequest } from '../../../core/models/auth/verify-email-request.model';
import { ResendVerificationRequest } from '../../../core/models/auth/resend-verification-request.model';
import { ActionButtonComponent } from '../../../shared/components/action-button/action-button.component';
import { PhoneNumbersFormComponent } from '../../../shared/components/forms/phone-numbers-form/phone-numbers-form.component';
import { AddressEditorComponent } from '../../../shared/components/forms/address-editor/address-editor.component';
import { OtpVerificationModalComponent } from '../../../shared/components/modals/otp-verification-modal/otp-verification-modal.component';

import { HttpContext } from '@angular/common/http';
import { SKIP_GLOBAL_ERROR_TOAST } from '../../../core/interceptors/error-interceptor';

@Component({
  selector: 'app-student-registration',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    RouterModule,
    ActionButtonComponent,
    PhoneNumbersFormComponent,
    AddressEditorComponent,
    OtpVerificationModalComponent
  ],
  templateUrl: './student-registration.component.html',
  styleUrl: './student-registration.component.css',
})
export class StudentRegistrationComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly apiService = inject(ApiService);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  // System Settings OTP Expiration
  public readonly otpValidityMinutes = signal<number>(3);

  // Enterprise Dual Verification Signals
  public readonly isMasterVerified = signal<boolean>(false);
  public readonly isEmailVerified = signal<boolean>(false);
  public readonly isPhoneVerified = signal<boolean>(false);
  public readonly verificationStep = signal<1 | 2>(1); // Step 1: Email Token, Step 2: Primary Mobile SMS OTP

  public readonly isVerifyingIndex = signal<boolean>(false);
  public readonly isRegistering = signal<boolean>(false);
  public readonly isVerifyModalOpen = signal<boolean>(false);
  public readonly isVerifyingToken = signal<boolean>(false);
  public readonly isVerifyingSms = signal<boolean>(false);
  public readonly isResendingToken = signal<boolean>(false);
  public readonly isResendingSms = signal<boolean>(false);

  public readonly verificationStatusText = signal<string | null>(null);
  public readonly verificationStatusType = signal<'success' | 'error' | null>(null);
  public readonly registrationErrorMessage = signal<string | null>(null);

  // Faculty Dropdown Data (loaded from API)
  public readonly faculties = signal<Faculty[]>([]);
  public readonly isFacultiesLoading = signal<boolean>(false);

  // Password Visibility Toggle Signals
  public readonly showPassword = signal<boolean>(false);
  public readonly showConfirmPassword = signal<boolean>(false);

  toggleShowPassword(): void {
    this.showPassword.update((val) => !val);
  }

  toggleShowConfirmPassword(): void {
    this.showConfirmPassword.update((val) => !val);
  }

  ngOnInit(): void {
    this.authService.getPasswordPolicy().subscribe({
      next: (res) => {
        const data = res?.data || res;
        const mins = data?.otpValidityMinutes ?? data?.OtpValidityMinutes ?? 3;
        if (mins > 0) {
          this.otpValidityMinutes.set(mins);
        }
      },
      error: () => {},
    });

    // Load faculties from database via API
    this.loadFaculties();
  }

  private loadFaculties(): void {
    this.isFacultiesLoading.set(true);
    this.apiService.get<ApiResponse<Faculty[]>>(this.apiService.routes.faculties.list).subscribe({
      next: (res) => {
        const data = res?.data || (res as any);
        if (Array.isArray(data)) {
          this.faculties.set(data.filter((f: Faculty) => f.isActive !== false));
        }
        this.isFacultiesLoading.set(false);
      },
      error: () => {
        this.isFacultiesLoading.set(false);
        this.toast.error('Failed to load faculties. Please refresh the page.');
      },
    });
  }

  // Forms
  public readonly registrationForm: FormGroup = this.fb.group({
    indexNumber: ['', [Validators.required]],
    fullName: [{ value: '', disabled: true }],
    email: ['', [Validators.required, Validators.email]],
    contactDetails: [''],
    phoneNumbers: this.fb.array([
      this.createPhoneControl('Primary Mobile')
    ]),
    address: this.fb.group({
      addressLine1: ['', [Validators.required]],
      addressLine2: [''],
      city: ['', [Validators.required]],
      districtOrProvince: ['Colombo'],
      postalCode: [''],
      country: ['Sri Lanka'],
    }),
    facultyId: ['', [Validators.required]],
    password: ['', [Validators.required]],
    confirmPassword: ['', [Validators.required]],
  });

  private createPhoneControl(defaultType: string = 'Primary Mobile'): FormGroup {
    const isMandatory = defaultType === 'Primary Mobile';
    const validators = isMandatory
      ? [Validators.required, Validators.pattern('^[+]*[(]?[0-9]{1,4}[)]?[-\\s./0-9]{7,15}$')]
      : [Validators.pattern('^[+]*[(]?[0-9]{1,4}[)]?[-\\s./0-9]{7,15}$')];

    return this.fb.group({
      phoneType: [defaultType, [Validators.required]],
      phoneNumber: ['', validators],
      isPrimary: [defaultType === 'Primary Mobile'],
      isVerified: [false]
    });
  }

  get phoneNumbersArray(): FormArray {
    return this.registrationForm.get('phoneNumbers') as FormArray;
  }

  get addressGroup(): FormGroup {
    return this.registrationForm.get('address') as FormGroup;
  }

  public getPrimaryMobileNumber(): string {
    const array = this.phoneNumbersArray;
    if (array && array.length > 0) {
      const primary = array.at(0)?.get('phoneNumber')?.value;
      if (primary && typeof primary === 'string' && primary.trim().length > 0) {
        return primary.trim();
      }
    }
    const cd = this.registrationForm?.get('contactDetails')?.value;
    if (cd && typeof cd === 'string' && cd.trim().length > 0) {
      return cd.trim();
    }
    return '';
  }

  verifyMasterIndex(): void {
    const indexNum = this.registrationForm.get('indexNumber')?.value?.trim();
    if (!indexNum) {
      this.toast.error('Please enter an Index Number to verify.');
      return;
    }

    this.isVerifyingIndex.set(true);
    this.verificationStatusText.set('Verifying against Registrar Master List...');
    this.verificationStatusType.set(null);

    this.apiService.get<ApiResponse<StudentMaster>>(this.apiService.routes.students.masterByIndex(indexNum)).subscribe({
      next: (res) => {
        this.isVerifyingIndex.set(false);
        const data = res.data || (res as any);
        
        if (data) {
          this.registrationForm.patchValue({
            fullName: data.fullName || 'Verified Student Record',
            facultyId: data.facultyId || 1,
          });

          this.isMasterVerified.set(true);
          this.verificationStatusText.set('✓ Index Verified in Master List! Known name pre-filled.');
          this.verificationStatusType.set('success');
          this.toast.success('Index Number validated against StudentMasterList!');
        }
      },
      error: (err) => {
        this.isVerifyingIndex.set(false);
        this.isMasterVerified.set(false);
        const errorMsg = err.error?.message || err.error?.Message || 'Index number not found in the university master list.';
        this.verificationStatusText.set(`✕ ${errorMsg}`);
        this.verificationStatusType.set('error');
        this.toast.error(errorMsg);
      },
    });
  }

  handleRegistration(): void {
    this.registrationErrorMessage.set(null);
    if (!this.isMasterVerified()) {
      this.toast.error('Please verify your Index Number against the Master List first.');
      return;
    }

    if (this.registrationForm.invalid) {
      this.registrationForm.markAllAsTouched();
      this.toast.warning('Please fill in all required registration fields including address and primary mobile.');
      return;
    }

    const formVal = this.registrationForm.getRawValue();

    if (formVal.password !== formVal.confirmPassword) {
      this.registrationErrorMessage.set('Account Password and Confirm Password do not match.');
      return;
    }

    this.isRegistering.set(true);

    const phoneList = formVal.phoneNumbers || [];
    const addr = formVal.address;

    const payload: RegisterStudentRequest = {
      indexNumber: formVal.indexNumber.trim(),
      email: formVal.email.trim(),
      password: formVal.password,
      facultyId: Number(formVal.facultyId),
      contactDetails: this.getPrimaryMobileNumber(),
      phoneNumbers: phoneList.map((p: any) => ({
        phoneType: p.phoneType,
        phoneNumber: p.phoneNumber.trim(),
        isPrimary: p.phoneType === 'Primary Mobile' || p.isPrimary,
        isVerified: false
      })),
      addresses: addr?.addressLine1 ? [{
        addressType: 'Permanent',
        addressLine1: addr.addressLine1.trim(),
        addressLine2: addr.addressLine2?.trim(),
        city: addr.city.trim(),
        districtOrProvince: addr.districtOrProvince,
        postalCode: addr.postalCode?.trim(),
        country: addr.country || 'Sri Lanka',
        isPrimary: true
      }] : []
    };

    const context = new HttpContext().set(SKIP_GLOBAL_ERROR_TOAST, true);
    this.apiService.post<ApiResponse<any>>(this.apiService.routes.students.register, payload, { context }).subscribe({
      next: () => {
        this.isRegistering.set(false);
        this.registrationErrorMessage.set(null);
        this.isEmailVerified.set(false);
        this.isPhoneVerified.set(false);
        this.verificationStep.set(1);
        this.toast.success('Student Account Created! Proceeding to Enterprise Dual Verification Gate...');
        this.isVerifyModalOpen.set(true);
      },
      error: (err) => {
        this.isRegistering.set(false);
        const errorMsg = err.error?.message || err.error?.Message || err.error || 'Failed to create student account.';
        this.registrationErrorMessage.set(errorMsg);
      },
    });
  }

  submitEmailVerification(token: string): void {
    if (!token) return;

    this.isVerifyingToken.set(true);
    const payload: VerifyEmailRequest = { token };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.verifyEmail, payload).subscribe({
      next: () => {
        this.isVerifyingToken.set(false);
        this.isEmailVerified.set(true);
        this.toast.success('Step 1 Complete: University Email Verified! Proceeding to Step 2: Primary Mobile SMS Verification.');
        this.verificationStep.set(2);
        // Automatically generate & dispatch Primary Mobile SMS OTP for Step 2
        this.resendSmsOtp();
      },
      error: (err) => {
        this.isVerifyingToken.set(false);
        const errorMsg = err.error?.message || err.error?.Message || 'Invalid or expired verification token.';
        this.toast.error(errorMsg);
      },
    });
  }

  submitSmsVerification(smsCode: string): void {
    if (!smsCode) return;

    this.isVerifyingSms.set(true);
    const formVal = this.registrationForm.getRawValue();
    const phoneNo = this.getPrimaryMobileNumber();

    const payload = {
      emailOrIndex: formVal.email?.trim() || formVal.indexNumber?.trim(),
      phoneNumber: phoneNo,
      otpCode: smsCode.trim()
    };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.verifyPhoneOtp, payload).subscribe({
      next: () => {
        this.isVerifyingSms.set(false);
        this.isPhoneVerified.set(true);
        this.toast.success(
          `Enterprise Registration Complete! Email and Primary Mobile (${phoneNo}) verified. Redirecting to sign in...`
        );
        this.isVerifyModalOpen.set(false);

        setTimeout(() => {
          this.router.navigate(['/auth/login']);
        }, 800);
      },
      error: (err) => {
        this.isVerifyingSms.set(false);
        const errorMsg = err.error?.message || err.error?.Message || 'Invalid SMS OTP code. Please try again.';
        this.toast.error(errorMsg);
      }
    });
  }

  resendVerificationToken(): void {
    const email = this.registrationForm.get('email')?.value?.trim();
    if (!email) {
      this.toast.warning('Please enter your email address to resend token.');
      return;
    }

    this.isResendingToken.set(true);
    const payload: ResendVerificationRequest = { email };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.resendVerification, payload).subscribe({
      next: () => {
        this.isResendingToken.set(false);
        this.toast.info('Fresh verification token dispatched to your email address.');
      },
      error: (err) => {
        this.isResendingToken.set(false);
        const errorMsg = err.error?.message || err.error?.Message || 'Failed to resend verification token.';
        this.toast.error(errorMsg);
      },
    });
  }

  resendSmsOtp(): void {
    this.isResendingSms.set(true);
    const formVal = this.registrationForm.getRawValue();
    const phoneNo = this.getPrimaryMobileNumber();

    const payload = {
      emailOrIndex: formVal.email?.trim() || formVal.indexNumber?.trim(),
      phoneNumber: phoneNo,
      purpose: 'Registration'
    };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.sendPhoneOtp, payload).subscribe({
      next: () => {
        this.isResendingSms.set(false);
        this.toast.info(`Fresh SMS OTP token dispatched to ${phoneNo}. Check SMS preview.`);
      },
      error: (err) => {
        this.isResendingSms.set(false);
        const errorMsg = err?.error?.message || err?.message || 'Failed to dispatch fresh SMS OTP token. Please try again.';
        this.toast.error(errorMsg);
      }
    });
  }
}
