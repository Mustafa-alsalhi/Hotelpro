export interface ChartSeriesPointDto {
  label: string;
  value: number;
  secondaryValue: number;
}

export interface DashboardSummaryDto {
  totalRooms: number;
  availableRooms: number;
  reservedRooms: number;
  occupiedRooms: number;
  cleaningRooms: number;
  maintenanceRooms: number;
  outOfServiceRooms: number;
  occupancyRatePercent: number;
  currentInHouseGuests: number;
  todayBookings: number;
  expectedArrivalsToday: number;
  expectedDeparturesToday: number;
  totalRevenues: number;
  totalExpenses: number;
  netOperatingResult: number;
  pendingHousekeepingTasks: number;
  openMaintenanceRequests: number;
  lowStockItemsCount: number;
  revenueVsExpenseSeries: ChartSeriesPointDto[];
  bookingsBySource: ChartSeriesPointDto[];
  roomTypeDistribution: ChartSeriesPointDto[];
  topServices: ChartSeriesPointDto[];
}

