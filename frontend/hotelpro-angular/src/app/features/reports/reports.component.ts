import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DashboardReportsService } from '../../core/services/dashboard-reports.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { OccupancyReportDto } from '../../core/models/report.model';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [FormsModule, MatTableModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.reports') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'تقارير الإشغال والحجوزات والمالية مع تصدير CSV وطباعة PDF' : 'Operational & financial reports with CSV/Excel export & PDF print' }}</p>
      </div>
      <div style="display:flex;gap:8px;flex-wrap:wrap;">
        <button mat-stroked-button (click)="downloadCsv('occupancy-csv')"><mat-icon>download</mat-icon> Occupancy CSV</button>
        <button mat-stroked-button (click)="downloadCsv('reservations-csv')"><mat-icon>download</mat-icon> Reservations CSV</button>
        <button mat-stroked-button (click)="downloadCsv('financial-csv')"><mat-icon>download</mat-icon> Financial CSV</button>
        <button mat-flat-button class="hp-btn-primary" (click)="printReport()"><mat-icon>print</mat-icon> Print / PDF</button>
      </div>
    </div>

    <div class="hp-card" style="padding:16px;margin-bottom:18px;display:flex;gap:12px;align-items:center;flex-wrap:wrap;">
      <mat-form-field appearance="outline"><mat-label>From</mat-label><input matInput type="date" [(ngModel)]="fromDate" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>To</mat-label><input matInput type="date" [(ngModel)]="toDate" /></mat-form-field>
      <button mat-flat-button class="hp-btn-gold" (click)="load()">Generate Report</button>
      @if (report(); as r) {
        <span style="font-weight:700;color:#0f2540;margin-inline-start:auto;">
          Average Occupancy: {{ r.averageOccupancyPercent }}%
        </span>
      }
    </div>

    @if (report(); as r) {
      <div class="hp-card" style="padding:16px;overflow-x:auto;">
        <table mat-table [dataSource]="r.rows" style="width:100%;">
          <ng-container matColumnDef="date"><th mat-header-cell *matHeaderCellDef>Date</th><td mat-cell *matCellDef="let row">{{ row.date }}</td></ng-container>
          <ng-container matColumnDef="serviceable"><th mat-header-cell *matHeaderCellDef>Serviceable Rooms</th><td mat-cell *matCellDef="let row">{{ row.totalServiceableRooms }}</td></ng-container>
          <ng-container matColumnDef="occupied"><th mat-header-cell *matHeaderCellDef>Occupied</th><td mat-cell *matCellDef="let row">{{ row.occupiedRooms }}</td></ng-container>
          <ng-container matColumnDef="oos"><th mat-header-cell *matHeaderCellDef>Out of Service</th><td mat-cell *matCellDef="let row">{{ row.outOfServiceRooms }}</td></ng-container>
          <ng-container matColumnDef="rate"><th mat-header-cell *matHeaderCellDef>Occupancy %</th><td mat-cell *matCellDef="let row" style="font-weight:700;color:#0f2540;">{{ row.occupancyRatePercent }}%</td></ng-container>
          <tr mat-header-row *matHeaderRowDef="['date','serviceable','occupied','oos','rate']"></tr>
          <tr mat-row *matRowDef="let row; columns: ['date','serviceable','occupied','oos','rate'];"></tr>
        </table>
      </div>
    }
  `
})
export class ReportsComponent implements OnInit {
  private readonly service = inject(DashboardReportsService);
  readonly i18n = inject(TranslationService);

  readonly report = signal<OccupancyReportDto | null>(null);
  fromDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10);
  toDate = new Date().toISOString().slice(0, 10);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.service.getOccupancyReport(this.fromDate, this.toDate).subscribe((r) => this.report.set(r));
  }

  downloadCsv(type: 'occupancy-csv' | 'reservations-csv' | 'financial-csv'): void {
    this.service.exportCsv(type, this.fromDate, this.toDate).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${type}-${this.fromDate}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  printReport(): void {
    window.print();
  }
}

