export interface CheckInRequest {
  reservationId: string;
  actualAdults: number;
  actualChildren: number;
  documentsVerified: boolean;
  notes?: string;
}

export interface CheckOutSummaryDto {
  reservationId: string;
  reservationNumber: string;
  guestName: string;
  accommodationTotal: number;
  servicesTotal: number;
  discountAmount: number;
  taxAndServiceTotal: number;
  depositAmount: number;
  grandTotal: number;
  balanceDue: number;
}

export interface CheckOutRequest {
  reservationId: string;
  finalPaymentSettled: number;
  exceptionalBalanceApproved: boolean;
  notes?: string;
}

