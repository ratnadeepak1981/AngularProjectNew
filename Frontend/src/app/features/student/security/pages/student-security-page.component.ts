import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ToastContainerComponent } from '../../../../shared/components/toast-container/toast-container.component';
import { PasswordChangeComponent } from '../../../../shared/components/password-change/password-change.component';

@Component({
  selector: 'app-student-security-page',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, ToastContainerComponent, PasswordChangeComponent],
  templateUrl: './student-security-page.component.html',
})
export class StudentSecurityPageComponent {}
