import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ComplaintManagementService } from '../services/complaint-management.service';
import { Complaint, ComplaintCategory } from '../../../../core/models/complaint/complaint.model';
import { ToastService } from '../../../../core/services/toast.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { TabComponent, TabItem } from '../../../../shared/components/tab-component/tab.component';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableColumn } from '../../../../shared/components/data-table/models/table-column.model';
import { SelectDropdownComponent } from '../../../../shared/components/select-dropdown/select-dropdown.component';
import { DropdownOption } from '../../../../core/models/common/dropdown-option.model';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { ConfirmModalComponent } from '../../../../shared/components/dialogs/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-complaint-management-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PageHeaderComponent,
    TabComponent,
    DataTableComponent,
    SelectDropdownComponent,
    ActionButtonComponent,
    ConfirmModalComponent,
  ],
  templateUrl: './complaint-management-page.component.html',
  styleUrl: './complaint-management-page.component.css',
})
export class ComplaintManagementPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly complaintService = inject(ComplaintManagementService);
  private readonly toast = inject(ToastService);

  public readonly activeTabId = signal<string>('tickets');
  public readonly isLoading = signal<boolean>(false);

  public readonly complaints = signal<Complaint[]>([]);
  public readonly categories = signal<ComplaintCategory[]>([]);

  // Resolve Ticket Modal Signals
  public readonly isResolveModalOpen = signal<boolean>(false);
  public readonly selectedComplaint = signal<Complaint | null>(null);
  public readonly isSubmittingResolve = signal<boolean>(false);

  // Category Modal Signals
  public readonly isCategoryModalOpen = signal<boolean>(false);
  public readonly isSubmittingCategory = signal<boolean>(false);

  // Confirm Modal Signals
  public readonly isConfirmOpen = signal<boolean>(false);
  public readonly confirmTitle = signal<string>('Confirm Action');
  public readonly confirmMessage = signal<string>('');
  public readonly confirmIcon = signal<string>('⚠️');
  public readonly confirmVariant = signal<'primary' | 'danger' | 'warning'>('primary');
  public readonly confirmButtonText = signal<string>('Proceed');
  public readonly confirmButtonIcon = signal<string>('✓');
  public pendingConfirmAction: (() => void) | null = null;

  // Reactive Forms
  public readonly resolveForm: FormGroup = this.fb.group({
    status: ['In Progress', [Validators.required]],
    resolutionNote: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]],
  });

  public readonly categoryForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
  });

  // Table Columns
  public readonly complaintColumns: TableColumn<Complaint>[] = [
    { key: 'studentName', header: 'Student Name / Index', sortable: true, filterable: true },
    { key: 'categoryName', header: 'Category', sortable: true, filterable: true },
    { key: 'description', header: 'Grievance Description', sortable: true, filterable: false },
    {
      key: 'status',
      header: 'Status',
      sortable: true,
      filterable: true,
      type: 'badge',
      badgeMap: {
        Pending: {
          label: 'PENDING TRIAGE',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-700',
        },
        'In Progress': {
          label: 'IN PROGRESS ⚡',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-sky-100 dark:bg-sky-950/60 text-sky-700 dark:text-sky-300 border border-sky-200 dark:border-sky-700',
        },
        Resolved: {
          label: 'RESOLVED ✓',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-700',
        },
        Rejected: {
          label: 'REJECTED',
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-rose-100 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-700',
        },
      },
    },
    { key: 'resolutionNote', header: 'Staff Resolution Remarks', sortable: false, filterable: false },
    { key: 'createdAt', header: 'Logged Date', sortable: true, filterable: false, type: 'date' },
    { key: 'actions', header: 'Actions', sortable: false, filterable: false, type: 'actions', align: 'right' },
  ];

  public readonly categoryColumns: TableColumn<ComplaintCategory>[] = [
    { key: 'name', header: 'Category Name', sortable: true, filterable: true },
    {
      key: 'isActive',
      header: 'Catalog Status',
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
          class: 'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border border-slate-300 dark:border-slate-700',
        },
      },
    },
    { key: 'actions', header: 'Actions', sortable: false, filterable: false, type: 'actions', align: 'right' },
  ];

  // All possible status options (filtered dynamically based on current complaint status)
  private readonly allStatusOptions: DropdownOption[] = [
    {
      value: 'In Progress',
      label: 'In Progress',
      icon: '⚡',
      badgeClass: 'bg-sky-100 dark:bg-sky-950/80 text-sky-700 dark:text-sky-300 border border-sky-200 dark:border-sky-700',
    },
    {
      value: 'Resolved',
      label: 'Resolved',
      icon: '✓',
      badgeClass: 'bg-emerald-100 dark:bg-emerald-950/80 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-700',
    },
    {
      value: 'Rejected',
      label: 'Rejected',
      icon: '✕',
      badgeClass: 'bg-rose-100 dark:bg-rose-950/80 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-700',
    },
  ];

  // BRD Status Transition: Only show valid next statuses for the selected complaint
  public readonly statusOptions = computed<DropdownOption[]>(() => {
    const current = this.selectedComplaint()?.status;
    if (current === 'Pending') {
      return this.allStatusOptions.filter(o => o.value === 'In Progress' || o.value === 'Rejected');
    }
    if (current === 'In Progress') {
      return this.allStatusOptions.filter(o => o.value === 'Resolved' || o.value === 'Rejected');
    }
    return [];
  });

  public readonly tabs = computed<TabItem[]>(() => [
    { id: 'tickets', label: 'Grievance Triage Tickets', icon: '🚨', count: this.complaints().length },
    { id: 'categories', label: 'Complaint Categories Catalog', icon: '📁', count: this.categories().length },
  ]);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);

    this.complaintService.getComplaints().subscribe({
      next: (data) => this.complaints.set(data),
      error: () => this.toast.error('Failed to load complaint tickets.'),
    });

    this.complaintService.getCategories().subscribe({
      next: (cats) => {
        this.categories.set(cats);
        this.isLoading.set(false);
      },
      error: () => {
        this.toast.error('Failed to load complaint categories.');
        this.isLoading.set(false);
      },
    });
  }

  onTabChange(tabId: string): void {
    this.activeTabId.set(tabId);
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

  openResolveModal(ticket: Complaint): void {
    this.selectedComplaint.set(ticket);
    const initialStatus = ticket.status === 'Pending' ? 'In Progress' : ticket.status;
    this.resolveForm.patchValue({
      status: initialStatus,
      resolutionNote: ticket.resolutionNote || '',
    });
    this.isResolveModalOpen.set(true);
  }

  closeResolveModal(): void {
    this.isResolveModalOpen.set(false);
    this.selectedComplaint.set(null);
  }

  onSelectResolveStatus(val: string | number): void {
    this.resolveForm.patchValue({ status: val });
  }

  submitResolveTicket(): void {
    if (this.resolveForm.invalid || !this.selectedComplaint()) {
      this.resolveForm.markAllAsTouched();
      return;
    }

    const ticket = this.selectedComplaint()!;
    const formVal = this.resolveForm.value;

    this.triggerConfirm({
      title: 'Update Complaint Status & Notes',
      message: `Are you sure you want to update ticket status to '${formVal.status}' for ${ticket.studentName || 'this student'}?`,
      icon: '✏️',
      variant: formVal.status === 'Rejected' ? 'danger' : 'primary',
      buttonText: 'Save Resolution & Notify Student',
      buttonIcon: '✓',
      action: () => {
        this.isSubmittingResolve.set(true);
        this.complaintService
          .updateComplaintStatus(ticket.id, {
            status: formVal.status,
            resolutionNote: formVal.resolutionNote.trim(),
          })
          .subscribe({
            next: () => {
              this.toast.success('Complaint ticket updated and student notified!');
              this.isSubmittingResolve.set(false);
              this.closeResolveModal();
              this.loadData();
            },
            error: (err) => {
              this.toast.error(err?.error?.message || 'Failed to update ticket.');
              this.isSubmittingResolve.set(false);
            },
          });
      },
    });
  }

  openCreateCategoryModal(): void {
    this.categoryForm.reset();
    this.isCategoryModalOpen.set(true);
  }

  closeCategoryModal(): void {
    this.isCategoryModalOpen.set(false);
  }

  submitCreateCategory(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.isSubmittingCategory.set(true);
    const name = this.categoryForm.value.name.trim();

    this.complaintService.createCategory(name).subscribe({
      next: () => {
        this.toast.success('Complaint category created successfully.');
        this.isSubmittingCategory.set(false);
        this.closeCategoryModal();
        this.loadData();
      },
      error: (err) => {
        this.toast.error(err?.error?.message || 'Failed to create category.');
        this.isSubmittingCategory.set(false);
      },
    });
  }

  promptDeactivateCategory(cat: ComplaintCategory): void {
    this.triggerConfirm({
      title: 'Deactivate Complaint Category',
      message: `Are you sure you want to deactivate category '${cat.name}'?`,
      icon: '🗑️',
      variant: 'danger',
      buttonText: 'Deactivate Category',
      buttonIcon: '🗑️',
      action: () => {
        this.complaintService.deleteCategory(cat.id).subscribe({
          next: () => {
            this.toast.info('Complaint category deactivated.');
            this.loadData();
          },
          error: (err) => this.toast.error(err?.error?.message || 'Failed to deactivate category.'),
        });
      },
    });
  }

  promptReactivateCategory(cat: ComplaintCategory): void {
    this.triggerConfirm({
      title: 'Reactivate Complaint Category',
      message: `Are you sure you want to reactivate '${cat.name}'? Students will immediately be able to select it when lodging complaints.`,
      icon: '🔄',
      variant: 'primary',
      buttonText: 'Reactivate Category',
      buttonIcon: '✓',
      action: () => {
        this.complaintService.updateCategory(cat.id, { name: cat.name, isActive: true }).subscribe({
          next: () => {
            this.toast.success(`Complaint category '${cat.name}' reactivated successfully.`);
            this.loadData();
          },
          error: (err) => this.toast.error(err?.error?.message || 'Failed to reactivate category.'),
        });
      },
    });
  }
}
