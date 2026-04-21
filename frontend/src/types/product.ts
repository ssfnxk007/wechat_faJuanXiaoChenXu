export interface ProductListItemDto {
  id: number
  name: string
  erpProductCode: string
  mainImageAssetId?: number
  mainImageUrl?: string
  detailImageAssetIds: number[]
  detailImageUrls: string[]
  erpOriginalPrice?: number
  salePrice?: number
  stockQuantity?: number
  isEnabled: boolean
  createdAt: string
}

export interface SaveProductRequest {
  name: string
  erpProductCode: string
  mainImageAssetId?: number
  detailImageAssetIds: number[]
  erpOriginalPrice?: number
  salePrice?: number
  stockQuantity?: number
  isEnabled: boolean
}
