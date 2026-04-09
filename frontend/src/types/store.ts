export interface StoreListItemDto {
  id: number
  code: string
  name: string
  contactName?: string
  contactPhone?: string
  isEnabled: boolean
  createdAt: string
}

export interface SaveStoreRequest {
  code: string
  name: string
  contactName?: string
  contactPhone?: string
  isEnabled: boolean
}
