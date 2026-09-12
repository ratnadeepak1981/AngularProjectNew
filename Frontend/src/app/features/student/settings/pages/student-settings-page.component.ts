import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StudentSettingsService } from '../services/student-settings.service';

@Component({
  selector: 'app-student-settings-page',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent],
  templateUrl: './student-settings-page.component.html',
})
export class StudentSettingsPageComponent {
  public readonly settingsService = inject(StudentSettingsService);

  public readonly themeService = this.settingsService.themeService;
  public readonly fontSize = this.settingsService.fontSize;
  public readonly headerGradient = this.settingsService.headerGradient;
  public readonly themeOptions = this.settingsService.themeOptions;
  public readonly fontSizes = this.settingsService.fontSizes;
  public readonly headerGradients = this.settingsService.headerGradients;

  public selectTheme(themeId: string): void {
    this.settingsService.selectTheme(themeId);
  }

  public toggleDarkMode(): void {
    this.settingsService.toggleDarkMode();
  }

  public setFontSize(size: string): void {
    this.settingsService.setFontSize(size);
  }

  public setHeaderGradient(gradientId: string): void {
    this.settingsService.setHeaderGradient(gradientId);
  }
}
