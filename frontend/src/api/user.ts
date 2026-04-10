import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { AuthLoginResultDto, BindMobileRequest, MiniProgramLoginRequest, UserListItemDto } from '@/types/user'

export interface UserListQuery {
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

export const getUserList = (params?: UserListQuery) =>
  http.get<never, ApiResponse<PagedResult<UserListItemDto>>>('/users', { params })

export const miniProgramLogin = (payload: MiniProgramLoginRequest) =>
  http.post<MiniProgramLoginRequest, ApiResponse<AuthLoginResultDto>>('/auth/mini-login', payload)

export const bindMobile = (payload: BindMobileRequest) =>
  http.post<BindMobileRequest, ApiResponse<boolean>>('/auth/bind-mobile', payload)
