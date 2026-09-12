import { Injectable, inject, signal } from '@angular/core';
import { ThemeOption } from '../models/system/theme-option.model';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  public static readonly THEMES: ThemeOption[] = [
    { id: 'oxford-navy', name: 'Oxford Royal Navy', icon: '🏛️' },
    { id: 'cambridge-emerald', name: 'Cambridge Emerald', icon: '🌲' },
    { id: 'harvard-crimson', name: 'Harvard Crimson', icon: '📜' },
    { id: 'mit-cyber-dark', name: 'MIT Tech Cyber', icon: '⚡' },
    { id: 'stanford-cardinal', name: 'Stanford Cardinal', icon: '☀️' },
  ];

  private readonly apiService = inject(ApiService, { optional: true });
  private readonly themeStorageKey = 'userPortalTheme';
  private readonly modeStorageKey = 'userPortalMode';

  public readonly currentTheme = signal<string>(this.getInitialTheme());
  public readonly isDarkMode = signal<boolean>(this.getInitialMode());

  constructor() {
    this.applyTheme(this.currentTheme(), this.isDarkMode());
    this.syncFromBackend();
  }

  private syncFromBackend(): void {
    if (!this.apiService) return;
    this.apiService.get<any>(this.apiService.routes.system.allSettings).subscribe({
      next: (res) => {
        const dict = res?.data || res || {};
        const backendTheme = dict['ThemeColor'];
        if (backendTheme && ThemeService.THEMES.some((t) => t.id === backendTheme)) {
          const userHasExplicitChoice = localStorage.getItem(this.themeStorageKey);
          if (!userHasExplicitChoice) {
            this.setTheme(backendTheme);
          }
        }
      },
      error: () => {
        // Fallback gracefully to local configuration
      },
    });
  }

  private getInitialTheme(): string {
    const saved = localStorage.getItem(this.themeStorageKey);
    return saved || 'oxford-navy';
  }

  private getInitialMode(): boolean {
    const saved = localStorage.getItem(this.modeStorageKey);
    if (saved !== null) {
      return saved === 'dark';
    }
    const savedTheme = localStorage.getItem(this.themeStorageKey);
    if (savedTheme === 'mit-cyber-dark') return true;
    return false;
  }

  public setTheme(themeId: string): void {
    this.currentTheme.set(themeId);
    localStorage.setItem(this.themeStorageKey, themeId);
    this.applyTheme(themeId, this.isDarkMode());
  }

  public toggleDarkMode(): void {
    const nextMode = !this.isDarkMode();
    this.isDarkMode.set(nextMode);
    localStorage.setItem(this.modeStorageKey, nextMode ? 'dark' : 'light');
    this.applyTheme(this.currentTheme(), nextMode);
  }

  public setDarkMode(isDark: boolean): void {
    this.isDarkMode.set(isDark);
    localStorage.setItem(this.modeStorageKey, isDark ? 'dark' : 'light');
    this.applyTheme(this.currentTheme(), isDark);
  }

  private applyTheme(themeId: string, isDark: boolean): void {
    if (typeof document !== 'undefined') {
      const root = document.documentElement;
      root.setAttribute('data-theme', themeId);
      root.setAttribute('data-mode', isDark ? 'dark' : 'light');

      if (isDark) {
        root.classList.add('dark');
      } else {
        root.classList.remove('dark');
      }
    }
  }
}
