export interface PermissionDto {
  id: string;
  code: string;
  module: string;
  action: string;
  description: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  permissions: string[];
}

