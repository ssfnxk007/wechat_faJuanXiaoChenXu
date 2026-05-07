<template>
  <view :class="['cm-page', themeClass]">
    <CmPullRefresh :refreshing="refreshing" @refresh="handleRefresh">
    <view class="cm-container">
    <view class="cm-nav-spacer"></view>

    <view class="page-topbar">
      <view class="page-back" @click="goBack">‹ 返回</view>
    </view>

    <view class="summary-card cm-card">
      <view>
        <text class="summary-title">核销记录</text>
        <text class="summary-subtitle">查看使用记录</text>
      </view>
      <view class="summary-badge">{{ summary.monthWriteOffCount }}</view>
    </view>

    <view class="stats-grid cm-section">
      <view class="stats-card cm-card" v-for="item in stats" :key="item.label">
        <text class="stats-value">{{ item.value }}</text>
        <text class="stats-label">{{ item.label }}</text>
      </view>
    </view>

    <view class="cm-section">
      <SectionHeader eyebrow="TIMELINE" title="核销时间轴" subtitle="按时间查看" />
      <view class="timeline-list">
        <view class="timeline-item" v-for="item in records" :key="item.id">
          <view class="timeline-line">
            <view class="timeline-dot"></view>
            <view class="timeline-bar"></view>
          </view>
          <view class="timeline-card cm-card">
            <view class="timeline-top">
              <view>
                <text class="timeline-title">{{ item.title }}</text>
                <text class="timeline-time">{{ item.time }}</text>
              </view>
              <text class="timeline-status">{{ item.status }}</text>
            </view>

            <view class="timeline-grid">
              <view class="timeline-meta">
                <text class="timeline-label">核销门店</text>
                <text class="timeline-value">{{ item.store }}</text>
              </view>
              <view class="timeline-meta">
                <text class="timeline-label">券项名称</text>
                <text class="timeline-value">{{ item.coupon }}</text>
              </view>
              <view class="timeline-meta">
                <text class="timeline-label">核销单号</text>
                <text class="timeline-value">{{ item.verifyNo }}</text>
              </view>
              <view class="timeline-meta">
                <text class="timeline-label">经办方式</text>
                <text class="timeline-value">{{ item.channel }}</text>
              </view>
            </view>

            <view class="timeline-foot">
              <text class="timeline-note">{{ item.note }}</text>
              <view class="timeline-tag">{{ item.tag }}</view>
            </view>
          </view>
        </view>
      </view>
    </view>

    <view class="rules-card cm-card cm-section">
      <text class="rules-title">记录说明</text>
      <view class="rules-list">
        <text class="rules-item">核销后即时写入。</text>
        <text class="rules-item">组合券按单张记录。</text>
        <text class="rules-item">可结合订单号核对。</text>
      </view>
    </view>
    </view>
    </CmPullRefresh>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import CmPullRefresh from '@/components/CmPullRefresh.vue'
import { useTheme } from '@/composables/use-theme'
import { ensureMiniProgramLogin, ensurePhoneReady } from '@/api/auth'
import { fetchMiniAppWriteOffRecords } from '@/api/verify-record'

const { themeClass } = useTheme()
const refreshing = ref(false)
const records = ref<VerifyRecordItem[]>([])
const summary = ref({
  totalWriteOffCount: 0,
  monthWriteOffCount: 0,
  unusedCouponCount: 0
})

async function handleRefresh() {
  if (refreshing.value) return
  refreshing.value = true
  try {
    await loadRecords()
    uni.showToast({ title: '已刷新', icon: 'none' })
  } finally {
    refreshing.value = false
  }
}

function goBack() {
  uni.switchTab({ url: '/pages/profile/index' })
}

interface StatItem {
  label: string
  value: string
  note: string
}

interface VerifyRecordItem {
  id: number
  title: string
  time: string
  status: string
  store: string
  coupon: string
  verifyNo: string
  channel: string
  note: string
  tag: string
}

const stats = computed<StatItem[]>(() => ([
  { label: '累计核销', value: String(summary.value.totalWriteOffCount), note: '累计' },
  { label: '近 30 天', value: String(summary.value.monthWriteOffCount), note: '近30天' },
  { label: '待使用权益', value: String(summary.value.unusedCouponCount), note: '待使用' }
]))

