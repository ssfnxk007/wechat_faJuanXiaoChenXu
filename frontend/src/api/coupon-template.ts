import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { CouponTemplateListItemDto, SaveCouponTemplateRequest } from '@/types/coupon'

export interface CouponTemplateListQuery {
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

export const getCouponTemplateList = (params?: CouponTemplateListQuery) =>
  http.get<never, ApiResponse<PagedResult<CouponTemplateListItemDto>>>('/coupontemplates', { params })

export const createCouponTemplate = (payload: SaveCouponTemplateRequest) =>
  http.post<SaveCouponTemplateRequest, ApiResponse<number>>('/coupontemplates', payload)

export const updateCouponTemplate = (id: number, payload: SaveCouponTemplateRequest) =>
  http.put<SaveCouponTemplateRequest, ApiResponse<number>>(`/coupontemplates/${id}`, payload)

export const deleteCouponTemplate = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/coupontemplates/${id}`)
