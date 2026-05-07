import { request } from '@/utils/request'

export async function fetchMiniAppWriteOffRecords() {
  const response = await request({ url: '/api/miniapp/users/writeoff-records' })
  const payload = response.data || {}

  return {
    totalWriteOffCount: Number(payload.totalWriteOffCount || 0),
    monthWriteOffCount: Number(payload.monthWriteOffCount || 0),
    unusedCouponCount: Number(payload.unusedCouponCount || 0),
    items: Array.isArray(payload.items) ? payload.items : []
  }
}
