import { Component, EventEmitter, Input, Output, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FeePaymentItem } from '../../services/student-billing.service';
import { ApiService } from '../../../../../core/services/api.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { ToastService } from '../../../../../core/services/toast.service';

@Component({
  selector: 'app-payment-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './payment-checkout.component.html',
  styleUrl: './payment-checkout.component.css',
})
export class PaymentCheckoutComponent implements OnInit, OnDestroy {
  private readonly toast = inject(ToastService);
  private readonly apiService = inject(ApiService);
  private readonly authService = inject(AuthService);

  @Input() item: FeePaymentItem | null = null;
  @Input() isSubmitting = false;

  @Output() submitPayment = new EventEmitter<{ channel: string; details: any }>();
  @Output() cancelCheckout = new EventEmitter<void>();

  // Payment Channels: 'card' | 'lankapay' | 'slip'
  public readonly selectedChannel = signal<'card' | 'lankapay' | 'slip'>('card');

  // Card Channel Signals
  public readonly cardholderName = signal<string>('Kamal Perera');
  public readonly cardNumber = signal<string>('4532 9876 5432 8892');
  public readonly expiryDate = signal<string>('12/28');
  public readonly cvc = signal<string>('789');
  public readonly selectedCardBrand = signal<string>('visa');

  // 3D Secure SMS OTP Modal Signals & System Settings Dynamic Countdown Timer
  public readonly is3DSecureOpen = signal<boolean>(false);
  public readonly cardOtpCode = signal<string>('');
  public readonly currentOtpToken = signal<string>('');
  public readonly validatedCardDetails = signal<any>(null);
  
  public readonly otpValidityMinutes = signal<number>(3);
  public readonly countdownSeconds = signal<number>(180);
  private timerInterval: any = null;

  public readonly formattedCountdown = computed<string>(() => {
    const total = Math.max(0, this.countdownSeconds());
    const mins = Math.floor(total / 60);
    const secs = total % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  });

  public readonly isExpired = computed<boolean>(() => this.countdownSeconds() <= 0);

  // LankaPay Channel Signals
  public readonly selectedLankaBank = signal<string>('combank_digital');
  public readonly bankAccountHolder = signal<string>('');
  public readonly lankaRefNo = signal<string>('');

  // Comprehensive Sri Lankan Licensed Banks Array (LankaPay Network)
  public readonly lankaBanks = [
    { id: 'combank_digital', name: 'Commercial Bank of Ceylon - ComBank Digital' },
    { id: 'sampath_vishwa', name: 'Sampath Bank PLC - Sampath Vishwa' },
    { id: 'hnb_solo', name: 'Hatton National Bank PLC - HNB Solo / Online' },
    { id: 'boc_smartpay', name: 'Bank of Ceylon (BOC) - SmartPay Direct' },
    { id: 'peoples_wave', name: "People's Bank - People's Wave Online" },
    { id: 'nsb_direct', name: 'National Savings Bank (NSB) - NSB Direct' },
    { id: 'ntb_online', name: 'Nations Trust Bank (NTB) - NTB Online' },
    { id: 'seylan_simplypay', name: 'Seylan Bank PLC - Seylan SimplyPay' },
    { id: 'dfcc_virtual', name: 'DFCC Bank PLC - DFCC Virtual Wallet' },
    { id: 'ndb_neos', name: 'NDB Bank PLC - NDB NEOS Direct' },
    { id: 'pan_asia', name: 'Pan Asia Banking Corporation - Pan Asia Online' },
    { id: 'cargills_online', name: 'Cargills Bank Limited - Cargills Online' },
    { id: 'union_bank', name: 'Union Bank of Colombo PLC - Union Bank Online' },
    { id: 'sdb_online', name: 'SANASA Development Bank (SDB) - SDB Online' },
    { id: 'amana_bank', name: 'Amana Bank PLC - Amana Internet Banking' },
  ];

  // Deposit Slip Channel Signals
  public readonly slipFileName = signal<string>('');
  public readonly slipRefNo = signal<string>('');
  public readonly slipTransferDate = signal<string>('');

