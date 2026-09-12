import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminDashboardService } from '../services/admin-dashboard.service';
import { AdminDashboardMetricsSummary } from '../../../../core/models/dashboard/admin-dashboard-metrics.model';
import { ToastService } from '../../../../core/services/toast.service';
import { DashboardCardComponent } from '../../../../shared/components/cards/dashboard-card/dashboard-card.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ActionButtonComponent } from '../../../../shared/components/action-button/action-button.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-admin-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    DashboardCardComponent,
    PageHeaderComponent,
    ActionButtonComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './admin-dashboard-page.component.html',
  styleUrl: './admin-dashboard-page.component.css',
})
export class AdminDashboardPageComponent implements OnInit {
  private readonly dashboardService = inject(AdminDashboardService);
  private readonly toast = inject(ToastService);

  // Critical Queue Signals
  public readonly pendingHostels = signal<number>(0);
  public readonly pendingComplaints = signal<number>(0);
  public readonly pendingCertificates = signal<number>(0);
  public readonly totalStudents = signal<number>(0);
  public readonly unreadSecurityAlerts = signal<number>(0);

  // Financial & Facility Signals
  public readonly pendingFeesCount = signal<number>(0);
  public readonly pendingFeesAmount = signal<number>(0);
  public readonly totalPaidFeesAmount = signal<number>(0);
  public readonly totalLabs = signal<number>(0);
  public readonly totalFaculties = signal<number>(0);
  public readonly isLoadingMetrics = signal<boolean>(true);

  // Computed Critical Triage Stats
  public readonly totalCriticalItems = computed(() => {
    return (
      this.pendingHostels() +
      this.pendingComplaints() +
      this.pendingCertificates() +
      this.pendingFeesCount() +
      this.unreadSecurityAlerts()
    );
  });

  public readonly defaultCurrency = signal<string>('LKR');

  public readonly feeCollectionRate = computed(() => {
    const total = this.pendingFeesAmount() + this.totalPaidFeesAmount();
    if (total === 0) return 100;
    return Math.round((this.totalPaidFeesAmount() / total) * 100);
  });

  public readonly formattedPendingFeesAmount = computed(() => {
    return `${this.defaultCurrency()} ${this.pendingFeesAmount().toLocaleString()}`;
  });

  // SVG Donut Chart Calculated Offsets (Circumference = 2 * PI * 40 = 251.32)
  public readonly donutCircumference = 251.32;

  public readonly donutSegments = computed(() => {
    const total = this.totalCriticalItems();
    if (total === 0) {
      return {
        fees: { dash: '0 251.32', offset: 0, percent: 0 },
        hostels: { dash: '0 251.32', offset: 0, percent: 0 },
        complaints: { dash: '0 251.32', offset: 0, percent: 0 },
        certs: { dash: '0 251.32', offset: 0, percent: 0 },
      };
    }

    const feesPct = this.pendingFeesCount() / total;
    const hostelsPct = this.pendingHostels() / total;
    const complaintsPct = this.pendingComplaints() / total;
    const certsPct = this.pendingCertificates() / total;

    const feesDash = feesPct * this.donutCircumference;
    const hostelsDash = hostelsPct * this.donutCircumference;
    const complaintsDash = complaintsPct * this.donutCircumference;
    const certsDash = certsPct * this.donutCircumference;

    let currentOffset = 0;
    const feesOffset = currentOffset;
    currentOffset -= feesDash;

    const hostelsOffset = currentOffset;
    currentOffset -= hostelsDash;

    const complaintsOffset = currentOffset;
    currentOffset -= complaintsDash;

    const certsOffset = currentOffset;

    return {
      fees: { dash: `${feesDash} ${this.donutCircumference}`, offset: feesOffset, percent: Math.round(feesPct * 100) },
      hostels: { dash: `${hostelsDash} ${this.donutCircumference}`, offset: hostelsOffset, percent: Math.round(hostelsPct * 100) },
      complaints: { dash: `${complaintsDash} ${this.donutCircumference}`, offset: complaintsOffset, percent: Math.round(complaintsPct * 100) },
      certs: { dash: `${certsDash} ${this.donutCircumference}`, offset: certsOffset, percent: Math.round(certsPct * 100) },
    };
  });

  ngOnInit(): void {
    this.loadAnalytics();
    this.dashboardService.getAllSystemSettings().subscribe({
      next: (dict: Record<string, string>) => {
        if (dict['DefaultCurrency']) {
          this.defaultCurrency.set(dict['DefaultCurrency']);
        }
      },
      error: () => {},
    });
  }

  loadAnalytics(): void {
    this.isLoadingMetrics.set(true);
    this.dashboardService.getAdminDashboardMetrics().subscribe({
      next: (summary: AdminDashboardMetricsSummary) => {
        this.pendingHostels.set(summary.pendingHostels);
        this.pendingComplaints.set(summary.pendingComplaints);
        this.pendingCertificates.set(summary.pendingCertificates);
        this.totalStudents.set(summary.totalStudents);
        this.pendingFeesCount.set(summary.pendingFeesCount);
        this.pendingFeesAmount.set(summary.pendingFeesAmount);
        this.totalPaidFeesAmount.set(summary.totalPaidFeesAmount);
        this.totalLabs.set(summary.totalLabs);
        this.totalFaculties.set(summary.totalFaculties);
        this.unreadSecurityAlerts.set(summary.unreadSecurityAlerts || 0);
        this.isLoadingMetrics.set(false);
      },
      error: () => {
        this.isLoadingMetrics.set(false);
        this.toast.error('Failed to reload administrative live metrics.');
      },
    });
  }
}
