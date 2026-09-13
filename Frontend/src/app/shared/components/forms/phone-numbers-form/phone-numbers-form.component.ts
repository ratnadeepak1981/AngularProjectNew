import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActionButtonComponent } from '../../action-button/action-button.component';

@Component({
  selector: 'app-phone-numbers-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ActionButtonComponent],
  template: `
    <div class="space-y-3 bg-slate-50/50 dark:bg-slate-800/30 p-4 sm:p-5 rounded-2xl border border-slate-200/80 dark:border-slate-800">
      <!-- Section Header with Add Button -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-2.5 border-b border-slate-200/70 dark:border-slate-800">
        <div>
          <h3 class="text-sm font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
            <span>📱</span>
            <span>Telephone & Mobile Contact Coordinates</span>
            <span class="text-xs font-normal text-slate-400 dark:text-slate-500">(Optional)</span>
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            Your Primary Mobile is used for 2-Factor SMS OTP authentication and is required before online fee payments.
          </p>
        </div>
        
        <div class="flex items-center gap-2 self-start sm:self-auto">
          @if (isPrimaryVerified) {
            <span class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-bold bg-emerald-100 dark:bg-emerald-950/80 text-emerald-700 dark:text-emerald-300 rounded-md border border-emerald-300">
              ✓ SMS OTP Verified
            </span>
          } @else {
            <span class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-bold bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 rounded-md border border-slate-300 dark:border-slate-700">
              📱 SMS OTP
            </span>
          }

          @if (!readonly) {
            <app-action-button
              size="sm"
              variant="outline"
              icon="➕"
              label="Add Phone"
              (btnClick)="addPhoneNumber()"
            ></app-action-button>
          }
        </div>
      </div>

      <!-- List of Phone Numbers -->
      <div class="space-y-2 pt-1">
        @for (ctrl of phoneNumbersArray.controls; track $index; let i = $index) {
          <div [formGroup]="$any(ctrl)" class="p-2.5 bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-700/80 flex flex-col sm:flex-row sm:items-center gap-2.5 shadow-2xs">
            <!-- Type Selector -->
            <div class="w-full sm:w-44 shrink-0">
              <select
                formControlName="phoneType"
                class="w-full h-10 px-3 py-2 text-sm rounded-lg border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/80 text-slate-800 dark:text-slate-100 font-semibold cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              >
                <option value="Primary Mobile">📱 Primary Mobile</option>
                <option value="Home Landline">☎️ Home Landline</option>
                <option value="Emergency Contact">🚨 Emergency Contact</option>
                <option value="Guardian Contact">👨‍👩‍👦 Guardian Contact</option>
              </select>
            </div>

            <!-- Phone Number Input -->
            <div class="flex-1 relative">
              <input
                type="tel"
                formControlName="phoneNumber"
                placeholder="e.g. +94 77 123 4567"
                class="w-full h-10 px-3.5 py-2 text-sm rounded-lg border bg-slate-50 dark:bg-slate-800/80 text-slate-900 dark:text-white font-mono placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                [ngClass]="{
                  'border-amber-400 dark:border-amber-500 focus:ring-amber-500 bg-amber-50/30 dark:bg-amber-950/20':
                    ctrl.get('phoneNumber')?.touched && ctrl.get('phoneNumber')?.invalid,
                  'border-slate-300 dark:border-slate-700':
                    !ctrl.get('phoneNumber')?.touched || !ctrl.get('phoneNumber')?.invalid
                }"
              />
            </div>

            <!-- Primary Tag / Remove Button -->
            <div class="flex items-center justify-end sm:justify-start gap-2 shrink-0">
              @if (i === 0) {
                <span class="text-xs font-bold px-3 py-1.5 rounded-lg bg-blue-100 dark:bg-blue-900/60 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800 whitespace-nowrap">
                  Primary Mobile
                </span>
              }
              @if (phoneNumbersArray.length > 1 && !readonly) {
                <button
                  type="button"
                  (click)="removePhoneNumber(i)"
                  class="h-10 px-3 text-rose-500 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/60 rounded-lg border border-transparent hover:border-rose-200 transition-all cursor-pointer text-sm font-bold flex items-center gap-1"
                  title="Remove this phone number"
                >
                  <span>✕</span>
                  <span class="sm:hidden text-xs">Remove</span>
                </button>
              }
            </div>
          </div>

          @if (ctrl.get('phoneNumber')?.touched && ctrl.get('phoneNumber')?.invalid) {
            <p class="text-xs text-amber-600 dark:text-amber-400 font-medium px-1 flex items-center gap-1">
              <span>⚠️</span>
              <span>Please enter a valid telephone number format (e.g. +94 77 123 4567).</span>
            </p>
          }
        }
      </div>

      <!-- Footnote reminder -->
      <div class="p-2.5 rounded-xl bg-slate-100/80 dark:bg-slate-800/70 border border-slate-200 dark:border-slate-700/70 flex items-center gap-2 text-xs text-slate-600 dark:text-slate-400">
        <span class="text-base">🛡️</span>
        <span class="leading-relaxed">
          <strong>Security Notice:</strong> The <strong>Primary Mobile Number</strong> is verified via 6-digit SMS OTP code for account security and critical notifications.
        </span>
      </div>
    </div>
  `
})
export class PhoneNumbersFormComponent {
  @Input({ required: true }) phoneNumbersArray!: FormArray;
  @Input() isPrimaryVerified: boolean = false;
  @Input() readonly: boolean = false;

  constructor(private fb: FormBuilder) {}

  addPhoneNumber(): void {
    const nextType = this.phoneNumbersArray.length === 1 ? 'Home Landline' : 'Emergency Contact';
    this.phoneNumbersArray.push(this.createPhoneGroup(nextType));
  }

  removePhoneNumber(index: number): void {
    if (this.phoneNumbersArray.length > 1) {
      this.phoneNumbersArray.removeAt(index);
    }
  }

  public createPhoneGroup(defaultType: string = 'Primary Mobile', number: string = '', isPrimary: boolean = false): FormGroup {
    return this.fb.group({
      phoneType: [defaultType, [Validators.required]],
      phoneNumber: [number, [Validators.pattern('^[+]*[(]?[0-9]{1,4}[)]?[-\\s./0-9]{7,15}$')]],
      isPrimary: [isPrimary || defaultType === 'Primary Mobile'],
      isVerified: [false]
    });
  }
}
