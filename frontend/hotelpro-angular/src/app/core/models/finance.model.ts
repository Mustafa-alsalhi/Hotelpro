import { PaymentMethod } from './payment.model';

export interface ExpenseCategoryDto {
  id: string;
  name: string;
  description?: string;
}

export interface ExpenseDto {
  id: string;
  expenseNumber: string;
  categoryId: string;
  categoryName: string;
  description: string;
  amount: number;
  expenseDate: string;
  paymentMethod: PaymentMethod;
  notes?: string;
}

export interface RevenueRecordDto {
  id: string;
  source: number;
  referenceId?: string;
  description: string;
  amount: number;
  recognizedDate: string;
}

export interface FinancialSummaryDto {
  from: string;
  to: string;
  invoicedTotal: number;
  collectedPaymentsTotal: number;
  recognizedRevenuesTotal: number;
  totalExpenses: number;
  netOperatingResult: number;
}

