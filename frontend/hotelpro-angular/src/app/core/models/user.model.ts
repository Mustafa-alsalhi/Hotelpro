export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string;
  isActive: boolean;
  preferredLanguage: string;
  employeeId?: string;
  createdAt: string;
  roles: string[];
}

export interface CreateUserRequest {
  email: string;
  fullName: string;
  password: string;
  phoneNumber?: string;
  preferredLanguage: string;
  roles: string[];
}

export interface UpdateUserRequest {
  fullName: string;
  phoneNumber?: string;
  preferredLanguage: string;
  isActive: boolean;
  roles: string[];
}

