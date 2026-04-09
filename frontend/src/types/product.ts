export interface ProductListItemDto {
  id: number
  name: string
  erpProductCode: string
  salePrice?: number
  isEnabled: boolean
  createdAt: string
}

export interface SaveProductRequest {
  name: string
  erpProductCode: string
  salePrice?: number
  isEnabled: boolean
}
