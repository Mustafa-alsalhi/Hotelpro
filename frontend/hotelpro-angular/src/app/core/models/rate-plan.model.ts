export interface RatePlanDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  isRefundable: boolean;
  weekendMultiplier: number;
  longStayMinNights: number;
  longStayDiscountPercent: number;
  corporateCode?: string;
  isActive: boolean;
}

export interface RoomRateDto {
  id: string;
  roomTypeId: string;
  roomTypeName: string;
  ratePlanId: string;
  ratePlanName: string;
  startDate: string;
  endDate: string;
  pricePerNight: number;
  priority: number;
}

