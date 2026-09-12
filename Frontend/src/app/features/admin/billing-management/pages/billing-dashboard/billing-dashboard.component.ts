import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AdminBillingService, FeePaymentItem, FeeTypeItem } from '../../services/admin-billing';
import { SystemSettingsService } from '../../../../../core/services/system-settings.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { TabComponent, TabItem } from '../../../../../shared/components/tab-component/tab.component';
import { ConfirmModalComponent } from '../../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';
import { AlertModalComponent } from '../../../../../shared/components/dialogs/alert-modal/alert-modal.component';
import { FeeAssignmentsListComponent } from '../../components/fee-assignments-list/fee-assignments-list.component';
import { FeeTypesListComponent } from '../../components/fee-types-list/fee-types-list.component';
import { AssignFeeModalComponent } from '../../components/assign-fee-modal/assign-fee-modal.component';
import { CreateFeeTypeModalComponent } from '../../components/create-fee-type-modal/create-fee-type-modal.component';
import { ActionButtonComponent } from '../../../../../shared/components/action-button/action-button.component';

@Component({
  selector: 'app-billing-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    PageHeaderComponent,
    TabComponent,
    FeeAssignmentsListComponent,
    FeeTypesListComponent,
    AssignFeeModalComponent,
    CreateFeeTypeModalComponent,
    ConfirmModalComponent,
    AlertModalComponent,
    ActionButtonComponent,
  ],
  templateUrl: './billing-dashboard.component.html',
  styleUrl: './billing-dashboard.component.css',
})
export class BillingDashboardComponent implements OnInit {
  private readonly billingService = inject(AdminBillingService);
  private readonly systemSettings = inject(SystemSettingsService);
  private readonly toast = inject(ToastService);

  // Active Tab State
  public readonly activeTabId = signal<string>('ledger');

  // Tab Configuration
  public readonly billingTabs = signal<TabItem[]>([
    { id: 'ledger', label: 'System Fee & Fine Payment Assignments', icon: '💳', count: 0 },
    { id: 'feetypes', label: 'Fee Types Directory', icon: '🏷️', count: 0 },
  ]);

  // Master Data Signals
  public readonly ledgerItems = signal<FeePaymentItem[]>([]);
  public readonly feeTypes = signal<FeeTypeItem[]>([
    { id: 1, name: 'Tuition Fee', isActive: true },
    { id: 2, name: 'Lab Fine / Equipment Fee', isActive: true },
    { id: 3, name: 'Hostel Accommodation Fee', isActive: true },
    { id: 4, name: 'Library Fine & Late Return', isActive: true },
    { id: 5, name: 'Student Identity Card Renewal Fee', isActive: true },
  ]);
  public readonly students = signal<any[]>([]);
  public readonly faculties = signal<any[]>([]);

  // Pagination Signals for Ledger
  public readonly ledgerCurrentPage = signal<number>(1);
  public readonly ledgerPageSize = signal<number>(5);
  public readonly ledgerTotalRecords = signal<number>(0);

  public readonly isLoadingLedger = signal<boolean>(false);
  public readonly isLoadingFeeTypes = signal<boolean>(false);

  // Modal Dialog States
  public readonly isAssignModalOpen = signal<boolean>(false);
  public readonly isCreateFeeTypeModalOpen = signal<boolean>(false);

  // Reusable Dialog Signals (Confirm & Alert)
  public readonly isConfirmOpen = signal<boolean>(false);
  public readonly confirmTitle = signal<string>('Confirm Action');
  public readonly confirmMessage = signal<string>('');
  public readonly confirmIcon = signal<string>('⚠️');
  public readonly confirmVariant = signal<'danger' | 'warning' | 'primary'>('primary');
  public readonly confirmButtonText = signal<string>('Proceed');
  public readonly confirmButtonIcon = signal<string>('✓');
  private pendingConfirmAction: (() => void) | null = null;

  public readonly isAlertOpen = signal<boolean>(false);
  public readonly alertTitle = signal<string>('Notice');
  public readonly alertMessage = signal<string>('');
  public readonly alertIcon = signal<string>('⚠️');
  public readonly alertVariant = signal<'danger' | 'warning' | 'info' | 'success'>('warning');
  public readonly alertButtonText = signal<string>('Understood');

  ngOnInit(): void {
    const sysSize = this.systemSettings.defaultPageSize();
    if (sysSize && sysSize > 0) {
      this.ledgerPageSize.set(sysSize);
    }
    this.loadAllData();
  }

  loadAllData(): void {
    this.loadLedger();
    this.loadFeeTypes();
    this.loadStudents();
    this.loadFaculties();
  }

  loadLedger(page = this.ledgerCurrentPage(), size = this.ledgerPageSize()): void {
    this.isLoadingLedger.set(true);
    this.billingService.getFormattedFeeLedger(page, size).subscribe({
      next: (res) => {
        this.ledgerItems.set(res.items);
        this.ledgerTotalRecords.set(res.totalRecords);
        this.updateTabCounts();
        this.isLoadingLedger.set(false);
      },
      error: () => {
        this.toast.error('Failed to load fee payment ledger.');
        this.isLoadingLedger.set(false);
      },
    });
  }

  onLedgerPageChange(newPage: number): void {
    this.ledgerCurrentPage.set(newPage);
    this.loadLedger(newPage, this.ledgerPageSize());
  }

  onLedgerPageSizeChange(newSize: number): void {
    this.ledgerPageSize.set(newSize);
    this.ledgerCurrentPage.set(1);
    this.loadLedger(1, newSize);
  }

