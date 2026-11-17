import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardSummaryDto } from '../models/dashboard.model';
import { OccupancyReportDto } from '../models/report.model';
import { NotificationDto } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class DashboardReportsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getDashboardSummary(from?: string, to?: string): Observable<DashboardSummaryDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<DashboardSummaryDto>(`${this.baseUrl}/dashboard/summary`, { params });
  }

  getOccupancyReport(from: string, to: string): Observable<OccupancyReportDto> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<OccupancyReportDto>(`${this.baseUrl}/reports/occupancy`, { params });
  }

  exportCsv(type: 'occupancy-csv' | 'reservations-csv' | 'financial-csv', from: string, to: string): Observable<Blob> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get(`${this.baseUrl}/reports/export/${type}`, { params, responseType: 'blob' });
  }

  getNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>(`${this.baseUrl}/notifications`);
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/notifications/unread-count`);
  }

  markAsRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/${id}/read`, {});
  }

  markAllAsRead(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/read-all`, {});
  }
}

