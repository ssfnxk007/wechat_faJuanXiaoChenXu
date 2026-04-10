export interface CouponPackListItemDto {
  id: number
  name: string
  imageAssetId?: number
  imageUrl?: string
  salePrice: number
  status: number
  perUserLimit: number
  saleStartTime?: string
  saleEndTime?: string
  remark?: string
  createdAt: string
}

export interface SaveCouponPackRequest {
  name: string
  imageAssetId?: number
  salePrice: number
  status: number
  perUserLimit: number
  saleStartTime?: string
  saleEndTime?: string
  remark?: string
}

export interface CouponOrderListItemDto {
  id: number
  orderNo: string
  appUserId: number
  couponPackId: number
  orderAmount: number
  status: number
  paidAt?: string
  createdAt: string
}

export interface CouponOrderPaymentDto {
  id: number
  paymentNo: string
  amount: number
  status: number
  channelTradeNo?: string
  rawCallback?: string
  paidAt?: string
  createdAt: string
}

export interface CouponOrderGrantedCouponDto {
  id: number
  couponTemplateId: number
  couponTemplateName: string
  templateType: number
  discountAmount?: number
  thresholdAmount?: number
  isAllStores: boolean
  isNewUserOnly: boolean
  couponCode: string
  status: number
  receivedAt: string
  effectiveAt: string
  expireAt: string
}

export interface CouponOrderDetailDto {
  id: number
  orderNo: string
  appUserId: number
  couponPackId: number
  couponPackName: string
  orderAmount: number
  status: number
  paymentNo?: string
  paidAt?: string
  createdAt: string
  payments: CouponOrderPaymentDto[]
  grantedCoupons: CouponOrderGrantedCouponDto[]
}

export interface CreateCouponOrderRequest {
  userId: number
  couponPackId: number
}