  loadFeeTypes(): void {
    this.isLoadingFeeTypes.set(true);
    this.billingService.getFormattedFeeTypes().subscribe({
      next: (list) => {
        this.feeTypes.set(list);
        this.updateTabCounts();
        this.isLoadingFeeTypes.set(false);
      },
      error: () => {
        this.updateTabCounts();
        this.isLoadingFeeTypes.set(false);
      },
    });
  }

  loadStudents(): void {
    this.billingService.getStudentsDirectory().subscribe({
      next: (res) => {
        const payload = res?.data || res || [];
        const items: any[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);
        this.students.set(items);
      },
      error: () => {
        this.students.set([]);
      },
    });
  }

  loadFaculties(): void {
    this.billingService.getFaculties().subscribe({
      next: (res) => {
        const payload = res?.data || res || [];
        const items: any[] = Array.isArray(payload) ? payload : (payload.items || payload.Items || []);
        this.faculties.set(items);
      },
      error: () => {
        this.faculties.set([]);
      },
    });
  }

  updateTabCounts(): void {
    this.billingTabs.set([
      { id: 'ledger', label: 'System Fee & Fine Payment Assignments', icon: '💳', count: this.ledgerTotalRecords() || this.ledgerItems().length },
      { id: 'feetypes', label: 'Fee Types Directory', icon: '🏷️', count: this.feeTypes().length },
    ]);
  }

  setActiveTab(tabId: string): void {
    this.activeTabId.set(tabId);
    if (tabId === 'ledger') {
      this.loadLedger();
    } else if (tabId === 'feetypes') {
      this.loadFeeTypes();
    }
  }

  // Cancel Unpaid Fee Workflow [BRD Rule 9]
  onCancelUnpaidFee(item: FeePaymentItem): void {
    if (item.status?.toUpperCase() === 'PAID') {
      this.showAlert(
        'Immutable Financial Clearing [BRD Rule 9]',
        'Settled & cleared invoices are immutable and cannot be cancelled or deleted.',
        '🔒',
        'danger'
      );
      return;
    }

    const sName = item.studentName || (item as any).StudentName || 'Student';
    this.confirmTitle.set('Cancel Unpaid Fee Assignment');
    this.confirmMessage.set(
      `Are you sure you want to cancel the unpaid fee assignment for ${sName} ($${(item.amount || 0).toFixed(2)})?`
    );
    this.confirmIcon.set('🗑️');
    this.confirmVariant.set('danger');
    this.confirmButtonText.set('Cancel Assignment');
    this.confirmButtonIcon.set('🗑️');

    this.pendingConfirmAction = () => {
      this.billingService.cancelUnpaidFee(item.id).subscribe({
        next: () => {
          this.toast.success('Unpaid fee assignment cancelled successfully.');
          this.loadLedger();
        },
        error: (err: any) => {
          this.toast.error(err?.error?.message || 'Failed to cancel fee assignment.');
        },
      });
    };
    this.isConfirmOpen.set(true);
  }

  // Toggle Fee Type Active / Deactive Workflow
  onToggleFeeTypeStatus(item: FeeTypeItem): void {
    const isActivating = !item.isActive;
    const actionLabel = isActivating ? 'Reactivate' : 'Deactivate';
    
    this.confirmTitle.set(`${actionLabel} Fee Type`);
    this.confirmMessage.set(
      `Are you sure you want to ${actionLabel.toLowerCase()} fee type "${item.name}"?`
    );
    this.confirmIcon.set(isActivating ? '🔄' : '🏷️');
    this.confirmVariant.set(isActivating ? 'primary' : 'danger');
    this.confirmButtonText.set(actionLabel);
    this.confirmButtonIcon.set(isActivating ? '🔄' : '🗑️');

    this.pendingConfirmAction = () => {
      this.billingService.toggleFeeTypeStatus(item.id).subscribe({
        next: () => {
          this.toast.success(`Fee type "${item.name}" ${isActivating ? 'reactivated' : 'deactivated'} successfully.`);
          this.loadFeeTypes();
        },
        error: (err: any) => {
          this.toast.error(err?.error?.message || `Failed to ${actionLabel.toLowerCase()} fee type.`);
        },
      });
    };
    this.isConfirmOpen.set(true);
  }

  // Deactivate Fee Type Workflow
  onDeactivateFeeType(item: FeeTypeItem): void {
    this.onToggleFeeTypeStatus(item);
  }

  // Trigger Confirmation Listener from Modals
  onTriggerConfirm(event: { title: string; message: string; icon: string; variant: 'primary' | 'danger' | 'warning'; action: () => void }): void {
    this.confirmTitle.set(event.title);
    this.confirmMessage.set(event.message);
    this.confirmIcon.set(event.icon);
    this.confirmVariant.set(event.variant);
    this.confirmButtonText.set('Confirm');
    this.confirmButtonIcon.set('✓');
    this.pendingConfirmAction = event.action;
    this.isConfirmOpen.set(true);
  }

  // Trigger Alert Listener from Modals
  onTriggerAlert(event: { title: string; message: string; icon: string; variant: 'danger' | 'warning' | 'info' | 'success' }): void {
    this.showAlert(event.title, event.message, event.icon, event.variant);
  }

  onConfirmAction(): void {
    this.isConfirmOpen.set(false);
    if (this.pendingConfirmAction) {
      this.pendingConfirmAction();
      this.pendingConfirmAction = null;
    }
  }

  onCancelConfirm(): void {
    this.isConfirmOpen.set(false);
    this.pendingConfirmAction = null;
  }

  showAlert(title: string, message: string, icon = '⚠️', variant: 'danger' | 'warning' | 'info' | 'success' = 'warning'): void {
    this.alertTitle.set(title);
    this.alertMessage.set(message);
    this.alertIcon.set(icon);
    this.alertVariant.set(variant);
    this.isAlertOpen.set(true);
  }

  closeAlert(): void {
    this.isAlertOpen.set(false);
  }
}
