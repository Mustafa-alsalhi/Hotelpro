import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { OperationsService } from '../../core/services/operations.service';
import { ReservationService } from '../../core/services/reservation.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { GuestServiceDto, ServiceOrderDto, ServiceOrderStatus } from '../../core/models/guest-service.model';
import { ReservationDto } from '../../core/models/reservation.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-guest-services',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.guestServices') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إدارة الخدمات الإضافية وطلبات النزلاء المقيمين' : 'Manage service catalog and in-house guest orders' }}</p>
      </div>
    </div>

    <div class="hp-card" style="padding:20px;margin-bottom:20px;">
      <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'طلب خدمة لنزيل مقيم' : 'New Service Order' }}</h3>
      <form [formGroup]="orderForm" (ngSubmit)="createOrder()" style="display:flex;flex-wrap:wrap;gap:12px;align-items:center;">
        <mat-form-field appearance="outline" style="min-width:220px;">
          <mat-label>Checked-In Stay</mat-label>
          <mat-select formControlName="reservationId" (selectionChange)="onReservationSelect($event.value)">
            @for (r of activeStays(); track r.id) {
              <mat-option [value]="r.id">{{ r.reservationNumber }} — {{ r.primaryGuestName }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="min-width:200px;">
          <mat-label>Service</mat-label>
          <mat-select formControlName="guestServiceId">
            @for (s of catalog(); track s.id) {
              <mat-option [value]="s.id">{{ s.name }} ({{ s.price }})</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" style="width:100px;">
          <mat-label>Qty</mat-label>
          <input matInput type="number" formControlName="quantity" />
        </mat-form-field>

        <button *appHasPermission="'GuestServices.Create'" mat-flat-button class="hp-btn-primary" type="submit" [disabled]="orderForm.invalid">
          Add Order
        </button>
      </form>
    </div>

    <div class="hp-card" style="padding:16px;overflow-x:auto;">
      <table mat-table [dataSource]="orders()" style="width:100%;">
        <ng-container matColumnDef="order"><th mat-header-cell *matHeaderCellDef>#</th><td mat-cell *matCellDef="let o" style="font-weight:700;">{{ o.orderNumber }}</td></ng-container>
        <ng-container matColumnDef="guest"><th mat-header-cell *matHeaderCellDef>Guest / Room</th><td mat-cell *matCellDef="let o">{{ o.guestName }} (#{{ o.roomNumber }})</td></ng-container>
        <ng-container matColumnDef="total"><th mat-header-cell *matHeaderCellDef>Total</th><td mat-cell *matCellDef="let o" style="font-weight:700;">{{ o.totalAmount }}</td></ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let o">
            <mat-select *appHasPermission="'GuestServices.Edit'" [value]="o.status" (selectionChange)="updateStatus(o.id, $event.value)" style="width:130px;">
              <mat-option [value]="1">Pending</mat-option>
              <mat-option [value]="2">Accepted</mat-option>
              <mat-option [value]="3">In Progress</mat-option>
              <mat-option [value]="4">Completed</mat-option>
              <mat-option [value]="5">Cancelled</mat-option>
            </mat-select>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="['order','guest','total','status']"></tr>
        <tr mat-row *matRowDef="let row; columns: ['order','guest','total','status'];"></tr>
      </table>
    </div>
  `
})
export class GuestServicesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ops = inject(OperationsService);
  private readonly resService = inject(ReservationService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly catalog = signal<GuestServiceDto[]>([]);
  readonly orders = signal<ServiceOrderDto[]>([]);
  readonly activeStays = signal<ReservationDto[]>([]);

  readonly orderForm = this.fb.nonNullable.group({
    reservationId: ['', Validators.required],
    roomId: ['', Validators.required],
    guestServiceId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.ops.getServiceCatalog(false).subscribe((c) => this.catalog.set(c));
    this.resService.getReservations(1, 50, '', 3).subscribe((r) => this.activeStays.set(r.items));
    this.loadOrders();
  }

  onReservationSelect(resId: string): void {
    const stay = this.activeStays().find((r) => r.id === resId);
    if (stay?.rooms?.[0]) {
      this.orderForm.patchValue({ roomId: stay.rooms[0].roomId });
    }
  }

  loadOrders(): void {
    this.ops.getServiceOrders().subscribe((res) => this.orders.set(res.items));
  }

  createOrder(): void {
    if (this.orderForm.invalid) return;
    const v = this.orderForm.getRawValue();
    this.ops.createServiceOrder({
      reservationId: v.reservationId,
      roomId: v.roomId,
      items: [{ guestServiceId: v.guestServiceId, quantity: v.quantity }]
    }).subscribe({
      next: () => {
        this.toast.success('Service order added to stay');
        this.loadOrders();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  updateStatus(id: string, status: ServiceOrderStatus): void {
    this.ops.updateServiceOrderStatus(id, status).subscribe({
      next: () => {
        this.toast.success('Order updated');
        this.loadOrders();
      }
    });
  }
}

