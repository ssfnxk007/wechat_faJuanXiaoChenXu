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
          <text class="summary-value success">已发放至我的券包</text>
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

      <view class="cm-section">
        <SectionHeader eyebrow="后续操作" title="接下来" subtitle="查看卡券、订单或继续购买" />
        <view class="action-grid">
          <view class="action-card cm-card" @click="goCouponList">
            <text class="action-title">查看我的券包</text>
            <text class="action-desc">到账卡券都可在券包查看。</text>
          </view>
          <view class="action-card cm-card" @click="goOrderList">
            <text class="action-title">查看订单记录</text>
            <text class="action-desc">可回看支付与发放结果。</text>
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
const resultSubtitle = computed(() => '券包已完成支付，系统已将本单卡券权益发放到你的券包。')
const displayAmount = computed(() => formatAmount(orderDetail.value.orderAmount))
const paidAtText = computed(() => formatDate(orderDetail.value.paidAt) || '已完成支付')

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

function goOrderList() {
  uni.navigateTo({ url: '/pages/order/list' })
}

async function loadOrderDetail(id) {
  if (!id || !session.userId) {
    return
  }

  try {
    const result = await getMiniAppUserOrderDetail(session.userId, id)
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
.granted-stack,
.action-grid {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
}
.granted-card,
.action-card {
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
.granted-title,
.action-title {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}
.granted-desc,
.action-desc {
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
</style>
