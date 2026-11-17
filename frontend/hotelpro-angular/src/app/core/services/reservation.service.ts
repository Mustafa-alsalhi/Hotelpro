import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.model';
import {
  CreateReservationRequest,
  ReservationDto,
  ReservationStatus
} from '../models/reservation.model';
import { BookingCalendarDto } from '../models/booking-calendar.model';
import { RoomDto } from '../models/room.model';

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reservations`;

  getReservations(
    pageNumber = 1,
    pageSize = 15,
    search = '',
    status?: ReservationStatus | null,
    fromDate?: string,
    toDate?: string
  ): Observable<PagedResult<ReservationDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<PagedResult<ReservationDto>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<ReservationDto> {
    return this.http.get<ReservationDto>(`${this.baseUrl}/${id}`);
  }

  getAvailableRooms(checkIn: string, checkOut: string, roomTypeId?: string): Observable<RoomDto[]> {
    let params = new HttpParams().set('checkIn', checkIn).set('checkOut', checkOut);
    if (roomTypeId) params = params.set('roomTypeId', roomTypeId);
    return this.http.get<RoomDto[]>(`${this.baseUrl}/available-rooms`, { params });
  }

  getCalendar(from: string, to: string): Observable<BookingCalendarDto> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<BookingCalendarDto>(`${this.baseUrl}/calendar`, { params });
  }

  create(payload: CreateReservationRequest): Observable<ReservationDto> {
    return this.http.post<ReservationDto>(this.baseUrl, payload);
  }

  confirm(id: string): Observable<ReservationDto> {
    return this.http.post<ReservationDto>(`${this.baseUrl}/${id}/confirm`, {});
  }

  cancel(id: string, reason: string): Observable<ReservationDto> {
    return this.http.post<ReservationDto>(`${this.baseUrl}/${id}/cancel`, { reason });
  }

  updateStay(id: string, payload: unknown): Observable<ReservationDto> {
    return this.http.put<ReservationDto>(`${this.baseUrl}/${id}/stay`, payload);
  }

  changeRoom(id: string, currentRoomId: string, newRoomId: string, reason?: string): Observable<ReservationDto> {
    return this.http.post<ReservationDto>(`${this.baseUrl}/${id}/change-room`, {
      currentRoomId,
      newRoomId,
      reason
    });
  }
}

