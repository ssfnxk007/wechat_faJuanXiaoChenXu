<template>
  <view :class="['cm-page', themeClass]">
    <view class="order-hero">
      <view class="order-hero-mask"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container order-hero-content">
        <view class="hero-copy">
          <text class="hero-eyebrow">ORDER CENTER</text>
          <text class="hero-title">订单列表</text>
          <text class="hero-subtitle">集中查看购买记录、支付状态与券包履约进度，重要信息一页掌握。</text>
        </view>

        <view class="hero-stats cm-card">
          <view class="stat-item" v-for="item in overviewStats" :key="item.label">
            <text class="stat-value">{{ item.value }}</text>
            <text class="stat-label">{{ item.label }}</text>
          </view>
        </view>
      </view>
    </view>

    <view class="cm-container order-body">
      <view class="status-tabs cm-section">
        <view
          v-for="item in statusTabs"
          :key="item.value"
          class="status-tab"
          :class="{ active: currentStatus === item.value }"
          @click="currentStatus = item.value"
        >
          <text class="status-tab-label">{{ item.label }}</text>
          <text class="status-tab-count">{{ item.count }}</text>
        </view>
      </view>

      <view class="summary-strip cm-card cm-section">
        <view class="summary-main">
          <text class="summary-title">近期订单概览</text>
          <text class="summary-text">已支付订单可前往券包详情查看可用权益；待付款订单将为您保留当前下单信息。</text>
        </view>
        <view class="summary-side">{{ currentStatusLabel }}</view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="ORDER LIST" title="订单明细" subtitle="按下单时间顺序展示，便于快速追踪" />
        <view class="order-stack">
          <view class="order-card cm-card" v-for="item in filteredOrders" :key="item.id">
            <view class="order-card-top">
              <view>
                <text class="order-no">订单号 {{ item.orderNo }}</text>
                <text class="order-time">{{ item.time }}</text>
              </view>
              <text class="order-status" :class="`status-${item.status}`">{{ item.statusText }}</text>
            </view>

            <view class="order-product">
              <view class="order-cover"></view>
              <view class="order-product-info">
                <text class="order-title">{{ item.title }}</text>
                <text class="order-desc">{{ item.desc }}</text>
                <view class="order-tags">
                  <text class="order-tag" v-for="tag in item.tags" :key="tag">{{ tag }}</text>
                </view>
              </view>
            </view>

            <view class="order-meta-grid">
              <view class="meta-item">
                <text class="meta-label">支付方式</text>
                <text class="meta-value">{{ item.payment }}</text>
              </view>
              <view class="meta-item">
                <text class="meta-label">金额</text>
                <text class="meta-value price">¥{{ item.amount }}</text>
              </view>
              <view class="meta-item">
                <text class="meta-label">履约状态</text>
                <text class="meta-value">{{ item.fulfillment }}</text>
              </view>
              <view class="meta-item">
                <text class="meta-label">适用门店</text>
                <text class="meta-value">{{ item.store }}</text>
              </view>
            </view>

            <view class="order-footer">
              <text class="order-note">{{ item.note }}</text>
              <view class="order-actions">
                <view class="ghost-action" @click.stop="goOrderResult(item.id)">查看详情</view>
                <view class="primary-action" @click.stop="goOrderResult(item.id)">{{ item.actionText }}</view>
              </view>
            </view>
          </view>
        </view>
        <view v-if="!filteredOrders.length" class="empty-card cm-card">暂无订单</view>
      </view>

      <view class="notice-card cm-card cm-section">
        <text class="notice-title">订单说明</text>
        <view class="notice-list">
          <text class="notice-item">支付成功后，券包权益将自动进入账户，可在“我的卡券”中查看。</text>
          <text class="notice-item">如遇门店网络波动，核销结果以门店系统与本页记录为准。</text>
          <text class="notice-item">订单关闭后不可恢复，请以提交页显示的支付时限为准。</text>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import { useTheme } from '@/composables/use-theme'
import { ensureMiniProgramLogin } from '@/api/auth'
import { fetchOrderList } from '@/api/order'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const currentStatus = ref('all')
const orders = ref([])

const loadOrders = async () => {
  await ensureMiniProgramLogin()
  const result = await fetchOrderList({
    userId: session.userId || undefined,
    pageIndex: 1,
    pageSize: 50
  })
  orders.value = result.items
}

onShow(() => {
  loadOrders()
})

const statusTabs = computed(() => {
  const list = orders.value || []
  return [
    { label: '全部', value: 'all', count: list.length },
    { label: '待付款', value: 'pending', count: list.filter((item) => item.status === 'pending').length },
    { label: '已支付', value: 'paid', count: list.filter((item) => item.status === 'paid').length },
    { label: '已完成', value: 'completed', count: list.filter((item) => item.status === 'completed').length }
  ]
})

const overviewStats = computed(() => {
  const list = orders.value || []
  return [
    { label: '累计订单', value: String(list.length) },
    { label: '待付款', value: String(list.filter((item) => item.status === 'pending').length) },
    { label: '待使用', value: String(list.filter((item) => item.fulfillment && item.fulfillment.indexOf('待使用') > -1).length) }
  ]
})

const currentStatusLabel = computed(() => statusTabs.value.find((item) => item.value === currentStatus.value)?.label || '全部')

function goOrderResult(id) {
  if (!id) {
    return
  }
  uni.navigateTo({ url: `/pages/order/result?orderId=${id}` })
}

const filteredOrders = computed(() => {
  if (currentStatus.value === 'all') {
    return orders.value
  }
  return orders.value.filter((item) => item.status === currentStatus.value)
})
</script>

