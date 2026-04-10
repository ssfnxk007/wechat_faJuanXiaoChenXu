import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { MediaAssetListItemDto, MediaAssetUploadResultDto, SaveMediaAssetRequest } from '@/types/media-asset'

export interface MediaAssetListQuery {
  bucketType?: string
  keyword?: string
  isEnabled?: boolean
  pageIndex?: number
  pageSize?: number
}

export const getMediaAssetList = (params?: MediaAssetListQuery) =>
  http.get<never, ApiResponse<PagedResult<MediaAssetListItemDto>>>('/mediaassets', { params })

export const createMediaAsset = (payload: SaveMediaAssetRequest) =>
  http.post<SaveMediaAssetRequest, ApiResponse<number>>('/mediaassets', payload)

export const updateMediaAsset = (id: number, payload: SaveMediaAssetRequest) =>
  http.put<SaveMediaAssetRequest, ApiResponse<number>>(`/mediaassets/${id}`, payload)

export const deleteMediaAsset = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/mediaassets/${id}`)

export const uploadMediaAssetFile = (file: File) => {
  const formData = new FormData()
  formData.append('file', file)

  return http.post<FormData, ApiResponse<MediaAssetUploadResultDto>>('/mediaassets/upload', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  })
}
