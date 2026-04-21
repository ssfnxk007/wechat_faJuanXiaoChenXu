export interface CouponTemplateListItemDto {
  id: number
  name: string
  imageAssetId?: number
  imageUrl?: string
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
  distributionMode: number
  salePrice?: number
  remark?: string
  productIds?: number[]
  storeIds?: number[]
  createdAt: string
}

export interface SaveCouponTemplateRequest {
  name: string
  imageAssetId?: number
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
  distributionMode: number
  salePrice?: number
  remark?: string
  productIds?: number[]
  storeIds?: number[]
}
