import { ReservationStatus } from './reservation.model';
import { RoomStatus } from './room.model';

export interface CalendarBookingBlockDto {
  reservationId: string;
  reservationNumber: string;
  guestName: string;
  status: ReservationStatus;
  checkInDate: string;
  checkOutDate: string;
}

export interface CalendarRoomRowDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  roomTypeName: string;
  currentRoomStatus: RoomStatus;
  bookings: CalendarBookingBlockDto[];
}

export interface BookingCalendarDto {
  from: string;
  to: string;
  rooms: CalendarRoomRowDto[];
}

