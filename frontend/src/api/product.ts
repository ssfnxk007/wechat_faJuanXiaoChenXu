import http from './http'
import type { ApiResponse } from '@/types/api'
import type { PagedResult } from '@/types/paged'
import type { ProductListItemDto, SaveProductRequest } from '@/types/product'

export interface ProductListQuery {
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

export const getProductList = (params?: ProductListQuery) =>
  http.get<never, ApiResponse<PagedResult<ProductListItemDto>>>('/products', { params })

export const createProduct = (payload: SaveProductRequest) =>
  http.post<SaveProductRequest, ApiResponse<number>>('/products', payload)

export const updateProduct = (id: number, payload: SaveProductRequest) =>
  http.put<SaveProductRequest, ApiResponse<number>>(`/products/${id}`, payload)

export const deleteProduct = (id: number) =>
  http.delete<never, ApiResponse<boolean>>(`/products/${id}`)
