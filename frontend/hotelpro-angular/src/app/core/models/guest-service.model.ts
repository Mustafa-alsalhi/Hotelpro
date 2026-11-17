export type ServiceCategory = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
export type ServiceOrderStatus = 1 | 2 | 3 | 4 | 5;

export interface GuestServiceDto {
  id: string;
  name: string;
  description?: string;
  category: ServiceCategory;
  price: number;
  taxable: boolean;
  isActive: boolean;
}

export interface ServiceOrderItemDto {
  id: string;
  guestServiceId: string;
  serviceName: string;
  quantity: number;
  unitPrice: number;
  taxAmount: number;
  lineTotal: number;
}

export interface ServiceOrderDto {
  id: string;
  orderNumber: string;
  reservationId: string;
  reservationNumber: string;
  roomId: string;
  roomNumber: string;
  guestId: string;
  guestName: string;
  status: ServiceOrderStatus;
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  requestedAt: string;
  completedAt?: string;
  notes?: string;
  items: ServiceOrderItemDto[];
}

