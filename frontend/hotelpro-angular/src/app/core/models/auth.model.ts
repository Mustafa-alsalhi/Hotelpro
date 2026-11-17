export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  preferredLanguage: 'ar' | 'en' | string;
  roles: string[];
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: CurrentUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

