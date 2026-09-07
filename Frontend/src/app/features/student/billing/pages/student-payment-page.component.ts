import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { StudentBillingService, FeePaymentItem } from '../services/student-billing.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { SystemSettingsService } from '../../../../core/services/system-settings.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { PaymentCheckoutComponent } from '../components/payment-checkout/payment-checkout.component';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { AlertModalComponent } from '../../../../shared/components/dialogs/alert-modal/alert-modal.component';

@Component({
  selector: 'app-student-payment-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    PageHeaderComponent,
    PaymentCheckoutComponent,
    ConfirmModalComponent,
    AlertModalComponent,
  ],
  templateUrl: './student-payment-page.component.html',
  styleUrl: './student-payment-page.component.css',
})
export class StudentPaymentPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly billingService = inject(StudentBillingService);
  private readonly settingsService = inject(SystemSettingsService);
  public readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);

  public readonly institutionName = signal<string>('University of Knowledge (UOK)');
  public readonly invoiceId = signal<number>(0);
  public readonly invoiceItem = signal<FeePaymentItem | null>(null);
  public readonly isLoading = signal<boolean>(false);
  public readonly isSubmitting = signal<boolean>(false);

  // Reusable Dialog Signals (Confirm & Alert)
  public readonly isConfirmOpen = signal<boolean>(false);
  public readonly confirmTitle = signal<string>('Confirm Clearing Payment');
  public readonly confirmMessage = signal<string>('');
  public readonly confirmIcon = signal<string>('🔒');
  public readonly confirmVariant = signal<'danger' | 'warning' | 'primary'>('primary');
  public readonly confirmButtonText = signal<string>('Authorize Payment');
  public readonly confirmButtonIcon = signal<string>('✓');
  private pendingPaymentPayload: { channel: string; details: any } | null = null;

  public readonly isAlertOpen = signal<boolean>(false);
  public readonly alertTitle = signal<string>('Payment Completed');
  public readonly alertMessage = signal<string>('');
  public readonly alertIcon = signal<string>('✓');
  public readonly alertVariant = signal<'danger' | 'warning' | 'info' | 'success'>('success');

  ngOnInit(): void {
    this.settingsService.getAllSettings().subscribe({
      next: (res) => {
        if (res?.data && res.data['InstitutionName']) {
          this.institutionName.set(res.data['InstitutionName']);
        }
      },
    });

    this.route.params.subscribe((p) => {
      const id = Number(p['id']);
      if (id && id > 0) {
        this.invoiceId.set(id);
        this.loadInvoiceDetails(id);
      } else {
        this.router.navigate(['/student/billing']);
      }
    });
  }

  loadInvoiceDetails(id: number): void {
    this.isLoading.set(true);
    this.billingService.getInvoiceDetails(id).subscribe({
      next: (found) => {
        if (found) {
          this.invoiceItem.set(found);
        } else {
          this.toast.error('Invoice details not found on your ledger.');
          this.router.navigate(['/student/billing']);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.toast.error('Failed to load invoice details.');
        this.isLoading.set(false);
      },
    });
  }

  onPaySubmit(event: { channel: string; details: any }): void {
    const item = this.invoiceItem();
    if (!item) return;

    const amt = Number(item.amount) || 0;
    const desc = item.feeTypeName || item.description || 'University Fee';

    this.confirmTitle.set('Confirm Financial Clearing Authorization');
    this.confirmMessage.set(
      `Are you sure you want to authorize simulated clearing payment of $${amt.toFixed(2)} for "${desc}"?`
    );
    this.confirmIcon.set('💳');
    this.confirmVariant.set('primary');
    this.confirmButtonText.set(`Confirm & Pay $${amt.toFixed(2)}`);
    this.confirmButtonIcon.set('🔒');

    this.pendingPaymentPayload = event;
    this.isConfirmOpen.set(true);
  }

  onConfirmPayment(): void {
    this.isConfirmOpen.set(false);
    const item = this.invoiceItem();
    if (!item || !this.pendingPaymentPayload) return;

    this.isSubmitting.set(true);
    const paymentPayload = {
      paymentChannel: this.pendingPaymentPayload.channel || 'card',
      ...(this.pendingPaymentPayload.details || {}),
    };
    this.billingService.payInvoice(item.id, paymentPayload).subscribe({
      next: (res) => {
        const recNo = res?.receiptNumber || res?.data?.receiptNumber || 'REC-' + Math.floor(100000 + Math.random() * 900000);
        this.isSubmitting.set(false);
        this.pendingPaymentPayload = null;

        this.alertTitle.set('Payment Clearance Successful!');
        this.alertMessage.set(
          `Your payment of $${(item.amount || 0).toFixed(2)} has been successfully cleared and posted. Official Bursar Receipt #${recNo} has been issued to your account.`
        );
        this.alertIcon.set('✓');
        this.alertVariant.set('success');
        this.isAlertOpen.set(true);
      },
      error: (err: any) => {
        this.toast.error(err?.error?.message || 'Payment clearing failed. Please try again.');
        this.isSubmitting.set(false);
        this.pendingPaymentPayload = null;
      },
    });
  }

  onCancelConfirm(): void {
    this.isConfirmOpen.set(false);
    this.pendingPaymentPayload = null;
  }

  onCloseAlert(): void {
    this.isAlertOpen.set(false);
    this.router.navigate(['/student/billing']);
  }

  backToLedger(): void {
    this.router.navigate(['/student/billing']);
  }
}
