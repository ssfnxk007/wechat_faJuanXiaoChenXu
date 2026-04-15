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
  const enableFallback = process.env.NODE_ENV !== 'production'
  const [packResult, productResult] = await Promise.all([
    requestWithFallback(
      { url: '/api/miniapp/coupon-packs', query },
      enableFallback ? () => ({ items: mockMallData.packs }) : undefined
    ),
    requestWithFallback(
      { url: '/api/miniapp/products', query },
      enableFallback ? () => ({ items: mockMallData.goods }) : undefined
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

export async function fetchMiniAppProductDetail(productId) {
  const targetId = Number(productId || 0)
  if (!targetId) {
    return null
  }

  const enableFallback = process.env.NODE_ENV !== 'production'
  const fallbackProduct = mockMallData.goods.find((item) => Number(item.id) === targetId)
  const result = await requestWithFallback(
    { url: `/api/miniapp/products/${targetId}` },
    enableFallback && fallbackProduct ? () => ({
      id: fallbackProduct.id,
      name: fallbackProduct.title,
      erpProductCode: fallbackProduct.tag,
      mainImageUrl: fallbackProduct.imageUrl || '',
      detailImageUrls: fallbackProduct.imageUrl ? [fallbackProduct.imageUrl] : [],
      salePrice: fallbackProduct.price,
      isEnabled: true,
      remark: fallbackProduct.desc,
      availableCoupons: buildFallbackCoupons(fallbackProduct),
    }) : undefined
  )

  const payload = result.data || {}
  return {
    id: Number(firstValue(payload, ['id'], targetId)),
    title: String(firstValue(payload, ['title', 'name'], fallbackProduct?.title || '商品详情')),
    desc: String(firstValue(payload, ['desc', 'remark'], fallbackProduct?.desc || '请以后端商品说明为准。')),
    price: String(firstValue(payload, ['price', 'salePrice'], fallbackProduct?.price || '')),
    tag: String(firstValue(payload, ['tag', 'erpProductCode'], fallbackProduct?.tag || '商品')),
    imageUrl: String(firstValue(payload, ['imageUrl', 'mainImageUrl'], fallbackProduct?.imageUrl || '')),
    highlights: [
      String(firstValue(payload, ['erpProductCode'], fallbackProduct?.tag || '精选商品')),
      firstValue(payload, ['salePrice'], fallbackProduct?.price) ? `参考价 ¥${firstValue(payload, ['salePrice'], fallbackProduct?.price)}` : '到店咨询',
      String(firstValue(payload, ['remark'], fallbackProduct?.desc || '支持搭配优惠券使用')),
    ],
    detailImages: Array.isArray(payload.detailImageUrls) && payload.detailImageUrls.length
      ? payload.detailImageUrls
      : (firstValue(payload, ['mainImageUrl'], fallbackProduct?.imageUrl) ? [String(firstValue(payload, ['mainImageUrl'], fallbackProduct?.imageUrl))] : []),
    availableCoupons: normalizeCoupons(payload.availableCoupons || payload.relatedCoupons, fallbackProduct),
    recommendedCoupons: normalizeCoupons(payload.recommendedCoupons, fallbackProduct, true),
  }
}

function normalizeCoupons(value, fallbackProduct, disableFallback = false) {
  if (Array.isArray(value) && value.length) {
    return value.map((item, index) => ({
      id: Number(firstValue(item, ['id', 'couponTemplateId'], Date.now() + index)),
      title: String(firstValue(item, ['title', 'name', 'couponTemplateName'], '可用优惠券')),
      desc: String(firstValue(item, ['desc', 'remark'], '适合当前商品使用')),
      amount: String(firstValue(item, ['amount', 'discountAmount'], '')),
      threshold: String(firstValue(item, ['threshold', 'thresholdAmount'], '')),
      type: String(firstValue(item, ['type', 'templateTypeText', 'templateType'], '优惠券')),
      badge: String(firstValue(item, ['badge', 'scopeText'], '去领取')),
      templateId: Number(firstValue(item, ['templateId', 'couponTemplateId', 'id'], 0)),
    }))
  }
  return disableFallback ? [] : buildFallbackCoupons(fallbackProduct)
}

function buildFallbackCoupons(product) {
  if (!product) return []
  return [
    {
      id: product.id * 10 + 1,
      title: `${product.title} 专享券`,
      desc: '适合当前商品使用，领取后可进入券包查看',
      amount: '10',
      threshold: '99',
      type: '满减券',
      badge: '去领取',
      templateId: 1,
    },
    {
      id: product.id * 10 + 2,
      title: '门店通用券',
      desc: '到店核销时可配合当前商品使用',
      amount: '5',
      threshold: '0',
      type: '无门槛券',
      badge: '看详情',
      templateId: 1,
    },
  ]
}
