import { AmenityDto } from './room-type.model';

export type RoomStatus = 1 | 2 | 3 | 4 | 5 | 6;

export const ROOM_STATUS_LABELS: Record<number, { en: string; ar: string; css: string }> = {
  1: { en: 'Available', ar: 'متاحة', css: 'hp-status-available' },
  2: { en: 'Reserved', ar: 'محجوزة', css: 'hp-status-reserved' },
  3: { en: 'Occupied', ar: 'مشغولة', css: 'hp-status-occupied' },
  4: { en: 'Cleaning', ar: 'قيد التنظيف', css: 'hp-status-cleaning' },
  5: { en: 'Maintenance', ar: 'تحت الصيانة', css: 'hp-status-maintenance' },
  6: { en: 'Out of Service', ar: 'خارج الخدمة', css: 'hp-status-oos' }
};

export interface RoomDto {
  id: string;
  roomTypeId: string;
  roomTypeName: string;
  roomNumber: string;
  floor: number;
  maxCapacity: number;
  bedCount: number;
  effectivePrice: number;
  basePriceOverride?: number;
  status: RoomStatus;
  description: string;
  notes?: string;
  imageUrls: string[];
  amenities: AmenityDto[];
}

