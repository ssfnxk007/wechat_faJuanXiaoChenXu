import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { CouponWriteOffRecordDto, CouponWriteOffRequest, CouponWriteOffResultDto, ManualGrantUserCouponsRequest, ManualGrantUserCouponsResultDto, UserCouponDetailDto, UserCouponListItemDto } from '@/types/user-coupon'

export interface UserCouponListQuery {
  userId?: number
  couponCode?: string
  pageIndex?: number
  pageSize?: number
}

export const getUserCouponList = (params?: UserCouponListQuery) =>
  http.get<never, ApiResponse<PagedResult<UserCouponListItemDto>>>('/usercoupons', { params })

export const getUserCouponDetail = (id: number) =>
  http.get<never, ApiResponse<UserCouponDetailDto>>(`/usercoupons/${id}`)

export const getUserCouponWriteOffRecords = (id: number) =>
  http.get<never, ApiResponse<CouponWriteOffRecordDto[]>>(`/usercoupons/${id}/writeoff-records`)

export const writeOffCoupon = (payload: CouponWriteOffRequest) =>
  http.post<CouponWriteOffRequest, ApiResponse<CouponWriteOffResultDto>>('/writeoff', payload)

export const manualGrantUserCoupons = (payload: ManualGrantUserCouponsRequest) =>
  http.post<ManualGrantUserCouponsRequest, ApiResponse<ManualGrantUserCouponsResultDto>>('/usercoupons/manual-grant', payload)
