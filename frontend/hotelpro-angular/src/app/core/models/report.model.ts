export interface OccupancyReportRowDto {
  date: string;
  totalServiceableRooms: number;
  occupiedRooms: number;
  outOfServiceRooms: number;
  occupancyRatePercent: number;
}

export interface OccupancyReportDto {
  from: string;
  to: string;
  averageOccupancyPercent: number;
  rows: OccupancyReportRowDto[];
}

