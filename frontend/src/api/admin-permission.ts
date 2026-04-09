import http from './http'
import type { ApiResponse } from '@/types/api'
import type {
  AdminAuthProfileDto,
  AdminMenuListItemDto,
  AdminPermissionListItemDto,
  AdminRoleListItemDto,
  AdminUserListItemDto,
  ResetAdminUserPasswordRequest,
  SaveAdminMenuRequest,
  SaveAdminRoleRequest,
  SaveAdminUserRequest,
} from '@/types/admin-permission'
import type { PagedResponse } from '@/types/common'

export const getAdminProfile = () =>
  http.get<never, ApiResponse<AdminAuthProfileDto>>('/adminauth/profile')

export const getAdminUserList = (params: { keyword?: string; pageIndex: number; pageSize: number }) =>
  http.get<never, ApiResponse<PagedResponse<AdminUserListItemDto>>>('/adminusers', { params })

export const createAdminUser = (payload: SaveAdminUserRequest) =>
  http.post<SaveAdminUserRequest, ApiResponse<number>>('/adminusers', payload)

export const updateAdminUser = (id: number, payload: SaveAdminUserRequest) =>
  http.put<SaveAdminUserRequest, ApiResponse<number>>(`/adminusers/${id}`, payload)

export const resetAdminUserPassword = (id: number, payload: ResetAdminUserPasswordRequest) =>
  http.post<ResetAdminUserPasswordRequest, ApiResponse<number>>(`/adminusers/${id}/reset-password`, payload)

export const deleteAdminUser = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/adminusers/${id}`)

export const getAdminRoleList = (params: { keyword?: string; pageIndex: number; pageSize: number }) =>
  http.get<never, ApiResponse<PagedResponse<AdminRoleListItemDto>>>('/adminroles', { params })

export const createAdminRole = (payload: SaveAdminRoleRequest) =>
  http.post<SaveAdminRoleRequest, ApiResponse<number>>('/adminroles', payload)

export const updateAdminRole = (id: number, payload: SaveAdminRoleRequest) =>
  http.put<SaveAdminRoleRequest, ApiResponse<number>>(`/adminroles/${id}`, payload)

export const deleteAdminRole = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/adminroles/${id}`)

export const getAdminMenuList = () =>
  http.get<never, ApiResponse<AdminMenuListItemDto[]>>('/adminmenus')

export const createAdminMenu = (payload: SaveAdminMenuRequest) =>
  http.post<SaveAdminMenuRequest, ApiResponse<number>>('/adminmenus', payload)

export const updateAdminMenu = (id: number, payload: SaveAdminMenuRequest) =>
  http.put<SaveAdminMenuRequest, ApiResponse<number>>(`/adminmenus/${id}`, payload)

export const deleteAdminMenu = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/adminmenus/${id}`)

export const getAdminPermissionList = () =>
  http.get<never, ApiResponse<AdminPermissionListItemDto[]>>('/adminpermissions')
