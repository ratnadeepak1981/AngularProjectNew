import { Component, EventEmitter, Input, Output, OnInit, OnChanges, OnDestroy, SimpleChanges, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-otp-verification-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    @if (isOpen) {
      <div class="fixed inset-0 z-[70] flex items-center justify-center p-4 bg-slate-900/70 backdrop-blur-sm animate-fade-in">
        <div class="bg-white dark:bg-slate-900 rounded-3xl max-w-md w-full p-6 shadow-2xl border border-slate-200 dark:border-slate-800 space-y-5">
          <!-- Modal Header -->
          <div class="flex items-center justify-between border-b border-slate-100 dark:border-slate-800 pb-3">
            <div class="flex items-center gap-2.5">
              <span class="text-2xl">{{ mode === 'dual' ? '🛡️' : '📱' }}</span>
              <div>
                <h3 class="text-base font-bold text-slate-900 dark:text-white">
                  {{ mode === 'dual' ? 'Enterprise Dual Verification' : 'Mobile OTP Verification' }}
                </h3>
                <p class="text-2xs text-slate-500">
                  {{ mode === 'dual' ? 'Step ' + currentStep + ' of 2 Identity Gate' : 'Verify new Primary Mobile number' }}
                </p>
              </div>
            </div>
            @if (canClose) {
              <button
                type="button"
                (click)="onCancel()"
                class="p-1.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg cursor-pointer"
              >
                ✕
              </button>
            }
          </div>

          <!-- Step 1: Email Token (Dual Mode Only) -->
          @if (mode === 'dual' && currentStep === 1) {
            <form [formGroup]="emailForm" (ngSubmit)="submitEmailToken()" class="space-y-4">
              <div class="p-3.5 rounded-xl bg-blue-50 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-800/60 text-xs text-blue-900 dark:text-blue-200">
                <p class="font-bold flex items-center gap-1.5 mb-1">
                  <span>✉️</span> Step 1: University Email Verification
                </p>
                <p class="text-2xs opacity-90">
                  Enter the verification token dispatched to <strong>{{ emailAddress }}</strong>.
                </p>
              </div>

              <div>
                <label class="block text-2xs font-bold uppercase tracking-wider text-slate-500 mb-1">
                  Email Verification Token <span class="text-rose-500">*</span>
                </label>
                <input
                  type="text"
                  formControlName="token"
                  placeholder="Paste token or enter GUID code..."
                  class="w-full px-3.5 py-2.5 text-xs rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-100 font-mono focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div class="flex items-center justify-between pt-2">
                <button
                  type="button"
                  (click)="resendEmail.emit()"
                  [disabled]="isResending"
                  class="text-2xs font-bold text-blue-600 hover:underline cursor-pointer disabled:opacity-50"
                >
                  🔄 Resend Email Token
                </button>

                <button
                  type="submit"
                  [disabled]="emailForm.invalid || isVerifying"
                  class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs rounded-xl transition-colors cursor-pointer disabled:opacity-50 flex items-center gap-1.5 shadow-sm"
                >
                  @if (isVerifying) { <span>⏳</span> } @else { <span>Verify Email ➔</span> }
                </button>
              </div>
            </form>
          }

          <!-- Step 2 (or Single Mode): Mobile SMS OTP -->
          @if ((mode === 'dual' && currentStep === 2) || mode === 'phone-only') {
            <form [formGroup]="otpForm" (ngSubmit)="submitSmsOtp()" class="space-y-4">
              <div class="p-3.5 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800/60 text-xs text-emerald-900 dark:text-emerald-200">
                <p class="font-bold flex items-center gap-1.5 mb-1">
                  <span>📱</span> Mobile SMS Authorization Code
                </p>
                <p class="text-2xs opacity-90">
                  Enter the 6-digit OTP code sent to <strong>{{ phoneNumber }}</strong>.
                </p>
              </div>

              <!-- Live 2-Minute Countdown Expiry Timer -->
              @if (!isExpired()) {
                <div class="flex items-center justify-between px-3.5 py-2 rounded-xl bg-amber-500/10 dark:bg-amber-950/30 border border-amber-400/40 dark:border-amber-700/50 text-xs">
                  <span class="text-amber-950 dark:text-amber-200 font-bold flex items-center gap-1.5">
                    <span>⏱️</span> Time Remaining:
                  </span>
                  <span class="font-mono font-black text-xs px-2.5 py-0.5 rounded-md bg-white dark:bg-slate-900 text-amber-700 dark:text-amber-300 border border-amber-300/80 dark:border-amber-700/60 shadow-2xs">
                    {{ formattedCountdown() }}
                  </span>
                </div>
              } @else {
                <div class="p-3 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-300 dark:border-rose-800/60 text-xs text-rose-800 dark:text-rose-200 font-bold flex items-center gap-2 animate-fade-in">
                  <span>⚠️</span>
                  <span>OTP code has expired. Please click "Resend SMS OTP" to generate a new code.</span>
                </div>
              }

              <div>
                <label class="block text-2xs font-bold uppercase tracking-wider text-slate-500 mb-1">
                  6-Digit SMS OTP Code <span class="text-rose-500">*</span>
                </label>
                <input
                  type="text"
                  formControlName="otpCode"
                  maxlength="6"
                  placeholder="• • • • • •"
                  [readonly]="isExpired()"
                  class="w-full px-4 py-3 text-center text-lg font-black tracking-widest rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-800 dark:text-slate-100 font-mono focus:ring-2 focus:ring-emerald-500 shadow-inner disabled:opacity-60"
                />
              </div>

              <div class="flex items-center justify-between pt-2">
                <button
                  type="button"
                  (click)="handleResendSms()"
                  [disabled]="isResending"
                  class="text-2xs font-bold text-emerald-600 hover:underline cursor-pointer disabled:opacity-50 flex items-center gap-1"
                >
                  <span>🔄</span> Resend SMS OTP
                </button>

                <div class="flex items-center gap-2">
                  @if (allowSkip) {
                    <button
                      type="button"
                      (click)="onSkip()"
                      class="px-3 py-2 text-xs font-semibold bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-xl transition-colors cursor-pointer flex items-center gap-1"
                      title="Skip SMS verification and verify later before payments"
                    >
                      <span>⏭️</span> Verify Later
                    </button>
                  }
                  @if (canClose) {
                    <button
                      type="button"
                      (click)="onCancel()"
                      class="px-3 py-2 text-xs font-semibold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded-xl cursor-pointer"
                    >
                      Cancel
                    </button>
                  }
                  <button
                    type="submit"
                    [disabled]="otpForm.invalid || isVerifying || isExpired()"
                    class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs rounded-xl transition-colors cursor-pointer disabled:opacity-50 flex items-center gap-1.5 shadow-sm"
                  >
                    @if (isVerifying) { <span>⏳</span> } @else { <span>Confirm OTP ✓</span> }
                  </button>
                </div>
              </div>
            </form>
          }
        </div>
      </div>
    }
  `
})
export class OtpVerificationModalComponent implements OnInit, OnChanges, OnDestroy {
  @Input() isOpen: boolean = false;
  @Input() mode: 'dual' | 'phone-only' = 'dual';
  @Input() currentStep: 1 | 2 = 1;
  @Input() emailAddress: string = '';
  @Input() phoneNumber: string = '';
  @Input() isVerifying: boolean = false;
  @Input() isResending: boolean = false;
  @Input() canClose: boolean = true;
  @Input() allowSkip: boolean = false;
  @Input() validityMinutes: number = 3;

  @Output() verifyEmail = new EventEmitter<string>();
  @Output() verifyOtp = new EventEmitter<string>();
  @Output() resendEmail = new EventEmitter<void>();
  @Output() resendSms = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();
  @Output() skipped = new EventEmitter<void>();

  // Dynamic Live Countdown Timer
  public readonly countdownSeconds = signal<number>(180);
  private timerInterval: any = null;

  public readonly formattedCountdown = computed<string>(() => {
    const total = Math.max(0, this.countdownSeconds());
    const mins = Math.floor(total / 60);
    const secs = total % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  });

  public readonly isExpired = computed<boolean>(() => this.countdownSeconds() <= 0);

  emailForm: FormGroup;
  otpForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.emailForm = this.fb.group({
      token: ['', [Validators.required]]
    });

    this.otpForm = this.fb.group({
      otpCode: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  ngOnInit(): void {
    if (this.isOpen && (this.mode === 'phone-only' || this.currentStep === 2)) {
      this.startTimer();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.otpForm.reset();
        this.startTimer();
      } else {
        this.stopTimer();
      }
    }
    if (changes['currentStep'] && this.currentStep === 2 && this.isOpen) {
      this.startTimer();
    }
    if (changes['validityMinutes'] && this.isOpen) {
      this.startTimer();
    }
  }

  ngOnDestroy(): void {
    this.stopTimer();
  }

  public startTimer(): void {
    this.stopTimer();
    const duration = (this.validityMinutes && this.validityMinutes > 0 ? this.validityMinutes : 3) * 60;
    this.countdownSeconds.set(duration);
    this.timerInterval = setInterval(() => {
      this.countdownSeconds.update(s => {
        if (s <= 1) {
          this.stopTimer();
          return 0;
        }
        return s - 1;
      });
    }, 1000);
  }

  public stopTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  public handleResendSms(): void {
    this.otpForm.reset();
    this.startTimer();
    this.resendSms.emit();
  }

  submitEmailToken(): void {
    if (this.emailForm.valid) {
      this.verifyEmail.emit(this.emailForm.value.token.trim());
    }
  }

  submitSmsOtp(): void {
    if (this.isExpired()) return;
    if (this.otpForm.valid) {
      this.verifyOtp.emit(this.otpForm.value.otpCode.trim());
    }
  }

  onCancel(): void {
    this.stopTimer();
    this.cancelled.emit();
  }

  onSkip(): void {
    this.stopTimer();
    this.skipped.emit();
  }
}
