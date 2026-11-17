export type HousekeepingStatus = 1 | 2 | 3 | 4 | 5;
export type TaskPriority = 1 | 2 | 3 | 4;

export interface HousekeepingTaskDto {
  id: string;
  roomId: string;
  roomNumber: string;
  floor: number;
  taskType: number;
  priority: TaskPriority;
  status: HousekeepingStatus;
  assignedToUserId?: string;
  scheduledFor: string;
  startedAt?: string;
  completedAt?: string;
  inspectedAt?: string;
  notes?: string;
  damageReported: boolean;
}