  // Real-World Payment Gateway Card Brands
  public readonly cardBrands = [
    { id: 'visa', name: 'Visa', icon: '💳', code: 'VISA' },
    { id: 'mastercard', name: 'Mastercard', icon: '🔴🟡', code: 'MC' },
    { id: 'amex', name: 'American Express', icon: '🌐', code: 'AMEX' },
    { id: 'unionpay', name: 'UnionPay', icon: '🟢🔴', code: 'UNIONPAY' },
    { id: 'discover', name: 'Discover / JCB', icon: '🟠', code: 'DISCOVER' },
  ];

  ngOnInit(): void {
    const profile = this.authService.userProfile();
    if (profile?.name) {
      this.cardholderName.set(profile.name);
    }
    this.loadOtpPolicy();
  }

  public getStudentPrimaryMobile(): string {
    const profile = this.authService.userProfile();
    if (profile) {
      if (profile.phoneNumbers && profile.phoneNumbers.length > 0) {
        const primary = profile.phoneNumbers.find((p) => p.isPrimary || p.phoneType === 'Primary Mobile');
        if (primary?.phoneNumber) return primary.phoneNumber.trim();
        return profile.phoneNumbers[0].phoneNumber.trim();
      }
      if (profile.contactDetails) {
        const match = profile.contactDetails.match(/\+?\d[\d\s\-]{7,15}\d/);
        if (match) return match[0].trim();
      }
    }
    return '+94771234566';
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

  ngOnDestroy(): void {
    this.stopTimer();
  }

  selectCardBrand(brandId: string): void {
    this.selectedCardBrand.set(brandId);
    if (brandId === 'amex') {
      this.cardNumber.set('3782 822468 31005');
      this.cvc.set('4321');
    } else if (brandId === 'visa') {
      this.cardNumber.set('4532 9876 5432 8892');
      this.cvc.set('789');
    } else if (brandId === 'mastercard') {
      this.cardNumber.set('5412 7512 3412 3456');
      this.cvc.set('654');
    } else if (brandId === 'unionpay') {
      this.cardNumber.set('6221 2600 1234 5678');
      this.cvc.set('321');
    } else {
      this.cardNumber.set('6011 0009 1234 5678');
      this.cvc.set('987');
    }
  }

  onSlipFileSelected(event: any): void {
    const file = event.target?.files?.[0];
    if (file) {
      this.slipFileName.set(file.name);
    }
  }

  onSubmit(): void {
    const ch = this.selectedChannel();

    if (ch === 'card') {
      const brand = this.selectedCardBrand();
      const rawNum = this.cardNumber().replace(/\s+/g, '');
      const cvcVal = this.cvc().trim();
      const expVal = this.expiryDate().trim();
      const nameVal = this.cardholderName().trim();

      if (nameVal.length < 3) {
        this.toast.error('Please enter a valid cardholder name (minimum 3 characters).');
        return;
      }

      const requiredDigits = brand === 'amex' ? 15 : 16;
      if (!/^\d+$/.test(rawNum) || rawNum.length !== requiredDigits) {
        this.toast.error(`${brand.toUpperCase()} cards require strictly ${requiredDigits} numeric digits.`);
        return;
      }

      const requiredCvcLength = brand === 'amex' ? 4 : 3;
      if (!/^\d+$/.test(cvcVal) || cvcVal.length !== requiredCvcLength) {
        this.toast.error(`${brand.toUpperCase()} CVC requires strictly ${requiredCvcLength} digits.`);
        return;
      }

      const cardDetails = {
        cardholderName: nameVal,
        cardNumber: rawNum,
        expiryDate: expVal,
        cvc: cvcVal,
        cardBrand: brand,
      };

      const initialOtp = Math.floor(100000 + Math.random() * 900000).toString();
      const userEmail = this.authService.userProfile()?.email || 'student@university.ac.lk';
      const userPhone = this.getStudentPrimaryMobile();
      this.validatedCardDetails.set(cardDetails);
      this.cardOtpCode.set('');
      this.currentOtpToken.set(initialOtp);
      this.is3DSecureOpen.set(true);
      this.startTimer();

      // Dispatch Payment OTP SMS via Shared API Endpoint
      this.apiService
        .post<any>('/sms/send', {
          phoneNumber: userPhone,
          email: userEmail,
          purpose: 'PaymentOtp',
          otpCode: initialOtp,
          amount: this.item?.amount || 1500.0,
          transactionId: `TXN-${this.item?.id || 101}`,
        })
        .subscribe({
          next: () => {
            this.toast.success(`3D Secure OTP dispatched to card mobile line (${userPhone})! Valid for ${this.otpValidityMinutes()} minutes.`);
          },
          error: () => {
            this.toast.success(`3D Secure OTP generated for ${userPhone}. Valid for ${this.otpValidityMinutes()} minutes.`);
          },
        });
    } else if (ch === 'lankapay') {
      const details = {
        bankPortal: this.selectedLankaBank(),
        accountHolder: this.bankAccountHolder(),
        referenceNo: this.lankaRefNo(),
      };
      this.submitPayment.emit({ channel: ch, details });
    } else {
      const details = {
        fileName: this.slipFileName() || 'Bank_Deposit_Slip.pdf',
        referenceNo: this.slipRefNo(),
        transferDate: this.slipTransferDate(),
      };
      this.submitPayment.emit({ channel: ch, details });
    }
  }

  onAuthorize3DSecure(): void {
    if (this.isExpired()) {
      this.toast.error('The Payment OTP has expired. Please click "Resend OTP" to receive a fresh code.');
      return;
    }

    const otp = this.cardOtpCode().trim();
    if (!otp || otp.length !== 6 || !/^\d+$/.test(otp)) {
      this.toast.error('Please enter a valid 6-digit numeric OTP code.');
      return;
    }

    // Invalid OTP Check
    const validToken = this.currentOtpToken();
    if (otp !== validToken) {
      this.toast.error('Invalid OTP code entered. Please check your SMS or click "Resend OTP".');
      return;
    }

    this.stopTimer();
    this.is3DSecureOpen.set(false);
    const details = {
      ...this.validatedCardDetails(),
      otpCode: otp,
    };
    this.submitPayment.emit({ channel: 'card', details });
  }

  resendOtp(): void {
    const newOtp = Math.floor(100000 + Math.random() * 900000).toString();
    const userEmail = this.authService.userProfile()?.email || 'student@university.ac.lk';
    const userPhone = this.getStudentPrimaryMobile();
    this.currentOtpToken.set(newOtp);
    this.cardOtpCode.set('');
    this.startTimer();

    this.apiService
      .post<any>('/sms/send', {
        phoneNumber: userPhone,
        email: userEmail,
        purpose: 'PaymentOtp',
        otpCode: newOtp,
        amount: this.item?.amount || 1500.0,
        transactionId: `TXN-${this.item?.id || 101}`,
      })
      .subscribe({
        next: () => {
          this.toast.success(`Fresh 3D Secure OTP dispatched to ${userPhone}! Valid for ${this.otpValidityMinutes()} minutes.`);
        },
        error: () => {
          this.toast.success(`Fresh 3D Secure OTP generated for ${userPhone}!`);
        },
      });
  }

  close3DSecureModal(): void {
    this.stopTimer();
    this.is3DSecureOpen.set(false);
    this.toast.info('Payment authorization cancelled.');
  }

  private startTimer(): void {
    this.stopTimer();
    const duration = (this.otpValidityMinutes() && this.otpValidityMinutes() > 0 ? this.otpValidityMinutes() : 3) * 60;
    this.countdownSeconds.set(duration);
    this.timerInterval = setInterval(() => {
      const current = this.countdownSeconds();
      if (current <= 1) {
        this.countdownSeconds.set(0);
        this.stopTimer();
      } else {
        this.countdownSeconds.set(current - 1);
      }
    }, 1000);
  }

  private stopTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }
}
