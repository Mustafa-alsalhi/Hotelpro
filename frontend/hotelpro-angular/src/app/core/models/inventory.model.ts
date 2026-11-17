export type PurchaseOrderStatus = 1 | 2 | 3 | 4;
export type StockTransactionType = 1 | 2 | 3 | 4;

export interface InventoryCategoryDto {
  id: string;
  name: string;
}

export interface InventoryItemDto {
  id: string;
  sku: string;
  name: string;
  categoryId: string;
  categoryName: string;
  unitOfMeasure: string;
  currentStock: number;
  reorderLevel: number;
  unitCost: number;
  isLowStock: boolean;
  isActive: boolean;
}

export interface SupplierDto {
  id: string;
  name: string;
  contactPerson?: string;
  phone: string;
  email?: string;
  address?: string;
  isActive: boolean;
}

export interface PurchaseOrderDto {
  id: string;
  orderNumber: string;
  supplierId: string;
  supplierName: string;
  orderDate: string;
  expectedDate?: string;
  status: PurchaseOrderStatus;
  totalAmount: number;
}

export interface StockTransactionDto {
  id: string;
  inventoryItemId: string;
  itemName: string;
  transactionType: StockTransactionType;
  quantity: number;
  balanceAfter: number;
  departmentId?: string;
  departmentName?: string;
  referenceNumber?: string;
  notes?: string;
  transactionDate: string;
}

