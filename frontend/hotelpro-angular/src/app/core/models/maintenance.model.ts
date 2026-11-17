export type MaintenanceStatus = 1 | 2 | 3 | 4 | 5 | 6;

export interface MaintenanceRequestDto {
  id: string;
  requestNumber: string;
  roomId?: string;
  roomNumber?: string;
  issueType: string;
  description: string;
  priority: number;
  status: MaintenanceStatus;
  assignedTechnicianUserId?: string;
  takeRoomOutOfService: boolean;
  sparePartsUsed?: string;
  cost: number;
  createdAt: string;
  completedAt?: string;
}

