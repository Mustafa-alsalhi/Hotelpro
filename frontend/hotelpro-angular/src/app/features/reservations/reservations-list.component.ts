import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ReservationService } from '../../core/services/reservation.service';
import { RateManagementService } from '../../core/services/rate-management.service';
import { HotelRoomsGuestsService } from '../../core/services/hotel-rooms-guests.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import {
  BookingSource,
  RESERVATION_STATUS_LABELS,
  ReservationDto,
  ReservationStatus
} from '../../core/models/reservation.model';
import { GuestDto } from '../../core/models/guest.model';
import { RatePlanDto } from '../../core/models/rate-plan.model';
import { RoomDto } from '../../core/models/room.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-reservations-list',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    HasPermissionDirective,
    EmptyStateComponent
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.reservations') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إنشاء وإدارة الحجوزات والتحقق التلقائي من التوفر' : 'Create and manage bookings with real-time availability checks' }}</p>
      </div>
      <button *appHasPermission="'Reservations.Create'" mat-flat-button class="hp-btn-primary" (click)="openNew()">
        <mat-icon>add_circle</mat-icon>
        <span>{{ i18n.lang() === 'ar' ? 'حجز جديد' : 'New Reservation' }}</span>
      </button>
    </div>

    <div class="hp-card" style="padding:16px;margin-bottom:16px;display:flex;flex-wrap:wrap;gap:12px;">
      <mat-form-field appearance="outline" style="flex:1;min-width:220px;">
        <mat-label>{{ i18n.t('common.search') }}</mat-label>
        <input matInput [(ngModel)]="search" (keyup.enter)="load(1)" />
        <mat-icon matSuffix style="cursor:pointer;" (click)="load(1)">search</mat-icon>
      </mat-form-field>
      <mat-form-field appearance="outline" style="min-width:170px;">
        <mat-label>{{ i18n.t('common.status') }}</mat-label>
        <mat-select [(ngModel)]="statusFilter" (selectionChange)="load(1)">
          <mat-option [value]="null">All</mat-option>
          @for (s of statusOptions; track s.value) {
            <mat-option [value]="s.value">{{ i18n.lang() === 'ar' ? s.ar : s.en }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>

    @if (showCreate()) {
      <div class="hp-card" style="padding:20px;margin-bottom:18px;">
        <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'إنشاء حجز جديد' : 'Create Reservation' }}</h3>
        <form [formGroup]="form" (ngSubmit)="createReservation()" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:12px;">
          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'النزيل' : 'Guest' }}</mat-label>
            <mat-select formControlName="primaryGuestId">
              @for (g of guests(); track g.id) { <mat-option [value]="g.id">{{ g.fullName }} ({{ g.phone }})</mat-option> }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'خطة السعر' : 'Rate Plan' }}</mat-label>
            <mat-select formControlName="ratePlanId">
              @for (rp of ratePlans(); track rp.id) { <mat-option [value]="rp.id">{{ rp.name }}</mat-option> }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'تاريخ الوصول' : 'Check-in Date' }}</mat-label>
            <input matInput type="date" formControlName="checkInDate" (change)="refreshAvailableRooms()" />
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>{{ i18n.lang() === 'ar' ? 'تاريخ المغادرة' : 'Check-out Date' }}</mat-label>
            <input matInput type="date" formControlName="checkOutDate" (change)="refreshAvailableRooms()" />
          </mat-form-field>

          <mat-form-field appearance="outline" style="grid-column:1/-1;">
            <mat-label>{{ i18n.lang() === 'ar' ? 'الغرف المتاحة للفترة' : 'Available Rooms for Dates' }}</mat-label>
            <mat-select formControlName="roomIds" multiple>
              @for (r of availableRooms(); track r.id) {
                <mat-option [value]="r.id">#{{ r.roomNumber }} — {{ r.roomTypeName }} ({{ r.effectivePrice }}/night)</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Adults</mat-label>
            <input matInput type="number" formControlName="adultsCount" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Children</mat-label>
            <input matInput type="number" formControlName="childrenCount" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Source</mat-label>
            <mat-select formControlName="source">
              <mat-option [value]="1">Direct Booking</mat-option>
              <mat-option [value]="2">Phone</mat-option>
              <mat-option [value]="3">Website</mat-option>
              <mat-option [value]="4">Travel Agent</mat-option>
              <mat-option [value]="5">Walk-in</mat-option>
              <mat-option [value]="6">Other</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Discount</mat-label>
            <input matInput type="number" formControlName="discountAmount" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Deposit Required</mat-label>
            <input matInput type="number" formControlName="depositRequired" />
          </mat-form-field>
          <div style="display:flex;align-items:center;">
            <mat-checkbox formControlName="confirmImmediately">{{ i18n.lang() === 'ar' ? 'تأكيد فوري' : 'Confirm Immediately' }}</mat-checkbox>
          </div>

          <div style="grid-column:1/-1;display:flex;justify-content:flex-end;gap:8px;">
            <button mat-stroked-button type="button" (click)="showCreate.set(false)">{{ i18n.t('common.cancel') }}</button>
            <button mat-flat-button class="hp-btn-primary" type="submit" [disabled]="form.invalid || saving()">{{ i18n.t('common.save') }}</button>
          </div>
        </form>
      </div>
    }

    @if (loading()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }

    <div class="hp-card" style="padding:16px;overflow-x:auto;">
      @if (!loading() && reservations().length === 0) {
        <app-empty-state icon="event_busy"></app-empty-state>
      } @else {
        <table mat-table [dataSource]="reservations()" style="width:100%;">
          <ng-container matColumnDef="number">
            <th mat-header-cell *matHeaderCellDef>#</th>
            <td mat-cell *matCellDef="let r">
              <div style="font-weight:700;color:#0f2540;">{{ r.reservationNumber }}</div>
              <div style="font-size:0.78rem;color:#64748b;">{{ r.ratePlanName }}</div>
            </td>
          </ng-container>
          <ng-container matColumnDef="guest">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'النزيل' : 'Guest' }}</th>
            <td mat-cell *matCellDef="let r">
              <div>{{ r.primaryGuestName }}</div>
              <div style="font-size:0.78rem;color:#64748b;">{{ r.primaryGuestPhone }}</div>
            </td>
          </ng-container>
          <ng-container matColumnDef="dates">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'الإقامة' : 'Stay' }}</th>
            <td mat-cell *matCellDef="let r">
              <div>{{ r.checkInDate }} → {{ r.checkOutDate }} ({{ r.nights }}N)</div>
              <div style="font-size:0.78rem;color:#64748b;">
                Rooms: {{ formatRooms(r) }}
              </div>
            </td>
          </ng-container>
          <ng-container matColumnDef="total">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.lang() === 'ar' ? 'الإجمالي' : 'Total' }}</th>
            <td mat-cell *matCellDef="let r" style="font-weight:700;color:#0f2540;">{{ r.totalAmount }}</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.status') }}</th>
            <td mat-cell *matCellDef="let r">
              <span class="hp-badge" [class]="statusCss(r.status)">{{ statusLabel(r.status) }}</span>
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>{{ i18n.t('common.actions') }}</th>
            <td mat-cell *matCellDef="let r">
              @if (r.status === 1) {
                <button *appHasPermission="'Reservations.Approve'" mat-icon-button color="primary" (click)="confirm(r)" title="Confirm">
                  <mat-icon>check_circle</mat-icon>
                </button>
              }
              @if (r.status === 1 || r.status === 2) {
                <button *appHasPermission="'Reservations.Cancel'" mat-icon-button color="warn" (click)="cancel(r)" title="Cancel">
                  <mat-icon>cancel</mat-icon>
                </button>
              }
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="['number','guest','dates','total','status','actions']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['number','guest','dates','total','status','actions'];"></tr>
        </table>
        <mat-paginator [length]="totalCount()" [pageSize]="pageSize" [pageIndex]="pageNumber - 1" [pageSizeOptions]="[10, 15, 25]" (page)="onPage($event)"></mat-paginator>
      }
    </div>
  `,
  styles: [`
    .hp-res-confirmed { background: #dbeafe; color: #1d4ed8; }
    .hp-res-checkedout { background: #e2e8f0; color: #475569; }
  `]
})
export class ReservationsListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly resService = inject(ReservationService);
  private readonly rateService = inject(RateManagementService);
  private readonly guestService = inject(HotelRoomsGuestsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly reservations = signal<ReservationDto[]>([]);
  readonly guests = signal<GuestDto[]>([]);
  readonly ratePlans = signal<RatePlanDto[]>([]);
  readonly availableRooms = signal<RoomDto[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly showCreate = signal(false);

  pageNumber = 1;
  pageSize = 15;
  search = '';
  statusFilter: ReservationStatus | null = null;

  readonly statusOptions = Object.entries(RESERVATION_STATUS_LABELS).map(([k, v]) => ({
    value: Number(k) as ReservationStatus,
    en: v.en,
    ar: v.ar
  }));

  readonly form = this.fb.nonNullable.group({
    primaryGuestId: ['', Validators.required],
    ratePlanId: ['', Validators.required],
    checkInDate: [this.todayStr(0), Validators.required],
    checkOutDate: [this.todayStr(2), Validators.required],
    roomIds: [<string[]>[], Validators.required],
    adultsCount: [1, [Validators.required, Validators.min(1)]],
    childrenCount: [0, [Validators.required, Validators.min(0)]],
    source: [1 as BookingSource, Validators.required],
    discountAmount: [0, [Validators.min(0)]],
    depositRequired: [0, [Validators.min(0)]],
    notes: [''],
    confirmImmediately: [true]
  });

  ngOnInit(): void {
    this.guestService.getGuests(1, 100).subscribe((res) => this.guests.set(res.items));
    this.rateService.getRatePlans(false).subscribe((plans) => this.ratePlans.set(plans));
    this.load();
  }

  load(page = this.pageNumber): void {
    this.pageNumber = page;
    this.loading.set(true);
    this.resService.getReservations(this.pageNumber, this.pageSize, this.search, this.statusFilter).subscribe({
      next: (res) => {
        this.reservations.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent): void {
    this.pageSize = e.pageSize;
    this.load(e.pageIndex + 1);
  }

  openNew(): void {
    this.showCreate.set(true);
    this.refreshAvailableRooms();
  }

  refreshAvailableRooms(): void {
    const { checkInDate, checkOutDate } = this.form.getRawValue();
    if (!checkInDate || !checkOutDate || checkOutDate <= checkInDate) {
      this.availableRooms.set([]);
      return;
    }
    this.resService.getAvailableRooms(checkInDate, checkOutDate).subscribe((rooms) => this.availableRooms.set(rooms));
  }

  createReservation(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.resService.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.showCreate.set(false);
        this.toast.success('Reservation created');
        this.load(1);
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.message || 'Could not create reservation');
      }
    });
  }

  confirm(r: ReservationDto): void {
    this.resService.confirm(r.id).subscribe({
      next: () => {
        this.toast.success('Reservation confirmed');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  cancel(r: ReservationDto): void {
    this.resService.cancel(r.id, 'Cancelled from front desk').subscribe({
      next: () => {
        this.toast.success('Reservation cancelled');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  formatRooms(r: ReservationDto): string {
    return r.rooms.map((rm) => rm.roomNumber).join(', ');
  }

  statusLabel(s: number): string {
    const item = RESERVATION_STATUS_LABELS[s];
    return item ? (this.i18n.lang() === 'ar' ? item.ar : item.en) : '';
  }

  statusCss(s: number): string {
    return RESERVATION_STATUS_LABELS[s]?.css ?? '';
  }

  private todayStr(offsetDays: number): string {
    const d = new Date();
    d.setDate(d.getDate() + offsetDays);
    return d.toISOString().slice(0, 10);
  }
}

