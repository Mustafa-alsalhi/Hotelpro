export type EmployeeStatus = 1 | 2 | 3 | 4;
export type AttendanceStatus = 1 | 2 | 3 | 4 | 5;

export interface DepartmentDto {
  id: string;
  name: string;
  code: string;
  description?: string;
}

export interface EmployeeDto {
  id: string;
  employeeCode: string;
  fullName: string;
  phone: string;
  email: string;
  jobTitle: string;
  departmentId: string;
  departmentName: string;
  hireDate: string;
  baseSalary: number;
  status: EmployeeStatus;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  userId?: string;
}

export interface AttendanceRecordDto {
  id: string;
  employeeId: string;
  employeeName: string;
  date: string;
  checkInTime?: string;
  checkOutTime?: string;
  status: AttendanceStatus;
  notes?: string;
}

