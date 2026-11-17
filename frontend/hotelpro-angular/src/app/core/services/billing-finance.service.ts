import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.model';
import { InvoiceDto, InvoiceStatus } from '../models/invoice.model';
import { PaymentDto, RecordPaymentRequest } from '../models/payment.model';
import {
  ExpenseCategoryDto,
  ExpenseDto,
  FinancialSummaryDto,
  RevenueRecordDto
} from '../models/finance.model';

@Injectable({ providedIn: 'root' })
export class BillingFinanceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getInvoices(pageNumber = 1, pageSize = 15, search = '', status?: InvoiceStatus | null): Observable<PagedResult<InvoiceDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<InvoiceDto>>(`${this.baseUrl}/billing/invoices`, { params });
  }

  issueInvoice(reservationId: string): Observable<InvoiceDto> {
    return this.http.post<InvoiceDto>(`${this.baseUrl}/billing/invoices/issue`, { reservationId });
  }

  getPayments(pageNumber = 1, pageSize = 20, invoiceId?: string): Observable<PagedResult<PaymentDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (invoiceId) params = params.set('invoiceId', invoiceId);
    return this.http.get<PagedResult<PaymentDto>>(`${this.baseUrl}/billing/payments`, { params });
  }

  recordPayment(payload: RecordPaymentRequest): Observable<PaymentDto> {
    return this.http.post<PaymentDto>(`${this.baseUrl}/billing/payments`, payload);
  }

  reversePayment(paymentId: string, reason: string): Observable<PaymentDto> {
    return this.http.post<PaymentDto>(`${this.baseUrl}/billing/payments/${paymentId}/reverse`, { reason });
  }

  getExpenseCategories(): Observable<ExpenseCategoryDto[]> {
    return this.http.get<ExpenseCategoryDto[]>(`${this.baseUrl}/finance/expense-categories`);
  }

  getExpenses(pageNumber = 1, pageSize = 20): Observable<PagedResult<ExpenseDto>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<ExpenseDto>>(`${this.baseUrl}/finance/expenses`, { params });
  }

  createExpense(payload: unknown): Observable<ExpenseDto> {
    return this.http.post<ExpenseDto>(`${this.baseUrl}/finance/expenses`, payload);
  }

  getRevenues(pageNumber = 1, pageSize = 20): Observable<PagedResult<RevenueRecordDto>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<RevenueRecordDto>>(`${this.baseUrl}/finance/revenues`, { params });
  }

  getFinancialSummary(from?: string, to?: string): Observable<FinancialSummaryDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<FinancialSummaryDto>(`${this.baseUrl}/finance/summary`, { params });
  }
}

