import { request } from '@/utils/request'
import { useSessionStore } from '@/store/session'
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

function toList(value) {
  return Array.isArray(value) ? value : []
}

function mapCoupon(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'couponTemplateId'], fallback.id || Date.now()),
    tag: String(firstValue(item, ['tag', 'typeName', 'type', 'scopeText'], fallback.tag || '门店通用')),
    title: String(firstValue(item, ['title', 'name', 'couponName'], fallback.title || '优惠券')),
    desc: String(firstValue(item, ['desc', 'description', 'summary', 'remark'], fallback.desc || '')),
    date: String(firstValue(item, ['date', 'validityText', 'expireText', 'validTo', 'validFrom'], fallback.date || '请以后端有效期为准')),
    amount: String(firstValue(item, ['amount', 'discountAmount', 'faceValue'], fallback.amount || '0')),
    type: String(firstValue(item, ['type', 'tag', 'typeName'], fallback.type || '优惠券')),
    meta: String(firstValue(item, ['meta', 'scopeText', 'remark'], fallback.meta || '门店通用'))
  }
}

function mapPack(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'couponPackId'], fallback.id || Date.now()),
    title: String(firstValue(item, ['title', 'name'], fallback.title || '券包')),
    subtitle: String(firstValue(item, ['subtitle', 'remark'], fallback.subtitle || '组合权益')),
    price: String(firstValue(item, ['price', 'salePrice', 'orderAmount'], fallback.price || '0')),
    desc: String(firstValue(item, ['desc', 'description', 'remark'], fallback.desc || '请以后端券包说明为准')),
    meta: String(firstValue(item, ['meta', 'limitText', 'saleTimeText'], fallback.meta || ''))
  }
}

function mapProduct(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'productId'], fallback.id || Date.now()),
    title: String(firstValue(item, ['title', 'name'], fallback.title || '商品')),
    desc: String(firstValue(item, ['desc', 'description'], fallback.desc || '')),
    price: String(firstValue(item, ['price', 'salePrice'], fallback.price || '0')),
    imageUrl: normalizeUploadedAssetUrl(firstValue(item, ['imageUrl', 'mainImageUrl'], fallback.imageUrl || '')),
    erpIsbnCode: String(firstValue(item, ['erpIsbnCode'], fallback.erpIsbnCode || '')),
    barcodeText: String(firstValue(item, ['erpIsbnCode'], fallback.barcodeText || '')),
    tag: String(firstValue(item, ['tag'], fallback.tag || ''))
  }
}

function normalizeCouponType(value, isNewUserOnly) {
  if (isNewUserOnly) {
    return '新人专享'
  }

  const normalized = Number(value)
  if (normalized === 2) return '无门槛券'
  if (normalized === 3) return '指定商品券'
  if (normalized === 4) return '满减券'
  return typeof value === 'string' && Number.isNaN(normalized) ? value : '活动券'
}

function normalizeCouponMeta(item, fallback = {}) {
  const validPeriodType = Number(item?.validPeriodType || 0)
  if (validPeriodType === 1 && item?.validFrom && item?.validTo) {
    return `${String(item.validFrom).slice(0, 10)} 至 ${String(item.validTo).slice(0, 10)}`
  }
  if (validPeriodType === 2 && item?.validDays) {
    return `领取后 ${item.validDays} 天有效`
  }
  return String(firstValue(item, ['meta', 'remark'], fallback.meta || '门店通用'))
}

function mapBanner(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'bannerId'], fallback.id || Date.now()),
    title: String(firstValue(item, ['title', 'name'], fallback.title || '')),
    subtitle: String(firstValue(item, ['subtitle'], fallback.subtitle || '')),
    ctaText: String(firstValue(item, ['ctaText'], fallback.ctaText || '')),
    illustrationUrl: normalizeUploadedAssetUrl(firstValue(item, ['illustrationUrl', 'imageUrl', 'fileUrl'], fallback.illustrationUrl || '')),
    linkUrl: String(firstValue(item, ['linkUrl', 'url'], fallback.linkUrl || ''))
  }
}

function normalizeHomeData(payload = {}) {
  const sourceCoupons = toList(payload.directCoupons || payload.featuredCoupons || payload.coupons)
  const newcomerSource = payload.newcomerCoupon || payload.welcomeCoupon || sourceCoupons.find((item) => item?.isNewUserOnly)
  const newcomer = newcomerSource ? mapCoupon(newcomerSource || {}, {}) : null
  const directCoupons = sourceCoupons.map((item) => {
    const mapped = mapCoupon(item, {})
    return {
      ...mapped,
      type: normalizeCouponType(item?.templateType || mapped.type, item?.isNewUserOnly),
      tag: normalizeCouponType(item?.templateType || mapped.tag, item?.isNewUserOnly),
      meta: normalizeCouponMeta(item, mapped)
    }
  })

  const featuredPacks = toList(payload.featuredPacks || payload.featuredCouponPacks || payload.couponPacks || payload.packItems)
    .map((item) => mapPack(item, {}))

  const products = toList(payload.products || payload.recommendedProducts || payload.productItems)
    .map((item) => mapProduct(item, {}))

  const banners = toList(payload.banners)
    .map((item) => mapBanner(item, {}))
    .filter((item) => item.illustrationUrl || item.title)

  return {
    banners,
    newcomerCoupon: newcomer,
    directCoupons,
    featuredPacks,
    products
  }
}

export async function fetchHomePageData() {
  const session = useSessionStore()
  const response = await request({ url: '/api/miniapp/home', query: { userId: session.userId || undefined } })
  const payload = response.data || {}

  return {
    theme: payload?.theme,
    ...normalizeHomeData(payload)
  }
}
