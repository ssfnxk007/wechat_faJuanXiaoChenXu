import { requestWithFallback } from '@/utils/request'
import { useSessionStore } from '@/store/session'

const mockOrders = [
  {
    id: 1,
    orderNo: 'CP202604110001',
    time: '2026-04-11 09:18',
    status: 'paid',
    statusText: '已支付',
    title: '春序礼遇券包',
    desc: '含满减券、门店通用券与精品专区券，可分次使用。',
    tags: ['券包权益', '门店通用'],
    payment: '微信支付',
    amount: '59.90',
    fulfillment: '待使用',
    store: '全门店可用',
    note: '订单已完成支付，券包已发放至账户，可按规则到店使用。',
    actionText: '查看券包'
  },
  {
    id: 2,
    orderNo: 'CP202604100126',
    time: '2026-04-10 18:42',
    status: 'pending',
    statusText: '待付款',
    title: '雅集焕新组合包',
    desc: '适合首次到店选择，含单次体验券与商品优惠权益。',
    tags: ['限时保留', '首次推荐'],
    payment: '待完成支付',
    amount: '39.90',
    fulfillment: '未生效',
    store: '指定门店可用',
    note: '请在支付时限内完成付款，超时后订单将自动关闭。',
    actionText: '立即支付'
  },
  {
    id: 3,
    orderNo: 'CP202604090083',
    time: '2026-04-09 14:06',
    status: 'completed',
    statusText: '已完成',
    title: '清和满减券包',
    desc: '适用于日常到店消费，券包内权益已全部核销完成。',
    tags: ['满减权益', '已核销'],
    payment: '微信支付',
    amount: '29.90',
    fulfillment: '已完成',
    store: '华亭路门店',
    note: '本订单内权益均已完成使用，可继续查看核销明细。',
    actionText: '查看记录'
  }
]

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
  if (normalized === 'paid' || normalized === '2' || normalized === '1') {
    return { value: 'paid', text: '已支付' }
  }
  if (normalized === 'pending' || normalized === 'pendingpayment') {
    return { value: 'pending', text: '待付款' }
  }
  if (normalized === 'refunded' || normalized === '3') {
    return { value: 'completed', text: '已退款' }
  }
  if (normalized === 'closed' || normalized === '4') {
    return { value: 'completed', text: '已关闭' }
  }

  return { value: 'paid', text: '已支付' }
}

function mapOrder(item, fallback = {}) {
  const fulfillment = String(firstValue(item, ['fulfillment', 'fulfillmentText', 'grantStatusText'], fallback.fulfillment || '待使用'))
  const statusInfo = resolveStatus(firstValue(item, ['status', 'statusCode', 'statusText'], fallback.status || 'paid'), fulfillment)
  const packName = String(firstValue(item, ['couponPackName'], ''))
  const templateName = String(firstValue(item, ['couponTemplateName'], ''))
  const isProductCoupon = Boolean(firstValue(item, ['isProductCoupon'], false))
  const title = String(firstValue(item, ['title'], packName || templateName || fallback.title || '订单'))

  let desc = String(firstValue(item, ['desc', 'description', 'remark'], fallback.desc || ''))
  if (!desc) {
    if (packName) {
      desc = '券包支付成功后会自动拆分并发券到我的券包。'
    } else if (isProductCoupon) {
      desc = '商品券支付成功后已发券，当前阶段显示待履约 / 待 ERP 处理。'
    } else {
      desc = '单张售卖券支付成功后会自动发券到我的券包。'
    }
  }

  const tags = Array.isArray(item?.tags) && item.tags.length
    ? item.tags
    : packName
      ? ['券包权益']
      : [isProductCoupon ? '商品券' : '单张售卖券']

  let note = String(firstValue(item, ['note', 'remark'], fallback.note || '请以后端订单状态为准'))
  if (!item?.note && !item?.remark) {
    note = isProductCoupon
      ? '商品券已发放，后续履约状态会在订单详情中持续更新。'
      : (packName ? '券包权益已发放后可在卡包中查看并使用。' : '单张售卖券支付成功后即可在卡包中查看。')
  }

  return {
    id: firstValue(item, ['id', 'orderId'], fallback.id || Date.now()),
    orderNo: String(firstValue(item, ['orderNo'], fallback.orderNo || '')),
    time: formatTime(firstValue(item, ['time', 'createdAt', 'paidAt'], fallback.time || '')),
    status: statusInfo.value,
    statusText: String(firstValue(item, ['statusText'], statusInfo.text)),
    title,
    desc,
    tags,
    payment: String(firstValue(item, ['payment', 'paymentText'], fallback.payment || '微信支付')),
    amount: String(firstValue(item, ['amount', 'orderAmount'], fallback.amount || '0')),
    fulfillment,
    store: String(firstValue(item, ['store', 'storeName', 'storeScopeText'], fallback.store || '请以后端适用门店为准')),
    note,
    actionText: String(firstValue(item, ['actionText'], fallback.actionText || '查看详情'))
  }
}

export async function fetchOrderList(query = {}) {
  const enableFallback = process.env.NODE_ENV !== 'production'
  const result = await requestWithFallback(
    { url: '/api/miniapp/orders', query },
    enableFallback ? () => ({ items: mockOrders, totalCount: mockOrders.length, pageIndex: 1, pageSize: mockOrders.length, totalPages: 1 }) : undefined
  )

  const items = toItems(result.data).map((item, index) => mapOrder(item, mockOrders[index] || {}))

  return {
    source: result.source,
    items: items.length ? items : mockOrders,
    totalCount: Number(result.data?.totalCount) || items.length || mockOrders.length,
    pageIndex: Number(result.data?.pageIndex) || 1,
    pageSize: Number(result.data?.pageSize) || items.length || mockOrders.length,
    totalPages: Number(result.data?.totalPages) || 1
  }
}
