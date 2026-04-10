export interface ProductListItemDto {
  id: number
  name: string
  erpProductCode: string
  mainImageAssetId?: number
  mainImageUrl?: string
  detailImageAssetIds: number[]
  detailImageUrls: string[]
  salePrice?: number
  isEnabled: boolean
  createdAt: string
}

export interface SaveProductRequest {
  name: string
  erpProductCode: string
  mainImageAssetId?: number
  detailImageAssetIds: number[]
  salePrice?: number
  isEnabled: boolean
}
