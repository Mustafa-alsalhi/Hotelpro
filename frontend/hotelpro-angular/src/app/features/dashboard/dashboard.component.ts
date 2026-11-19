import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { DashboardReportsService } from '../../core/services/dashboard-reports.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ChartSeriesPointDto, DashboardSummaryDto } from '../../core/models/dashboard.model';

interface DonutSegment {
  label: string;
  value: number;
  secondaryValue: number;
  color: string;
  percent: number;
  dashArray: string;
  dashOffset: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    DecimalPipe,
    FormsModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatButtonToggleModule
  ],
  template: `
    <div class="hp-dash-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.dashboard') }}</h1>
        <p class="hp-page-subtitle">
          {{ isAr() ? 'لوحة المؤشرات التنفيذية والتحليلات الحية من قاعدة البيانات' : 'Executive KPIs & live analytics from SQL Server' }}
        </p>
      </div>

      <div class="hp-filter-bar">
        <div class="hp-presets">
          <button mat-stroked-button (click)="applyPreset('today')" [class.hp-preset-active]="activePreset() === 'today'">
            {{ isAr() ? 'اليوم' : 'Today' }}
          </button>
          <button mat-stroked-button (click)="applyPreset('7d')" [class.hp-preset-active]="activePreset() === '7d'">
            {{ isAr() ? '7 أيام' : '7 Days' }}
          </button>
          <button mat-stroked-button (click)="applyPreset('month')" [class.hp-preset-active]="activePreset() === 'month'">
            {{ isAr() ? 'هذا الشهر' : 'This Month' }}
          </button>
          <button mat-stroked-button (click)="applyPreset('6m')" [class.hp-preset-active]="activePreset() === '6m'">
            {{ isAr() ? '6 أشهر' : '6 Months' }}
          </button>
        </div>

        <div class="hp-date-inputs">
          <mat-form-field appearance="outline" class="hp-date-field">
            <mat-label>{{ isAr() ? 'من' : 'From' }}</mat-label>
            <input matInput type="date" [(ngModel)]="fromDate" (change)="activePreset.set('custom')" />
          </mat-form-field>
          <mat-form-field appearance="outline" class="hp-date-field">
            <mat-label>{{ isAr() ? 'إلى' : 'To' }}</mat-label>
            <input matInput type="date" [(ngModel)]="toDate" (change)="activePreset.set('custom')" />
          </mat-form-field>
          <button mat-flat-button class="hp-btn-primary" (click)="load()" [disabled]="loading()">
            <mat-icon>refresh</mat-icon>
            <span>{{ isAr() ? 'تطبيق' : 'Apply' }}</span>
          </button>
        </div>
      </div>
    </div>

    @if (loading()) {
      <mat-progress-bar mode="indeterminate" style="margin-bottom:16px;border-radius:4px;"></mat-progress-bar>
    }

    @if (errorMsg()) {
      <div class="hp-card hp-error-state">
        <mat-icon color="warn" style="font-size:36px;width:36px;height:36px;">error_outline</mat-icon>
        <div>
          <div style="font-weight:700;color:#991b1b;">{{ isAr() ? 'تعذر تحميل بيانات لوحة التحكم' : 'Failed to load dashboard data' }}</div>
          <div style="font-size:0.85rem;color:#64748b;margin-top:2px;">{{ errorMsg() }}</div>
        </div>
        <button mat-flat-button color="warn" (click)="load()">
          <mat-icon>refresh</mat-icon>
          <span>{{ isAr() ? 'إعادة المحاولة' : 'Retry' }}</span>
        </button>
      </div>
    }

    @if (summary(); as s) {
      <!-- Row 1: Primary Executive KPI Cards -->
      <div class="hp-hero-kpis">
        <a routerLink="/finance" class="hp-card hp-stat-card hp-stat-navy">
          <div class="hp-stat-top">
            <span class="hp-stat-label">{{ isAr() ? 'إجمالي الإيرادات' : 'Total Revenues' }}</span>
            <span class="hp-stat-icon" style="background:rgba(21,128,61,0.12);color:#15803d;"><mat-icon>trending_up</mat-icon></span>
          </div>
          <div class="hp-stat-value" style="color:#15803d;">{{ s.totalRevenues | number:'1.2-2' }}</div>
          <div class="hp-stat-foot">{{ isAr() ? 'خلال الفترة المحددة' : 'In selected period' }}</div>
        </a>

        <a routerLink="/finance" class="hp-card hp-stat-card">
          <div class="hp-stat-top">
            <span class="hp-stat-label">{{ isAr() ? 'إجمالي المصروفات' : 'Total Expenses' }}</span>
            <span class="hp-stat-icon" style="background:rgba(185,28,28,0.12);color:#b91c1c;"><mat-icon>trending_down</mat-icon></span>
          </div>
          <div class="hp-stat-value" style="color:#b91c1c;">{{ s.totalExpenses | number:'1.2-2' }}</div>
          <div class="hp-stat-foot">{{ isAr() ? 'المصروفات التشغيلية' : 'Operating expenses' }}</div>
        </a>

        <a routerLink="/reports" class="hp-card hp-stat-card hp-stat-gold">
          <div class="hp-stat-top">
            <span class="hp-stat-label">{{ isAr() ? 'صافي النتيجة التشغيلية' : 'Net Operating Result' }}</span>
            <span class="hp-stat-icon" style="background:rgba(201,162,39,0.16);color:#a16207;"><mat-icon>account_balance_wallet</mat-icon></span>
          </div>
          <div class="hp-stat-value" [style.color]="s.netOperatingResult >= 0 ? '#0f2540' : '#b91c1c'">
            {{ s.netOperatingResult | number:'1.2-2' }}
          </div>
          <div class="hp-stat-foot">
            {{ isAr() ? 'هامش الربح:' : 'Margin:' }} {{ profitMarginPercent() | number:'1.1-1' }}%
          </div>
        </a>

        <a routerLink="/rooms" class="hp-card hp-stat-card">
          <div class="hp-stat-top">
            <span class="hp-stat-label">{{ isAr() ? 'نسبة الإشغال الحية' : 'Live Occupancy Rate' }}</span>
            <span class="hp-stat-icon" style="background:rgba(15,37,64,0.1);color:#0f2540;"><mat-icon>hotel</mat-icon></span>
          </div>
          <div class="hp-stat-value" style="color:#c9a227;">{{ s.occupancyRatePercent | number:'1.1-1' }}%</div>
          <div class="hp-stat-foot">
            {{ s.occupiedRooms }} / {{ s.totalRooms - s.outOfServiceRooms }}
            {{ isAr() ? 'غرفة صالحة للخدمة' : 'serviceable rooms' }}
          </div>
        </a>
      </div>

      <!-- Row 2: Operational Quick Metrics -->
      <div class="hp-ops-kpis">
        <a routerLink="/guests" class="hp-card hp-mini-kpi">
          <mat-icon class="hp-mini-icon" style="color:#0f2540;">groups</mat-icon>
          <div>
            <div class="hp-mini-val">{{ s.currentInHouseGuests }}</div>
            <div class="hp-mini-lbl">{{ isAr() ? 'النزلاء المقيمون' : 'In-House Guests' }}</div>
          </div>
        </a>

        <a routerLink="/front-desk" class="hp-card hp-mini-kpi">
          <mat-icon class="hp-mini-icon" style="color:#15803d;">login</mat-icon>
          <div>
            <div class="hp-mini-val">{{ s.expectedArrivalsToday }}</div>
            <div class="hp-mini-lbl">{{ isAr() ? 'وصول متوقع اليوم' : 'Arrivals Today' }}</div>
          </div>
        </a>

        <a routerLink="/front-desk" class="hp-card hp-mini-kpi">
          <mat-icon class="hp-mini-icon" style="color:#1d4ed8;">logout</mat-icon>
          <div>
            <div class="hp-mini-val">{{ s.expectedDeparturesToday }}</div>
            <div class="hp-mini-lbl">{{ isAr() ? 'مغادرة متوقعة اليوم' : 'Departures Today' }}</div>
          </div>
        </a>

        <a routerLink="/housekeeping" class="hp-card hp-mini-kpi">
          <mat-icon class="hp-mini-icon" style="color:#b45309;">cleaning_services</mat-icon>
          <div>
            <div class="hp-mini-val">{{ s.pendingHousekeepingTasks }}</div>
            <div class="hp-mini-lbl">{{ isAr() ? 'مهام نظافة معلقة' : 'Pending HK Tasks' }}</div>
          </div>
        </a>

        <a routerLink="/maintenance" class="hp-card hp-mini-kpi">
          <mat-icon class="hp-mini-icon" style="color:#7e22ce;">build</mat-icon>
          <div>
            <div class="hp-mini-val">{{ s.openMaintenanceRequests }}</div>
            <div class="hp-mini-lbl">{{ isAr() ? 'طلبات صيانة مفتوحة' : 'Open Maintenance' }}</div>
          </div>
        </a>

        <a routerLink="/inventory" class="hp-card hp-mini-kpi">
          <mat-icon class="hp-mini-icon" style="color:#b91c1c;">warning_amber</mat-icon>
          <div>
            <div class="hp-mini-val" [style.color]="s.lowStockItemsCount > 0 ? '#b91c1c' : '#0f2540'">{{ s.lowStockItemsCount }}</div>
            <div class="hp-mini-lbl">{{ isAr() ? 'تنبيهات نقص المخزون' : 'Low Stock Alerts' }}</div>
          </div>
        </a>
      </div>

      <!-- Row 3: Charts (Monthly Revenues vs Expenses + Room Occupancy Donut) -->
      <div class="hp-charts-grid-main">
        <!-- Chart 1: Monthly Revenues vs Expenses Comparison -->
        <div class="hp-card hp-chart-card">
          <div class="hp-chart-head">
            <div>
              <h3 class="hp-chart-title">
                {{ financeChartMode() === 'monthly'
                  ? (isAr() ? 'الإيرادات والمصروفات الشهرية' : 'Monthly Revenues vs Expenses')
                  : (isAr() ? 'مقارنة الإيرادات والمصروفات اليومية' : 'Daily Revenues vs Expenses') }}
              </h3>
              <p class="hp-chart-sub">
                {{ isAr() ? 'مقارنة الأداء المالي من سجلات الإيرادات والمصروفات' : 'Financial comparison from RevenueRecords & Expenses' }}
              </p>
            </div>
            <mat-button-toggle-group [value]="financeChartMode()" (change)="financeChartMode.set($event.value)" class="hp-toggle-sm">
              <mat-button-toggle value="monthly">{{ isAr() ? 'شهري' : 'Monthly' }}</mat-button-toggle>
              <mat-button-toggle value="daily">{{ isAr() ? 'يومي' : 'Daily' }}</mat-button-toggle>
            </mat-button-toggle-group>
          </div>

          @if (activeFinanceSeries().length === 0) {
            <div class="hp-empty-chart">
              <mat-icon>bar_chart</mat-icon>
              <span>{{ isAr() ? 'لا توجد حركات مالية في الفترة المحددة' : 'No financial records in selected range' }}</span>
            </div>
          } @else {
            <div class="hp-legend-row">
              <span class="hp-legend-item"><span class="hp-dot" style="background:#0f2540;"></span>{{ isAr() ? 'الإيرادات' : 'Revenues' }}</span>
              <span class="hp-legend-item"><span class="hp-dot" style="background:#c9a227;"></span>{{ isAr() ? 'المصروفات' : 'Expenses' }}</span>
            </div>

            <div class="hp-svg-wrap" dir="ltr">
              <svg viewBox="0 0 640 240" class="hp-bar-svg" preserveAspectRatio="xMidYMid meet">
                <!-- Gridlines -->
                @for (tick of [0, 0.25, 0.5, 0.75, 1]; track tick) {
                  <line x1="48" [attr.y1]="200 - tick * 170" x2="625" [attr.y2]="200 - tick * 170" stroke="#e2e8f0" stroke-dasharray="3,3" />
                  <text x="42" [attr.y]="204 - tick * 170" text-anchor="end" font-size="10" fill="#64748b">
                    {{ (maxFinanceVal() * tick) | number:'1.0-0' }}
                  </text>
                }

                <!-- Bars -->
                @for (pt of activeFinanceSeries(); track pt.label; let idx = $index) {
                  <g class="hp-bar-group"
                     (mouseenter)="hoveredFinance.set(pt)"
                     (mouseleave)="hoveredFinance.set(null)">
                    <!-- Revenue Bar -->
                    <rect
                      [attr.x]="barX(idx, activeFinanceSeries().length, 0)"
                      [attr.y]="barY(pt.value)"
                      [attr.width]="barWidth(activeFinanceSeries().length)"
                      [attr.height]="barHeight(pt.value)"
                      rx="4"
                      fill="#0f2540"
                    />
                    <!-- Expense Bar -->
                    <rect
                      [attr.x]="barX(idx, activeFinanceSeries().length, 1)"
                      [attr.y]="barY(pt.secondaryValue)"
                      [attr.width]="barWidth(activeFinanceSeries().length)"
                      [attr.height]="barHeight(pt.secondaryValue)"
                      rx="4"
                      fill="#c9a227"
                    />
                    <!-- Label -->
                    <text
                      [attr.x]="groupCenterX(idx, activeFinanceSeries().length)"
                      y="222"
                      text-anchor="middle"
                      font-size="10"
                      fill="#475569">
                      {{ shortLabel(pt.label) }}
                    </text>
                  </g>
                }
              </svg>
            </div>

            @if (hoveredFinance(); as hf) {
              <div class="hp-hover-pill">
                <strong>{{ hf.label }}</strong> —
                <span style="color:#0f2540;">{{ isAr() ? 'إيراد:' : 'Rev:' }} {{ hf.value | number:'1.2-2' }}</span> |
                <span style="color:#a16207;">{{ isAr() ? 'مصروف:' : 'Exp:' }} {{ hf.secondaryValue | number:'1.2-2' }}</span> |
                <span [style.color]="(hf.value - hf.secondaryValue) >= 0 ? '#15803d' : '#b91c1c'">
                  {{ isAr() ? 'الصافي:' : 'Net:' }} {{ (hf.value - hf.secondaryValue) | number:'1.2-2' }}
                </span>
              </div>
            }
          }
        </div>

        <!-- Chart 2: Room Occupancy & Status Donut -->
        <div class="hp-card hp-chart-card">
          <div class="hp-chart-head">
            <div>
              <h3 class="hp-chart-title">{{ isAr() ? 'نسبة إشغال وحالات الغرف' : 'Room Occupancy & Statuses' }}</h3>
              <p class="hp-chart-sub">{{ isAr() ? 'توزيع حالات الغرف الحالية' : 'Live distribution of room statuses' }}</p>
            </div>
          </div>

          @if (s.totalRooms === 0) {
            <div class="hp-empty-chart">
              <mat-icon>meeting_room</mat-icon>
              <span>{{ isAr() ? 'لا توجد غرف مسجلة' : 'No rooms configured' }}</span>
            </div>
          } @else {
            <div class="hp-donut-layout">
              <div class="hp-donut-box">
                <svg viewBox="0 0 160 160" class="hp-donut-svg">
                  <circle cx="80" cy="80" r="58" fill="none" stroke="#f1f5f9" stroke-width="18" />
                  @for (seg of roomStatusSegments(); track seg.label) {
                    <circle
                      cx="80"
                      cy="80"
                      r="58"
                      fill="none"
                      [attr.stroke]="seg.color"
                      stroke-width="18"
                      [attr.stroke-dasharray]="seg.dashArray"
                      [attr.stroke-dashoffset]="seg.dashOffset"
                      transform="rotate(-90 80 80)"
                      style="cursor:pointer;transition:opacity 0.2s;"
                      [matTooltip]="seg.label + ': ' + seg.value + ' (' + (seg.percent | number:'1.0-1') + '%)'"
                    />
                  }
                </svg>
                <div class="hp-donut-center">
                  <div class="hp-donut-pct">{{ s.occupancyRatePercent | number:'1.0-1' }}%</div>
                  <div class="hp-donut-lbl">{{ isAr() ? 'إشغال' : 'Occupied' }}</div>
                </div>
              </div>

              <div class="hp-status-list">
                @for (seg of roomStatusSegments(); track seg.label) {
                  <div class="hp-status-row">
                    <div style="display:flex;align-items:center;gap:8px;">
                      <span class="hp-dot" [style.background]="seg.color"></span>
                      <span>{{ seg.label }}</span>
                    </div>
                    <strong>{{ seg.value }}</strong>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      </div>

      <!-- Row 4: Booking Statuses + Revenue vs Expense Visual + Room Type Performance -->
      <div class="hp-charts-grid-sub">
        <!-- Chart 3: Booking Status Breakdown -->
        <div class="hp-card hp-chart-card">
          <div class="hp-chart-head">
            <div>
              <h3 class="hp-chart-title">{{ isAr() ? 'حالات الحجوزات' : 'Reservation Statuses' }}</h3>
              <p class="hp-chart-sub">{{ isAr() ? 'توزيع الحجوزات خلال الفترة المحددة' : 'Reservations breakdown in selected period' }}</p>
            </div>
          </div>

          @if (totalBookingsInPeriod() === 0) {
            <div class="hp-empty-chart">
              <mat-icon>event_busy</mat-icon>
              <span>{{ isAr() ? 'لا توجد حجوزات في هذه الفترة' : 'No reservations in this period' }}</span>
            </div>
          } @else {
            <div class="hp-donut-layout">
              <div class="hp-donut-box">
                <svg viewBox="0 0 160 160" class="hp-donut-svg">
                  <circle cx="80" cy="80" r="58" fill="none" stroke="#f1f5f9" stroke-width="18" />
                  @for (seg of bookingStatusSegments(); track seg.label) {
                    <circle
                      cx="80"
                      cy="80"
                      r="58"
                      fill="none"
                      [attr.stroke]="seg.color"
                      stroke-width="18"
                      [attr.stroke-dasharray]="seg.dashArray"
                      [attr.stroke-dashoffset]="seg.dashOffset"
                      transform="rotate(-90 80 80)"
                      [matTooltip]="translateBookingStatus(seg.label) + ': ' + seg.value"
                    />
                  }
                </svg>
                <div class="hp-donut-center">
                  <div class="hp-donut-pct">{{ totalBookingsInPeriod() }}</div>
                  <div class="hp-donut-lbl">{{ isAr() ? 'حجز' : 'Bookings' }}</div>
                </div>
              </div>

              <div class="hp-status-list">
                @for (seg of bookingStatusSegments(); track seg.label) {
                  <div class="hp-status-row">
                    <div style="display:flex;align-items:center;gap:8px;">
                      <span class="hp-dot" [style.background]="seg.color"></span>
                      <span>{{ translateBookingStatus(seg.label) }}</span>
                    </div>
                    <div style="display:flex;align-items:center;gap:8px;">
                      <span style="font-size:0.75rem;color:#64748b;">{{ seg.secondaryValue | number:'1.0-0' }}</span>
                      <strong>{{ seg.value }}</strong>
                    </div>
                  </div>
                }
              </div>
            </div>
          }
        </div>

        <!-- Chart 4: Revenue vs Expense Ratio Comparison -->
        <div class="hp-card hp-chart-card">
          <div class="hp-chart-head">
            <div>
              <h3 class="hp-chart-title">{{ isAr() ? 'مقارنة الإيرادات بالمصروفات' : 'Revenue vs Expense Summary' }}</h3>
              <p class="hp-chart-sub">{{ isAr() ? 'ملخص التدفق التشغيلي للفترة' : 'Period operating ratio & net result' }}</p>
            </div>
          </div>

          @if (s.totalRevenues === 0 && s.totalExpenses === 0) {
            <div class="hp-empty-chart">
              <mat-icon>account_balance</mat-icon>
              <span>{{ isAr() ? 'لا توجد بيانات مالية لهذه الفترة' : 'No financial data for this period' }}</span>
            </div>
          } @else {
            <div class="hp-ratio-bars">
              <div class="hp-ratio-item">
                <div class="hp-ratio-head">
                  <span>{{ isAr() ? 'الإيرادات' : 'Revenues' }}</span>
                  <strong style="color:#15803d;">{{ s.totalRevenues | number:'1.2-2' }}</strong>
                </div>
                <div class="hp-track">
                  <div class="hp-fill" style="background:#15803d;" [style.width.%]="revSharePercent()"></div>
                </div>
              </div>

              <div class="hp-ratio-item">
                <div class="hp-ratio-head">
                  <span>{{ isAr() ? 'المصروفات' : 'Expenses' }}</span>
                  <strong style="color:#b91c1c;">{{ s.totalExpenses | number:'1.2-2' }}</strong>
                </div>
                <div class="hp-track">
                  <div class="hp-fill" style="background:#b91c1c;" [style.width.%]="expSharePercent()"></div>
                </div>
              </div>

              <div class="hp-net-box" [style.border-color]="s.netOperatingResult >= 0 ? '#15803d' : '#b91c1c'">
                <span>{{ isAr() ? 'صافي النتيجة التشغيلية' : 'Net Operating Result' }}</span>
                <strong [style.color]="s.netOperatingResult >= 0 ? '#15803d' : '#b91c1c'">
                  {{ s.netOperatingResult | number:'1.2-2' }}
                </strong>
              </div>
            </div>
          }
        </div>
      </div>

      <!-- Row 5: Room Type Performance Table & Bars -->
      <div class="hp-card hp-chart-card" style="margin-top:18px;">
        <div class="hp-chart-head">
          <div>
            <h3 class="hp-chart-title">{{ isAr() ? 'أداء أنواع الغرف' : 'Room Type Performance' }}</h3>
            <p class="hp-chart-sub">
              {{ isAr() ? 'الإشغال، عدد الحجوزات، والإيرادات المحققة لكل نوع غرفة' : 'Live occupancy, bookings count, and revenue per room type' }}
            </p>
          </div>
          <a mat-stroked-button routerLink="/room-types">
            <mat-icon>hotel_class</mat-icon>
            <span>{{ isAr() ? 'إدارة أنواع الغرف' : 'Manage Room Types' }}</span>
          </a>
        </div>

        @if (!s.roomTypePerformance || s.roomTypePerformance.length === 0) {
          <div class="hp-empty-chart">
            <mat-icon>king_bed</mat-icon>
            <span>{{ isAr() ? 'لا توجد أنواع غرف مسجلة' : 'No room type performance data' }}</span>
          </div>
        } @else {
          <div class="hp-table-wrap">
            <table class="hp-perf-table">
              <thead>
                <tr>
                  <th>{{ isAr() ? 'نوع الغرفة' : 'Room Type' }}</th>
                  <th>{{ isAr() ? 'إجمالي الغرف' : 'Rooms' }}</th>
                  <th>{{ isAr() ? 'مشغولة الآن' : 'Occupied' }}</th>
                  <th>{{ isAr() ? 'نسبة الإشغال' : 'Occupancy %' }}</th>
                  <th>{{ isAr() ? 'الحجوزات' : 'Bookings' }}</th>
                  <th>{{ isAr() ? 'الإيرادات' : 'Revenue' }}</th>
                </tr>
              </thead>
              <tbody>
                @for (rt of s.roomTypePerformance; track rt.roomTypeName) {
                  <tr>
                    <td><strong>{{ rt.roomTypeName }}</strong></td>
                    <td>{{ rt.totalRooms }}</td>
                    <td>{{ rt.occupiedRooms }}</td>
                    <td style="min-width:140px;">
                      <div style="display:flex;align-items:center;gap:8px;">
                        <div class="hp-track" style="flex:1;height:8px;">
                          <div class="hp-fill" style="background:#c9a227;" [style.width.%]="rt.occupancyPercent"></div>
                        </div>
                        <span style="font-size:0.8rem;font-weight:700;color:#0f2540;">{{ rt.occupancyPercent | number:'1.0-1' }}%</span>
                      </div>
                    </td>
                    <td><span class="hp-badge hp-badge-gold">{{ rt.bookingsCount }}</span></td>
                    <td><strong style="color:#15803d;">{{ rt.revenue | number:'1.2-2' }}</strong></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>

      <!-- Row 6: Bookings by Source & Top Guest Services -->
      <div class="hp-charts-grid-sub" style="margin-top:18px;">
        <div class="hp-card hp-chart-card">
          <h3 class="hp-chart-title" style="margin-bottom:12px;">{{ isAr() ? 'مصادر الحجوزات' : 'Bookings by Source' }}</h3>
          @if (s.bookingsBySource.length === 0) {
            <div class="hp-empty-chart">
              <mat-icon>travel_explore</mat-icon>
              <span>{{ isAr() ? 'لا توجد بيانات مصادر حجوزات' : 'No booking source data' }}</span>
            </div>
          } @else {
            @for (item of s.bookingsBySource; track item.label) {
              <div class="hp-status-row">
                <span>{{ item.label }}</span>
                <span class="hp-badge hp-badge-gold">{{ item.value }}</span>
              </div>
            }
          }
        </div>

        <div class="hp-card hp-chart-card">
          <h3 class="hp-chart-title" style="margin-bottom:12px;">{{ isAr() ? 'أكثر الخدمات طلباً' : 'Top Guest Services' }}</h3>
          @if (s.topServices.length === 0) {
            <div class="hp-empty-chart">
              <mat-icon>room_service</mat-icon>
              <span>{{ isAr() ? 'لا توجد طلبات خدمات مسجلة' : 'No service orders recorded' }}</span>
            </div>
          } @else {
            @for (srv of s.topServices; track srv.label) {
              <div class="hp-status-row">
                <span>{{ srv.label }}</span>
                <div style="display:flex;gap:10px;align-items:center;">
                  <span style="font-size:0.8rem;color:#64748b;">{{ srv.value }}x</span>
                  <strong style="color:#0f2540;">{{ srv.secondaryValue | number:'1.2-2' }}</strong>
                </div>
              </div>
            }
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .hp-dash-header { display: flex; justify-content: space-between; align-items: flex-end; gap: 16px; flex-wrap: wrap; margin-bottom: 20px; }
    .hp-filter-bar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
    .hp-presets { display: flex; gap: 6px; flex-wrap: wrap; }
    .hp-preset-active { background: #0f2540 !important; color: #fff !important; border-color: #0f2540 !important; }
    .hp-date-inputs { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
    .hp-date-field { width: 145px; }
    .hp-error-state { padding: 16px 20px; display: flex; align-items: center; gap: 14px; justify-content: space-between; flex-wrap: wrap; border: 1px solid #fecaca; background: #fef2f2; margin-bottom: 18px; }

    .hp-hero-kpis { display: grid; grid-template-columns: repeat(auto-fit, minmax(230px, 1fr)); gap: 16px; }
    .hp-stat-card { padding: 18px; text-decoration: none; color: inherit; display: flex; flex-direction: column; gap: 8px; transition: transform 0.16s ease, box-shadow 0.16s ease; border-top: 3px solid transparent; }
    .hp-stat-card:hover { transform: translateY(-2px); box-shadow: 0 10px 24px rgba(15,37,64,0.1); }
    .hp-stat-navy { border-top-color: #0f2540; }
    .hp-stat-gold { border-top-color: #c9a227; }
    .hp-stat-top { display: flex; justify-content: space-between; align-items: center; }
    .hp-stat-label { font-size: 0.85rem; color: #64748b; font-weight: 600; }
    .hp-stat-icon { width: 38px; height: 38px; border-radius: 10px; display: inline-flex; align-items: center; justify-content: center; }
    .hp-stat-value { font-size: 1.65rem; font-weight: 800; color: #0f2540; line-height: 1.1; }
    .hp-stat-foot { font-size: 0.78rem; color: #64748b; }

    .hp-ops-kpis { display: grid; grid-template-columns: repeat(auto-fit, minmax(175px, 1fr)); gap: 12px; margin-top: 14px; }
    .hp-mini-kpi { padding: 14px; text-decoration: none; color: inherit; display: flex; align-items: center; gap: 12px; transition: transform 0.15s ease; }
    .hp-mini-kpi:hover { transform: translateY(-2px); }
    .hp-mini-icon { font-size: 28px; width: 28px; height: 28px; }
    .hp-mini-val { font-size: 1.25rem; font-weight: 800; color: #0f2540; }
    .hp-mini-lbl { font-size: 0.78rem; color: #64748b; }

    .hp-charts-grid-main { display: grid; grid-template-columns: 1.55fr 1fr; gap: 18px; margin-top: 18px; }
    .hp-charts-grid-sub { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 18px; margin-top: 18px; }
    @media (max-width: 1024px) { .hp-charts-grid-main { grid-template-columns: 1fr; } }

    .hp-chart-card { padding: 20px; }
    .hp-chart-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; flex-wrap: wrap; margin-bottom: 14px; }
    .hp-chart-title { margin: 0; font-size: 1.05rem; font-weight: 700; color: #0f2540; }
    .hp-chart-sub { margin: 2px 0 0; font-size: 0.8rem; color: #64748b; }

    .hp-legend-row { display: flex; gap: 16px; margin-bottom: 8px; font-size: 0.8rem; color: #475569; }
    .hp-legend-item { display: inline-flex; align-items: center; gap: 6px; }
    .hp-dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; flex-shrink: 0; }

    .hp-svg-wrap { width: 100%; overflow-x: auto; }
    .hp-bar-svg { width: 100%; min-width: 420px; height: auto; display: block; }
    .hp-bar-group { cursor: pointer; transition: opacity 0.15s; }
    .hp-bar-group:hover { opacity: 0.82; }
    .hp-hover-pill { margin-top: 8px; padding: 8px 12px; border-radius: 8px; background: #f8fafc; border: 1px solid #e2e8f0; font-size: 0.82rem; display: inline-block; }

    .hp-donut-layout { display: flex; align-items: center; gap: 20px; flex-wrap: wrap; justify-content: center; }
    .hp-donut-box { position: relative; width: 160px; height: 160px; flex-shrink: 0; }
    .hp-donut-svg { width: 160px; height: 160px; }
    .hp-donut-center { position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; pointer-events: none; }
    .hp-donut-pct { font-size: 1.35rem; font-weight: 800; color: #0f2540; }
    .hp-donut-lbl { font-size: 0.75rem; color: #64748b; }
    .hp-status-list { flex: 1; min-width: 170px; }
    .hp-status-row { display: flex; justify-content: space-between; align-items: center; padding: 7px 0; border-bottom: 1px solid #f1f5f9; font-size: 0.86rem; }
    .hp-status-row:last-child { border-bottom: none; }

    .hp-ratio-bars { display: flex; flex-direction: column; gap: 16px; margin-top: 8px; }
    .hp-ratio-head { display: flex; justify-content: space-between; margin-bottom: 6px; font-size: 0.88rem; }
    .hp-track { height: 10px; background: #e2e8f0; border-radius: 999px; overflow: hidden; }
    .hp-fill { height: 100%; border-radius: 999px; transition: width 0.3s ease; }
    .hp-net-box { margin-top: 6px; padding: 12px 14px; border-radius: 10px; background: #f8fafc; border-inline-start: 4px solid; display: flex; justify-content: space-between; align-items: center; }

    .hp-table-wrap { overflow-x: auto; }
    .hp-perf-table { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
    .hp-perf-table th { text-align: start; padding: 10px 12px; background: #f8fafc; color: #475569; font-weight: 700; border-bottom: 1px solid #e2e8f0; }
    .hp-perf-table td { padding: 11px 12px; border-bottom: 1px solid #f1f5f9; }
    .hp-empty-chart { padding: 36px 16px; text-align: center; color: #94a3b8; display: flex; flex-direction: column; align-items: center; gap: 8px; font-size: 0.88rem; }
  `]
})
export class DashboardComponent implements OnInit {
  private readonly service = inject(DashboardReportsService);
  readonly i18n = inject(TranslationService);

