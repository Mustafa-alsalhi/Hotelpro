export type PaymentMethod = 1 | 2 | 3 | 4;

export interface PaymentDto {
  id: string;
  receiptNumber: string;
  invoiceId: string;
  invoiceNumber: string;
  reservationId: string;
  amount: number;
  paymentMethod: PaymentMethod;
  referenceNumber?: string;
  paymentDate: string;
  isReversal: boolean;
  reversedPaymentId?: string;
  idempotencyKey: string;
  notes?: string;
}

export interface RecordPaymentRequest {
  invoiceId: string;
  amount: number;
  paymentMethod: PaymentMethod;
  referenceNumber?: string;
  idempotencyKey: string;
  notes?: string;
}

