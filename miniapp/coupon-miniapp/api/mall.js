import { requestWithFallback } from '@/utils/request'

const mockMallData = {
  packs: [
    { id: 1, title: '春序礼遇券包', subtitle: '组合权益 · 门店可核销', price: '29.9', desc: '含满减券、门店通用券与精品专区券，可分次使用。', meta: '限购 1 份' },
    { id: 2, title: '雅礼组合券包', subtitle: '门店专享 · 节气精选', price: '79.9', desc: '适合礼赠和高客单商品组合使用', meta: '每人限购 2 份' }
  ],
  goods: [
    { id: 1, title: '云锦礼盒', desc: '适合搭配指定商品券，适合节日礼赠。', price: '128', tag: '指定商品券' },
    { id: 2, title: '东方香礼', desc: '适合与满减券联动，提升客单价。', price: '168', tag: '满减可用' },
    { id: 3, title: '四时雅饮', desc: '轻消费场景友好，适合无门槛券核销。', price: '88', tag: '无门槛券' },
    { id: 4, title: '臻选伴手礼', desc: '门店常卖款，适合券包权益组合带动。', price: '218', tag: '券包推荐' }
  ]
}

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

function mapPack(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'couponPackId'], fallback.id || Date.now()),
    title: String(firstValue(item, ['title', 'name'], fallback.title || '券包')),
    subtitle: String(firstValue(item, ['subtitle', 'remark'], fallback.subtitle || '组合权益')),
    price: String(firstValue(item, ['price', 'salePrice'], fallback.price || '0')),
    desc: String(firstValue(item, ['desc', 'description', 'remark'], fallback.desc || '请以后端说明为准')),
    meta: String(firstValue(item, ['meta', 'limitText', 'saleTimeText'], fallback.meta || ''))
  }
}

function mapProduct(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'productId'], fallback.id || Date.now()),
    title: String(firstValue(item, ['title', 'name'], fallback.title || '商品')),
    desc: String(firstValue(item, ['desc', 'description', 'erpProductCode'], fallback.desc || '')),
    price: String(firstValue(item, ['price', 'salePrice'], fallback.price || '0')),
    tag: String(firstValue(item, ['tag', 'erpProductCode'], fallback.tag || '商品')),
    imageUrl: String(firstValue(item, ['imageUrl', 'mainImageUrl'], fallback.imageUrl || ''))
  }
}

export async function fetchMallPageData(query = {}) {
  const [packResult, productResult] = await Promise.all([
    requestWithFallback(
      { url: '/api/miniapp/coupon-packs', query },
      () => ({ items: mockMallData.packs })
    ),
    requestWithFallback(
      { url: '/api/miniapp/products', query },
      () => ({ items: mockMallData.goods })
    )
  ])

  const packs = toItems(packResult.data).map((item, index) => mapPack(item, mockMallData.packs[index] || {}))
  const goods = toItems(productResult.data).map((item, index) => mapProduct(item, mockMallData.goods[index] || {}))

  return {
    source: packResult.source === 'remote' || productResult.source === 'remote' ? 'mixed' : 'fallback',
    packs: packs.length ? packs : mockMallData.packs,
    goods: goods.length ? goods : mockMallData.goods
  }
}
