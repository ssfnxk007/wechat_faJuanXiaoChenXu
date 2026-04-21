<template>
  <view :class="['cm-page', 'sale-detail-page', themeClass]">
    <view class="sale-hero">
      <view class="sale-hero-mask"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container sale-hero-content">
        <view class="sale-topbar">
          <view class="sale-back" @click="goBack">‹ 返回</view>
          <view class="sale-status">{{ saleTypeText }}</view>
        </view>

        <view class="sale-copy">
          <text class="sale-eyebrow">SALE COUPON</text>
          <text class="sale-title">{{ detail.name }}</text>
          <text class="sale-subtitle">{{ saleDescription }}</text>
        </view>

        <view class="sale-tags">
          <text class="sale-tag">{{ couponTypeText }}</text>
          <text class="sale-tag">{{ detail.isAllStores ? '全门店可用' : '指定范围可用' }}</text>
          <text class="sale-tag">{{ detail.isProductCoupon ? '待履约商品券' : '支付后自动发券' }}</text>
        </view>
      </view>
    </view>

    <view class="cm-container sale-body">
      <view class="price-card cm-card">
        <view class="price-main">
          <text class="price-label">当前售价</text>
          <view class="price-row">
            <text class="price-unit">¥</text>
            <text class="price-value">{{ formatAmount(detail.salePrice) }}</text>
          </view>
          <text class="price-note">{{ priceNote }}</text>
        </view>
        <view class="price-badge">{{ detail.perUserLimit || 1 }} 份限购</view>
      </view>

      <view class="overview-grid">
        <view class="overview-card cm-card" v-for="item in overviewItems" :key="item.label">
          <text class="overview-label">{{ item.label }}</text>
          <text class="overview-value">{{ item.value }}</text>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="DETAIL" title="使用说明" subtitle="下单前先看清楚" />
        <view class="detail-stack">
          <view class="detail-card cm-card" v-for="item in detailItems" :key="item.label">
            <text class="detail-label">{{ item.label }}</text>
            <text class="detail-value">{{ item.value }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="FLOW" title="购买流程" subtitle="支付后的状态变化" />
        <view class="step-stack">
          <view class="step-card cm-card" v-for="item in steps" :key="item.step">
            <text class="step-index">{{ item.step }}</text>
            <view class="step-copy-block">
              <text class="step-title">{{ item.title }}</text>
              <text class="step-desc">{{ item.desc }}</text>
            </view>
          </view>
        </view>
      </view>

      <view class="buy-bar cm-card">
        <view class="buy-left">
          <text class="buy-title">{{ detail.name }}</text>
          <text class="buy-summary">{{ detail.fulfillmentHint || '支付成功后自动发放至我的券包。' }}</text>
        </view>
        <view class="buy-right">
          <text class="buy-price">¥{{ formatAmount(detail.salePrice) }}</text>
          <view class="buy-button" :class="{ disabled: buying }" @click="handleBuy">{{ buyButtonText }}</view>
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
import {
  completeMiniAppOrderPayment,
  createMiniAppOrder,
  createMiniAppOrderPayment,
  getMiniAppSaleCouponDetail
} from '@/api/miniapp'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const buying = ref(false)
const detail = ref({
  id: 0,
  name: '售卖券',
  templateType: 0,
  salePrice: 0,
  thresholdAmount: 0,
  discountAmount: 0,
  validPeriodType: 0,
  validDays: 0,
  validFrom: '',
  validTo: '',
  isAllStores: true,
  perUserLimit: 1,
  templateRemark: '',
  productSummary: '',
  fulfillmentHint: '',
  isProductCoupon: false
})

const saleTypeText = computed(() => detail.value.isProductCoupon ? '商品券' : '单张售卖券')
const couponTypeText = computed(() => ({
  1: '新人券',
  2: '无门槛券',
  3: '商品券',
  4: '满减券'
}[Number(detail.value.templateType)] || '优惠券'))

const saleDescription = computed(() => {
  const discountText = Number(detail.value.thresholdAmount || 0) > 0
    ? `满 ${formatAmount(detail.value.thresholdAmount)} 元减 ${formatAmount(detail.value.discountAmount)} 元`
    : `立减 ${formatAmount(detail.value.discountAmount)} 元`
  const fulfillmentText = detail.value.fulfillmentHint || '支付成功后自动发券'
  return `${discountText} · ${fulfillmentText}`
})

const priceNote = computed(() => {
  if (detail.value.isProductCoupon) {
    return `购买后先发券，再进入待履约状态。${detail.value.productSummary ? `对应商品：${detail.value.productSummary}` : ''}`
  }
  return '支付成功后自动发放到我的券包，可在订单页查看。'
})

const overviewItems = computed(() => ([
  { label: '券类型', value: couponTypeText.value },
  { label: '使用范围', value: detail.value.isAllStores ? '全部门店可用' : '指定范围可用' },
  { label: '限购规则', value: `每位用户限购 ${detail.value.perUserLimit || 1} 份` }
]))

const detailItems = computed(() => {
  const validText = Number(detail.value.validPeriodType) === 2 && detail.value.validDays
    ? `支付后 ${detail.value.validDays} 天有效`
    : (detail.value.validFrom && detail.value.validTo ? `${String(detail.value.validFrom).slice(0, 10)} 至 ${String(detail.value.validTo).slice(0, 10)}` : '以下单页说明为准')

  const items = [
    { label: '优惠内容', value: Number(detail.value.thresholdAmount || 0) > 0 ? `满 ${formatAmount(detail.value.thresholdAmount)} 元减 ${formatAmount(detail.value.discountAmount)} 元` : `立减 ${formatAmount(detail.value.discountAmount)} 元` },
    { label: '有效期', value: validText },
    { label: '履约说明', value: detail.value.fulfillmentHint || '支付成功后自动发券' },
    { label: '活动备注', value: detail.value.templateRemark || '请以后端规则为准' }
  ]

  if (detail.value.productSummary) {
    items.splice(2, 0, { label: '目标商品', value: detail.value.productSummary })
  }

  return items
})

const steps = computed(() => detail.value.isProductCoupon
  ? [
      { step: '01', title: '提交订单', desc: '确认商品券信息并完成支付。' },
      { step: '02', title: '自动发券', desc: '支付成功后商品券会先进入你的券包。' },
      { step: '03', title: '等待履约', desc: '当前阶段显示待履约 / 待 ERP 处理。' }
    ]
  : [
      { step: '01', title: '提交订单', desc: '确认单张售卖券信息并完成支付。' },
      { step: '02', title: '自动发券', desc: '支付成功后自动发放到我的券包。' },
      { step: '03', title: '查看使用', desc: '可在卡包或订单页查看到账结果。' }
    ])

const buyButtonText = computed(() => buying.value ? '正在提交...' : '立即购买')

function formatAmount(value) {
  const amount = Number(value || 0)
  return Number.isFinite(amount) ? amount.toFixed(2).replace(/\.00$/, '') : '0'
}

function goBack() {
  uni.navigateBack({ delta: 1 })
}

function navigateToResult(orderId) {
  uni.navigateTo({ url: `/pages/order/result?orderId=${orderId}` })
}

async function handleBuy() {
  if (buying.value || !detail.value.id) {
    return
  }

  buying.value = true
  uni.showLoading({ title: '提交中', mask: true })

  try {
    await ensureMiniProgramLogin()
    if (!session.userId) {
      throw new Error('请先完成微信授权后再购买')
    }

    const ready = await ensurePhoneReady({
      force: true,
      redirect: `/pages/sale-coupon/detail?id=${detail.value.id}`
    })
    if (!ready) {
      return
    }

    const order = await createMiniAppOrder({
      couponTemplateId: detail.value.id
    })

    const paymentResult = await createMiniAppOrderPayment(order.orderId, {})
    if (paymentResult.paid || paymentResult.payment?.isMock) {
      uni.showToast({ title: '购买成功', icon: 'success' })
      navigateToResult(order.orderId)
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
      rawCallback: 'miniapp-request-payment-success'
    })

    uni.showToast({ title: '支付成功', icon: 'success' })
    navigateToResult(order.orderId)
    return
    // #endif

    // #ifndef MP-WEIXIN
    uni.showToast({ title: '请在微信小程序中完成支付', icon: 'none' })
    navigateToResult(order.orderId)
    // #endif
  } catch (error) {
    console.warn('[sale-coupon-detail] handleBuy failed', error)
    const message = error?.errMsg?.includes('cancel')
      ? '已取消支付'
      : (error?.message || '购买失败，请稍后重试')
    uni.showToast({ title: message, icon: 'none' })
  } finally {
    uni.hideLoading()
    buying.value = false
  }
}

