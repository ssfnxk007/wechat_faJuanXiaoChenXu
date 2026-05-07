export type ProductDirectPurchaseValidPeriodType = 1 | 2

export interface ProductListItemDto {
  id: number
  name: string
  erpProductCode: string
  erpIsbnCode?: string | null
  mainImageAssetId?: number | null
  mainImageUrl?: string | null
  detailImageAssetIds: number[]
  detailImageUrls: string[]
  erpOriginalPrice?: number | null
  salePrice?: number | null
  stockQuantity?: number | null
  showInMiniApp?: boolean | null
  isEnabled: boolean
  directPurchaseValidPeriodType?: ProductDirectPurchaseValidPeriodType | null
  directPurchaseValidDays?: number | null
  directPurchaseValidFrom?: string | null
  directPurchaseValidTo?: string | null
  createdAt: string
}

export interface SaveProductRequest {
  name: string
  erpProductCode: string
  erpIsbnCode?: string
  mainImageAssetId?: number
  detailImageAssetIds: number[]
  erpOriginalPrice?: number
  salePrice?: number
  stockQuantity?: number
  showInMiniApp: boolean
  isEnabled: boolean
  directPurchaseValidPeriodType?: ProductDirectPurchaseValidPeriodType
  directPurchaseValidDays?: number
  directPurchaseValidFrom?: string
  directPurchaseValidTo?: string
}