async function loadRecords() {
  await ensureMiniProgramLogin()
  const ready = await ensurePhoneReady({
    force: true,
    redirect: '/pages/verify-record/index'
  })
  if (!ready) {
    records.value = []
    summary.value = {
      totalWriteOffCount: 0,
      monthWriteOffCount: 0,
      unusedCouponCount: 0
    }
    return
  }

  const result = await fetchMiniAppWriteOffRecords()
  summary.value = {
    totalWriteOffCount: result.totalWriteOffCount,
    monthWriteOffCount: result.monthWriteOffCount,
    unusedCouponCount: result.unusedCouponCount
  }
  records.value = result.items.map((item: any) => ({
    id: Number(item.id || 0),
    title: String(item.title || ''),
    time: String(item.time || '').replace('T', ' ').slice(0, 16),
    status: String(item.status || ''),
    store: String(item.store || ''),
    coupon: String(item.coupon || ''),
    verifyNo: String(item.verifyNo || ''),
    channel: String(item.channel || ''),
    note: String(item.note || ''),
    tag: String(item.tag || '')
  }))
}

onShow(() => {
  loadRecords().catch((error) => {
    console.warn('[verify-record] load failed', error)
    uni.showToast({ title: error?.message || '加载失败', icon: 'none' })
  })
})
</script>

<style lang="scss" scoped>
.page-topbar {
  display: flex;
  justify-content: flex-start;
  margin-bottom: 18rpx;
}

.page-back {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 56rpx;
  padding: 0 22rpx;
  border-radius: 999rpx;
  background: rgba(255, 252, 246, 0.86);
  border: 1rpx solid $cm-border-soft;
  color: $cm-text-primary;
  font-size: 24rpx;
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
  min-width: 72rpx;
  min-height: 48rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
  font-size: 22rpx;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14rpx;
}

.stats-card {
  display: grid;
  gap: 8rpx;
  padding: 22rpx 18rpx;
  text-align: center;
}

.stats-value {
  color: $cm-text-primary;
  font-size: 34rpx;
  font-weight: 700;
}

.stats-label {
  color: $cm-text-secondary;
  font-size: 22rpx;
}

.timeline-list {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
}

.timeline-item {
  display: grid;
  grid-template-columns: 36rpx 1fr;
  gap: 14rpx;
}

.timeline-line {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.timeline-dot {
  width: 18rpx;
  height: 18rpx;
  margin-top: 24rpx;
  border-radius: 50%;
  background: linear-gradient(135deg, #2d5b48 0%, #b79b63 100%);
}

.timeline-bar {
  flex: 1;
  width: 2rpx;
  background: rgba(95, 116, 83, 0.18);
}

.timeline-item:last-child .timeline-bar {
  opacity: 0;
}

.timeline-card {
  display: grid;
  gap: 20rpx;
  padding: 26rpx;
}

.timeline-top,
.timeline-foot {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18rpx;
}

.timeline-title {
  display: block;
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}

.timeline-time {
  display: block;
  margin-top: 8rpx;
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.timeline-status,
.timeline-tag {
  padding: 10rpx 18rpx;
  border-radius: 999rpx;
  background: rgba(45, 91, 72, 0.1);
  color: $cm-primary;
  font-size: 22rpx;
}

.timeline-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18rpx;
  padding: 22rpx;
  border-radius: 24rpx;
  background: rgba(247, 242, 233, 0.7);
}

.timeline-meta {
  display: grid;
  gap: 8rpx;
}

.timeline-label {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.timeline-value {
  color: $cm-text-primary;
  font-size: 25rpx;
}

.timeline-note {
  flex: 1;
  color: $cm-text-secondary;
  font-size: 23rpx;
  line-height: 1.7;
}

.rules-card {
  display: grid;
  gap: 16rpx;
  padding: 28rpx;
}

.rules-title {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}

.rules-list {
  display: grid;
  gap: 12rpx;
}

.rules-item {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}

.theme-light .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
  color: #475569;
}

.theme-light .summary-badge {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-candy .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(191, 219, 254, 0.6);
  color: #2563EB;
}

.theme-candy .summary-badge {
  background: rgba(59, 130, 246, 0.08);
  color: #2563EB;
}

.theme-orange .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(254, 215, 170, 0.6);
  color: #EA580C;
}

.theme-orange .summary-badge {
  background: rgba(249, 115, 22, 0.08);
  color: #EA580C;
}

.theme-red .page-back {
  background: #ffffff;
  border: 1rpx solid rgba(255, 205, 210, 0.6);
  color: #E53935;
}

.theme-red .summary-badge {
  background: rgba(239, 83, 80, 0.08);
  color: #E53935;
}
</style>
