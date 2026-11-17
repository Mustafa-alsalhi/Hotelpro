import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTabsModule } from '@angular/material/tabs';
import { ReservationService } from '../../core/services/reservation.service';
import { OperationsService } from '../../core/services/operations.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { ReservationDto } from '../../core/models/reservation.model';
import { CheckOutSummaryDto } from '../../core/models/check-in-out.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-front-desk',
  standalone: true,
  imports: [
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatTabsModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.lang() === 'ar' ? 'الاستقبال: الوصول والمغادرة' : 'Front Desk: Check-In & Check-Out' }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إتمام إجراءات وصول النزلاء وتسوية الحسابات عند المغادرة' : 'Manage guest arrivals, document verification, and stay settlements' }}</p>
      </div>
    </div>

    <mat-tab-group>
      <mat-tab [label]="i18n.t('nav.checkIn')">
        <div class="hp-card" style="padding:16px;margin-top:14px;overflow-x:auto;">
          <table mat-table [dataSource]="confirmedReservations()" style="width:100%;">
            <ng-container matColumnDef="number"><th mat-header-cell *matHeaderCellDef>#</th><td mat-cell *matCellDef="let r" style="font-weight:700;">{{ r.reservationNumber }}</td></ng-container>
            <ng-container matColumnDef="guest"><th mat-header-cell *matHeaderCellDef>Guest</th><td mat-cell *matCellDef="let r">{{ r.primaryGuestName }} ({{ r.primaryGuestPhone }})</td></ng-container>
            <ng-container matColumnDef="dates"><th mat-header-cell *matHeaderCellDef>Stay</th><td mat-cell *matCellDef="let r">{{ r.checkInDate }} → {{ r.checkOutDate }}</td></ng-container>
            <ng-container matColumnDef="rooms"><th mat-header-cell *matHeaderCellDef>Rooms</th><td mat-cell *matCellDef="let r">{{ formatRooms(r) }}</td></ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let r">
                <button *appHasPermission="'Reservations.CheckIn'" mat-flat-button class="hp-btn-primary" (click)="performCheckIn(r)">
                  <mat-icon>login</mat-icon>
                  <span>Check-In</span>
                </button>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['number','guest','dates','rooms','actions']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['number','guest','dates','rooms','actions'];"></tr>
          </table>
        </div>
      </mat-tab>

      <mat-tab [label]="i18n.t('nav.checkOut')">
        <div class="hp-card" style="padding:16px;margin-top:14px;overflow-x:auto;">
          <table mat-table [dataSource]="checkedInReservations()" style="width:100%;">
            <ng-container matColumnDef="number"><th mat-header-cell *matHeaderCellDef>#</th><td mat-cell *matCellDef="let r" style="font-weight:700;">{{ r.reservationNumber }}</td></ng-container>
            <ng-container matColumnDef="guest"><th mat-header-cell *matHeaderCellDef>Guest</th><td mat-cell *matCellDef="let r">{{ r.primaryGuestName }}</td></ng-container>
            <ng-container matColumnDef="dates"><th mat-header-cell *matHeaderCellDef>Stay</th><td mat-cell *matCellDef="let r">{{ r.checkInDate }} → {{ r.checkOutDate }}</td></ng-container>
            <ng-container matColumnDef="rooms"><th mat-header-cell *matHeaderCellDef>Rooms</th><td mat-cell *matCellDef="let r">{{ formatRooms(r) }}</td></ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let r">
                <button *appHasPermission="'Reservations.CheckOut'" mat-flat-button class="hp-btn-gold" (click)="loadSummary(r.id)">
                  <mat-icon>logout</mat-icon>
                  <span>Settle & Check-Out</span>
                </button>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['number','guest','dates','rooms','actions']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['number','guest','dates','rooms','actions'];"></tr>
          </table>
        </div>

        @if (checkoutSummary(); as s) {
          <div class="hp-card" style="padding:20px;margin-top:16px;max-width:520px;">
            <h3 style="margin-top:0;color:#0f2540;">{{ s.reservationNumber }} — {{ s.guestName }}</h3>
            <div style="display:grid;grid-template-columns:1fr auto;gap:8px;font-size:0.92rem;">
              <span>Accommodation:</span><strong>{{ s.accommodationTotal }}</strong>
              <span>Guest Services:</span><strong>{{ s.servicesTotal }}</strong>
              <span>Tax & Service Charge:</span><strong>{{ s.taxAndServiceTotal }}</strong>
              <span>Discount:</span><strong>-{{ s.discountAmount }}</strong>
              <span>Deposit Paid:</span><strong>-{{ s.depositAmount }}</strong>
              <span style="font-size:1.05rem;color:#0f2540;">Balance Due:</span>
              <strong style="font-size:1.1rem;color:#b91c1c;">{{ s.balanceDue }}</strong>
            </div>
            <div style="margin-top:14px;display:flex;gap:10px;align-items:center;">
              <mat-form-field appearance="outline" style="flex:1;">
                <mat-label>Final Payment</mat-label>
                <input matInput type="number" [(ngModel)]="finalPayment" />
              </mat-form-field>
              <button mat-flat-button class="hp-btn-primary" (click)="performCheckOut(s.reservationId)">
                Complete Check-Out
              </button>
            </div>
          </div>
        }
      </mat-tab>
    </mat-tab-group>
  `
})
export class FrontDeskComponent implements OnInit {
  private readonly resService = inject(ReservationService);
  private readonly opsService = inject(OperationsService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly confirmedReservations = signal<ReservationDto[]>([]);
  readonly checkedInReservations = signal<ReservationDto[]>([]);
  readonly checkoutSummary = signal<CheckOutSummaryDto | null>(null);
  finalPayment = 0;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.resService.getReservations(1, 50, '', 2).subscribe((r) => this.confirmedReservations.set(r.items));
    this.resService.getReservations(1, 50, '', 3).subscribe((r) => this.checkedInReservations.set(r.items));
  }

  performCheckIn(r: ReservationDto): void {
    this.opsService.checkIn({
      reservationId: r.id,
      actualAdults: r.adultsCount,
      actualChildren: r.childrenCount,
      documentsVerified: true
    }).subscribe({
      next: () => {
        this.toast.success(`Checked in ${r.reservationNumber}`);
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Check-in failed')
    });
  }

  loadSummary(reservationId: string): void {
    this.opsService.getCheckOutSummary(reservationId).subscribe((s) => {
      this.checkoutSummary.set(s);
      this.finalPayment = s.balanceDue;
    });
  }

  performCheckOut(reservationId: string): void {
    this.opsService.checkOut({
      reservationId,
      finalPaymentSettled: this.finalPayment,
      exceptionalBalanceApproved: false
    }).subscribe({
      next: () => {
        this.toast.success('Check-out completed. Room moved to Cleaning.');
        this.checkoutSummary.set(null);
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Check-out failed')
    });
  }

  formatRooms(r: ReservationDto): string {
    return r.rooms.map((rm) => rm.roomNumber).join(', ');
  }
}

