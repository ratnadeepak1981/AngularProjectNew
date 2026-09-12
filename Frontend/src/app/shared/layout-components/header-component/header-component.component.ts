import { Component, OnInit, OnDestroy, inject, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ApiService } from '../../../core/services/api.service';
import { ThemeService } from '../../../core/services/theme.service';
import { ToastService } from '../../../core/services/toast.service';
import { ThemeOption } from '../../../core/models/system/theme-option.model';
import { PasswordChangeComponent } from '../../components/password-change/password-change.component';

@Component({
  selector: 'app-header-component',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PasswordChangeComponent],
  templateUrl: './header-component.component.html',
  styleUrl: './header-component.component.css',
})
export class HeaderComponentComponent implements OnInit, OnDestroy {
  public readonly authService = inject(AuthService);
  public readonly themeService = inject(ThemeService);
  private readonly apiService = inject(ApiService);
  private readonly toast = inject(ToastService);

  public readonly isPasswordModalOpen = signal<boolean>(false);
  public readonly isProfileMenuOpen = signal<boolean>(false);
  public readonly unreadAlertsCount = signal<number>(0);
  public readonly isConnected = signal<boolean>(true);
  public readonly themes: ThemeOption[] = ThemeService.THEMES;
  private healthCheckInterval: any = null;

  public readonly userInitials = computed(() => {
    const profile = this.authService.userProfile();
    let name = profile?.name?.trim();
    if (!name || name === 'Administrator Account' || name === 'Student Account') {
      if (profile?.email) {
        name = profile.email.split('@')[0].replace(/[._-]/g, ' ');
      } else {
        name = this.authService.isAdmin() ? 'Admin User' : 'Student User';
      }
    }
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2 && parts[0] && parts[1]) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  });

  public readonly displayName = computed(() => {
    const profile = this.authService.userProfile();
    if (profile?.name && profile.name !== 'Administrator Account') {
      return profile.name;
    }
    if (profile?.email) {
      return profile.email;
    }
    return this.authService.role() === 'SuperAdmin' ? 'Super Administrator' : this.authService.isAdmin() ? 'Administrator' : 'Student';
  });

  public readonly roleBadgeLabel = computed(() => {
    const role = this.authService.role();
    if (role === 'SuperAdmin') return 'Super Admin';
    if (role === 'Admin') return 'Administrator';
    return 'Student';
  });

  public toggleProfileMenu(event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.isProfileMenuOpen.update((open) => !open);
  }

  public closeProfileMenu(): void {
    this.isProfileMenuOpen.set(false);
  }

  @HostListener('document:click')
  public onDocumentClick(): void {
    if (this.isProfileMenuOpen()) {
      this.isProfileMenuOpen.set(false);
    }
  }

  ngOnInit(): void {
    this.checkBackendHealth();
    this.loadUnreadNotifications();
    this.healthCheckInterval = setInterval(() => {
      this.checkBackendHealth();
      this.loadUnreadNotifications();
    }, 15000);
  }

  ngOnDestroy(): void {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
    }
  }

  loadUnreadNotifications(): void {
    if (!this.authService.isAuthenticated()) return;

    if (this.authService.isAdmin()) {
      this.apiService.get<any>(this.apiService.routes.auditLogs.list, { isSuccess: false, isReviewed: false, pageSize: 1 }).subscribe({
        next: (res) => {
          const payload = res?.data || res || {};
          const count = payload.totalCount ?? payload.totalRecords ?? (Array.isArray(payload) ? payload.length : (payload.items?.length || 0));
          this.unreadAlertsCount.set(count);
        },
        error: () => this.unreadAlertsCount.set(0),
      });
    } else {
      const studentId = this.authService.userProfile()?.id || 0;
      if (studentId > 0) {
        this.apiService.get<any>(this.apiService.routes.notifications.studentFeed(studentId)).subscribe({
          next: (res) => {
            const payload = res?.data || res || [];
            const items: any[] = Array.isArray(payload) ? payload : (payload.items || []);
            const count = items.filter((n) => !n.isRead).length;
            this.unreadAlertsCount.set(count);
          },
          error: () => this.unreadAlertsCount.set(0),
        });
      }
    }
  }

  checkBackendHealth(): void {
    this.apiService.get(this.apiService.routes.labs.list).subscribe({
      next: () => this.isConnected.set(true),
      error: (err) => {
        if (err.status && err.status !== 0) {
          this.isConnected.set(true);
        } else {
          this.isConnected.set(false);
        }
      },
    });
  }

  onThemeChange(themeId: string): void {
    this.themeService.setTheme(themeId);
  }

  onToggleDarkMode(): void {
    this.themeService.toggleDarkMode();
  }

  openPasswordModal(): void {
    this.isPasswordModalOpen.set(true);
  }

  closePasswordModal(): void {
    this.isPasswordModalOpen.set(false);
  }

  onPasswordChangeSuccess(): void {
    this.toast.success('Your password has been changed successfully!');
    this.isPasswordModalOpen.set(false);
  }

  onLogout(): void {
    this.authService.logout();
  }
}
