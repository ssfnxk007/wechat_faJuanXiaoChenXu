import http from './http'
import type { ApiResponse } from '@/types/api'
import type { CouponPackItemDto, SaveCouponPackItemRequest } from '@/types/coupon-pack-item'

export const getCouponPackItemList = (couponPackId: number) =>
  http.get<never, ApiResponse<CouponPackItemDto[]>>(`/couponpackitems/${couponPackId}`)

export const saveCouponPackItem = (payload: SaveCouponPackItemRequest) =>
  http.post<SaveCouponPackItemRequest, ApiResponse<number>>('/couponpackitems', payload)

export const updateCouponPackItem = (id: number, payload: SaveCouponPackItemRequest) =>
  http.put<SaveCouponPackItemRequest, ApiResponse<number>>(`/couponpackitems/${id}`, payload)

export const deleteCouponPackItem = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/couponpackitems/${id}`)
