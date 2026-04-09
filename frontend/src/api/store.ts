import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { SaveStoreRequest, StoreListItemDto } from '@/types/store'

export interface StoreListQuery {
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

export const getStoreList = (params?: StoreListQuery) =>
  http.get<never, ApiResponse<PagedResult<StoreListItemDto>>>('/stores', { params })

export const createStore = (payload: SaveStoreRequest) =>
  http.post<SaveStoreRequest, ApiResponse<number>>('/stores', payload)

export const updateStore = (id: number, payload: SaveStoreRequest) =>
  http.put<SaveStoreRequest, ApiResponse<number>>(`/stores/${id}`, payload)

export const deleteStore = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/stores/${id}`)
