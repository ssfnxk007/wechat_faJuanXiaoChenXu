import { request } from '@/utils/request'

function firstValue(item, keys, fallback = '') {
  for (let index = 0; index < keys.length; index += 1) {
    const value = item?.[keys[index]]
    if (value !== undefined && value !== null && value !== '') {
      return value
    }
  }
  return fallback
}

function toItems(payload) {
  if (Array.isArray(payload)) {
    return payload
  }
  if (Array.isArray(payload?.items)) {
    return payload.items
  }
  return []
}

function formatTime(value) {
  if (!value) {
    return ''
  }

  if (typeof value === 'string') {
    return value.replace('T', ' ').slice(0, 16)
  }

  return String(value)
}

function resolveStatus(status, fulfillment) {
  const normalized = String(status || '').toLowerCase()
  const fulfillmentText = String(fulfillment || '')

  if (normalized === 'completed' || fulfillmentText.includes('完成') || fulfillmentText.includes('已核销')) {
    return { value: 'completed', text: '已完成' }
  }
  if (normalized === 'paid' || normalized === '2') {
    return { value: 'paid', text: '已支付' }
  }
  if (normalized === 'pending' || normalized === 'pendingpayment' || normalized === '1') {
    return { value: 'pending', text: '待付款' }
  }
  if (normalized === 'refunded' || normalized === '3') {
    return { value: 'completed', text: '已退款' }
  }
  if (normalized === 'closed' || normalized === '4') {
    return { value: 'completed', text: '已关闭' }
  }

  return { value: 'pending', text: '状态未确认' }
}

function mapOrder(item) {
  const fulfillment = String(firstValue(item, ['fulfillment', 'fulfillmentText', 'grantStatusText'], '待使用'))
  const statusInfo = resolveStatus(firstValue(item, ['status', 'statusCode', 'statusText'], 'paid'), fulfillment)
  const packName = String(firstValue(item, ['couponPackName'], ''))
  const templateName = String(firstValue(item, ['couponTemplateName'], ''))
  const isProductCoupon = Boolean(firstValue(item, ['isProductCoupon'], false))
  const title = String(firstValue(item, ['title'], packName || templateName || '订单'))

  let desc = String(firstValue(item, ['desc', 'description', 'remark'], ''))
  if (!desc) {
    if (packName) {
      desc = '支付后自动发券到卡包。'
    } else if (isProductCoupon) {
      desc = '支付后发券，待履约。'
    } else {
      desc = '支付后自动发券到卡包。'
    }
  }

  const tags = Array.isArray(item?.tags) && item.tags.length
    ? item.tags
    : packName
      ? ['券包权益']
      : [isProductCoupon ? '商品券' : '单张售卖券']

  let note = String(firstValue(item, ['note', 'remark'], '以后端状态为准'))
  if (!item?.note && !item?.remark) {
    note = statusInfo.value === 'paid'
      ? (isProductCoupon ? '商品券已发放，待履约。' : (packName ? '权益已发放，可在卡包查看。' : '已发券，可在卡包查看。'))
      : '订单未确认支付完成，暂未发券。'
  }

  return {
    id: firstValue(item, ['id', 'orderId'], Date.now()),
    orderNo: String(firstValue(item, ['orderNo'], '')),
    time: formatTime(firstValue(item, ['time', 'createdAt', 'paidAt'], '')),
    status: statusInfo.value,
    statusText: String(firstValue(item, ['statusText'], statusInfo.text)),
    title,
    desc,
    tags,
    payment: String(firstValue(item, ['payment', 'paymentText'], '微信支付')),
    amount: String(firstValue(item, ['amount', 'orderAmount'], '0')),
    fulfillment,
    store: String(firstValue(item, ['store', 'storeName', 'storeScopeText'], '以后端适用门店为准')),
    note,
    actionText: String(firstValue(item, ['actionText'], '查看详情'))
  }
}

export async function fetchOrderList(query = {}) {
  const response = await request({ url: '/api/miniapp/orders', query })
  const payload = response.data || {}
  const items = toItems(payload).map((item) => mapOrder(item))

  return {
    items,
    totalCount: Number(payload?.totalCount) || items.length,
    pageIndex: Number(payload?.pageIndex) || 1,
    pageSize: Number(payload?.pageSize) || items.length,
    totalPages: Number(payload?.totalPages) || 1
  }
}