async function loadDetail(id) {
  const result = await getMiniAppSaleCouponDetail(id)
  if (!result) {
    uni.showToast({ title: '售卖券不存在', icon: 'none' })
    return
  }

  detail.value = {
    ...detail.value,
    ...result,
    id: Number(result.id || 0),
    isProductCoupon: Number(result.templateType) === 3
  }
}

onLoad(async (options = {}) => {
  const id = Number(options.id || options.templateId || 0)
  if (!id) {
    uni.showToast({ title: '售卖券不存在', icon: 'none' })
    return
  }
  await loadDetail(id)
})
</script>

<style lang="scss" scoped>
.sale-detail-page {
  background: linear-gradient(180deg, #f7f2e8 0%, #f3ede2 44%, #f8f6f1 100%);
}

.sale-hero {
  position: relative;
  overflow: hidden;
  min-height: 396rpx;
  background: linear-gradient(135deg, #244536 0%, #416451 52%, #9d8a5f 100%);
  color: #fffaf4;
  border-bottom-left-radius: 42rpx;
  border-bottom-right-radius: 42rpx;
}

.sale-hero-mask {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at top right, rgba(255, 248, 229, 0.22), transparent 28%),
    radial-gradient(circle at left bottom, rgba(255, 255, 255, 0.1), transparent 24%);
}

.sale-hero-content {
  position: relative;
  display: grid;
  gap: 28rpx;
  padding-top: 18rpx;
  padding-bottom: 38rpx;
}

.sale-topbar,
.price-row,
.step-card,
.buy-bar,
.buy-right {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
}

.sale-topbar {
  justify-content: space-between;
}

.sale-back,
.sale-status,
.sale-tag,
.price-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 52rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  background: rgba(255, 255, 255, 0.14);
  font-size: 22rpx;
}

