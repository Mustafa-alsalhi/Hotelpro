import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.model';
import { HotelSettingsDto } from '../models/hotel-settings.model';
import { AmenityDto, RoomTypeDto } from '../models/room-type.model';
import { RoomDto, RoomStatus } from '../models/room.model';
import { GuestDto } from '../models/guest.model';

@Injectable({ providedIn: 'root' })
export class HotelRoomsGuestsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  readonly settings = signal<HotelSettingsDto | null>(null);

  getHotelSettings(): Observable<HotelSettingsDto> {
    return this.http.get<HotelSettingsDto>(`${this.baseUrl}/hotel-settings`).pipe(
      tap((s) => this.settings.set(s))
    );
  }

  updateHotelSettings(payload: Omit<HotelSettingsDto, 'hotelId'>): Observable<HotelSettingsDto> {
    return this.http.put<HotelSettingsDto>(`${this.baseUrl}/hotel-settings`, payload).pipe(
      tap((s) => this.settings.set(s))
    );
  }

  getAmenities(): Observable<AmenityDto[]> {
    return this.http.get<AmenityDto[]>(`${this.baseUrl}/amenities`);
  }

  createAmenity(name: string, icon: string, category: string): Observable<AmenityDto> {
    return this.http.post<AmenityDto>(`${this.baseUrl}/amenities`, { name, icon, category });
  }

  getRoomTypes(includeInactive = true): Observable<RoomTypeDto[]> {
    return this.http.get<RoomTypeDto[]>(`${this.baseUrl}/room-types`, {
      params: new HttpParams().set('includeInactive', includeInactive)
    });
  }

  createRoomType(payload: unknown): Observable<RoomTypeDto> {
    return this.http.post<RoomTypeDto>(`${this.baseUrl}/room-types`, payload);
  }

  updateRoomType(id: string, payload: unknown): Observable<RoomTypeDto> {
    return this.http.put<RoomTypeDto>(`${this.baseUrl}/room-types/${id}`, payload);
  }

  deleteRoomType(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/room-types/${id}`);
  }

  getRooms(
    pageNumber = 1,
    pageSize = 25,
    search = '',
    floor?: number | null,
    roomTypeId = '',
    status?: RoomStatus | null
  ): Observable<PagedResult<RoomDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (floor !== null && floor !== undefined) params = params.set('floor', floor);
    if (roomTypeId) params = params.set('roomTypeId', roomTypeId);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<RoomDto>>(`${this.baseUrl}/rooms`, { params });
  }

  createRoom(payload: unknown): Observable<RoomDto> {
    return this.http.post<RoomDto>(`${this.baseUrl}/rooms`, payload);
  }

  updateRoom(id: string, payload: unknown): Observable<RoomDto> {
    return this.http.put<RoomDto>(`${this.baseUrl}/rooms/${id}`, payload);
  }

  changeRoomStatus(id: string, status: RoomStatus, reason?: string): Observable<RoomDto> {
    return this.http.patch<RoomDto>(`${this.baseUrl}/rooms/${id}/status`, { status, reason });
  }

  getGuests(pageNumber = 1, pageSize = 15, search = '', isVip?: boolean | null): Observable<PagedResult<GuestDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (isVip !== null && isVip !== undefined) params = params.set('isVip', isVip);
    return this.http.get<PagedResult<GuestDto>>(`${this.baseUrl}/guests`, { params });
  }

  getGuestById(id: string): Observable<GuestDto> {
    return this.http.get<GuestDto>(`${this.baseUrl}/guests/${id}`);
  }

  createGuest(payload: unknown): Observable<GuestDto> {
    return this.http.post<GuestDto>(`${this.baseUrl}/guests`, payload);
  }

  updateGuest(id: string, payload: unknown): Observable<GuestDto> {
    return this.http.put<GuestDto>(`${this.baseUrl}/guests/${id}`, payload);
  }

  deleteGuest(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/guests/${id}`);
  }
}

