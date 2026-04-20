<template>
  <view :class="['cm-page', 'cm-container', 'coupon-page', themeClass]">
    <view class="cm-nav-spacer"></view>

    <view class="summary-card cm-card">
      <view>
        <text class="summary-title">我的卡券</text>
        <text class="summary-subtitle">已领取卡券都在这里。</text>
      </view>
      <view class="summary-badge">{{ currentStatusLabel }}</view>
    </view>

    <view class="segment-row cm-section">
      <view
        v-for="item in statusOptions"
        :key="item.value"
        class="segment-item"
        :class="{ active: item.value === currentStatus }"
        @click="changeStatus(item.value)"
      >
        {{ item.label }}
      </view>
    </view>

    <view v-if="coupons.length" class="coupon-stack cm-section">
      <view v-for="item in coupons" :key="item.id" @click="goDetail(item.id)">
        <CouponCard
          :tag="item.tag"
          :title="item.title"
          :desc="item.desc"
          :date="item.date"
          :amount="item.amount"
          action-text="查看详情"
        />
      </view>
    </view>

    <view v-else class="empty-card cm-card cm-section">
      <text class="empty-title">{{ emptyState.title }}</text>
      <text class="empty-desc">{{ emptyState.desc }}</text>
      <view class="empty-actions">
        <view class="ghost-action" @click="goHome">回首页</view>
        <view class="primary-action" @click="goMall">去商城</view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import CouponCard from '@/components/CouponCard.vue'
import { useTheme } from '@/composables/use-theme'
import { getMiniAppUserCoupons } from '@/api/miniapp'
import { ensureMiniProgramLogin } from '@/api/auth'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const currentStatus = ref(1)
const coupons = ref([])

const statusOptions = [
  { value: 1, label: '可用券' },
  { value: 2, label: '已使用' },
  { value: 3, label: '已过期' }
]

const currentStatusLabel = computed(() => statusOptions.find((item) => item.value === currentStatus.value)?.label || '可用券')
const emptyState = computed(() => {
  if (!session.userId) {
    return {
      title: '暂未获取卡券',
      desc: '领券或购券后会显示在这里。'
    }
  }

  if (currentStatus.value === 2) {
    return {
      title: '暂无已使用卡券',
      desc: '核销后会显示在这里。'
    }
  }

  if (currentStatus.value === 3) {
    return {
      title: '暂无过期卡券',
      desc: '可优先使用有效卡券。'
    }
  }

  return {
    title: '暂无可用卡券',
    desc: '可先去首页领券或购买券包。'
  }
})

function mapCoupon(item) {
  const typeMap = {
    1: '新人专享',
    2: '无门槛券',
    3: '指定商品',
    4: '满减券'
  }

  return {
    id: item.id,
    tag: typeMap[item.templateType] || '优惠券',
    title: item.couponTemplateName,
    desc: item.thresholdAmount ? `满 ${item.thresholdAmount} 元减 ${item.discountAmount || 0} 元` : `立减 ${item.discountAmount || 0} 元`,
    date: `${String(item.effectiveAt).slice(0, 10)} 至 ${String(item.expireAt).slice(0, 10)}`,
    amount: String(item.discountAmount || 0)
  }
}

async function loadCoupons() {
  try {
    await ensureMiniProgramLogin()
  } catch (error) {
    console.warn('[coupon-list] ensure login failed', error)
    uni.showToast({ title: '微信登录失败，请稍后重试', icon: 'none' })
    coupons.value = []
    return
  }

  if (!session.userId) {
    coupons.value = []
    return
  }

  try {
    const result = await getMiniAppUserCoupons({
      status: currentStatus.value,
      pageIndex: 1,
      pageSize: 20
    })

    const items = Array.isArray(result?.items) ? result.items.map(mapCoupon) : []
    coupons.value = items
  } catch (error) {
    coupons.value = []
    console.warn('[coupon-list] loadCoupons failed', error)
    uni.showToast({ title: '加载券列表失败', icon: 'none' })
  }
}

function changeStatus(status) {
  if (currentStatus.value === status) {
    return
  }
  currentStatus.value = status
  loadCoupons()
}

function goDetail(id) {
  uni.navigateTo({ url: id ? `/pages/coupon/detail?id=${id}` : '/pages/coupon/detail' })
}

