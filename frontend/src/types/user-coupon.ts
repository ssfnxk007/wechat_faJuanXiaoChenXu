export interface UserCouponListItemDto {
  id: number
  appUserId: number
  couponTemplateId: number
  couponCode: string
  status: number
  effectiveAt: string
  expireAt: string
  receivedAt: string
}

export interface UserCouponDetailDto {
  id: number
  appUserId: number
  couponTemplateId: number
  couponTemplateName: string
  templateType: number
  validPeriodType: number
  discountAmount?: number
  thresholdAmount?: number
  validDays?: number
  validFrom?: string
  validTo?: string
  isNewUserOnly: boolean
  isAllStores: boolean
  perUserLimit: number
  templateEnabled: boolean
  templateRemark?: string
  couponCode: string
  status: number
  effectiveAt: string
  expireAt: string
  receivedAt: string
}

export interface CouponWriteOffRequest {
  couponCode: string
  storeId: number
  productId?: number
  operatorName?: string
  deviceCode?: string
}

export interface CouponWriteOffResultDto {
  userCouponId: number
  couponCode: string
  appUserId: number
  couponTemplateId: number
  message: string
}

export interface CouponWriteOffRecordDto {
  id: number
  userCouponId: number
  couponCode: string
  storeId: number
  storeName: string
  operatorName?: string
  deviceCode?: string
  writeOffAt: string
  createdAt: string
}

export interface ManualGrantUserCouponsRequest {
  couponTemplateId: number
  appUserIds: number[]
  quantityPerUser: number
}

export interface ManualGrantUserCouponItemDto {
  appUserId: number
  success: boolean
  grantedCount: number
  message: string
}

export interface ManualGrantUserCouponsResultDto {
  couponTemplateId: number
  successCount: number
  failureCount: number
  items: ManualGrantUserCouponItemDto[]
}

export interface ImportGrantUserCouponsResultDto {
  couponTemplateId: number
  quantityPerUser: number
  totalRows: number
  parsedUserCount: number
  successCount: number
  failureCount: number
  invalidRows: string[]
  items: ManualGrantUserCouponItemDto[]
}
