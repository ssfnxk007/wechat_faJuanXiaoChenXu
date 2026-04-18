import type { PagedResult } from '@/types/paged'

export interface ShareTrackingSummaryItemDto {
  date: string
  targetType: string
  targetKey: string
  targetId: number | null
  shareIntentCount: number
  openCount: number
  openRate: number
}

export interface ShareTrackingDetailItemDto {
  id: number
  eventType: string
  shareId: string
  eventKey: string
  fromUserId: number | null
  openUserId: number | null
  visitorKey: string | null
  targetType: string
  targetKey: string
  targetId: number | null
  pagePath: string
  scene: string | null
  clientTime: string | null
  createdAt: string
}

export interface ShareTrackingSummaryQuery {
  dateFrom?: string
  dateTo?: string
  targetType?: string
  targetKey?: string
  couponTemplateId?: number
}

export interface ShareTrackingDetailsQuery extends ShareTrackingSummaryQuery {
  eventType?: string
  pageIndex?: number
  pageSize?: number
}

export type ShareTrackingDetailsPagedResult = PagedResult<ShareTrackingDetailItemDto>
