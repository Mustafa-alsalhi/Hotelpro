import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { BillingFinanceService } from '../../core/services/billing-finance.service';
import { ReservationService } from '../../core/services/reservation.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import { INVOICE_STATUS_LABELS, InvoiceDto } from '../../core/models/invoice.model';
import { PaymentDto, PaymentMethod } from '../../core/models/payment.model';
import { ReservationDto } from '../../core/models/reservation.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [
    FormsModule,
    MatTableModule,
    MatTabsModule,
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
        <h1 class="hp-page-title">{{ i18n.t('nav.billing') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إصدار الفواتير، تسجيل الدفعات الجزئية والكاملة، وعكس القيود المالية' : 'Issue invoices, record partial/full payments, and manage reversals' }}</p>
      </div>
    </div>

    <div class="hp-card" style="padding:16px;margin-bottom:18px;display:flex;gap:12px;flex-wrap:wrap;align-items:center;">
      <mat-form-field appearance="outline" style="min-width:260px;">
        <mat-label>{{ i18n.lang() === 'ar' ? 'إصدار فاتورة لحجز' : 'Issue Invoice for Reservation' }}</mat-label>
        <mat-select [(ngModel)]="selectedReservationId">
          @for (r of reservations(); track r.id) {
            <mat-option [value]="r.id">{{ r.reservationNumber }} — {{ r.primaryGuestName }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <button *appHasPermission="'Billing.Create'" mat-flat-button class="hp-btn-primary" [disabled]="!selectedReservationId" (click)="issueInvoice()">
        <mat-icon>receipt_long</mat-icon>
        <span>{{ i18n.lang() === 'ar' ? 'إصدار فاتورة' : 'Issue Invoice' }}</span>
      </button>
    </div>

    <mat-tab-group>
      <mat-tab [label]="i18n.lang() === 'ar' ? 'الفواتير' : 'Invoices'">
        <div class="hp-card" style="padding:16px;margin-top:14px;overflow-x:auto;">
          <table mat-table [dataSource]="invoices()" style="width:100%;">
            <ng-container matColumnDef="number"><th mat-header-cell *matHeaderCellDef>#</th><td mat-cell *matCellDef="let i" style="font-weight:700;">{{ i.invoiceNumber }}</td></ng-container>
            <ng-container matColumnDef="guest"><th mat-header-cell *matHeaderCellDef>Guest</th><td mat-cell *matCellDef="let i">{{ i.guestName }} ({{ i.reservationNumber }})</td></ng-container>
            <ng-container matColumnDef="total"><th mat-header-cell *matHeaderCellDef>Total</th><td mat-cell *matCellDef="let i" style="font-weight:700;">{{ i.grandTotal }}</td></ng-container>
            <ng-container matColumnDef="balance"><th mat-header-cell *matHeaderCellDef>Balance</th><td mat-cell *matCellDef="let i" style="color:#b91c1c;font-weight:700;">{{ i.balanceDue }}</td></ng-container>
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let i"><span class="hp-badge" [class]="statusCss(i.status)">{{ statusLabel(i.status) }}</span></td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let i">
                @if (i.balanceDue > 0 && i.status !== 5) {
                  <button *appHasPermission="'Billing.ManagePayments'" mat-flat-button class="hp-btn-gold" (click)="selectForPayment(i)">
                    Pay
                  </button>
                }
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['number','guest','total','balance','status','actions']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['number','guest','total','balance','status','actions'];"></tr>
          </table>
        </div>

        @if (payingInvoice(); as inv) {
          <div class="hp-card" style="padding:20px;margin-top:16px;max-width:520px;">
            <h3 style="margin-top:0;color:#0f2540;">Record Payment for {{ inv.invoiceNumber }}</h3>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px;">
              <mat-form-field appearance="outline">
                <mat-label>Amount (Max {{ inv.balanceDue }})</mat-label>
                <input matInput type="number" [(ngModel)]="payAmount" />
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Method</mat-label>
                <mat-select [(ngModel)]="payMethod">
                  <mat-option [value]="1">Cash</mat-option>
                  <mat-option [value]="2">Bank Transfer</mat-option>
                  <mat-option [value]="3">Card</mat-option>
                  <mat-option [value]="4">Other</mat-option>
                </mat-select>
              </mat-form-field>
            </div>
            <div style="display:flex;gap:8px;justify-content:flex-end;">
              <button mat-stroked-button (click)="payingInvoice.set(null)">Cancel</button>
              <button mat-flat-button class="hp-btn-primary" (click)="submitPayment()">Submit Payment</button>
            </div>
          </div>
        }
      </mat-tab>

      <mat-tab [label]="i18n.lang() === 'ar' ? 'سجل المدفوعات' : 'Payments'">
        <div class="hp-card" style="padding:16px;margin-top:14px;overflow-x:auto;">
          <table mat-table [dataSource]="payments()" style="width:100%;">
            <ng-container matColumnDef="receipt"><th mat-header-cell *matHeaderCellDef>Receipt</th><td mat-cell *matCellDef="let p" style="font-weight:700;">{{ p.receiptNumber }}</td></ng-container>
            <ng-container matColumnDef="invoice"><th mat-header-cell *matHeaderCellDef>Invoice</th><td mat-cell *matCellDef="let p">{{ p.invoiceNumber }}</td></ng-container>
            <ng-container matColumnDef="amount"><th mat-header-cell *matHeaderCellDef>Amount</th><td mat-cell *matCellDef="let p" [style.color]="p.isReversal ? '#b91c1c' : '#15803d'" style="font-weight:700;">{{ p.amount }}</td></ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let p">
                @if (!p.isReversal && p.amount > 0) {
                  <button *appHasPermission="'Billing.Cancel'" mat-icon-button color="warn" (click)="reverse(p.id)" title="Reverse Payment">
                    <mat-icon>undo</mat-icon>
                  </button>
                }
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['receipt','invoice','amount','actions']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['receipt','invoice','amount','actions'];"></tr>
          </table>
        </div>
      </mat-tab>
    </mat-tab-group>
  `
})
export class BillingComponent implements OnInit {
  private readonly billing = inject(BillingFinanceService);
  private readonly resService = inject(ReservationService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly invoices = signal<InvoiceDto[]>([]);
  readonly payments = signal<PaymentDto[]>([]);
  readonly reservations = signal<ReservationDto[]>([]);
  readonly payingInvoice = signal<InvoiceDto | null>(null);

  selectedReservationId = '';
  payAmount = 0;
  payMethod: PaymentMethod = 1;

  ngOnInit(): void {
    this.resService.getReservations(1, 50).subscribe((r) => this.reservations.set(r.items));
    this.load();
  }

  load(): void {
    this.billing.getInvoices().subscribe((r) => this.invoices.set(r.items));
    this.billing.getPayments().subscribe((r) => this.payments.set(r.items));
  }

  issueInvoice(): void {
    this.billing.issueInvoice(this.selectedReservationId).subscribe({
      next: () => {
        this.toast.success('Invoice issued');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  selectForPayment(inv: InvoiceDto): void {
    this.payingInvoice.set(inv);
    this.payAmount = inv.balanceDue;
  }

  submitPayment(): void {
    const inv = this.payingInvoice();
    if (!inv) return;
    this.billing.recordPayment({
      invoiceId: inv.id,
      amount: this.payAmount,
      paymentMethod: this.payMethod,
      idempotencyKey: crypto.randomUUID()
    }).subscribe({
      next: () => {
        this.toast.success('Payment recorded');
        this.payingInvoice.set(null);
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Payment failed')
    });
  }

  reverse(id: string): void {
    this.billing.reversePayment(id, 'Reversed by accountant').subscribe({
      next: () => {
        this.toast.success('Payment reversed');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  statusLabel(s: number): string {
    const item = INVOICE_STATUS_LABELS[s];
    return item ? (this.i18n.lang() === 'ar' ? item.ar : item.en) : '';
  }

  statusCss(s: number): string {
    return INVOICE_STATUS_LABELS[s]?.css ?? '';
  }
}

