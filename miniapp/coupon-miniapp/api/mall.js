import { requestWithFallback } from '@/utils/request'

const mockMallData = {
  packs: [
    { id: 1, title: '春序礼遇券包', subtitle: '组合权益 · 门店可核销', price: '29.9', desc: '含满减券、门店通用券与精品专区券，可分次使用。', meta: '限购 1 份' },
    { id: 2, title: '雅礼组合券包', subtitle: '门店专享 · 节气精选', price: '79.9', desc: '适合礼赠和高客单商品组合使用', meta: '每人限购 2 份' }
  ],
  standaloneCoupons: [
    { id: 101, title: '门店通用立减券', subtitle: '单张售卖券', price: '19.9', amount: '20', threshold: '99', desc: '适合常规到店消费场景，购买后自动入包。', meta: '满 99 元可用', fulfillmentHint: '支付成功后立即发券' },
    { id: 102, title: '轻享无门槛券', subtitle: '单张售卖券', price: '9.9', amount: '10', threshold: '0', desc: '轻消费场景友好，适合快速转化。', meta: '无门槛使用', fulfillmentHint: '支付成功后立即发券' }
  ],
  productCoupons: [
    { id: 201, title: '云锦礼盒商品券', subtitle: '商品券', price: '39.9', amount: '50', threshold: '0', desc: '购买后发放商品券，后续等待商品履约。', meta: '对应商品：云锦礼盒', productSummary: '云锦礼盒', fulfillmentHint: '支付成功后待 ERP 处理' }
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

function formatCouponType(templateType) {
  return ({
    1: '新人券',
    2: '无门槛券',
    3: '商品券',
    4: '满减券'
  }[Number(templateType)] || '优惠券')
}

function buildCouponMeta(item, fallback = {}) {
  const threshold = firstValue(item, ['threshold', 'thresholdAmount'], fallback.threshold || '')
  const productSummary = firstValue(item, ['productSummary'], fallback.productSummary || '')
  if (productSummary) {
    return `对应商品：${productSummary}`
  }
  if (threshold && String(threshold) !== '0') {
    return `满 ${threshold} 元可用`
  }
  return String(firstValue(item, ['meta'], fallback.meta || '无门槛使用'))
}

function mapSaleCoupon(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'couponTemplateId'], fallback.id || Date.now()),
    title: String(firstValue(item, ['title', 'name'], fallback.title || '售卖券')),
    subtitle: String(firstValue(item, ['subtitle'], fallback.subtitle || formatCouponType(firstValue(item, ['templateType'], 0)))),
    price: String(firstValue(item, ['price', 'salePrice'], fallback.price || '0')),
    amount: String(firstValue(item, ['amount', 'discountAmount'], fallback.amount || '')),
    threshold: String(firstValue(item, ['threshold', 'thresholdAmount'], fallback.threshold || '')),
    desc: String(firstValue(item, ['desc', 'remark', 'templateRemark'], fallback.desc || '支付后自动发放')),
    meta: buildCouponMeta(item, fallback),
    productSummary: String(firstValue(item, ['productSummary'], fallback.productSummary || '')),
    fulfillmentHint: String(firstValue(item, ['fulfillmentHint'], fallback.fulfillmentHint || '支付成功后立即发券')),
    imageUrl: String(firstValue(item, ['imageUrl'], fallback.imageUrl || '')),
    templateType: Number(firstValue(item, ['templateType'], fallback.templateType || 0))
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

export async function fetchMallPageData() {
  const enableFallback = process.env.NODE_ENV !== 'production'
  const result = await requestWithFallback(
    { url: '/api/miniapp/mall' },
    enableFallback ? () => mockMallData : undefined
  )

  const payload = result.data || {}
  const packs = toItems(payload.packs).map((item, index) => mapPack(item, mockMallData.packs[index] || {}))
  const standaloneCoupons = toItems(payload.standaloneCoupons).map((item, index) => mapSaleCoupon(item, mockMallData.standaloneCoupons[index] || {}))
  const productCoupons = toItems(payload.productCoupons).map((item, index) => mapSaleCoupon(item, mockMallData.productCoupons[index] || {}))
  const goods = toItems(payload.products).map((item, index) => mapProduct(item, mockMallData.goods[index] || {}))

  return {
    source: result.source,
    packs: packs.length ? packs : mockMallData.packs,
    standaloneCoupons: standaloneCoupons.length ? standaloneCoupons : mockMallData.standaloneCoupons,
    productCoupons: productCoupons.length ? productCoupons : mockMallData.productCoupons,
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
    erpOriginalPrice: String(firstValue(payload, ['erpOriginalPrice'], '')),
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
      distributionMode: Number(firstValue(item, ['distributionMode'], 0)),
      salePrice: String(firstValue(item, ['salePrice'], '')),
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
      distributionMode: 0,
      salePrice: '',
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
      distributionMode: 0,
      salePrice: '',
    },
  ]
}