.sale-copy {
  display: grid;
  gap: 14rpx;
}

.sale-eyebrow {
  color: rgba(247, 235, 208, 0.82);
  font-size: 22rpx;
  letter-spacing: 3rpx;
}

.sale-title {
  font-size: 46rpx;
  font-weight: 700;
}

.sale-subtitle {
  max-width: 640rpx;
  font-size: 26rpx;
  line-height: 1.8;
  color: rgba(255, 250, 243, 0.9);
}

.sale-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 14rpx;
}

.sale-body {
  margin-top: -28rpx;
  padding-bottom: 36rpx;
}

.price-card,
.overview-grid,
.detail-stack,
.step-stack {
  margin-top: 22rpx;
}

.price-card {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18rpx;
  padding: 28rpx;
}

.price-main {
  display: grid;
  gap: 14rpx;
}

.price-label,
.price-note,
.overview-label,
.detail-label,
.step-desc,
.buy-summary {
  color: $cm-text-secondary;
  font-size: 22rpx;
  line-height: 1.7;
}

.price-unit,
.price-value,
.buy-price {
  color: $cm-primary-strong;
  font-weight: 700;
}

.price-unit {
  font-size: 30rpx;
}

.price-value {
  font-size: 56rpx;
}

.overview-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16rpx;
}

.overview-card,
.detail-card,
.step-card {
  padding: 22rpx 24rpx;
}

.overview-card,
.detail-card {
  display: grid;
  gap: 10rpx;
}

.overview-value,
.detail-value,
.step-title,
.buy-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}

.detail-stack,
.step-stack {
  display: grid;
  gap: 18rpx;
}

.step-index {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 72rpx;
  height: 72rpx;
  border-radius: 20rpx;
  background: rgba(36, 69, 54, 0.08);
  color: $cm-primary-strong;
  font-size: 24rpx;
  font-weight: 700;
}

.step-copy-block {
  flex: 1;
  display: grid;
  gap: 8rpx;
}