<style lang="scss" scoped>
.order-hero {
  position: relative;
  overflow: hidden;
  padding-bottom: 46rpx;
  background: linear-gradient(135deg, #244536 0%, #416451 52%, #9d8a5f 100%);
}

.order-hero-mask {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at top right, rgba(255, 245, 221, 0.2) 0, rgba(255, 245, 221, 0) 38%),
    linear-gradient(180deg, rgba(255, 255, 255, 0.06) 0%, rgba(255, 255, 255, 0) 48%);
}

.order-hero-content {
  position: relative;
  z-index: 1;
}

.hero-copy {
  display: grid;
  gap: 16rpx;
  color: #fffaf3;
}

.hero-eyebrow {
  color: rgba(247, 235, 208, 0.82);
  font-size: 22rpx;
  letter-spacing: 3rpx;
}

.hero-title {
  font-size: 48rpx;
  font-weight: 700;
}

.hero-subtitle {
  max-width: 640rpx;
  font-size: 26rpx;
  line-height: 1.8;
  color: rgba(255, 250, 243, 0.88);
}

.hero-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16rpx;
  margin-top: 28rpx;
  padding: 24rpx;
  background: rgba(255, 248, 238, 0.14);
  backdrop-filter: blur(18px);
}

.stat-item {
  display: grid;
  gap: 8rpx;
  padding: 16rpx;
  border-radius: 24rpx;
  background: rgba(255, 252, 246, 0.14);
}

.stat-value {
  color: #fffaf3;
  font-size: 38rpx;
  font-weight: 700;
}

.stat-label {
  color: rgba(255, 250, 243, 0.74);
  font-size: 22rpx;
}

.order-body {
  margin-top: -34rpx;
  padding-top: 0;
}

.status-tabs {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14rpx;
}

.status-tab {
  display: grid;
  gap: 6rpx;
  justify-items: center;
  padding: 18rpx 10rpx;
  border-radius: 24rpx;
  border: 1rpx solid $cm-border-soft;
  background: rgba(255, 252, 246, 0.86);
  color: $cm-text-secondary;
}

.status-tab.active {
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf8;
  border-color: transparent;
  box-shadow: 0 18rpx 36rpx rgba(45, 91, 72, 0.18);
}

.status-tab-label {
  font-size: 24rpx;
}

.status-tab-count {
  font-size: 30rpx;
  font-weight: 700;
}

.summary-strip {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20rpx;
  padding: 28rpx;
}

.summary-main {
  display: grid;
  gap: 10rpx;
}

.summary-title {
  color: $cm-text-primary;
  font-size: 32rpx;
  font-weight: 700;
}

.summary-text {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}

.summary-side {
  min-width: 132rpx;
  padding: 18rpx 20rpx;
  border-radius: 999rpx;
  background: rgba(45, 91, 72, 0.1);
  color: $cm-primary;
  text-align: center;
  font-size: 24rpx;
}

.order-stack {
  display: grid;
  gap: 22rpx;
  margin-top: 18rpx;
}

.empty-card {
  padding: 36rpx 28rpx;
  color: $cm-text-secondary;
  text-align: center;
  font-size: 24rpx;
}

.order-card {
  display: grid;
  gap: 22rpx;
  padding: 28rpx;
}

.order-card-top,
.order-footer {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20rpx;
}

.order-no {
  display: block;
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}

.order-time {
  display: block;
  margin-top: 8rpx;
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.order-status {
  padding: 10rpx 18rpx;
  border-radius: 999rpx;
  font-size: 22rpx;
}

.status-paid,
.status-completed {
  background: rgba(45, 91, 72, 0.1);
  color: $cm-primary;
}

.status-pending {
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
}

.order-product {
  display: grid;
  grid-template-columns: 152rpx 1fr;
  gap: 18rpx;
  align-items: center;
}

.order-cover {
  height: 152rpx;
  border-radius: 28rpx;
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.18) 0%, rgba(183, 155, 99, 0.24) 100%);
}

.order-product-info {
  display: grid;
  gap: 10rpx;
}

.order-title {
  color: $cm-text-primary;
  font-size: 32rpx;
  font-weight: 700;
}

.order-desc {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.7;
}

.order-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 10rpx;
}

.order-tag {
  padding: 8rpx 16rpx;
  border-radius: 999rpx;
  background: rgba(95, 116, 83, 0.1);
  color: $cm-primary;
  font-size: 20rpx;
}

.order-meta-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18rpx;
  padding: 22rpx;
  border-radius: 28rpx;
  background: rgba(247, 242, 233, 0.76);
}

.meta-item {
  display: grid;
  gap: 8rpx;
}

.meta-label {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.meta-value {
  color: $cm-text-primary;
  font-size: 26rpx;
}

.meta-value.price {
  color: $cm-primary-strong;
  font-weight: 700;
}

.order-note {
  flex: 1;
  color: $cm-text-secondary;
  font-size: 23rpx;
  line-height: 1.7;
}

.order-actions {
  display: flex;
  gap: 12rpx;
}

.ghost-action,
.primary-action {
  min-width: 148rpx;
  padding: 18rpx 22rpx;
  border-radius: 999rpx;
  text-align: center;
  font-size: 24rpx;
}

.ghost-action {
  border: 1rpx solid $cm-border-soft;
  color: $cm-text-secondary;
  background: rgba(255, 252, 246, 0.7);
}

.primary-action {
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf8;
}

.notice-card {
  display: grid;
  gap: 16rpx;
  padding: 28rpx;
}

.notice-title {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}

.notice-list {
  display: grid;
  gap: 12rpx;
}

.notice-item {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}

.theme-light .order-cover {
  background: linear-gradient(180deg, #f8fafc 0%, #eef2f7 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .order-tag {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-light .order-meta-grid,
.theme-light .notice-card {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .meta-value.price {
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
</style>
