export type InvoiceStatus = 1 | 2 | 3 | 4 | 5;

export const INVOICE_STATUS_LABELS: Record<number, { en: string; ar: string; css: string }> = {
  1: { en: 'Draft', ar: 'مسودة', css: 'hp-badge-gold' },
  2: { en: 'Issued', ar: 'صادرة', css: 'hp-res-confirmed' },
  3: { en: 'Partially Paid', ar: 'مدفوعة جزئياً', css: 'hp-badge-gold' },
  4: { en: 'Paid', ar: 'مدفوعة', css: 'hp-badge-success' },
  5: { en: 'Cancelled', ar: 'ملغاة', css: 'hp-badge-danger' }
};

export interface InvoiceItemDto {
  id: string;
  itemType: number;
  description: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  reservationId: string;
  reservationNumber: string;
  guestId: string;
  guestName: string;
  issueDate: string;
  dueDate: string;
  accommodationTotal: number;
  servicesTotal: number;
  discountTotal: number;
  taxTotal: number;
  serviceChargeTotal: number;
  grandTotal: number;
  paidAmount: number;
  balanceDue: number;
  status: InvoiceStatus;
  cancellationReason?: string;
  items: InvoiceItemDto[];
}

