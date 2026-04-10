export interface CouponTemplateListItemDto {
  id: number
  name: string
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
  isEnabled: boolean
  remark?: string
  productIds?: number[]
  createdAt: string
}

export interface SaveCouponTemplateRequest {
  name: string
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
  isEnabled: boolean
  remark?: string
  productIds?: number[]
}
