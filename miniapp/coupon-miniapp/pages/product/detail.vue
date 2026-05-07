<template>
  <view :class="['cm-page', themeClass]">
    <view class="cm-nav-spacer"></view>
    <view class="cm-container product-body">
      <view class="product-topbar">
        <view class="product-back" @click="goBack">返回</view>
        <view class="product-badge">商品详情</view>
      </view>

      <view class="product-gallery" v-if="galleryImages.length">
        <image
          v-for="(image, index) in galleryImages"
          :key="`${image}-${index}`"
          class="gallery-image cm-card"
          :src="image"
          mode="widthFix"
        />
      </view>

      <view class="cm-section">
        <view class="product-info-card cm-card">
          <text class="product-title">{{ detail.title }}</text>
          <view class="price-row">
            <text class="price-unit">￥</text>
            <text class="price-value">{{ detail.price || '--' }}</text>
            <view class="hero-tag" v-if="detail.tag">{{ detail.tag }}</view>
          </view>
          <view class="price-compare-row" v-if="showPriceCompare">
            <text class="origin-price">ERP 售价 ￥{{ detail.erpOriginalPrice }}</text>
            <text class="discount-price">立省 ￥{{ discountAmount }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="DESCRIPTION" title="商品说明" subtitle="用于商品展示与活动承接" />
        <view class="desc-card cm-card">
          <text class="desc-text">{{ detail.desc || '请以后端商品说明为准。' }}</text>
        </view>
      </view>

      <view class="cm-section" v-if="detail.highlights.length">
        <SectionHeader eyebrow="HIGHLIGHTS" title="商品亮点" subtitle="帮助用户快速了解商品卖点" />
        <view class="highlight-stack">
          <view class="highlight-card cm-card" v-for="item in detail.highlights" :key="item">
            <text class="highlight-text">{{ item }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section" v-if="detail.availableCoupons.length">
        <SectionHeader eyebrow="AVAILABLE COUPONS" title="商品专享券" subtitle="这些券与当前商品直接关联" />
        <view v-if="detail.canDirectPurchase" class="coupon-hint cm-card">
          <text class="coupon-hint-text">以下优惠券仅作补充参考，不影响当前商品直接购买。</text>
        </view>
        <view class="coupon-stack">
          <view class="coupon-card cm-card" v-for="coupon in detail.availableCoupons" :key="coupon.id">
            <view class="coupon-top">
              <view>
                <text class="coupon-type">{{ coupon.type }}</text>
                <text class="coupon-title">{{ coupon.title }}</text>
              </view>
              <view class="coupon-amount-box">
                <text class="coupon-amount">{{ coupon.amount || '--' }}</text>
                <text class="coupon-unit">元</text>
              </view>
            </view>
            <text class="coupon-desc">{{ coupon.desc }}</text>
            <view class="coupon-footer">
              <text class="coupon-threshold">{{ formatThreshold(coupon.threshold, '仅当前商品可用') }}</text>
              <view class="coupon-action" @click="openCoupon(coupon)">查看券详情</view>
            </view>
          </view>
        </view>
      </view>

      <view class="cm-section" v-if="detail.recommendedCoupons.length">
        <SectionHeader eyebrow="RECOMMENDED COUPONS" title="推荐优惠券" subtitle="当前商品也适合搭配这些通用券" />
        <view v-if="detail.canDirectPurchase" class="coupon-hint cm-card coupon-hint-secondary">
          <text class="coupon-hint-text">推荐券不是购买前提，是否领取不影响直接下单。</text>
        </view>
        <view class="coupon-stack coupon-stack-secondary">
          <view class="coupon-card cm-card coupon-card-secondary" v-for="coupon in detail.recommendedCoupons" :key="`recommend-${coupon.id}`">
            <view class="coupon-top">
              <view>
                <text class="coupon-type">{{ coupon.type }}</text>
                <text class="coupon-title">{{ coupon.title }}</text>
              </view>
              <view class="coupon-amount-box secondary-box">
                <text class="coupon-amount">{{ coupon.amount || '--' }}</text>
                <text class="coupon-unit">元</text>
              </view>
            </view>
            <text class="coupon-desc">{{ coupon.desc }}</text>
            <view class="coupon-footer">
              <text class="coupon-threshold">{{ formatThreshold(coupon.threshold, '通用可领') }}</text>
              <view class="coupon-action" @click="openCoupon(coupon)">查看券详情</view>
            </view>
          </view>
        </view>
      </view>

      <view class="buy-bar cm-card">
        <view class="buy-left">
          <text class="buy-title">{{ detail.title }}</text>
          <text class="buy-summary">{{ buySummaryText }}</text>
        </view>
        <view class="buy-right">
          <text class="buy-price">￥{{ detail.price || '--' }}</text>
          <view class="buy-button" @click="handleBuyProduct">{{ buyButtonText }}</view>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import { useTheme } from '@/composables/use-theme'
import { ensureMiniProgramLogin, ensurePhoneReady } from '@/api/auth'
import { fetchMiniAppProductDetail } from '@/api/mall'
import { completeMiniAppOrderPayment, createMiniAppOrder, createMiniAppOrderPayment } from '@/api/miniapp'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const buying = ref(false)

const detail = ref({
  id: 0,
  title: '商品详情',
  desc: '',
  erpOriginalPrice: '',
  price: '',
  tag: '',
  imageUrl: '',
  highlights: [],
  detailImages: [],
  canDirectPurchase: false,
  directPurchaseValidityText: '',
  availableCoupons: [],
  recommendedCoupons: [],
})

const buyButtonText = computed(() => buying.value
  ? '下单中'
  : (detail.value.canDirectPurchase ? '去购买' : '暂不可购'))
const buySummaryText = computed(() => {
  if (!detail.value.canDirectPurchase) {
    return detail.value.directPurchaseValidityText || '当前商品暂未开启直购'
  }

  return detail.value.directPurchaseValidityText || '可直接购买，支付成功后自动发放1张商品提货券'
})
const showPriceCompare = computed(() => {
  const origin = Number(detail.value.erpOriginalPrice || 0)
  const price = Number(detail.value.price || 0)
  return origin > 0 && price > 0 && origin > price
})
const discountAmount = computed(() => {
  const origin = Number(detail.value.erpOriginalPrice || 0)
  const price = Number(detail.value.price || 0)
  if (origin > price) {
    return (origin - price).toFixed(2)
  }
  return '0.00'
})

const galleryImages = computed(() => {
  const list = []
  const cover = detail.value.imageUrl
  if (cover) list.push(cover)
  detail.value.detailImages.forEach(img => {
    if (img && !list.includes(img)) list.push(img)
  })
  return list
})

onLoad(async (options = {}) => {
  const id = Number(options.id || options.productId || 0)
  if (!id) {
    uni.showToast({ title: '商品不存在', icon: 'none' })
    return
  }

  const result = await fetchMiniAppProductDetail(id)
  if (!result) {
    uni.showToast({ title: '商品不存在', icon: 'none' })
    return
  }

  detail.value = {
    id: result.id,
    title: result.title,
    desc: result.desc,
    erpOriginalPrice: result.erpOriginalPrice,
    price: result.price,
    tag: result.tag,
    imageUrl: result.imageUrl,
    highlights: Array.isArray(result.highlights) ? result.highlights : [],
    detailImages: Array.isArray(result.detailImages) ? result.detailImages : [],
    canDirectPurchase: Boolean(result.canDirectPurchase),
    directPurchaseValidityText: result.directPurchaseValidityText || '',
    availableCoupons: Array.isArray(result.availableCoupons) ? result.availableCoupons : [],
    recommendedCoupons: Array.isArray(result.recommendedCoupons) ? result.recommendedCoupons : [],
  }
})

function goBack() {
  uni.navigateBack({ delta: 1 })
}

function openCoupon(coupon) {
  const templateId = Number(coupon?.templateId || coupon?.id || 0)
  if (!templateId) {
    uni.showToast({ title: '券信息不存在', icon: 'none' })
    return
  }
  if (Number(coupon?.distributionMode) === 1) {
    uni.navigateTo({ url: `/pages/sale-coupon/detail?id=${templateId}` })
    return
  }
  uni.navigateTo({ url: `/pages/coupon/detail?templateId=${templateId}` })
}

async function handleBuyProduct() {
  if (buying.value) {
    return
  }

  if (!detail.value.canDirectPurchase || !detail.value.id) {
    uni.showToast({ title: detail.value.directPurchaseValidityText || '暂不可购买', icon: 'none' })
    return
  }

  buying.value = true
  uni.showLoading({ title: '正在下单', mask: true })

  try {
    await ensureMiniProgramLogin()
    if (!session.userId) {
      await ensureMiniProgramLogin({ force: true })
    }

    if (!session.userId) {
      await new Promise((resolve) => {
        uni.showModal({
          title: '登录失败',
          content: session.loginMessage || '请先完成登录后再购买商品',
          confirmText: '去我的',
          cancelText: '取消',
          success: (result) => {
            if (result.confirm) {
              uni.switchTab({ url: '/pages/profile/index' })
            }
            resolve()
          },
          fail: () => resolve()
        })
      })
      return
    }

    const ready = await ensurePhoneReady({
      force: true,
      redirect: `/pages/product/detail?id=${detail.value.id}`
    })
    if (!ready) {
      return
    }

    const order = await createMiniAppOrder({ productId: detail.value.id })
    const paymentResult = await createMiniAppOrderPayment(order.orderId, {})
    if (paymentResult.paid || paymentResult.payment?.isMock) {
      uni.showToast({ title: '购买成功', icon: 'success' })
      uni.navigateTo({ url: `/pages/order/result?orderId=${order.orderId}` })
      return
    }

    // #ifdef MP-WEIXIN
    await new Promise((resolve, reject) => {
      uni.requestPayment({
        provider: 'wxpay',
        timeStamp: paymentResult.payment?.timeStamp || '',
        nonceStr: paymentResult.payment?.nonceStr || '',
        package: paymentResult.payment?.packageValue || '',
        signType: paymentResult.payment?.signType || 'RSA',
        paySign: paymentResult.payment?.paySign || '',
        success: resolve,
        fail: reject
      })
    })

    await completeMiniAppOrderPayment(order.orderId, {
      paymentNo: paymentResult.payment?.paymentNo,
      channelTradeNo: paymentResult.payment?.prepayId,
      rawCallback: 'miniapp-product-direct-purchase-success'
    })

    uni.showToast({ title: '支付成功', icon: 'success' })
    uni.navigateTo({ url: `/pages/order/result?orderId=${order.orderId}` })
    return
    // #endif

    // #ifndef MP-WEIXIN
    uni.showToast({ title: '请在微信小程序中完成支付', icon: 'none' })
    uni.navigateTo({ url: `/pages/order/result?orderId=${order.orderId}` })
    // #endif
  } catch (error) {
    console.warn('[product-detail] direct purchase failed', error)
    const message = error?.errMsg?.includes('cancel')
      ? '已取消支付'
      : (error?.message || '购买失败，请稍后再试')
    uni.showToast({ title: message, icon: 'none' })
  } finally {
    uni.hideLoading()
    buying.value = false
  }
}

function formatThreshold(value, fallback) {
  if (value && value !== '0') {
    return `满${value}元可用`
  }
  return fallback
}
</script>

<style lang="scss" scoped>
.product-body {
  padding-top: 18rpx;
  padding-bottom: 40rpx;
}

.product-topbar,
.coupon-top,
.coupon-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
}

.product-topbar {
  margin-bottom: 20rpx;
}

.product-back,
.product-badge,
.hero-tag,
.coupon-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 48rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  font-size: 22rpx;
}

