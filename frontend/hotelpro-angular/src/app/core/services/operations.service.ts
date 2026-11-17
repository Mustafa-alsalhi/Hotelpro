import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.model';
import { CheckInRequest, CheckOutRequest, CheckOutSummaryDto } from '../models/check-in-out.model';
import { GuestServiceDto, ServiceOrderDto, ServiceOrderStatus } from '../models/guest-service.model';
import { HousekeepingStatus, HousekeepingTaskDto } from '../models/housekeeping.model';
import { MaintenanceRequestDto, MaintenanceStatus } from '../models/maintenance.model';

@Injectable({ providedIn: 'root' })
export class OperationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  checkIn(payload: CheckInRequest): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/front-desk/check-in`, payload);
  }

  getCheckOutSummary(reservationId: string): Observable<CheckOutSummaryDto> {
    return this.http.get<CheckOutSummaryDto>(`${this.baseUrl}/front-desk/check-out/${reservationId}/summary`);
  }

  checkOut(payload: CheckOutRequest): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/front-desk/check-out`, payload);
  }

  getServiceCatalog(includeInactive = true): Observable<GuestServiceDto[]> {
    return this.http.get<GuestServiceDto[]>(`${this.baseUrl}/guest-services/catalog`, {
      params: new HttpParams().set('includeInactive', includeInactive)
    });
  }

  createService(payload: unknown): Observable<GuestServiceDto> {
    return this.http.post<GuestServiceDto>(`${this.baseUrl}/guest-services/catalog`, payload);
  }

  getServiceOrders(pageNumber = 1, pageSize = 20, status?: ServiceOrderStatus | null): Observable<PagedResult<ServiceOrderDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<ServiceOrderDto>>(`${this.baseUrl}/guest-services/orders`, { params });
  }

  createServiceOrder(payload: unknown): Observable<ServiceOrderDto> {
    return this.http.post<ServiceOrderDto>(`${this.baseUrl}/guest-services/orders`, payload);
  }

  updateServiceOrderStatus(id: string, status: ServiceOrderStatus): Observable<ServiceOrderDto> {
    return this.http.patch<ServiceOrderDto>(`${this.baseUrl}/guest-services/orders/${id}/status`, { status });
  }

  getHousekeepingTasks(pageNumber = 1, pageSize = 25, status?: HousekeepingStatus | null): Observable<PagedResult<HousekeepingTaskDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<HousekeepingTaskDto>>(`${this.baseUrl}/housekeeping`, { params });
  }

  createHousekeepingTask(payload: unknown): Observable<HousekeepingTaskDto> {
    return this.http.post<HousekeepingTaskDto>(`${this.baseUrl}/housekeeping`, payload);
  }

  updateHousekeepingTask(id: string, payload: unknown): Observable<HousekeepingTaskDto> {
    return this.http.put<HousekeepingTaskDto>(`${this.baseUrl}/housekeeping/${id}`, payload);
  }

  getMaintenanceRequests(pageNumber = 1, pageSize = 25, status?: MaintenanceStatus | null): Observable<PagedResult<MaintenanceRequestDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<MaintenanceRequestDto>>(`${this.baseUrl}/maintenance`, { params });
  }

  createMaintenanceRequest(payload: unknown): Observable<MaintenanceRequestDto> {
    return this.http.post<MaintenanceRequestDto>(`${this.baseUrl}/maintenance`, payload);
  }

  updateMaintenanceRequest(id: string, payload: unknown): Observable<MaintenanceRequestDto> {
    return this.http.put<MaintenanceRequestDto>(`${this.baseUrl}/maintenance/${id}`, payload);
  }
}

