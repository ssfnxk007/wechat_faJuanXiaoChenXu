<template>
  <view :class="['cm-page', themeClass]">
    <CmPullRefresh :refreshing="refreshing" @refresh="handleRefresh">
    <view class="cm-container">
    <view class="cm-nav-spacer"></view>

    <view class="record-hero cm-card">
      <view class="record-hero-copy">
        <text class="record-eyebrow">VERIFY RECORD</text>
        <text class="record-title">核销记录</text>
        <text class="record-subtitle">每次核销记录都在这里。</text>
      </view>
      <view class="record-highlight">
        <text class="record-highlight-value">12</text>
        <text class="record-highlight-label">本月核销次数</text>
      </view>
    </view>

    <view class="stats-grid cm-section">
      <view class="stats-card cm-card" v-for="item in stats" :key="item.label">
        <text class="stats-value">{{ item.value }}</text>
        <text class="stats-label">{{ item.label }}</text>
        <text class="stats-note">{{ item.note }}</text>
      </view>
    </view>

    <view class="filter-panel cm-card cm-section">
      <view class="filter-item">
        <text class="filter-label">记录范围</text>
        <text class="filter-value">近 30 天</text>
      </view>
      <view class="filter-item">
        <text class="filter-label">核销门店</text>
        <text class="filter-value">全部门店</text>
      </view>
      <view class="filter-item">
        <text class="filter-label">记录状态</text>
        <text class="filter-value">核销成功</text>
      </view>
    </view>

    <view class="cm-section">
      <SectionHeader eyebrow="TIMELINE" title="核销时间轴" subtitle="按时间查看使用记录" />
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
        <text class="rules-item">核销成功后记录即时写入，如遇网络延迟，请以门店最终受理结果为准。</text>
        <text class="rules-item">组合券包将按单张券依次记录，便于区分不同权益的使用情况。</text>
        <text class="rules-item">若需核对历史订单，可结合订单列表中的订单号进行交叉查询。</text>
      </view>
    </view>
    </view>
    </CmPullRefresh>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import SectionHeader from '@/components/SectionHeader.vue'
import CmPullRefresh from '@/components/CmPullRefresh.vue'
import { useTheme } from '@/composables/use-theme'

const { themeClass } = useTheme()
const refreshing = ref(false)

async function handleRefresh() {
  if (refreshing.value) return
  refreshing.value = true
  await new Promise((resolve) => setTimeout(resolve, 600))
  uni.showToast({ title: '已为您刷新', icon: 'none' })
  refreshing.value = false
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

const stats: StatItem[] = [
  { label: '累计核销', value: '36', note: '累计' },
  { label: '本月到店', value: '12', note: '近30天' },
  { label: '待使用权益', value: '4', note: '待使用' }
]

const records: VerifyRecordItem[] = [
  {
    id: 1,
    title: '春序礼遇券包首张券已使用',
    time: '2026-04-11 10:36',
    status: '核销成功',
    store: '南京西路门店',
    coupon: '门店通用满减券',
    verifyNo: 'HX202604110032',
    channel: '门店扫码',
    note: '本次已完成核销。',
    tag: '部分使用'
  },
  {
    id: 2,
    title: '新客到店礼券已完成使用',
    time: '2026-04-09 16:20',
    status: '核销成功',
    store: '华亭路门店',
    coupon: '新客礼券',
    verifyNo: 'HX202604090118',
    channel: '收银台核销',
    note: '新人礼券已使用完成。',
    tag: '已完成'
  },
  {
    id: 3,
    title: '节令优享券包商品券核销',
    time: '2026-04-07 19:48',
    status: '核销成功',
    store: '静安寺门店',
    coupon: '指定商品优惠券',
    verifyNo: 'HX202604070076',
    channel: '店员受理',
    note: '商品券已使用，其余权益仍可用。',
    tag: '商品权益'
  }
]
</script>

<style lang="scss" scoped>
.record-hero {
  display: flex;
  align-items: stretch;
  justify-content: space-between;
  gap: 20rpx;
  overflow: hidden;
  padding: 28rpx;
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.94) 0%, rgba(95, 116, 83, 0.9) 62%, rgba(183, 155, 99, 0.76) 100%);
}

.record-hero-copy {
  display: grid;
  gap: 14rpx;
  color: #fffaf3;
}

.record-eyebrow {
  color: rgba(247, 235, 208, 0.84);
  font-size: 22rpx;
  letter-spacing: 3rpx;
}

.record-title {
  font-size: 44rpx;
  font-weight: 700;
}

.record-subtitle {
  max-width: 460rpx;
  color: rgba(255, 250, 243, 0.88);
  font-size: 24rpx;
  line-height: 1.8;
}

.record-highlight {
  display: grid;
  align-content: center;
  justify-items: center;
  min-width: 172rpx;
  padding: 22rpx 18rpx;
  border-radius: 28rpx;
  background: rgba(255, 252, 246, 0.14);
}

.record-highlight-value {
  color: #fffdf8;
  font-size: 52rpx;
  font-weight: 700;
}

.record-highlight-label {
  margin-top: 8rpx;
  color: rgba(255, 250, 243, 0.78);
  font-size: 22rpx;
  text-align: center;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16rpx;
}

.stats-card {
  display: grid;
  gap: 8rpx;
  padding: 24rpx;
}

.stats-value {
  color: $cm-primary-strong;
  font-size: 40rpx;
  font-weight: 700;
}

.stats-label {
  color: $cm-text-primary;
  font-size: 26rpx;
}

.stats-note {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.filter-panel {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16rpx;
  padding: 24rpx;
}

.filter-item {
  display: grid;
  gap: 8rpx;
  padding: 18rpx;
  border-radius: 22rpx;
  background: rgba(247, 242, 233, 0.76);
}

.filter-label {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.filter-value {
  color: $cm-text-primary;
  font-size: 26rpx;
  font-weight: 600;
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
</style>
