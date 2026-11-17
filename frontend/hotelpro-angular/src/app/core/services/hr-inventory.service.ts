import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.model';
import { AttendanceRecordDto, DepartmentDto, EmployeeDto } from '../models/employee.model';
import {
  InventoryCategoryDto,
  InventoryItemDto,
  PurchaseOrderDto,
  StockTransactionDto,
  SupplierDto
} from '../models/inventory.model';

@Injectable({ providedIn: 'root' })
export class HrInventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getDepartments(): Observable<DepartmentDto[]> {
    return this.http.get<DepartmentDto[]>(`${this.baseUrl}/employees/departments`);
  }

  getEmployees(pageNumber = 1, pageSize = 20, search = ''): Observable<PagedResult<EmployeeDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<EmployeeDto>>(`${this.baseUrl}/employees`, { params });
  }

  createEmployee(payload: unknown): Observable<EmployeeDto> {
    return this.http.post<EmployeeDto>(`${this.baseUrl}/employees`, payload);
  }

  getAttendance(date: string): Observable<AttendanceRecordDto[]> {
    return this.http.get<AttendanceRecordDto[]>(`${this.baseUrl}/employees/attendance`, {
      params: new HttpParams().set('date', date)
    });
  }

  recordAttendance(payload: unknown): Observable<AttendanceRecordDto> {
    return this.http.post<AttendanceRecordDto>(`${this.baseUrl}/employees/attendance`, payload);
  }

  getInventoryCategories(): Observable<InventoryCategoryDto[]> {
    return this.http.get<InventoryCategoryDto[]>(`${this.baseUrl}/inventory/categories`);
  }

  getInventoryItems(pageNumber = 1, pageSize = 25, lowStockOnly = false): Observable<PagedResult<InventoryItemDto>> {
    let params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    if (lowStockOnly) params = params.set('lowStockOnly', true);
    return this.http.get<PagedResult<InventoryItemDto>>(`${this.baseUrl}/inventory/items`, { params });
  }

  createInventoryItem(payload: unknown): Observable<InventoryItemDto> {
    return this.http.post<InventoryItemDto>(`${this.baseUrl}/inventory/items`, payload);
  }

  getSuppliers(): Observable<SupplierDto[]> {
    return this.http.get<SupplierDto[]>(`${this.baseUrl}/inventory/suppliers`);
  }

  createSupplier(payload: unknown): Observable<SupplierDto> {
    return this.http.post<SupplierDto>(`${this.baseUrl}/inventory/suppliers`, payload);
  }

  getPurchaseOrders(): Observable<PagedResult<PurchaseOrderDto>> {
    return this.http.get<PagedResult<PurchaseOrderDto>>(`${this.baseUrl}/inventory/purchase-orders`);
  }

  createPurchaseOrder(payload: unknown): Observable<PurchaseOrderDto> {
    return this.http.post<PurchaseOrderDto>(`${this.baseUrl}/inventory/purchase-orders`, payload);
  }

  receivePurchaseOrder(id: string): Observable<PurchaseOrderDto> {
    return this.http.post<PurchaseOrderDto>(`${this.baseUrl}/inventory/purchase-orders/${id}/receive`, {});
  }

  getStockTransactions(): Observable<PagedResult<StockTransactionDto>> {
    return this.http.get<PagedResult<StockTransactionDto>>(`${this.baseUrl}/inventory/transactions`);
  }

  recordStockTransaction(payload: unknown): Observable<StockTransactionDto> {
    return this.http.post<StockTransactionDto>(`${this.baseUrl}/inventory/transactions`, payload);
  }
}

