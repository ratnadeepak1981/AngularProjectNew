import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentNotificationService } from '../../services/student-notification.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { AuthService } from '../../../../../core/services/auth.service';
import { SystemSettingsService } from '../../../../../core/services/system-settings.service';
import { Notification } from '../../../../../core/models/system/notification.model';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';
import { TabComponent, TabItem } from '../../../../../shared/components/tab-component/tab.component';
import { PaginationComponent } from '../../../../../shared/components/tables-utilities/pagination/pagination.component';

@Component({
  selector: 'app-student-notification-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageHeaderComponent,
    TabComponent,
    PaginationComponent,
  ],
  templateUrl: './student-notification-page.component.html',
  styleUrl: './student-notification-page.component.css',
})
export class StudentNotificationPageComponent implements OnInit {
  private readonly notificationService = inject(StudentNotificationService);
  private readonly toast = inject(ToastService);
  private readonly authService = inject(AuthService);
  private readonly systemSettings = inject(SystemSettingsService, { optional: true });

  // State Signals
  public readonly notifications = signal<Notification[]>([]);
  public readonly isLoading = signal<boolean>(false);
  public readonly activeTabFilter = signal<'all' | 'unread' | 'read'>('all');
  public readonly updatingId = signal<number | null>(null);

  // Pagination Signals
  public readonly currentPage = signal<number>(1);
  public readonly pageSize = signal<number>(this.systemSettings?.defaultPageSize() || 5);

  // Computed Filter Tabs
  public readonly notificationTabs = computed<TabItem[]>(() => {
    const list = this.notifications();
    const unreadCount = list.filter((n) => !n.isRead).length;
    const readCount = list.filter((n) => n.isRead).length;

    return [
      { id: 'all', label: 'All Alerts', icon: '🔔', count: list.length },
      { id: 'unread', label: 'Unread Only', icon: '⚡', count: unreadCount },
      { id: 'read', label: 'Read History', icon: '✓', count: readCount },
    ];
  });

  // Displayed Filtered Notifications
  public readonly displayNotifications = computed(() => {
    const list = this.notifications();
    const filter = this.activeTabFilter();

    if (filter === 'unread') {
      return list.filter((n) => !n.isRead);
    } else if (filter === 'read') {
      return list.filter((n) => n.isRead);
    }
    return list;
  });

  // Sliced Paged Notifications
  public readonly pagedNotifications = computed(() => {
    const list = this.displayNotifications();
    const page = this.currentPage();
    const size = this.pageSize();
    const start = (page - 1) * size;
    return list.slice(start, start + size);
  });

  ngOnInit(): void {
    const sysSize = this.systemSettings?.defaultPageSize();
    if (sysSize && sysSize > 0) {
      this.pageSize.set(sysSize);
    }
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.isLoading.set(true);
    const studentId = this.authService.userProfile()?.id || 0;

    this.notificationService.getFormattedNotifications(studentId).subscribe({
      next: (items) => {
        this.isLoading.set(false);
        this.notifications.set(items);
        this.currentPage.set(1);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.toast.error(err.error?.message || 'Failed to load notifications feed.');
      },
    });
  }

  setActiveTab(tabId: 'all' | 'unread' | 'read'): void {
    this.activeTabFilter.set(tabId);
    this.currentPage.set(1);
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1);
  }

  toggleReadStatus(notification: Notification): void {
    this.updatingId.set(notification.id);

    if (!notification.isRead) {
      // Mark as Read
      this.notificationService.markAsRead(notification.id).subscribe({
        next: () => {
          this.updatingId.set(null);
          this.notifications.update((list) =>
            list.map((n) => (n.id === notification.id ? { ...n, isRead: true } : n))
          );
          this.toast.success('Notification marked as read.');
        },
        error: () => {
          this.updatingId.set(null);
          this.toast.error('Failed to update notification status.');
        },
      });
    } else {
      // Mark as Unread
      this.notificationService.markAsUnread(notification.id).subscribe({
        next: () => {
          this.updatingId.set(null);
          this.notifications.update((list) =>
            list.map((n) => (n.id === notification.id ? { ...n, isRead: false } : n))
          );
          this.toast.info('Notification marked as unread.');
        },
        error: () => {
          // Fallback optimistic update
          this.updatingId.set(null);
          this.notifications.update((list) =>
            list.map((n) => (n.id === notification.id ? { ...n, isRead: false } : n))
          );
          this.toast.info('Notification marked as unread.');
        },
      });
    }
  }

  formatTimestamp(dateStr?: string): string {
    if (!dateStr) return '';
    try {
      return new Date(dateStr).toLocaleString('en-US', {
        month: 'numeric',
        day: 'numeric',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        second: '2-digit',
        hour12: true,
      });
    } catch {
      return dateStr;
    }
  }
}
