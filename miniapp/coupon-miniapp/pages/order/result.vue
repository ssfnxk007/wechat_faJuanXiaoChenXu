<template>
  <view :class="['cm-page', 'order-result-page', themeClass]">
    <view class="result-hero">
      <view class="result-mask"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container result-hero-content">
        <view class="result-topbar">
          <view class="result-back" @click="goMall">‹ 返回商城</view>
        </view>

        <view class="result-status-block">
          <view class="result-icon">✓</view>
          <text class="result-title">{{ resultTitle }}</text>
          <text class="result-subtitle">{{ resultSubtitle }}</text>
        </view>
      </view>
    </view>

    <view class="cm-container result-body">
      <view class="summary-card cm-card">
        <view class="summary-row">
          <text class="summary-label">订单号</text>
          <text class="summary-value mono">{{ orderDetail.orderNo }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label">支付金额</text>
          <text class="summary-value price">¥{{ displayAmount }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label">支付时间</text>
          <text class="summary-value">{{ paidAtText }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label">权益去向</text>
          <text class="summary-value success">{{ entitlementText }}</text>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="到账权益" title="本次发放结果" subtitle="本次到账卡券" />
        <view class="granted-stack">
          <view class="granted-card cm-card" v-for="item in grantedCoupons" :key="item.id">
            <view class="granted-top">
              <text class="granted-type">{{ item.type }}</text>
              <text class="granted-count">{{ item.meta }}</text>
            </view>
            <text class="granted-title">{{ item.title }}</text>
            <text class="granted-desc">{{ item.desc }}</text>
          </view>
        </view>
      </view>

      <view class="bottom-actions">
        <view class="ghost-action" @click="goMall">继续逛商城</view>
        <view class="primary-action" @click="goCouponList">前往券包</view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import { useTheme } from '@/composables/use-theme'
import { getMiniAppUserOrderDetail } from '@/api/miniapp'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const orderDetail = ref({
  orderNo: 'YJ20260411000128',
  orderAmount: 29.9,
  paidAt: '',
  grantedCoupons: []
})
const grantedCoupons = ref([
  {
    id: 1,
    type: '无门槛券',
    title: '门店通用券',
    desc: '消费时出示二维码即可。',
    meta: '已到账'
  }
])

const resultTitle = computed(() => '支付成功')
const resultSubtitle = computed(() => {
  if (orderDetail.value.isProductCoupon) {
    return '商品券已完成支付，系统已发券并标记为待履约 / 待 ERP 处理。'
  }
  if (orderDetail.value.couponTemplateId) {
    return '单张售卖券已完成支付，系统已将本单卡券发放到你的券包。'
  }
  return '券包已完成支付，系统已将本单卡券权益发放到你的券包。'
})
const displayAmount = computed(() => formatAmount(orderDetail.value.orderAmount))
const paidAtText = computed(() => formatDate(orderDetail.value.paidAt) || '已完成支付')
const entitlementText = computed(() => orderDetail.value.isProductCoupon ? '已发放商品券，待后续履约' : '已发放至我的券包')

function formatAmount(value) {
  const amount = Number(value || 0)
  return Number.isFinite(amount) ? amount.toFixed(2) : '0.00'
}

function formatDate(value) {
  if (!value) {
    return ''
  }
  return String(value).replace('T', ' ').slice(0, 16)
}

function getTemplateTypeLabel(templateType) {
  return ({
    1: '新人券',
    2: '无门槛券',
    3: '指定商品券',
    4: '满减券'
  }[templateType] || '优惠券')
}

function buildCouponDesc(item) {
  if (item.thresholdAmount) {
    return `满 ${formatAmount(item.thresholdAmount)} 元减 ${formatAmount(item.discountAmount)} 元。`
  }
  return `立减 ${formatAmount(item.discountAmount)} 元。`
}

function buildCouponMeta(item) {
  const start = formatDate(item.effectiveAt)
  const end = formatDate(item.expireAt)
  if (start && end) {
    return `${start.slice(0, 10)} 至 ${end.slice(0, 10)}`
  }
  return '已到账'
}

function goMall() {
  uni.switchTab({ url: '/pages/mall/index' })
}

function goCouponList() {
  uni.switchTab({ url: '/pages/coupon/index' })
}

async function loadOrderDetail(id) {
  if (!id) {
    return
  }

  try {
    const result = await getMiniAppUserOrderDetail(id)
    if (!result) {
      return
    }

    orderDetail.value = result
    if (Array.isArray(result.grantedCoupons) && result.grantedCoupons.length) {
      grantedCoupons.value = result.grantedCoupons.map((item) => ({
        id: item.id,
        type: getTemplateTypeLabel(item.templateType),
        title: item.couponTemplateName,
        desc: buildCouponDesc(item),
        meta: buildCouponMeta(item)
      }))
    } else if (result.couponTemplateName) {
      grantedCoupons.value = [{
        id: result.id,
        type: result.isProductCoupon ? '商品券' : '售卖券',
        title: result.couponTemplateName,
        desc: result.isProductCoupon ? '商品券已到账，当前阶段待履约 / 待 ERP 处理。' : '单张售卖券已到账，可前往卡包查看。',
        meta: result.fulfillmentStatusText || '已到账'
      }]
    }
  } catch (error) {
    console.warn('[order-result] loadOrderDetail failed', error)
  }
}

onLoad((options) => {
  const id = options?.id || options?.orderId
  loadOrderDetail(id)
})
</script>

<style lang="scss" scoped>
.order-result-page {
  background: linear-gradient(180deg, #f7f2e8 0%, #f3ede2 44%, #f8f6f1 100%);
}
.result-hero {
  position: relative;
  overflow: hidden;
  min-height: 420rpx;
  background: linear-gradient(135deg, #264337 0%, #4b6650 52%, #aa9664 100%);
  color: #fffaf4;
  border-bottom-left-radius: 42rpx;
  border-bottom-right-radius: 42rpx;
}
.result-mask {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at top right, rgba(255, 248, 229, 0.22), transparent 28%),
    radial-gradient(circle at left bottom, rgba(255, 255, 255, 0.1), transparent 24%);
}
.result-hero-content {
  position: relative;
  display: grid;
  gap: 34rpx;
  padding-top: 18rpx;
  padding-bottom: 40rpx;
}
.result-topbar {
  display: flex;
  align-items: center;
  justify-content: flex-start;
}
.result-back {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 56rpx;
  padding: 0 20rpx;
  border-radius: 999rpx;
  background: rgba(255, 255, 255, 0.14);
  font-size: 24rpx;
}
.result-status-block {
  display: grid;
  justify-items: start;
  gap: 14rpx;
}
.result-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 108rpx;
  height: 108rpx;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.18);
  font-size: 52rpx;
  font-weight: 700;
}
.result-title {
  font-size: 52rpx;
  font-weight: 700;
}
.result-subtitle {
  max-width: 620rpx;
  font-size: 25rpx;
  line-height: 1.8;
  opacity: 0.92;
}
.result-body {
  margin-top: -28rpx;
  padding-bottom: 36rpx;
}
.summary-card {
  position: relative;
  z-index: 2;
  display: grid;
  gap: 18rpx;
  padding: 30rpx;
}
.summary-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20rpx;
}
.summary-label {
  color: $cm-text-secondary;
  font-size: 24rpx;
}
.summary-value {
  color: $cm-text-primary;
  font-size: 26rpx;
  text-align: right;
}
.summary-value.mono {
  font-family: 'Courier New', monospace;
}
.summary-value.price {
  color: $cm-primary-strong;
  font-size: 34rpx;
  font-weight: 700;
}
.summary-value.success {
  color: $cm-success;
}
.granted-stack {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
}
.granted-card {
  display: grid;
  gap: 10rpx;
  padding: 24rpx 26rpx;
}
.granted-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}
.granted-type {
  color: $cm-accent-gold;
  font-size: 22rpx;
}
.granted-count {
  color: $cm-primary;
  font-size: 22rpx;
}
.granted-title {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}
.granted-desc {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}
.bottom-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16rpx;
  margin-top: 22rpx;
}
.ghost-action,
.primary-action {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 84rpx;
  border-radius: 999rpx;
  font-size: 26rpx;
}
.ghost-action {
  background: rgba(45, 91, 72, 0.08);
  color: $cm-primary;
}
.primary-action {
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf8;
}