function goHome() {
  uni.switchTab({ url: '/pages/index/index' })
}

function goMall() {
  uni.switchTab({ url: '/pages/mall/index' })
}

onShow(() => {
  loadCoupons()
})
</script>

<style lang="scss" scoped>
.coupon-page {
  padding-bottom: 36rpx;
}
.summary-card {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18rpx;
  padding: 28rpx;
}
.summary-title {
  display: block;
  color: $cm-text-primary;
  font-size: 34rpx;
  font-weight: 700;
}
.summary-subtitle {
  display: block;
  margin-top: 10rpx;
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}
.summary-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 48rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
  font-size: 22rpx;
}
.segment-row {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14rpx;
}
.segment-item {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 76rpx;
  border-radius: 999rpx;
  background: rgba(255, 252, 246, 0.8);
  color: $cm-text-secondary;
  font-size: 26rpx;
  border: 1rpx solid $cm-border-soft;
}
.segment-item.active {
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf9;
  border-color: transparent;
}
.coupon-stack {
  display: grid;
  gap: 20rpx;
}
.empty-card {
  display: grid;
  gap: 18rpx;
  padding: 34rpx 28rpx;
  text-align: center;
}
.empty-title {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}
.empty-desc {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}
.empty-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14rpx;
  margin-top: 8rpx;
}
.ghost-action,
.primary-action {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 80rpx;
  border-radius: 999rpx;
  font-size: 24rpx;
}
.ghost-action {
  background: rgba(45, 91, 72, 0.08);
  color: $cm-primary;
}
.primary-action {
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf8;
}

.theme-light .summary-badge {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-light .segment-item {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .segment-item.active {
  background: #111827;
  color: #ffffff;
  border-color: #111827;
}

.theme-light .empty-card {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .ghost-action {
  background: rgba(15, 23, 42, 0.05);
  color: #475569;
}

.theme-light .primary-action {
  background: #111827;
  color: #ffffff;
}

/* ========== Candy Theme ========== */
.theme-candy .summary-badge {
  background: rgba(59, 130, 246, 0.1);
  color: #2563EB;
}

.theme-candy .segment-item {
  background: #ffffff;
  border: 1rpx solid rgba(191, 219, 254, 0.6);
  color: #3B82F6;
}

.theme-candy .segment-item.active {
  background: linear-gradient(135deg, #3B82F6 0%, #60A5FA 100%);
  color: #ffffff;
  border-color: transparent;
}

.theme-candy .empty-card {
  background: #ffffff;
  border: 1rpx solid rgba(191, 219, 254, 0.6);
}

.theme-candy .ghost-action {
  background: rgba(59, 130, 246, 0.05);
  color: #2563EB;
}

.theme-candy .primary-action {
  background: linear-gradient(135deg, #3B82F6 0%, #60A5FA 100%);
  color: #ffffff;
}


/* ========== Orange Theme ========== */
.theme-orange .summary-badge {
  background: rgba(249, 115, 22, 0.1);
  color: #EA580C;
}

.theme-orange .segment-item {
  background: #ffffff;
  border: 1rpx solid rgba(254, 215, 170, 0.6);
  color: #F97316;
}

.theme-orange .segment-item.active {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  color: #ffffff;
  border-color: transparent;
}

.theme-orange .empty-card {
  background: #ffffff;
  border: 1rpx solid rgba(254, 215, 170, 0.6);
}

.theme-orange .ghost-action {
  background: rgba(249, 115, 22, 0.05);
  color: #EA580C;
}

.theme-orange .primary-action {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  color: #ffffff;
}

/* ========== Red Theme ========== */
.theme-red .summary-badge {
  background: rgba(239, 83, 80, 0.1);
  color: #E53935;
}

.theme-red .segment-item {
  background: #ffffff;
  border: 1rpx solid rgba(255, 205, 210, 0.6);
  color: #EF5350;
}

.theme-red .segment-item.active {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  color: #ffffff;
  border-color: transparent;
}

.theme-red .empty-card {
  background: #ffffff;
  border: 1rpx solid rgba(255, 205, 210, 0.6);
}

.theme-red .ghost-action {
  background: rgba(239, 83, 80, 0.05);
  color: #E53935;
}

.theme-red .primary-action {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  color: #ffffff;
}
</style>
