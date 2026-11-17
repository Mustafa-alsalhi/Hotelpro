import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/pagination.model';
import { CreateUserRequest, UpdateUserRequest, UserDto } from '../models/user.model';
import { PermissionDto, RoleDto } from '../models/role.model';

@Injectable({ providedIn: 'root' })
export class UsersRolesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getUsers(pageNumber = 1, pageSize = 15, search = '', isActive?: boolean | null, role = ''): Observable<PagedResult<UserDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (isActive !== null && isActive !== undefined) params = params.set('isActive', isActive);
    if (role) params = params.set('role', role);

    return this.http.get<PagedResult<UserDto>>(`${this.baseUrl}/users`, { params });
  }

  createUser(payload: CreateUserRequest): Observable<UserDto> {
    return this.http.post<UserDto>(`${this.baseUrl}/users`, payload);
  }

  updateUser(id: string, payload: UpdateUserRequest): Observable<UserDto> {
    return this.http.put<UserDto>(`${this.baseUrl}/users/${id}`, payload);
  }

  toggleUserActive(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/users/${id}/toggle-active`, {});
  }

  getRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(`${this.baseUrl}/roles`);
  }

  getAllPermissions(): Observable<PermissionDto[]> {
    return this.http.get<PermissionDto[]>(`${this.baseUrl}/roles/permissions`);
  }

  createRole(name: string, description: string, permissionCodes: string[]): Observable<RoleDto> {
    return this.http.post<RoleDto>(`${this.baseUrl}/roles`, { name, description, permissionCodes });
  }

  updateRolePermissions(roleId: string, description: string, permissionCodes: string[]): Observable<RoleDto> {
    return this.http.put<RoleDto>(`${this.baseUrl}/roles/${roleId}/permissions`, { description, permissionCodes });
  }

  deleteRole(roleId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/roles/${roleId}`);
  }
}

