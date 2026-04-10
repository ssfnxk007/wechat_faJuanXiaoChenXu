export interface MediaAssetListItemDto {
  id: number
  name: string
  fileUrl: string
  mediaType: string
  bucketType: string
  tags: string[]
  sort: number
  isEnabled: boolean
  createdAt: string
}

export interface SaveMediaAssetRequest {
  name: string
  fileUrl: string
  mediaType: string
  bucketType: string
  tags: string[]
  sort: number
  isEnabled: boolean
}

export interface MediaAssetUploadResultDto {
  fileName: string
  fileUrl: string
}
