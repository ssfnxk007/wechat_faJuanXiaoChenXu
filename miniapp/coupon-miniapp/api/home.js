import { requestWithFallback } from '@/utils/request'
import { useSessionStore } from '@/store/session'

const mockHomeData = {
  banners: [
    { id: 1, title: '新人有礼', imageUrl: '', linkUrl: '/pages/activity/detail?key=newcomer' },
    { id: 2, title: '免费领取', imageUrl: '', linkUrl: '/pages/activity/detail?key=free' },
    { id: 3, title: '到店核销', imageUrl: '', linkUrl: '/pages/activity/detail?key=writeoff' }
  ],
  newcomerCoupon: {
    id: 1,
    tag: '新人专享',
    title: '新人礼券',
    desc: '无门槛立减 10 元，门店通用',
    date: '领取后立即生效，7 天内有效',
    amount: '10'
  },
  directCoupons: [
    { id: 1, type: '无门槛券', title: '门店通用立减券', desc: '全场通用，消费即可抵扣 8 元', meta: '今日可领 · 门店通用' },
    { id: 2, type: '满减券', title: '春日满减券', desc: '满 100 元减 20 元，适合日常选购', meta: '限时活动 · 领取后 7 天有效' },
    { id: 3, type: '指定商品券', title: '精品专区券', desc: '指定商品立减 30 元，适用精品专区', meta: '商品券 · 指定商品可用' },
    { id: 4, type: '活动券', title: '周末到店礼券', desc: '周末到店即可使用，支持门店核销', meta: '活动发放 · 指定门店可用' }
  ],
  featuredPacks: [
    { id: 1, title: '春序礼遇券包', subtitle: '组合权益 · 门店可核销', price: '29.9', desc: '含 2 张无门槛券 + 3 张满减券 + 1 张指定商品券', meta: '限购 1 份' },
    { id: 2, title: '节气雅礼券包', subtitle: '节气限定 · 适用精品专区', price: '59.9', desc: '含 1 张指定商品券 + 2 张满减券 + 2 张无门槛券', meta: '活动期内可购' }
  ],
  products: [
    { id: 1, title: '云锦礼盒', desc: '适合搭配指定商品券使用', price: '128', tag: '指定商品券' },
    { id: 2, title: '东方香礼', desc: '支持门店核销与券包组合', price: '168', tag: '满减可用' },
    { id: 3, title: '四时雅饮', desc: '轻巧日常，适合首单使用', price: '88', tag: '新人推荐' },
    { id: 4, title: '甄选伴手礼', desc: '礼赠与自用皆可', price: '218', tag: '券包精选' }
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

function toList(value) {
  return Array.isArray(value) ? value : []
}

function mapCoupon(item, fallback = {}) {
  return {
    id: firstValue(item, ['id', 'couponTemplateId'], fallback.id || Date.now()),
    tag: String(firstValue(item, ['tag', 'typeName', 'type', 'scopeText', 'templateType'], fallback.tag || '门店通用')),
    title: String(firstValue(item, ['title', 'name', 'couponName'], fallback.title || '优惠券')),
    desc: String(firstValue(item, ['desc', 'description', 'summary', 'remark'], fallback.desc || '')),
    date: String(firstValue(item, ['date', 'validityText', 'expireText', 'validTo', 'validFrom'], fallback.date || '请以后端有效期为准')),
    amount: String(firstValue(item, ['amount', 'discountAmount', 'faceValue'], fallback.amount || '0')),
    type: String(firstValue(item, ['type', 'tag', 'typeName', 'templateType'], fallback.type || '优惠券')),
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
    desc: String(firstValue(item, ['desc', 'description', 'erpProductCode'], fallback.desc || '')),
    price: String(firstValue(item, ['price', 'salePrice'], fallback.price || '0')),
    tag: String(firstValue(item, ['tag', 'erpProductCode'], fallback.tag || '商品')),
    imageUrl: String(firstValue(item, ['imageUrl', 'mainImageUrl'], fallback.imageUrl || ''))
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
    illustrationUrl: String(firstValue(item, ['illustrationUrl', 'imageUrl', 'fileUrl'], fallback.illustrationUrl || '')),
    linkUrl: String(firstValue(item, ['linkUrl', 'url'], fallback.linkUrl || ''))
  }
}

function normalizeHomeData(payload = {}) {
  const sourceCoupons = toList(payload.directCoupons || payload.featuredCoupons || payload.coupons)
  const newcomerSource = payload.newcomerCoupon || payload.welcomeCoupon || sourceCoupons.find((item) => item?.isNewUserOnly)
  const newcomer = mapCoupon(newcomerSource || {}, mockHomeData.newcomerCoupon)
  const directCoupons = sourceCoupons
    .map((item, index) => {
      const mapped = mapCoupon(item, mockHomeData.directCoupons[index] || {})
      return {
        ...mapped,
        type: normalizeCouponType(item?.templateType || mapped.type, item?.isNewUserOnly),
        tag: normalizeCouponType(item?.templateType || mapped.tag, item?.isNewUserOnly),
        meta: normalizeCouponMeta(item, mapped)
      }
    })

  const featuredPacks = (toList(payload.featuredPacks || payload.featuredCouponPacks || payload.couponPacks || payload.packItems) || mockHomeData.featuredPacks)
    .map((item, index) => mapPack(item, mockHomeData.featuredPacks[index] || {}))

  const products = (toList(payload.products || payload.recommendedProducts || payload.productItems) || mockHomeData.products)
    .map((item, index) => mapProduct(item, mockHomeData.products[index] || {}))

  const banners = toList(payload.banners)
    .map((item, index) => mapBanner(item, mockHomeData.banners[index] || {}))
    .filter((item) => item.illustrationUrl || item.title)

  return {
    banners: banners.length ? banners : mockHomeData.banners,
    newcomerCoupon: newcomer,
    directCoupons: directCoupons.length ? directCoupons : mockHomeData.directCoupons,
    featuredPacks: featuredPacks.length ? featuredPacks : mockHomeData.featuredPacks,
    products: products.length ? products : mockHomeData.products
  }
}

export async function fetchHomePageData() {
  const session = useSessionStore()
  const enableFallback = process.env.NODE_ENV !== 'production'
  const result = await requestWithFallback(
    { url: '/api/miniapp/home', query: { userId: session.userId || undefined } },
    enableFallback ? () => mockHomeData : undefined
  )

  return {
    source: result.source,
    theme: result.data?.theme,
    ...normalizeHomeData(result.data)
  }
}