  readonly summary = signal<DashboardSummaryDto | null>(null);
  readonly loading = signal(false);
  readonly errorMsg = signal<string | null>(null);
  readonly activePreset = signal<'today' | '7d' | 'month' | '6m' | 'custom'>('month');
  readonly financeChartMode = signal<'monthly' | 'daily'>('monthly');
  readonly hoveredFinance = signal<ChartSeriesPointDto | null>(null);

  readonly isAr = computed(() => this.i18n.lang() === 'ar');

  fromDate = this.formatDate(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  toDate = this.formatDate(new Date());

  readonly profitMarginPercent = computed(() => {
    const s = this.summary();
    if (!s || s.totalRevenues <= 0) return 0;
    return (s.netOperatingResult / s.totalRevenues) * 100;
  });

  readonly activeFinanceSeries = computed(() => {
    const s = this.summary();
    if (!s) return [];
    return this.financeChartMode() === 'monthly'
      ? (s.monthlyRevenueSeries ?? [])
      : (s.revenueVsExpenseSeries ?? []);
  });

  readonly maxFinanceVal = computed(() => {
    const list = this.activeFinanceSeries();
    const max = list.reduce((acc, p) => Math.max(acc, p.value, p.secondaryValue), 0);
    return max > 0 ? max : 100;
  });

  readonly revSharePercent = computed(() => {
    const s = this.summary();
    if (!s) return 0;
    const max = Math.max(s.totalRevenues, s.totalExpenses, 1);
    return (s.totalRevenues / max) * 100;
  });

  readonly expSharePercent = computed(() => {
    const s = this.summary();
    if (!s) return 0;
    const max = Math.max(s.totalRevenues, s.totalExpenses, 1);
    return (s.totalExpenses / max) * 100;
  });

  readonly roomStatusSegments = computed<DonutSegment[]>(() => {
    const s = this.summary();
    if (!s) return [];
    const ar = this.isAr();
    const items = [
      { label: ar ? 'متاحة' : 'Available', value: s.availableRooms, secondaryValue: 0, color: '#15803d' },
      { label: ar ? 'مشغولة' : 'Occupied', value: s.occupiedRooms, secondaryValue: 0, color: '#b91c1c' },
      { label: ar ? 'محجوزة' : 'Reserved', value: s.reservedRooms, secondaryValue: 0, color: '#1d4ed8' },
      { label: ar ? 'تنظيف' : 'Cleaning', value: s.cleaningRooms, secondaryValue: 0, color: '#d97706' },
      { label: ar ? 'صيانة' : 'Maintenance', value: s.maintenanceRooms, secondaryValue: 0, color: '#7e22ce' },
      { label: ar ? 'خارج الخدمة' : 'Out of Service', value: s.outOfServiceRooms, secondaryValue: 0, color: '#64748b' }
    ].filter((i) => i.value > 0);
    return this.buildDonut(items);
  });

  readonly bookingStatusSegments = computed<DonutSegment[]>(() => {
    const s = this.summary();
    if (!s || !s.bookingsByStatus) return [];
    const colorMap: Record<string, string> = {
      Pending: '#d97706',
      Confirmed: '#1d4ed8',
      CheckedIn: '#15803d',
      CheckedOut: '#0f2540',
      Cancelled: '#b91c1c',
      NoShow: '#64748b'
    };
    const items = s.bookingsByStatus
      .filter((b) => b.value > 0)
      .map((b) => ({
        label: b.label,
        value: b.value,
        secondaryValue: b.secondaryValue,
        color: colorMap[b.label] ?? '#0f2540'
      }));
    return this.buildDonut(items);
  });

  readonly totalBookingsInPeriod = computed(() =>
    (this.summary()?.bookingsByStatus ?? []).reduce((sum, b) => sum + b.value, 0)
  );

  ngOnInit(): void {
    this.load();
  }

  applyPreset(preset: 'today' | '7d' | 'month' | '6m'): void {
    this.activePreset.set(preset);
    const now = new Date();
    const end = new Date(now);
    let start = new Date(now);

    if (preset === '7d') {
      start.setDate(now.getDate() - 6);
    } else if (preset === 'month') {
      start = new Date(now.getFullYear(), now.getMonth(), 1);
    } else if (preset === '6m') {
      start = new Date(now.getFullYear(), now.getMonth() - 5, 1);
    }

    this.fromDate = this.formatDate(start);
    this.toDate = this.formatDate(end);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMsg.set(null);
    this.service.getDashboardSummary(this.fromDate, this.toDate).subscribe({
      next: (res) => {
        this.summary.set(res);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMsg.set(err?.error?.detail ?? err?.message ?? 'Server error');
        this.loading.set(false);
      }
    });
  }

  translateBookingStatus(status: string): string {
    if (!this.isAr()) return status;
    const map: Record<string, string> = {
      Pending: 'قيد الانتظار',
      Confirmed: 'مؤكد',
      CheckedIn: 'مسجل دخول',
      CheckedOut: 'مغادر',
      Cancelled: 'ملغي',
      NoShow: 'لم يحضر'
    };
    return map[status] ?? status;
  }

  barWidth(count: number): number {
    const slot = 570 / Math.max(count, 1);
    return Math.min(24, Math.max(6, slot * 0.34));
  }

  barX(index: number, count: number, subIdx: number): number {
    const slot = 570 / Math.max(count, 1);
    const w = this.barWidth(count);
    const center = 52 + index * slot + slot / 2;
    return subIdx === 0 ? center - w - 2 : center + 2;
  }

  groupCenterX(index: number, count: number): number {
    const slot = 570 / Math.max(count, 1);
    return 52 + index * slot + slot / 2;
  }

  barHeight(val: number): number {
    return (val / this.maxFinanceVal()) * 170;
  }

  barY(val: number): number {
    return 200 - this.barHeight(val);
  }

  shortLabel(label: string): string {
    return label.length > 7 ? label.slice(5) : label;
  }

  private buildDonut(items: { label: string; value: number; secondaryValue: number; color: string }[]): DonutSegment[] {
    const total = items.reduce((sum, i) => sum + i.value, 0);
    if (total <= 0) return [];
    const circumference = 2 * Math.PI * 58;
    let offset = 0;
    return items.map((item) => {
      const pct = (item.value / total) * 100;
      const len = (item.value / total) * circumference;
      const seg: DonutSegment = {
        ...item,
        percent: pct,
        dashArray: `${len} ${circumference - len}`,
        dashOffset: -offset
      };
      offset += len;
      return seg;
    });
  }

  private formatDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