.buy-bar {
  position: sticky;
  bottom: 20rpx;
  margin-top: 28rpx;
  padding: 24rpx 26rpx;
  background: rgba(255, 253, 248, 0.96);
  backdrop-filter: blur(18px);
}

.buy-left {
  flex: 1;
  display: grid;
  gap: 10rpx;
}

.buy-right {
  min-width: 220rpx;
  justify-content: flex-end;
}

.buy-button {
  min-width: 152rpx;
  padding: 18rpx 24rpx;
  border-radius: 999rpx;
  background: linear-gradient(135deg, #325d49 0%, #5f7453 100%);
  color: #fffdf8;
  text-align: center;
  font-size: 24rpx;
  font-weight: 600;
}

.buy-button.disabled {
  opacity: 0.6;
}

.theme-candy.sale-detail-page {
  background: linear-gradient(180deg, #eff6ff 0%, #f8fbff 44%, #ffffff 100%);
}

.theme-candy .sale-hero {
  background: linear-gradient(135deg, #e0e7ff 0%, #dbeafe 52%, #bfdbfe 100%);
  color: #1e3a8a;
}

.theme-candy .sale-eyebrow,
.theme-candy .sale-subtitle {
  color: #3b82f6;
}

.theme-candy .sale-back,
.theme-candy .sale-status,
.theme-candy .sale-tag,
.theme-candy .price-badge {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(191, 219, 254, 0.7);
  color: #2563eb;
}

.theme-candy .price-unit,
.theme-candy .price-value,
.theme-candy .buy-price,
.theme-candy .step-index {
  color: #2563eb;
}

.theme-candy .step-index {
  background: rgba(59, 130, 246, 0.08);
}

.theme-candy .buy-button {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
}

.theme-light .sale-hero {
  background: linear-gradient(135deg, #e2e8f0 0%, #cbd5e1 52%, #94a3b8 100%);
  color: #0f172a;
}

.theme-light .sale-back,
.theme-light .sale-status,
.theme-light .sale-tag,
.theme-light .price-badge {
  background: rgba(255, 255, 255, 0.8);
  color: #334155;
}

.theme-light .price-unit,
.theme-light .price-value,
.theme-light .buy-price,
.theme-light .step-index {
  color: #111827;
}

.theme-light .step-index {
  background: rgba(15, 23, 42, 0.06);
}

.theme-light .buy-button {
  background: #111827;
}

.theme-orange.sale-detail-page {
  background: linear-gradient(180deg, #fffbf5 0%, #fff7ed 44%, #ffffff 100%);
}

.theme-orange .sale-hero {
  background: linear-gradient(135deg, #fff7ed 0%, #fed7aa 52%, #fdba74 100%);
  color: #9a3412;
}

.theme-orange .sale-back,
.theme-orange .sale-status,
.theme-orange .sale-tag,
.theme-orange .price-badge {
  background: rgba(255, 255, 255, 0.82);
  color: #ea580c;
}

.theme-orange .price-unit,
.theme-orange .price-value,
.theme-orange .buy-price,
.theme-orange .step-index {
  color: #ea580c;
}

.theme-orange .step-index {
  background: rgba(249, 115, 22, 0.08);
}

.theme-orange .buy-button {
  background: linear-gradient(135deg, #f97316 0%, #fb923c 100%);
}

.theme-red.sale-detail-page {
  background: linear-gradient(180deg, #ffebee 0%, #fff1f2 44%, #ffffff 100%);
}

.theme-red .sale-hero {
  background: linear-gradient(135deg, #ffebee 0%, #ffcdd2 52%, #ef9a9a 100%);
  color: #b71c1c;
}

.theme-red .sale-back,
.theme-red .sale-status,
.theme-red .sale-tag,
.theme-red .price-badge {
  background: rgba(255, 255, 255, 0.82);
  color: #e53935;
}

.theme-red .price-unit,
.theme-red .price-value,
.theme-red .buy-price,
.theme-red .step-index {
  color: #e53935;
}

.theme-red .step-index {
  background: rgba(239, 83, 80, 0.08);
}

.theme-red .buy-button {
  background: linear-gradient(135deg, #ef5350 0%, #f48080 100%);
}
</style>
