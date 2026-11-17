import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RatePlanDto, RoomRateDto } from '../models/rate-plan.model';
import { RateQuoteDto, RateQuoteRequest } from '../models/rate-quote.model';

@Injectable({ providedIn: 'root' })
export class RateManagementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/rate-plans`;

  getRatePlans(includeInactive = true): Observable<RatePlanDto[]> {
    return this.http.get<RatePlanDto[]>(this.baseUrl, {
      params: new HttpParams().set('includeInactive', includeInactive)
    });
  }

  createRatePlan(payload: unknown): Observable<RatePlanDto> {
    return this.http.post<RatePlanDto>(this.baseUrl, payload);
  }

  updateRatePlan(id: string, payload: unknown): Observable<RatePlanDto> {
    return this.http.put<RatePlanDto>(`${this.baseUrl}/${id}`, payload);
  }

  getRoomRates(roomTypeId?: string, ratePlanId?: string): Observable<RoomRateDto[]> {
    let params = new HttpParams();
    if (roomTypeId) params = params.set('roomTypeId', roomTypeId);
    if (ratePlanId) params = params.set('ratePlanId', ratePlanId);
    return this.http.get<RoomRateDto[]>(`${this.baseUrl}/room-rates`, { params });
  }

  createRoomRate(payload: unknown): Observable<RoomRateDto> {
    return this.http.post<RoomRateDto>(`${this.baseUrl}/room-rates`, payload);
  }

  deleteRoomRate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/room-rates/${id}`);
  }

  calculateQuote(request: RateQuoteRequest): Observable<RateQuoteDto> {
    return this.http.post<RateQuoteDto>(`${this.baseUrl}/quote`, request);
  }
}

