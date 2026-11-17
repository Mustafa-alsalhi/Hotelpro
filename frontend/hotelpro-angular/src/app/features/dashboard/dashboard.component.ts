import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DashboardReportsService } from '../../core/services/dashboard-reports.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { DashboardSummaryDto } from '../../core/models/dashboard.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.dashboard') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'المؤشرات التشغيلية والمالية الحية للفندق' : 'Live operational & financial KPIs from database' }}</p>
      </div>
      <div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">
        <mat-form-field appearance="outline" style="width:150px;"><mat-label>From</mat-label><input matInput type="date" [(ngModel)]="fromDate" /></mat-form-field>
        <mat-form-field appearance="outline" style="width:150px;"><mat-label>To</mat-label><input matInput type="date" [(ngModel)]="toDate" /></mat-form-field>
        <button mat-flat-button class="hp-btn-primary" (click)="load()">
          <mat-icon>refresh</mat-icon>
          <span>{{ i18n.lang() === 'ar' ? 'تحديث' : 'Filter' }}</span>
        </button>
      </div>
    </div>

    @if (loading()) { <mat-progress-bar mode="indeterminate"></mat-progress-bar> }

    @if (summary(); as s) {
      <div class="hp-kpi-grid">
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'إجمالي الغرف' : 'Total Rooms' }}</span><strong class="hp-kpi-val">{{ s.totalRooms }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'متاحة' : 'Available' }}</span><strong class="hp-kpi-val" style="color:#15803d;">{{ s.availableRooms }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'مشغولة' : 'Occupied' }}</span><strong class="hp-kpi-val" style="color:#b91c1c;">{{ s.occupiedRooms }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'محجوزة' : 'Reserved' }}</span><strong class="hp-kpi-val" style="color:#1d4ed8;">{{ s.reservedRooms }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'قيد التنظيف' : 'Cleaning' }}</span><strong class="hp-kpi-val" style="color:#b45309;">{{ s.cleaningRooms }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'تحت الصيانة' : 'Maintenance' }}</span><strong class="hp-kpi-val" style="color:#7e22ce;">{{ s.maintenanceRooms }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'نسبة الإشغال' : 'Occupancy' }}</span><strong class="hp-kpi-val" style="color:#c9a227;">{{ s.occupancyRatePercent }}%</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'النزلاء الحاليون' : 'In-House Guests' }}</span><strong class="hp-kpi-val">{{ s.currentInHouseGuests }}</strong></div>
      </div>

      <div class="hp-kpi-grid" style="margin-top:14px;">
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'وصول اليوم المتوقع' : 'Arrivals Today' }}</span><strong class="hp-kpi-val">{{ s.expectedArrivalsToday }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'مغادرة اليوم المتوقعة' : 'Departures Today' }}</span><strong class="hp-kpi-val">{{ s.expectedDeparturesToday }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'إجمالي الإيرادات' : 'Total Revenues' }}</span><strong class="hp-kpi-val" style="color:#15803d;">{{ s.totalRevenues }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'إجمالي المصروفات' : 'Total Expenses' }}</span><strong class="hp-kpi-val" style="color:#b91c1c;">{{ s.totalExpenses }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'صافي النتيجة التشغيلية' : 'Net Operating' }}</span><strong class="hp-kpi-val" style="color:#c9a227;">{{ s.netOperatingResult }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'مهام نظافة معلقة' : 'Pending HK Tasks' }}</span><strong class="hp-kpi-val">{{ s.pendingHousekeepingTasks }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'طلبات صيانة مفتوحة' : 'Open Maintenance' }}</span><strong class="hp-kpi-val">{{ s.openMaintenanceRequests }}</strong></div>
        <div class="hp-card hp-kpi"><span class="hp-kpi-label">{{ i18n.lang() === 'ar' ? 'أصناف منخفضة المخزون' : 'Low Stock Alerts' }}</span><strong class="hp-kpi-val" style="color:#b91c1c;">{{ s.lowStockItemsCount }}</strong></div>
      </div>

      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:18px;margin-top:20px;">
        <div class="hp-card" style="padding:18px;">
          <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'توزيع أنواع الغرف' : 'Room Type Distribution' }}</h3>
          @for (item of s.roomTypeDistribution; track item.label) {
            <div style="margin-bottom:10px;">
              <div style="display:flex;justify-content:space-between;font-size:0.85rem;margin-bottom:3px;">
                <span>{{ item.label }}</span><strong>{{ item.value }}</strong>
              </div>
              <div style="height:8px;background:#e2e8f0;border-radius:4px;overflow:hidden;">
                <div style="height:100%;background:#0f2540;" [style.width.%]="s.totalRooms ? (item.value / s.totalRooms) * 100 : 0"></div>
              </div>
            </div>
          }
        </div>

        <div class="hp-card" style="padding:18px;">
          <h3 style="margin-top:0;color:#0f2540;">{{ i18n.lang() === 'ar' ? 'مصادر الحجوزات' : 'Bookings by Source' }}</h3>
          @for (item of s.bookingsBySource; track item.label) {
            <div style="display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid #f1f5f9;">
              <span>{{ item.label }}</span>
              <span class="hp-badge hp-badge-gold">{{ item.value }}</span>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .hp-kpi-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(175px, 1fr)); gap: 14px; }
    .hp-kpi { padding: 16px; display: flex; flex-direction: column; gap: 6px; }
    .hp-kpi-label { font-size: 0.8rem; color: #64748b; }
    .hp-kpi-val { font-size: 1.45rem; font-weight: 800; color: #0f2540; }
  `]
})
export class DashboardComponent implements OnInit {
  private readonly service = inject(DashboardReportsService);
  readonly i18n = inject(TranslationService);

  readonly summary = signal<DashboardSummaryDto | null>(null);
  readonly loading = signal(false);

  fromDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10);
  toDate = new Date().toISOString().slice(0, 10);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getDashboardSummary(this.fromDate, this.toDate).subscribe({
      next: (res) => {
        this.summary.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