.product-back,
.product-badge {
  background: rgba(15, 23, 42, 0.06);
  color: $cm-text-primary;
}

.hero-tag {
  background: $cm-primary;
  color: #fff;
  font-size: 22rpx;
}

.product-gallery {
  display: grid;
  gap: 18rpx;
  margin-bottom: 24rpx;
}

.product-info-card {
  padding: 28rpx;
  display: grid;
  gap: 16rpx;
}

.product-title {
  color: $cm-text-primary;
  font-size: 40rpx;
  font-weight: 700;
}

.desc-text,
.highlight-text,
.coupon-desc,
.coupon-threshold {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.7;
}

.price-row {
  display: flex;
  align-items: baseline;
  gap: 8rpx;
  flex-wrap: wrap;
}
.price-compare-row {
  display: flex;
  align-items: center;
  gap: 12rpx;
}

.price-unit,
.price-value,
.coupon-amount {
  color: $cm-primary-strong;
  font-weight: 700;
}

.price-unit {
  font-size: 28rpx;
}

.price-value {
  font-size: 46rpx;
}
.origin-price {
  color: $cm-text-tertiary;
  font-size: 22rpx;
  text-decoration: line-through;
}
.discount-price {
  color: $cm-warning;
  font-size: 22rpx;
  font-weight: 700;
}

