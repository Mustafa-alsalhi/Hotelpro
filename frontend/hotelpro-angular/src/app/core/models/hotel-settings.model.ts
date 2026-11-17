export interface HotelSettingsDto {
  hotelId: string;
  name: string;
  legalName?: string;
  logoUrl?: string;
  address: string;
  phone: string;
  email: string;
  taxNumber?: string;
  currencyCode: string;
  currencySymbol: string;
  timeZoneId: string;
  dateFormat: string;
  defaultLanguage: string;
  defaultCheckInTime: string;
  defaultCheckOutTime: string;
  taxRatePercent: number;
  serviceChargePercent: number;
  requireInspectionAfterCleaning: boolean;
  allowCheckoutWithBalancePermission: boolean;
  cancellationPolicyHours: number;
  cancellationFeePercent: number;
}

