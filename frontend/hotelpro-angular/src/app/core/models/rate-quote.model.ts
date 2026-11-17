export interface RateQuoteRequest {
  roomTypeId: string;
  ratePlanId: string;
  checkInDate: string;
  checkOutDate: string;
  roomsCount: number;
  manualDiscountAmount: number;
}

export interface NightlyRateBreakdownDto {
  date: string;
  baseRate: number;
  appliedMultiplier: number;
  nightRate: number;
}

export interface RateQuoteDto {
  nights: number;
  averageNightlyRate: number;
  subTotal: number;
  longStayDiscount: number;
  manualDiscount: number;
  taxAmount: number;
  serviceChargeAmount: number;
  grandTotal: number;
  breakdown: NightlyRateBreakdownDto[];
}

