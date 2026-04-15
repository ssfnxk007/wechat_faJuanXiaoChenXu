import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { BannerListItemDto, SaveBannerRequest } from '@/types/banner'

export interface BannerListQuery {
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

export const getBannerList = (params?: BannerListQuery) =>
  http.get<never, ApiResponse<PagedResult<BannerListItemDto>>>('/banners', { params })

export const createBanner = (payload: SaveBannerRequest) =>
  http.post<SaveBannerRequest, ApiResponse<number>>('/banners', payload)

export const updateBanner = (id: number, payload: SaveBannerRequest) =>
  http.put<SaveBannerRequest, ApiResponse<number>>(`/banners/${id}`, payload)

export const deleteBanner = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/banners/${id}`)
