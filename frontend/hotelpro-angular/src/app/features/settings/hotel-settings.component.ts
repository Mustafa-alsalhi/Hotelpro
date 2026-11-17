import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-hotel-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressBarModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.lang() === 'ar' ? 'إعدادات الفندق' : 'Hotel Settings' }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'بيانات الفندق، العملة، الضرائب، وسياسات الإلغاء والتشغيل' : 'Hotel profile, currency, taxes, and operational policies' }}</p>
      </div>
      <button
        *appHasPermission="'Settings.ManageSettings'"
        mat-flat-button
        class="hp-btn-primary"
        [disabled]="form.invalid || saving()"
        (click)="save()">
        <mat-icon>save</mat-icon>
        <span>{{ i18n.t('common.save') }}</span>
      </button>
    </div>

    @if (loading()) {
      <mat-progress-bar mode="indeterminate"></mat-progress-bar>
    }

    <form [formGroup]="form" class="hp-settings-grid">
      <div class="hp-card" style="padding:20px;">
        <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'معلومات الفندق' : 'Hotel Identity' }}</h3>
        <div class="hp-form-grid">
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'اسم الفندق' : 'Hotel Name' }}</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الاسم القانوني' : 'Legal Name' }}</mat-label>
            <input matInput formControlName="legalName" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الهاتف' : 'Phone' }}</mat-label>
            <input matInput formControlName="phone" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.t('auth.email') }}</mat-label>
            <input matInput formControlName="email" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الرقم الضريبي' : 'Tax Number' }}</mat-label>
            <input matInput formControlName="taxNumber" />
          </mat-form-field>
          <mat-form-field appearance="outline" style="grid-column:1/-1;">
            <mat-label>{{ i18n.lang() === 'ar' ? 'العنوان' : 'Address' }}</mat-label>
            <input matInput formControlName="address" />
          </mat-form-field>
        </div>
      </div>

      <div class="hp-card" style="padding:20px;">
        <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'الإعدادات الإقليمية والمالية' : 'Regional & Financial' }}</h3>
        <div class="hp-form-grid">
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'رمز العملة' : 'Currency Code' }}</mat-label>
            <input matInput formControlName="currencyCode" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'علامة العملة' : 'Currency Symbol' }}</mat-label>
            <input matInput formControlName="currencySymbol" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'المنطقة الزمنية' : 'Time Zone' }}</mat-label>
            <input matInput formControlName="timeZoneId" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'تنسيق التاريخ' : 'Date Format' }}</mat-label>
            <mat-select formControlName="dateFormat">
              <mat-option value="yyyy-MM-dd">yyyy-MM-dd</mat-option>
              <mat-option value="dd/MM/yyyy">dd/MM/yyyy</mat-option>
              <mat-option value="MM/dd/yyyy">MM/dd/yyyy</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'نسبة الضريبة (%)' : 'Tax Rate (%)' }}</mat-label>
            <input matInput type="number" formControlName="taxRatePercent" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'رسوم الخدمة (%)' : 'Service Charge (%)' }}</mat-label>
            <input matInput type="number" formControlName="serviceChargePercent" />
          </mat-form-field>
        </div>
      </div>

      <div class="hp-card" style="padding:20px;grid-column:1/-1;">
        <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'سياسات التشغيل والحجز' : 'Operational Policies' }}</h3>
        <div class="hp-form-grid">
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'وقت الوصول الافتراضي' : 'Default Check-In' }}</mat-label>
            <input matInput type="time" formControlName="defaultCheckInTime" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'وقت المغادرة الافتراضي' : 'Default Check-Out' }}</mat-label>
            <input matInput type="time" formControlName="defaultCheckOutTime" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'ساعات سياسة الإلغاء' : 'Cancellation Policy (Hours)' }}</mat-label>
            <input matInput type="number" formControlName="cancellationPolicyHours" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'رسوم الإلغاء (%)' : 'Cancellation Fee (%)' }}</mat-label>
            <input matInput type="number" formControlName="cancellationFeePercent" />
          </mat-form-field>
        </div>
        <div style="display:flex;flex-wrap:wrap;gap:20px;margin-top:8px;">
          <mat-checkbox formControlName="requireInspectionAfterCleaning">
            {{ i18n.lang() === 'ar' ? 'يتطلب فحص المشرف بعد التنظيف' : 'Require supervisor inspection after cleaning' }}
          </mat-checkbox>
          <mat-checkbox formControlName="allowCheckoutWithBalancePermission">
            {{ i18n.lang() === 'ar' ? 'السماح بالمغادرة مع رصيد مستحق بصلاحية استثنائية' : 'Allow exceptional check-out with balance' }}
          </mat-checkbox>
        </div>
      </div>
    </form>
  `,
  styles: [`
    .hp-settings-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(380px, 1fr)); gap: 20px; }
    .hp-form-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 12px; }
  `]
})
export class HotelSettingsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    legalName: [''],
    logoUrl: [''],
    address: ['', Validators.required],
    phone: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    taxNumber: [''],
    currencyCode: ['SAR', Validators.required],
    currencySymbol: ['ر.س', Validators.required],
    timeZoneId: ['Arab Standard Time', Validators.required],
    dateFormat: ['yyyy-MM-dd', Validators.required],
    defaultLanguage: ['ar', Validators.required],
    defaultCheckInTime: ['14:00', Validators.required],
    defaultCheckOutTime: ['12:00', Validators.required],
    taxRatePercent: [15, [Validators.required, Validators.min(0), Validators.max(100)]],
    serviceChargePercent: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    requireInspectionAfterCleaning: [true],
    allowCheckoutWithBalancePermission: [false],
    cancellationPolicyHours: [24, [Validators.required, Validators.min(0)]],
    cancellationFeePercent: [0, [Validators.required, Validators.min(0), Validators.max(100)]]
  });

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getHotelSettings().subscribe({
      next: (s) => {
        this.form.patchValue({
          ...s,
          legalName: s.legalName ?? '',
          logoUrl: s.logoUrl ?? '',
          taxNumber: s.taxNumber ?? '',
          defaultCheckInTime: s.defaultCheckInTime.slice(0, 5),
          defaultCheckOutTime: s.defaultCheckOutTime.slice(0, 5)
        });
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Failed to load settings');
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    const v = this.form.getRawValue();
    this.service.updateHotelSettings({
      ...v,
      defaultCheckInTime: `${v.defaultCheckInTime}:00`,
      defaultCheckOutTime: `${v.defaultCheckOutTime}:00`
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Settings saved');
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.message || 'Failed to save settings');
      }
    });
  }
}

