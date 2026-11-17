export interface AmenityDto {
  id: string;
  name: string;
  icon: string;
  category: string;
}

export interface RoomTypeDto {
  id: string;
  name: string;
  code: string;
  description: string;
  basePrice: number;
  maxAdults: number;
  maxChildren: number;
  bedCount: number;
  areaSqm: number;
  isActive: boolean;
  amenities: AmenityDto[];
}

