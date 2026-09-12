import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { PasswordChangeComponent } from '../../../../shared/components/password-change/password-change.component';

@Component({
  selector: 'app-student-security-page',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, PasswordChangeComponent],
  templateUrl: './student-security-page.component.html',
})
export class StudentSecurityPageComponent {}
