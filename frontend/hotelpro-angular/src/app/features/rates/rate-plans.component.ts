import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RateManagementService } from '../../core/services/rate-management.service';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { RatePlanDto, RoomRateDto } from '../../core/models/rate-plan.model';
import { RoomTypeDto } from '../../core/models/room-type.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-rate-plans',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.rates') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'خطط الأسعار، مضاعفات نهاية الأسبوع، والأسعار الموسمية' : 'Rate plans, weekend multipliers, and seasonal room rates' }}</p>
      </div>
    </div>

    <div style="display:grid;grid-template-columns:1fr 360px;gap:20px;align-items:start;margin-bottom:24px;">
      <div class="hp-card" style="padding:16px;overflow-x:auto;">
        <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'خطط الأسعار' : 'Rate Plans' }}</h3>
        <table mat-table [dataSource]="ratePlans()" style="width:100%;">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'الخطة' : 'Plan' }}</th>
            <td mat-cell *matCellDef="let rp">
              <div style="font-weight:700;color:#0f2540;">{{ rp.name }}</div>
              <div style="font-size:0.78rem;color:#64748b;">{{ rp.code }} • {{ rp.isRefundable ? 'Refundable' : 'Non-Refundable' }}</div>
            </td>
          </ng-container>
          <ng-container matColumnDef="rules">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'القواعد' : 'Rules' }}</th>
            <td mat-cell *matCellDef="let rp">
              Weekend ×{{ rp.weekendMultiplier }} • Long Stay ({{ rp.longStayMinNights }}+ nights): -{{ rp.longStayDiscountPercent }}%
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.actions') }}</th>
            <td mat-cell *matCellDef="let rp">
              <button *appHasPermission="'Rates.Edit'" mat-icon-button (click)="editPlan(rp)"><mat-icon>edit</mat-icon></button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="['name','rules','actions']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['name','rules','actions'];"></tr>
        </table>
      </div>

      <div class="hp-card" style="padding:20px;">
        <h3 style="margin-top:0;color:#0f2540;">{{ editingPlanId() ? i18n.t('common.edit') : i18n.t('common.add') }}</h3>
        <form [formGroup]="planForm" (ngSubmit)="savePlan()" style="display:flex;flex-direction:column;gap:8px;">
          <mat-form-field appearance="outline">
            <mat-label>Name</mat-label>
            <input matInput formControlName="name" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Code</mat-label>
            <input matInput formControlName="code" />
          </mat-form-field>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;">
            <mat-form-field appearance="outline">
              <mat-label>Weekend ×</mat-label>
              <input matInput type="number" step="0.05" formControlName="weekendMultiplier" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Long Stay Nights</mat-label>
              <input matInput type="number" formControlName="longStayMinNights" />
            </mat-form-field>
          </div>
          <mat-form-field appearance="outline">
            <mat-label>Long Stay Discount (%)</mat-label>
            <input matInput type="number" formControlName="longStayDiscountPercent" />
          </mat-form-field>
          <mat-checkbox formControlName="isRefundable">Refundable</mat-checkbox>
          <mat-checkbox formControlName="isActive">{{ i18n.t('common.active') }}</mat-checkbox>
          <button *appHasPermission="'Rates.Create'" mat-flat-button class="hp-btn-primary" type="submit" [disabled]="planForm.invalid">
            {{ i18n.t('common.save') }}
          </button>
        </form>
      </div>
    </div>

    <div class="hp-card" style="padding:20px;">
      <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'الأسعار الموسمية المخصصة' : 'Seasonal Room Rates' }}</h3>
      <form [formGroup]="rateForm" (ngSubmit)="addSeasonalRate()" style="display:flex;flex-wrap:wrap;gap:10px;align-items:center;margin-bottom:14px;">
        <mat-form-field appearance="outline" style="min-width:170px;">
          <mat-label>Room Type</mat-label>
          <mat-select formControlName="roomTypeId">
            @for (rt of roomTypes(); track rt.id) { <mat-option [value]="rt.id">{{ rt.name }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" style="min-width:170px;">
          <mat-label>Rate Plan</mat-label>
          <mat-select formControlName="ratePlanId">
            @for (rp of ratePlans(); track rp.id) { <mat-option [value]="rp.id">{{ rp.name }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Start</mat-label>
          <input matInput type="date" formControlName="startDate" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>End</mat-label>
          <input matInput type="date" formControlName="endDate" />
        </mat-form-field>
        <mat-form-field appearance="outline" style="width:120px;">
          <mat-label>Price/Night</mat-label>
          <input matInput type="number" formControlName="pricePerNight" />
        </mat-form-field>
        <button *appHasPermission="'Rates.Create'" mat-flat-button class="hp-btn-gold" type="submit" [disabled]="rateForm.invalid">
          {{ i18n.t('common.add') }}
        </button>
      </form>

      <table mat-table [dataSource]="roomRates()" style="width:100%;">
        <ng-container matColumnDef="roomType"><th mat-header-cell *matHeaderCellDef>Room Type</th><td mat-cell *matCellDef="let r">{{ r.roomTypeName }}</td></ng-container>
        <ng-container matColumnDef="ratePlan"><th mat-header-cell *matHeaderCellDef>Plan</th><td mat-cell *matCellDef="let r">{{ r.ratePlanName }}</td></ng-container>
        <ng-container matColumnDef="dates"><th mat-header-cell *matHeaderCellDef>Period</th><td mat-cell *matCellDef="let r">{{ r.startDate }} → {{ r.endDate }}</td></ng-container>
        <ng-container matColumnDef="price"><th mat-header-cell *matHeaderCellDef>Price</th><td mat-cell *matCellDef="let r" style="font-weight:700;">{{ r.pricePerNight }}</td></ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let r">
            <button *appHasPermission="'Rates.Delete'" mat-icon-button color="warn" (click)="deleteRate(r.id)"><mat-icon>delete</mat-icon></button>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="['roomType','ratePlan','dates','price','actions']"></tr>
        <tr mat-row *matRowDef="let row; columns: ['roomType','ratePlan','dates','price','actions'];"></tr>
      </table>
    </div>
  `
})
export class RatePlansComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rateService = inject(RateManagementService);
  private readonly roomService = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly ratePlans = signal<RatePlanDto[]>([]);
  readonly roomRates = signal<RoomRateDto[]>([]);
  readonly roomTypes = signal<RoomTypeDto[]>([]);
  readonly editingPlanId = signal<string | null>(null);

  readonly planForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    code: ['', Validators.required],
    description: [''],
    isRefundable: [true],
    weekendMultiplier: [1.15, Validators.required],
    longStayMinNights: [7, Validators.required],
    longStayDiscountPercent: [10, Validators.required],
    corporateCode: [''],
    isActive: [true]
  });

  readonly rateForm = this.fb.nonNullable.group({
    roomTypeId: ['', Validators.required],
    ratePlanId: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    pricePerNight: [500, [Validators.required, Validators.min(0)]],
    priority: [1]
  });

  ngOnInit(): void {
    this.roomService.getRoomTypes(true).subscribe((rt) => this.roomTypes.set(rt));
    this.load();
  }

  load(): void {
    this.rateService.getRatePlans(true).subscribe((p) => this.ratePlans.set(p));
    this.rateService.getRoomRates().subscribe((r) => this.roomRates.set(r));
  }

  editPlan(rp: RatePlanDto): void {
    this.editingPlanId.set(rp.id);
    this.planForm.patchValue({
      ...rp,
      description: rp.description ?? '',
      corporateCode: rp.corporateCode ?? ''
    });
  }

  savePlan(): void {
    if (this.planForm.invalid) return;
    const id = this.editingPlanId();
    const obs = id
      ? this.rateService.updateRatePlan(id, this.planForm.getRawValue())
      : this.rateService.createRatePlan(this.planForm.getRawValue());
    obs.subscribe({
      next: () => {
        this.toast.success('Rate plan saved');
        this.editingPlanId.set(null);
        this.planForm.reset({ name: '', code: '', description: '', isRefundable: true, weekendMultiplier: 1.15, longStayMinNights: 7, longStayDiscountPercent: 10, corporateCode: '', isActive: true });
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Error saving rate plan')
    });
  }

  addSeasonalRate(): void {
    if (this.rateForm.invalid) return;
    this.rateService.createRoomRate(this.rateForm.getRawValue()).subscribe({
      next: () => {
        this.toast.success('Seasonal rate added');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed to add rate')
    });
  }

  deleteRate(id: string): void {
    this.rateService.deleteRoomRate(id).subscribe({
      next: () => {
        this.toast.success('Deleted');
        this.load();
      }
    });
  }
}