.coupon-type {
  color: #fffaf4;
}

.desc-card,
.highlight-card,
.coupon-card {
  padding: 24rpx;
}

.highlight-stack,
.coupon-stack {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
}

.coupon-hint {
  margin-top: 18rpx;
  padding: 18rpx 24rpx;
  background: rgba(59, 130, 246, 0.08);
  border: 1rpx solid rgba(59, 130, 246, 0.16);
}

.coupon-hint-secondary {
  background: rgba(148, 163, 184, 0.08);
  border-color: rgba(148, 163, 184, 0.16);
}

.coupon-hint-text {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.6;
}

.gallery-image {
  width: 100%;
  border-radius: 24rpx;
  overflow: hidden;
}

.coupon-card {
  display: grid;
  gap: 16rpx;
}

.coupon-card-secondary {
  background: rgba(255, 255, 255, 0.92);
}

.coupon-title {
  display: block;
  margin-top: 10rpx;
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}

.coupon-amount-box {
  display: flex;
  align-items: baseline;
  gap: 4rpx;
  color: $cm-primary-strong;
}

.secondary-box {
  color: $cm-text-primary;
}

.coupon-amount {
  font-size: 42rpx;
}

.coupon-action {
  background: $cm-primary;
  color: #fff;
}

.buy-bar {
  position: sticky;
  bottom: 24rpx;
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20rpx;
  margin-top: 22rpx;
  padding: 22rpx 24rpx;
  backdrop-filter: blur(18rpx);
  z-index: 10;
}

