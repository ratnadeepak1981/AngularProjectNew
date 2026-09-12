import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../../core/services/api.service';
import { ToastService } from '../../../../core/services/toast.service';
import { AdminUser } from '../../../../core/models/admin-management/admin-user.model';
import { ApiResponse } from '../../../../core/models/common/api-response.model';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../shared/components/data-table/models/table-column.model';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-admin-management-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PageHeaderComponent,
    DataTableComponent,
    ActionButtonComponent,
    ConfirmModalComponent,
  ],
  templateUrl: './admin-management-page.component.html',
  styleUrl: './admin-management-page.component.css',
})
export class AdminManagementPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly apiService = inject(ApiService);
  private readonly toast = inject(ToastService);

  public readonly isLoading = signal<boolean>(false);
  public readonly admins = signal<AdminUser[]>([]);

  // Create Admin Modal Signals
  public readonly isCreateModalOpen = signal<boolean>(false);
  public readonly isSubmittingCreate = signal<boolean>(false);
  public readonly showPassword = signal<boolean>(false);

  // Confirm Action Modal Signals
  public readonly isConfirmOpen = signal<boolean>(false);
  public readonly confirmTitle = signal<string>('Confirm Action');
  public readonly confirmMessage = signal<string>('');
  public readonly confirmIcon = signal<string>('⚠️');
  public readonly confirmVariant = signal<'primary' | 'danger' | 'warning'>('primary');
  public readonly confirmButtonText = signal<string>('Proceed');
  public readonly confirmButtonIcon = signal<string>('✓');
  public pendingConfirmAction: (() => void) | null = null;

  // Reactive Form
  public readonly createAdminForm: FormGroup = this.fb.group({
    fullName: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  // Table Columns (Displays only Admin users)
  public readonly adminColumns: TableColumn<AdminUser>[] = [
    { key: 'fullName', header: 'Admin Full Name', sortable: true, filterable: true },
    { key: 'email', header: 'Admin Email Address', sortable: true, filterable: true },
    {
      key: 'role',
      header: 'Role',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        Admin: {
          label: 'ADMINISTRATOR',
          class: 'inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold theme-badge-role shadow-2xs',
        },
      },
    },
    {
      key: 'isActive',
      header: 'Account Status',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        true: {
          label: 'ACTIVE',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-700',
        },
        false: {
          label: 'DEACTIVATED',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-rose-100 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-700',
        },
      },
    },
    { key: 'createdAt', header: 'Created Date', sortable: true, filterable: false, type: 'date' },
    { key: 'lastPasswordChangedAt', header: 'Last Password Change', sortable: true, filterable: false, type: 'date' },
    { key: 'actions', header: 'Actions', sortable: false, filterable: false, type: 'actions', align: 'right' },
  ];

  ngOnInit(): void {
    this.loadAdmins();
  }

  loadAdmins(): void {
    this.isLoading.set(true);
    this.apiService.get<ApiResponse<AdminUser[]>>(this.apiService.routes.adminManagement.list).subscribe({
      next: (res) => {
        const data = res.data || (res as any);
        if (Array.isArray(data)) {
          this.admins.set(data);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to load Admin user accounts.');
        this.isLoading.set(false);
      },
    });
  }

  toggleShowPassword(): void {
    this.showPassword.update((v) => !v);
  }

  openCreateModal(): void {
    this.createAdminForm.reset();
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
  }

  submitCreateAdmin(): void {
    if (this.createAdminForm.invalid) {
      this.createAdminForm.markAllAsTouched();
      return;
    }

    const payload = this.createAdminForm.value;
    this.isSubmittingCreate.set(true);

    this.apiService.post<ApiResponse<AdminUser>>(this.apiService.routes.adminManagement.create, payload).subscribe({
      next: () => {
        this.toast.success(`Admin account '${payload.email}' created successfully!`);
        this.isSubmittingCreate.set(false);
        this.closeCreateModal();
        this.loadAdmins();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to create Admin user account.');
        this.isSubmittingCreate.set(false);
      },
    });
  }

  triggerConfirm(opts: {
    title: string;
    message: string;
    icon: string;
    variant: 'primary' | 'danger' | 'warning';
    buttonText: string;
    buttonIcon: string;
    action: () => void;
  }): void {
    this.confirmTitle.set(opts.title);
    this.confirmMessage.set(opts.message);
    this.confirmIcon.set(opts.icon);
    this.confirmVariant.set(opts.variant);
    this.confirmButtonText.set(opts.buttonText);
    this.confirmButtonIcon.set(opts.buttonIcon);
    this.pendingConfirmAction = opts.action;
    this.isConfirmOpen.set(true);
  }

  onConfirmAction(): void {
    if (this.pendingConfirmAction) {
      this.pendingConfirmAction();
    }
    this.isConfirmOpen.set(false);
  }

  onCancelConfirm(): void {
    this.isConfirmOpen.set(false);
    this.pendingConfirmAction = null;
  }

  promptToggleStatus(admin: AdminUser): void {
    const actionText = admin.isActive ? 'deactivate' : 'reactivate';
    this.triggerConfirm({
      title: `${admin.isActive ? 'Deactivate' : 'Reactivate'} Admin Account`,
      message: `Are you sure you want to ${actionText} Admin account profile '${admin.email}'?`,
      icon: admin.isActive ? '🛑' : '⚡',
      variant: admin.isActive ? 'warning' : 'primary',
      buttonText: `${admin.isActive ? 'Deactivate' : 'Reactivate'} Account`,
      buttonIcon: '✓',
      action: () => {
        this.apiService
          .put<ApiResponse<AdminUser>>(this.apiService.routes.adminManagement.toggleStatus(admin.id), {
            isActive: !admin.isActive,
          })
          .subscribe({
            next: () => {
              this.toast.success(`Admin account '${admin.email}' ${actionText}d successfully.`);
              this.loadAdmins();
            },
            error: (err) => this.toast.error(err?.error?.message || `Failed to ${actionText} Admin account.`),
          });
      },
    });
  }

  promptResetPassword(admin: AdminUser): void {
    this.triggerConfirm({
      title: 'Confirm Admin Password Reset',
      message: `Are you sure you want to initiate a password reset and unlock account access for '${admin.email}'? A password reset email will be dispatched directly to their address.`,
      icon: '🔑',
      variant: 'warning',
      buttonText: 'Dispatch Reset Link',
      buttonIcon: 'key',
      action: () => {
        this.apiService
          .post<ApiResponse<any>>(this.apiService.routes.adminManagement.resetPassword(admin.id), {})
          .subscribe({
            next: (res) => {
              this.toast.success(res?.message || `Password reset link successfully dispatched to '${admin.email}'.`);
              this.loadAdmins();
            },
            error: (err) => this.toast.error(err?.error?.message || 'Failed to reset Admin account password.'),
          });
      },
    });
  }
}
