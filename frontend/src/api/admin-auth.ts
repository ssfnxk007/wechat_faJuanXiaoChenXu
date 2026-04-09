import http from './http'
import type { AdminLoginRequest, AdminLoginResultDto, ApiResponse } from '@/types/api'
import type { AdminAuthProfileDto } from '@/types/admin-permission'

export const adminLogin = (payload: AdminLoginRequest) =>
  http.post<AdminLoginRequest, ApiResponse<AdminLoginResultDto>>('/adminauth/login', payload)

export const getAdminProfile = () =>
  http.get<never, ApiResponse<AdminAuthProfileDto>>('/adminauth/profile')
