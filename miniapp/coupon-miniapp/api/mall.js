import { request } from '@/utils/request'
import { normalizeUploadedAssetUrl } from '@/utils/asset-url'

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
    return `满${threshold}元可用`
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
    imageUrl: normalizeUploadedAssetUrl(firstValue(item, ['imageUrl'], fallback.imageUrl || '')),
    templateType: Number(firstValue(item, ['templateType'], fallback.templateType || 0))
  }
}

function mapProduct(item, fallback = {}) {
  return {
    id: Number(firstValue(item, ['id', 'productId'], fallback.id || Date.now())),
    title: String(firstValue(item, ['title', 'name'], fallback.title || '商品')),
    desc: String(firstValue(item, ['desc', 'description'], fallback.desc || '')),
    price: String(firstValue(item, ['price', 'salePrice'], fallback.price || '0')),
    imageUrl: normalizeUploadedAssetUrl(firstValue(item, ['imageUrl', 'mainImageUrl'], fallback.imageUrl || '')),
    erpIsbnCode: String(firstValue(item, ['erpIsbnCode'], fallback.erpIsbnCode || '')),
    barcodeText: String(firstValue(item, ['erpIsbnCode'], fallback.barcodeText || ''))
  }
}

export async function fetchMallPageData() {
  const response = await request({ url: '/api/miniapp/mall' })
  const payload = response.data || {}
  const packs = toItems(payload.packs).map((item) => mapPack(item, {}))
  const standaloneCoupons = toItems(payload.standaloneCoupons).map((item) => mapSaleCoupon(item, {}))
  const productCoupons = toItems(payload.productCoupons).map((item) => mapSaleCoupon(item, {}))
  const goods = toItems(payload.products).map((item) => mapProduct(item, {}))

  return {
    packs,
    standaloneCoupons,
    productCoupons,
    goods
  }
}

export async function fetchMiniAppProductList(params = {}) {
  const pageIndex = Number(params.pageIndex || 1)
  const pageSize = Number(params.pageSize || 8)
  const response = await request({
    url: '/api/miniapp/products',
    query: {
      keyword: params.keyword || undefined,
      pageIndex,
      pageSize
    }
  })
  const payload = response.data || {}
  const items = toItems(payload).map((item) => mapProduct(item, {}))

  return {
    items,
    totalCount: Number(payload.totalCount || items.length || 0),
    pageIndex: Number(payload.pageIndex || pageIndex),
    pageSize: Number(payload.pageSize || pageSize),
    totalPages: Number(payload.totalPages || 1)
  }
}

export async function fetchMiniAppProductDetail(productId) {
  const targetId = Number(productId || 0)
  if (!targetId) {
    return null
  }

  const response = await request({ url: `/api/miniapp/products/${targetId}` })
  const payload = response.data || {}
  const salePrice = firstValue(payload, ['salePrice', 'price'], '')
  const erpIsbnCode = firstValue(payload, ['erpIsbnCode'], '')

  return {
    id: Number(firstValue(payload, ['id'], targetId)),
    title: String(firstValue(payload, ['title', 'name'], '商品详情')),
    desc: String(firstValue(payload, ['desc', 'remark'], '')),
    erpOriginalPrice: String(firstValue(payload, ['erpOriginalPrice'], '')),
    price: String(firstValue(payload, ['price', 'salePrice'], '')),
    tag: '',
    imageUrl: normalizeUploadedAssetUrl(firstValue(payload, ['imageUrl', 'mainImageUrl'], '')),
    erpIsbnCode: String(erpIsbnCode || ''),
    canDirectPurchase: Boolean(firstValue(payload, ['canDirectPurchase'], false)),
    directPurchaseValidPeriodType: Number(firstValue(payload, ['directPurchaseValidPeriodType'], 0)),
    directPurchaseValidDays: Number(firstValue(payload, ['directPurchaseValidDays'], 0)),
    directPurchaseValidFrom: String(firstValue(payload, ['directPurchaseValidFrom'], '')),
    directPurchaseValidTo: String(firstValue(payload, ['directPurchaseValidTo'], '')),
    directPurchaseValidityText: String(firstValue(payload, ['directPurchaseValidityText'], '')),
    highlights: [
      erpIsbnCode ? `ERP ISBN码：${erpIsbnCode}` : '',
      salePrice ? `当前售价：￥${salePrice}` : '',
      String(firstValue(payload, ['directPurchaseValidityText'], '')),
      String(firstValue(payload, ['remark'], '')),
    ].filter(Boolean),
    detailImages: Array.isArray(payload.detailImageUrls) && payload.detailImageUrls.length
      ? payload.detailImageUrls.map((url) => normalizeUploadedAssetUrl(url))
      : (firstValue(payload, ['mainImageUrl'], '') ? [normalizeUploadedAssetUrl(firstValue(payload, ['mainImageUrl'], ''))] : []),
    availableCoupons: normalizeCoupons(payload.availableCoupons || payload.relatedCoupons, null),
    recommendedCoupons: normalizeCoupons(payload.recommendedCoupons, null, true),
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
      type: String(firstValue(item, ['type', 'templateTypeText'], '')) || formatCouponType(firstValue(item, ['templateType'], 0)),
      badge: String(firstValue(item, ['badge', 'scopeText'], '去领券')),
      templateId: Number(firstValue(item, ['templateId', 'couponTemplateId', 'id'], 0)),
      distributionMode: Number(firstValue(item, ['distributionMode'], 0)),
      salePrice: String(firstValue(item, ['salePrice'], '')),
    }))
  }

  if (!disableFallback && fallbackProduct) {
    return [{
      id: Number(fallbackProduct.id || Date.now()),
      title: '商品专享券',
      desc: '当前商品支持直购，也可搭配专享券使用。',
      amount: String(fallbackProduct.price || ''),
      threshold: '0',
      type: '商品券',
      badge: '当前商品',
      templateId: 0,
      distributionMode: 0,
      salePrice: String(fallbackProduct.price || ''),
    }]
  }

  return []
}