.theme-light .order-result-page {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.theme-light .result-hero {
  background: linear-gradient(180deg, #ffffff 0%, #eef2ff 100%);
  color: #0f172a;
}

.theme-light .result-mask {
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.86), transparent 32%),
    radial-gradient(circle at left bottom, rgba(226, 232, 240, 0.42), transparent 26%);
}

.theme-light .result-back,
.theme-light .result-icon {
  background: rgba(15, 23, 42, 0.06);
  color: #334155;
}

.theme-light .result-subtitle,
.theme-light .granted-desc,
.theme-light .summary-label {
  color: #64748b;
}

.theme-light .summary-card,
.theme-light .granted-card {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
  box-shadow: 0 14rpx 36rpx rgba(15, 23, 42, 0.05);
}

.theme-light .summary-value.price,
.theme-light .granted-count {
  color: #111827;
}

.theme-light .ghost-action {
  background: rgba(15, 23, 42, 0.05);
  color: #475569;
}

.theme-light .primary-action {
  background: #111827;
  color: #ffffff;
}

.theme-candy .order-result-page {
  background: linear-gradient(180deg, #eff6ff 0%, #f8fafc 100%);
}

.theme-candy .result-hero {
  background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 52%, #93c5fd 100%);
  color: #1e3a8a;
}

.theme-candy .result-mask {
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.88), transparent 30%),
    radial-gradient(circle at left bottom, rgba(191, 219, 254, 0.58), transparent 24%);
}

