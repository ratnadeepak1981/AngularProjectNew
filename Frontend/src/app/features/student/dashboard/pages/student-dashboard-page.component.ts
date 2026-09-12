import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { ApiService } from '../../../../core/services/api.service';
import { ToastService } from '../../../../core/services/toast.service';
import { StudentDashboardService } from '../services/student-dashboard.service';
import { DashboardCardComponent } from '../../../../shared/components/cards/dashboard-card/dashboard-card.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { PhoneNumbersFormComponent } from '../../../../shared/components/forms/phone-numbers-form/phone-numbers-form.component';
import { AddressEditorComponent } from '../../../../shared/components/forms/address-editor/address-editor.component';
import { OtpVerificationModalComponent } from '../../../../shared/components/modals/otp-verification-modal/otp-verification-modal.component';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { Notification } from '../../../../core/models/system/notification.model';
import { StudentPhoneNumber } from '../../../../core/models/student/student-phone-number.model';
import { StudentAddress } from '../../../../core/models/student/student-address.model';
import { UpdateStudentProfileRequest } from '../../../../core/models/auth/student-profile.model';
import { ApiResponse } from '../../../../core/models/common/api-response.model';

@Component({
  selector: 'app-student-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    DashboardCardComponent,
    PageHeaderComponent,
    ActionButtonComponent,
    PhoneNumbersFormComponent,
    AddressEditorComponent,
    OtpVerificationModalComponent,
  ],
  templateUrl: './student-dashboard-page.component.html',
  styleUrl: './student-dashboard-page.component.css',
})
export class StudentDashboardPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dashboardService = inject(StudentDashboardService);
  private readonly apiService = inject(ApiService);
  public readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);

  public readonly studentId = signal<number>(0);
  public readonly indexNumber = signal<string>('Loading...');
  public readonly facultyName = signal<string>('Faculty of Applied Sciences');
  public readonly isSavingProfile = signal<boolean>(false);
  public readonly isLoadingMetrics = signal<boolean>(true);
  public readonly isEditProfileModalOpen = signal<boolean>(false);

  // OTP Verification for Primary Mobile Update
  public readonly initialPrimaryMobile = signal<string>('');
  public readonly isPrimaryPhoneVerified = signal<boolean>(true);
  public readonly isOtpModalOpen = signal<boolean>(false);
  public readonly isVerifyingSms = signal<boolean>(false);
  public readonly isResendingSms = signal<boolean>(false);
  public readonly otpValidityMinutes = signal<number>(3);
  public readonly pendingUpdatePayload = signal<UpdateStudentProfileRequest | null>(null);

  // Address Display Summary
  public readonly currentAddressSummary = signal<string>('Not provided');

  // 6 Live Metric Signals
  public readonly hostelStatus = signal<string>('Checking...');
  public readonly activeLabBookings = signal<number>(0);
  public readonly registeredEvents = signal<number>(0);
  public readonly outstandingFees = signal<number>(0);
  public readonly certificateStatus = signal<string>('Checking...');
  public readonly complaintStatus = signal<string>('Checking...');

  // Recent Notifications Signal & Filter State
  public readonly recentNotifications = signal<Notification[]>([]);
  public readonly feedFilter = signal<string>('ALL');

  public readonly filteredNotifications = computed(() => {
    const list = this.recentNotifications();
    const filter = this.feedFilter();
    if (filter === 'ALL') return list;
    if (filter === 'LABS') return list.filter(n => n.type?.toLowerCase().includes('lab'));
    if (filter === 'HOSTELS') return list.filter(n => n.type?.toLowerCase().includes('hostel'));
    if (filter === 'PAYMENTS') return list.filter(n => n.type?.toLowerCase().includes('fee') || n.type?.toLowerCase().includes('pay'));
    return list;
  });

  public readonly profileForm: FormGroup = this.fb.group({
    fullName: ['', [Validators.required]],
    contactDetails: [''],
    phoneNumbers: this.fb.array([
      this.createPhoneControl('Primary Mobile')
    ]),
    address: this.fb.group({
      id: [0],
      addressType: ['Permanent'],
      addressLine1: ['', [Validators.required]],
      addressLine2: [''],
      city: ['', [Validators.required]],
      districtOrProvince: ['Colombo'],
      postalCode: [''],
      country: ['Sri Lanka'],
    }),
  });

  private readonly formValues = toSignal(this.profileForm.valueChanges, {
    initialValue: this.profileForm.value,
  });

  public readonly primaryPhoneNumber = computed(() => {
    const formVal = this.formValues();
    const list = formVal?.phoneNumbers;
    if (list && list.length > 0 && list[0]?.phoneNumber) {
      return list[0].phoneNumber;
    }
    const profile = this.authService.userProfile();
    return profile?.contactDetails || '+94 77 123 4567';
  });

  public readonly allPhoneNumbersList = computed(() => {
    const formVal = this.formValues();
    const list = formVal?.phoneNumbers;
    if (list && list.length > 0) {
      const items = list.filter((p: any) => p && p.phoneNumber && p.phoneNumber.trim() !== '');
      if (items.length > 0) return items;
    }
    const profile = this.authService.userProfile();
    if (profile?.contactDetails) {
      return [{ phoneType: 'Primary Mobile', phoneNumber: profile.contactDetails }];
    }
    return [{ phoneType: 'Primary Mobile', phoneNumber: '+94 77 123 4567' }];
  });

  private createPhoneControl(defaultType: string = 'Primary Mobile', numberValue: string = '', isPrimary: boolean = true, isVerified: boolean = false): FormGroup {
    const isMandatory = defaultType === 'Primary Mobile';
    const validators = isMandatory
      ? [Validators.required, Validators.pattern('^[+]*[(]?[0-9]{1,4}[)]?[-\\s./0-9]{7,15}$')]
      : [Validators.pattern('^[+]*[(]?[0-9]{1,4}[)]?[-\\s./0-9]{7,15}$')];

    return this.fb.group({
      phoneType: [defaultType, [Validators.required]],
      phoneNumber: [numberValue, validators],
      isPrimary: [defaultType === 'Primary Mobile' || isPrimary],
      isVerified: [isVerified]
    });
  }

  get phoneNumbersArray(): FormArray {
    return this.profileForm.get('phoneNumbers') as FormArray;
  }

  get addressGroup(): FormGroup {
    return this.profileForm.get('address') as FormGroup;
  }

  public openEditProfileModal(): void {
    this.isEditProfileModalOpen.set(true);
  }

  public closeEditProfileModal(): void {
    this.isEditProfileModalOpen.set(false);
  }

  ngOnInit(): void {
    const profile = this.authService.userProfile();
    const storedId = profile?.id || 0;
    this.studentId.set(storedId);

    if (profile) {
      if (profile.phoneVerified !== undefined) {
        this.isPrimaryPhoneVerified.set(profile.phoneVerified);
      } else if (profile.phoneNumbers && profile.phoneNumbers.length > 0) {
        const prim = profile.phoneNumbers.find(p => p.isPrimary || p.phoneType === 'Primary Mobile');
        if (prim) {
          this.isPrimaryPhoneVerified.set(prim.isVerified);
          this.initialPrimaryMobile.set(prim.phoneNumber);
        }
      }
    }

    if (storedId > 0) {
      this.loadProfile(storedId);
      this.loadMetrics(storedId);
    }
  }

  loadProfile(id: number): void {
    this.dashboardService.getStudentProfile(id).subscribe({
      next: (res) => {
        const student = res?.data || res;
        if (student) {
          const name = student.fullName || student.name || student.FullName || student.Name || '';
          const index = student.indexNumber || student.IndexNumber || 'N/A';
          const faculty = student.facultyName || student.FacultyName || student.faculty?.name || 'Faculty of Computing';
          const contact = student.contactDetails || student.ContactDetails || '';

          this.indexNumber.set(index);
          this.facultyName.set(faculty);

          this.profileForm.patchValue({
            fullName: name,
            contactDetails: contact,
          });

          // Populate Phone Numbers FormArray
          const phoneList: StudentPhoneNumber[] = student.phoneNumbers || student.PhoneNumbers || [];
          if (phoneList && phoneList.length > 0) {
            this.phoneNumbersArray.clear();
            let primaryMobile = '';
            let primVerified = false;
            phoneList.forEach((p) => {
              const isPrim = p.isPrimary || p.phoneType === 'Primary Mobile';
              if (isPrim && !primaryMobile) {
                primaryMobile = p.phoneNumber;
                primVerified = p.isVerified;
              }
              this.phoneNumbersArray.push(this.createPhoneControl(p.phoneType, p.phoneNumber, isPrim, p.isVerified));
            });
            this.initialPrimaryMobile.set(primaryMobile);
            this.isPrimaryPhoneVerified.set(primVerified);
          } else if (contact) {
            this.phoneNumbersArray.clear();
            const numbers = contact.split('|').map((s: string) => s.trim()).filter(Boolean);
            if (numbers.length > 0) {
              numbers.forEach((numStr: string, idx: number) => {
                let type = idx === 0 ? 'Primary Mobile' : 'Home Landline';
                let num = numStr;
                if (numStr.includes(':')) {
                  const parts = numStr.split(':');
                  type = parts[0].trim();
                  num = parts[1].trim();
                }
                if (idx === 0) {
                  this.initialPrimaryMobile.set(num);
                  this.isPrimaryPhoneVerified.set(student.phoneVerified ?? false);
                }
                this.phoneNumbersArray.push(this.createPhoneControl(type, num, idx === 0, student.phoneVerified ?? false));
              });
            } else {
              this.initialPrimaryMobile.set(contact);
              this.isPrimaryPhoneVerified.set(student.phoneVerified ?? false);
              this.phoneNumbersArray.push(this.createPhoneControl('Primary Mobile', contact, true, student.phoneVerified ?? false));
            }
          }

          // Populate Addresses
          const addressList: StudentAddress[] = student.addresses || student.Addresses || [];
          if (addressList && addressList.length > 0) {
            const primAddr = addressList.find(a => a.isPrimary) || addressList[0];
            this.addressGroup.patchValue({
              id: primAddr.id || 0,
              addressType: primAddr.addressType || 'Permanent',
              addressLine1: primAddr.addressLine1 || '',
              addressLine2: primAddr.addressLine2 || '',
              city: primAddr.city || '',
              districtOrProvince: primAddr.districtOrProvince || 'Colombo',
              postalCode: primAddr.postalCode || '',
              country: primAddr.country || 'Sri Lanka',
            });
            this.currentAddressSummary.set(`${primAddr.addressLine1}, ${primAddr.city}`);
          }

          this.authService.updateStoredProfile({
            id,
            name,
            indexNumber: index,
            facultyName: faculty,
            contactDetails: contact,
          });
        }
      },
      error: () => {},
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      this.toast.warning('Please complete all required fields (Street Address & City).');
      return;
    }

    const formVal = this.profileForm.getRawValue();
    const phoneList = formVal.phoneNumbers || [];
    const addr = formVal.address;

    const primaryPhoneItem = phoneList.find((p: any) => p.phoneType === 'Primary Mobile' || p.isPrimary) || phoneList[0];
    const formattedContact = primaryPhoneItem?.phoneNumber?.trim() || (formVal.contactDetails || '');

    const currentFaculty = this.facultyName();
    let facultyId = 1;
    if (currentFaculty.includes('Computing')) {
      facultyId = 1;
    } else if (currentFaculty.includes('Business') || currentFaculty.includes('Commerce')) {
      facultyId = 3;
    } else if (currentFaculty.includes('Science')) {
      facultyId = 2;
    } else if (currentFaculty.includes('Humanities')) {
      facultyId = 4;
    }

    const payload: UpdateStudentProfileRequest = {
      fullName: formVal.fullName.trim(),
      contactDetails: formattedContact,
      facultyId,
      phoneNumbers: phoneList.map((p: any) => ({
        phoneType: p.phoneType,
        phoneNumber: p.phoneNumber.trim(),
        isPrimary: p.phoneType === 'Primary Mobile' || p.isPrimary,
        isVerified: p.isVerified ?? false
      })),
      addresses: addr?.addressLine1 ? [{
        id: addr.id || 0,
        addressType: addr.addressType || 'Permanent',
        addressLine1: addr.addressLine1.trim(),
        addressLine2: addr.addressLine2?.trim(),
        city: addr.city.trim(),
        districtOrProvince: addr.districtOrProvince,
        postalCode: addr.postalCode?.trim(),
        country: addr.country || 'Sri Lanka',
        isPrimary: true
      }] : []
    };

    // Find current Primary Mobile
    const normalizePhone = (num: string) => (num || '').replace(/[\s\-\(\)]/g, '').trim();
    const currentPrimary = phoneList.find((p: any) => p.phoneType === 'Primary Mobile' || p.isPrimary)?.phoneNumber?.trim();
    const prevPrimary = this.initialPrimaryMobile()?.trim();

    // Check if Primary Mobile Number has changed
    const primaryChanged = Boolean(
      currentPrimary &&
      prevPrimary &&
      normalizePhone(currentPrimary) !== normalizePhone(prevPrimary)
    );

    if (primaryChanged) {
      // Primary mobile modified -> Trigger OTP verification flow
      this.pendingUpdatePayload.set(payload);
      this.dispatchPhoneOtpForUpdate(currentPrimary);
      this.isOtpModalOpen.set(true);
      return;
    }

    // No Primary Mobile change -> Direct update
    this.executeProfileUpdate(payload);
  }

  private dispatchPhoneOtpForUpdate(phoneNumber: string): void {
    const profile = this.authService.userProfile();
    const req = {
      emailOrIndex: profile?.email || this.indexNumber(),
      phoneNumber,
      purpose: 'PrimaryMobileUpdate'
    };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.sendPhoneOtp, req).subscribe({
      next: (res) => {
        if (res.data?.validityMinutes) {
          this.otpValidityMinutes.set(res.data.validityMinutes);
        }
        this.toast.info(`Primary Mobile changed. OTP security code sent to ${phoneNumber}.`);
      },
      error: () => {
        this.toast.info(`Primary Mobile changed. OTP code generated for ${phoneNumber}.`);
      }
    });
  }

  public openDirectPhoneVerification(): void {
    const currentPrimary = this.initialPrimaryMobile();
    if (!currentPrimary) {
      this.openEditProfileModal();
      return;
    }
    this.pendingUpdatePayload.set(null);
    this.dispatchPhoneOtpForUpdate(currentPrimary);
    this.isOtpModalOpen.set(true);
  }

  public submitPhoneOtpForProfileUpdate(otpCode: string): void {
    const payload = this.pendingUpdatePayload();
    if (payload) {
      this.isVerifyingSms.set(true);
      payload.mobileOtpCode = otpCode.trim();
      this.executeProfileUpdate(payload, true);
      return;
    }

    // Direct OTP verification flow for existing unverified primary mobile
    const profile = this.authService.userProfile();
    const phone = this.initialPrimaryMobile();
    const req = {
      emailOrIndex: profile?.email || this.indexNumber(),
      phoneNumber: phone,
      otpCode: otpCode.trim()
    };

    this.isVerifyingSms.set(true);
    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.verifyPhoneOtp, req).subscribe({
      next: () => {
        this.isVerifyingSms.set(false);
        this.isOtpModalOpen.set(false);
        this.isPrimaryPhoneVerified.set(true);
        this.toast.success('Primary mobile verified successfully! All payment and campus services are now unlocked.');
        if (this.studentId() > 0) {
          this.loadProfile(this.studentId());
        }
      },
      error: (err) => {
        this.isVerifyingSms.set(false);
        const msg = err.error?.message || err.error?.Message || 'Invalid or expired OTP code.';
        this.toast.error(msg);
      }
    });
  }

  public resendPhoneOtpForProfileUpdate(): void {
    this.isResendingSms.set(true);
    const formVal = this.profileForm.getRawValue();
    const currentPrimary = formVal.phoneNumbers?.find((p: any) => p.phoneType === 'Primary Mobile' || p.isPrimary)?.phoneNumber?.trim();
    const profile = this.authService.userProfile();

    const req = {
      emailOrIndex: profile?.email || this.indexNumber(),
      phoneNumber: currentPrimary || this.initialPrimaryMobile(),
      purpose: 'PrimaryMobileUpdate'
    };

    this.apiService.post<ApiResponse<any>>(this.apiService.routes.account.sendPhoneOtp, req).subscribe({
      next: (res) => {
        this.isResendingSms.set(false);
        if (res.data?.validityMinutes) {
          this.otpValidityMinutes.set(res.data.validityMinutes);
        }
        this.toast.info('Fresh OTP sent to your new primary mobile number.');
      },
      error: () => {
        this.isResendingSms.set(false);
        this.toast.info('Fresh OTP sent to your new primary mobile number.');
      }
    });
  }

  private executeProfileUpdate(payload: UpdateStudentProfileRequest, isFromOtp: boolean = false): void {
    this.isSavingProfile.set(true);
    this.dashboardService
      .updateStudentProfile(this.studentId(), payload)
      .subscribe({
        next: () => {
          this.isSavingProfile.set(false);
          this.isVerifyingSms.set(false);
          this.isOtpModalOpen.set(false);
          this.closeEditProfileModal();

          const newPrimary = payload.phoneNumbers?.find(p => p.isPrimary || p.phoneType === 'Primary Mobile')?.phoneNumber;
          if (newPrimary) {
            this.initialPrimaryMobile.set(newPrimary);
            this.isPrimaryPhoneVerified.set(true);
          }

          if (payload.addresses && payload.addresses.length > 0) {
            const a = payload.addresses[0];
            this.currentAddressSummary.set(`${a.addressLine1}, ${a.city}`);
          }

          this.toast.success('Profile details updated successfully.');
          this.authService.updateStoredProfile({
            name: payload.fullName,
            contactDetails: payload.contactDetails,
          });
        },
        error: (err) => {
          this.isSavingProfile.set(false);
          this.isVerifyingSms.set(false);
          const msg = err.error?.message || err.error?.Message || 'Failed to update profile.';
          
          if (msg && msg.toLowerCase().includes('otp')) {
            this.pendingUpdatePayload.set(payload);
            const formVal = this.profileForm.getRawValue();
            const currentPrimary = formVal.phoneNumbers?.find((p: any) => p.phoneType === 'Primary Mobile' || p.isPrimary)?.phoneNumber?.trim();
            this.dispatchPhoneOtpForUpdate(currentPrimary || this.initialPrimaryMobile());
            this.isOtpModalOpen.set(true);
            return;
          }

          this.toast.error(msg);
        },
      });
  }

  loadMetrics(id: number): void {
    this.isLoadingMetrics.set(true);
    this.dashboardService.getDashboardMetrics(id).subscribe({
      next: (summary) => {
        this.hostelStatus.set(summary.hostelStatus);
        this.activeLabBookings.set(summary.activeLabBookings);
        this.outstandingFees.set(summary.outstandingFees);
        this.registeredEvents.set(summary.registeredEvents);
        this.certificateStatus.set(summary.certificateStatus);
        this.complaintStatus.set(summary.complaintStatus);
        this.recentNotifications.set(summary.recentNotifications);
        this.isLoadingMetrics.set(false);
      },
      error: () => {
        this.isLoadingMetrics.set(false);
      },
    });
  }

  public setFeedFilter(filter: string): void {
    this.feedFilter.set(filter);
  }

  public formatNoticeType(type?: string): string {
    if (!type) return 'Notice Alert';
    if (type === 'LabBookingConfirmed') return 'Lab Booking Confirmed';
    if (type === 'HostelApplicationSubmitted') return 'Hostel Application Submitted';
    if (type === 'FeePaymentCleared') return 'Fee Payment Cleared';
    return type.replace(/([A-Z])/g, ' $1').trim();
  }
}