.buy-left,
.buy-right {
  display: grid;
  gap: 8rpx;
}

.buy-left {
  flex: 1;
  min-width: 0;
}

.buy-right {
  flex-shrink: 0;
  justify-items: end;
  gap: 10rpx;
}

.buy-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}

.buy-summary {
  color: $cm-text-secondary;
  font-size: 22rpx;
}

.buy-price {
  color: $cm-primary-strong;
  font-size: 42rpx;
  font-weight: 700;
  line-height: 1;
}

.buy-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 64rpx;
  min-width: 168rpx;
  padding: 0 32rpx;
  border-radius: 999rpx;
  background: $cm-primary;
  color: #fff;
  font-size: 24rpx;
  font-weight: 700;
}

.theme-light .product-back,
.theme-light .product-badge,
.theme-light .hero-tag,
.theme-light .coupon-type {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-candy .product-back,
.theme-candy .product-badge,
.theme-candy .hero-tag,
.theme-candy .coupon-type {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(191, 219, 254, 0.7);
  color: #2563eb;
}

.theme-candy .product-info-card,
.theme-candy .coupon-card,
.theme-candy .coupon-card-secondary,
.theme-candy .buy-bar {
  background: linear-gradient(180deg, #ffffff 0%, #eff6ff 100%);
  border: 1rpx solid rgba(191, 219, 254, 0.7);
  box-shadow: 0 18rpx 44rpx rgba(37, 99, 235, 0.06);
}

.theme-candy .coupon-amount-box {
  color: #2563eb;
}

.theme-candy .secondary-box {
  color: #1e3a8a;
}

.theme-candy .coupon-action,
.theme-candy .buy-button {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
  color: #ffffff;
  box-shadow: 0 18rpx 36rpx rgba(37, 99, 235, 0.18);
}

.theme-candy .buy-title {
  color: #1e3a8a;
}

.theme-candy .buy-summary {
  color: #64748b;
}

.theme-candy .buy-price {
  color: #2563eb;
}

/* ========== Orange Theme ========== */
.theme-orange .product-back,
.theme-orange .product-badge,
.theme-orange .hero-tag,
.theme-orange .coupon-type {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(254, 215, 170, 0.7);
  color: #EA580C;
}

.theme-orange .product-info-card,
.theme-orange .coupon-card,
.theme-orange .coupon-card-secondary,
.theme-orange .buy-bar {
  background: linear-gradient(180deg, #ffffff 0%, #FFFBF5 100%);
  border: 1rpx solid rgba(254, 215, 170, 0.7);
  box-shadow: 0 18rpx 44rpx rgba(234, 88, 12, 0.06);
}

.theme-orange .coupon-amount-box {
  color: #EA580C;
}

.theme-orange .secondary-box {
  color: #9A3412;
}

.theme-orange .coupon-action,
.theme-orange .buy-button {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  color: #ffffff;
  box-shadow: 0 18rpx 36rpx rgba(234, 88, 12, 0.18);
}

.theme-orange .buy-title {
  color: #9A3412;
}

.theme-orange .buy-summary {
  color: #64748b;
}

.theme-orange .buy-price {
  color: #EA580C;
}

/* ========== Red Theme ========== */
.theme-red .product-back,
.theme-red .product-badge,
.theme-red .hero-tag,
.theme-red .coupon-type {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(255, 205, 210, 0.7);
  color: #E53935;
}

.theme-red .product-info-card,
.theme-red .coupon-card,
.theme-red .coupon-card-secondary,
.theme-red .buy-bar {
  background: linear-gradient(180deg, #ffffff 0%, #FFEBEE 100%);
  border: 1rpx solid rgba(255, 205, 210, 0.7);
  box-shadow: 0 18rpx 44rpx rgba(229, 57, 53, 0.06);
}

.theme-red .coupon-amount-box {
  color: #E53935;
}

.theme-red .secondary-box {
  color: #B71C1C;
}

.theme-red .coupon-action,
.theme-red .buy-button {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  color: #ffffff;
  box-shadow: 0 18rpx 36rpx rgba(229, 57, 53, 0.18);
}

.theme-red .buy-title {
  color: #B71C1C;
}

.theme-red .buy-summary {
  color: #64748b;
}

.theme-red .buy-price {
  color: #E53935;
}
</style>