.theme-candy .result-back {
  background: rgba(255, 255, 255, 0.5);
  color: #2563eb;
  border: 1rpx solid rgba(191, 219, 254, 0.75);
}

.theme-candy .result-icon {
  background: rgba(255, 255, 255, 0.54);
  color: #2563eb;
}

.theme-candy .result-subtitle,
.theme-candy .granted-desc,
.theme-candy .summary-label {
  color: #3b82f6;
}

.theme-candy .summary-card,
.theme-candy .granted-card {
  background: #ffffff;
  border: 1rpx solid rgba(191, 219, 254, 0.75);
  box-shadow: 0 16rpx 40rpx rgba(37, 99, 235, 0.08);
}

.theme-candy .granted-type {
  background: rgba(59, 130, 246, 0.08);
  color: #2563eb;
  width: fit-content;
  padding: 8rpx 16rpx;
  border-radius: 999rpx;
}

.theme-candy .granted-count,
.theme-candy .summary-value.price {
  color: #2563eb;
}

.theme-candy .summary-value.success {
  color: #0f766e;
}

.theme-candy .ghost-action {
  background: rgba(59, 130, 246, 0.06);
  color: #2563eb;
  border: 1rpx solid rgba(191, 219, 254, 0.65);
}

.theme-candy .primary-action {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
  color: #ffffff;
}

/* ========== Orange Theme ========== */
.theme-orange .order-result-page {
  background: linear-gradient(180deg, #FFFBF5 0%, #FFFBF5 100%);
}

.theme-orange .result-hero {
  background: linear-gradient(135deg, #FFEDD5 0%, #FED7AA 52%, #FDBA74 100%);
  color: #9A3412;
}

.theme-orange .result-mask {
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.88), transparent 30%),
    radial-gradient(circle at left bottom, rgba(254, 215, 170, 0.58), transparent 24%);
}

.theme-orange .result-back {
  background: rgba(255, 255, 255, 0.5);
  color: #EA580C;
  border: 1rpx solid rgba(254, 215, 170, 0.75);
}

.theme-orange .result-icon {
  background: rgba(255, 255, 255, 0.54);
  color: #EA580C;
}

.theme-orange .result-subtitle,
.theme-orange .granted-desc,
.theme-orange .summary-label {
  color: #F97316;
}

.theme-orange .summary-card,
.theme-orange .granted-card {
  background: #ffffff;
  border: 1rpx solid rgba(254, 215, 170, 0.75);
  box-shadow: 0 16rpx 40rpx rgba(234, 88, 12, 0.08);
}

.theme-orange .granted-type {
  background: rgba(249, 115, 22, 0.08);
  color: #EA580C;
  width: fit-content;
  padding: 8rpx 16rpx;
  border-radius: 999rpx;
}

.theme-orange .granted-count,
.theme-orange .summary-value.price {
  color: #EA580C;
}

.theme-orange .summary-value.success {
  color: #0f766e;
}

.theme-orange .ghost-action {
  background: rgba(249, 115, 22, 0.06);
  color: #EA580C;
  border: 1rpx solid rgba(254, 215, 170, 0.65);
}

.theme-orange .primary-action {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  color: #ffffff;
}

/* ========== Red Theme ========== */
.theme-red .order-result-page {
  background: linear-gradient(180deg, #FFEBEE 0%, #FFFBFA 100%);
}

.theme-red .result-hero {
  background: linear-gradient(135deg, #FFCDD2 0%, #FFCDD2 52%, #FECDD3 100%);
  color: #B71C1C;
}

.theme-red .result-mask {
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.88), transparent 30%),
    radial-gradient(circle at left bottom, rgba(255, 205, 210, 0.58), transparent 24%);
}

.theme-red .result-back {
  background: rgba(255, 255, 255, 0.5);
  color: #E53935;
  border: 1rpx solid rgba(255, 205, 210, 0.75);
}

.theme-red .result-icon {
  background: rgba(255, 255, 255, 0.54);
  color: #E53935;
}

.theme-red .result-subtitle,
.theme-red .granted-desc,
.theme-red .summary-label {
  color: #EF5350;
}

.theme-red .summary-card,
.theme-red .granted-card {
  background: #ffffff;
  border: 1rpx solid rgba(255, 205, 210, 0.75);
  box-shadow: 0 16rpx 40rpx rgba(229, 57, 53, 0.08);
}

.theme-red .granted-type {
  background: rgba(239, 83, 80, 0.08);
  color: #E53935;
  width: fit-content;
  padding: 8rpx 16rpx;
  border-radius: 999rpx;
}

.theme-red .granted-count,
.theme-red .summary-value.price {
  color: #E53935;
}

.theme-red .summary-value.success {
  color: #0f766e;
}

.theme-red .ghost-action {
  background: rgba(239, 83, 80, 0.06);
  color: #E53935;
  border: 1rpx solid rgba(255, 205, 210, 0.65);
}

.theme-red .primary-action {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  color: #ffffff;
}
</style>
