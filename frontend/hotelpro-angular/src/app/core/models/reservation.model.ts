export type BookingSource = 1 | 2 | 3 | 4 | 5 | 6;
export type ReservationStatus = 1 | 2 | 3 | 4 | 5 | 6;

export const RESERVATION_STATUS_LABELS: Record<number, { en: string; ar: string; css: string }> = {
  1: { en: 'Pending', ar: 'قيد الانتظار', css: 'hp-badge-gold' },
  2: { en: 'Confirmed', ar: 'مؤكد', css: 'hp-res-confirmed' },
  3: { en: 'Checked In', ar: 'تم الوصول', css: 'hp-badge-success' },
  4: { en: 'Checked Out', ar: 'تم المغادرة', css: 'hp-res-checkedout' },
  5: { en: 'Cancelled', ar: 'ملغى', css: 'hp-badge-danger' },
  6: { en: 'No Show', ar: 'لم يحضر', css: 'hp-badge-danger' }
};

export interface ReservationRoomDto {
  id: string;
  roomId: string;
  roomNumber: string;
  roomTypeId: string;
  roomTypeName: string;
  agreedNightlyRate: number;
  nights: number;
  totalRoomAmount: number;
  checkInDate: string;
  checkOutDate: string;
}

export interface ReservationDto {
  id: string;
  reservationNumber: string;
  primaryGuestId: string;
  primaryGuestName: string;
  primaryGuestPhone: string;
  ratePlanId: string;
  ratePlanName: string;
  checkInDate: string;
  checkOutDate: string;
  nights: number;
  adultsCount: number;
  childrenCount: number;
  source: BookingSource;
  status: ReservationStatus;
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  serviceChargeAmount: number;
  totalAmount: number;
  depositRequired: number;
  notes?: string;
  cancellationReason?: string;
  createdAt: string;
  rooms: ReservationRoomDto[];
}

export interface CreateReservationRequest {
  primaryGuestId: string;
  ratePlanId: string;
  checkInDate: string;
  checkOutDate: string;
  adultsCount: number;
  childrenCount: number;
  source: BookingSource;
  roomIds: string[];
  discountAmount: number;
  depositRequired: number;
  notes?: string;
  confirmImmediately: boolean;
}

