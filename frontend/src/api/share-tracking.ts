import http from './http'
import type { ApiResponse } from '@/types/api'
import type {
  ShareTrackingDetailItemDto,
  ShareTrackingDetailsPagedResult,
  ShareTrackingDetailsQuery,
  ShareTrackingSummaryItemDto,
  ShareTrackingSummaryQuery,
} from '@/types/share-tracking'

export const getShareTrackingSummary = (params?: ShareTrackingSummaryQuery) =>
  http.get<never, ApiResponse<ShareTrackingSummaryItemDto[]>>('/share-tracking/summary', { params })

export const getShareTrackingDetails = (params?: ShareTrackingDetailsQuery) =>
  http.get<never, ApiResponse<ShareTrackingDetailsPagedResult>>('/share-tracking/details', { params })

export type { ShareTrackingSummaryItemDto, ShareTrackingDetailItemDto }
