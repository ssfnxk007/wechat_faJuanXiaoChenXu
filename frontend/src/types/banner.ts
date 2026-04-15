export interface BannerListItemDto {
  id: number
  title: string
  imageAssetId: number
  imageUrl: string
  linkUrl?: string
  sort: number
  isEnabled: boolean
  createdAt: string
}

export interface SaveBannerRequest {
  title: string
  imageAssetId: number
  linkUrl?: string
  sort: number
  isEnabled: boolean
}
