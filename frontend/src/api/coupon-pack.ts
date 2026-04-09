import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { CouponOrderDetailDto, CouponOrderListItemDto, CouponPackListItemDto, CreateCouponOrderRequest, SaveCouponPackRequest } from '@/types/coupon-pack'

export interface CouponPackListQuery {
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

export interface CouponOrderListQuery {
  pageIndex?: number
  pageSize?: number
}

export const getCouponPackList = (params?: CouponPackListQuery) =>
  http.get<never, ApiResponse<PagedResult<CouponPackListItemDto>>>('/couponpacks', { params })

export const createCouponPack = (payload: SaveCouponPackRequest) =>
  http.post<SaveCouponPackRequest, ApiResponse<number>>('/couponpacks', payload)

export const updateCouponPack = (id: number, payload: SaveCouponPackRequest) =>
  http.put<SaveCouponPackRequest, ApiResponse<number>>(`/couponpacks/${id}`, payload)

export const deleteCouponPack = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/couponpacks/${id}`)

export const getCouponOrderList = (params?: CouponOrderListQuery) =>
  http.get<never, ApiResponse<PagedResult<CouponOrderListItemDto>>>('/couponorders', { params })

export const getCouponOrderDetail = (id: number) =>
  http.get<never, ApiResponse<CouponOrderDetailDto>>(`/couponorders/${id}`)

export const createCouponOrder = (payload: CreateCouponOrderRequest) =>
  http.post<CreateCouponOrderRequest, ApiResponse<number>>('/couponorders', payload)
