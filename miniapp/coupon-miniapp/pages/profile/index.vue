<template>
  <view :class="['cm-page', 'profile-page', themeClass]">
    <CmPullRefresh :refreshing="refreshing" @refresh="handleRefresh">
    <view class="cm-container">
    <view class="cm-nav-spacer"></view>

    <view class="profile-hero cm-card">
      <view class="profile-avatar">{{ avatarText }}</view>
      <view class="profile-meta">
        <text class="profile-name">{{ profileName }}</text>
        <text class="profile-id">{{ profileSubtitle }}</text>
      </view>
      <view class="profile-badge">已授权</view>
    </view>

    <view class="profile-stats cm-card">
      <view class="stat-item" v-for="item in stats" :key="item.label">
        <text class="stat-value">{{ item.value }}</text>
        <text class="stat-label">{{ item.label }}</text>
      </view>
    </view>

    <view class="cm-section">
      <SectionHeader eyebrow="ACCOUNT" title="个人中心" subtitle="订单与卡券管理" />
      <view class="menu-stack">
        <view class="menu-card cm-card" v-for="item in menus" :key="item.title" @click="handleMenuClick(item)">
          <view class="menu-copy">
            <text class="menu-title">{{ item.title }}</text>
            <text class="menu-desc">{{ item.desc }}</text>
          </view>
          <text class="menu-arrow">›</text>
        </view>
      </view>
    </view>
    </view>
    </CmPullRefresh>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import SectionHeader from '@/components/SectionHeader.vue'
import CmPullRefresh from '@/components/CmPullRefresh.vue'
import { useTheme } from '@/composables/use-theme'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const refreshing = ref(false)

async function handleRefresh() {
  if (refreshing.value) return
  refreshing.value = true
  await new Promise((resolve) => setTimeout(resolve, 700))
  uni.showToast({ title: '已为您刷新', icon: 'none' })
  refreshing.value = false
}
const profileName = computed(() => session.nickname || '云锦臻选用户')
const profileSubtitle = computed(() => {
  if (session.userId) {
    return `用户编号 ${session.userId} · 已完成微信授权`
  }
  return '首次授权后自动建档'
})
const avatarText = computed(() => {
  const name = String(profileName.value || '').trim()
  return name ? name.slice(0, 2).toUpperCase() : 'YJ'
})

const handleMenuClick = (item) => {
  if (!item.route) {
    return
  }

  if (item.openType === 'switchTab') {
    uni.switchTab({ url: item.route })
    return
  }

  uni.navigateTo({ url: item.route })
}

const menus = [
  { title: '我的订单', desc: '查看购买记录', route: '/pages/order/list' },
  { title: '我的券包', desc: '查看全部卡券', route: '/pages/coupon/index', openType: 'switchTab' },
  { title: '核销记录', desc: '查看历史核销', route: '/pages/verify-record/index' },
  { title: '规则说明', desc: '查看使用规则', route: '/pages/rules/index' }
]
</script>

<style lang="scss" scoped>
.profile-page {
  padding-bottom: 36rpx;
}
.profile-hero {
  display: grid;
  grid-template-columns: 104rpx 1fr auto;
  align-items: center;
  gap: 22rpx;
  padding: 30rpx;
}
.profile-avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 104rpx;
  height: 104rpx;
  border-radius: 50%;
  background: linear-gradient(135deg, #2d5b48 0%, #8b9c6d 100%);
  color: #fffdf8;
  font-size: 34rpx;
  font-weight: 700;
}
.profile-meta {
  display: grid;
  gap: 10rpx;
}
.profile-name {
  color: $cm-text-primary;
  font-size: 34rpx;
  font-weight: 700;
}
.profile-id {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.7;
}
.profile-badge {
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
.profile-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14rpx;
  margin-top: 18rpx;
  padding: 18rpx;
}
.stat-item {
  display: grid;
  gap: 8rpx;
  padding: 18rpx 12rpx;
  border-radius: 24rpx;
  background: rgba(45, 91, 72, 0.05);
  text-align: center;
}
.stat-value {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}
.stat-label {
  color: $cm-text-secondary;
  font-size: 22rpx;
}
.menu-stack {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
}
.menu-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
  padding: 26rpx 28rpx;
}
.menu-copy {
  display: grid;
  gap: 10rpx;
}
.menu-title {
  display: block;
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}
.menu-desc {
  display: block;
  color: $cm-text-secondary;
  font-size: 22rpx;
}
.menu-arrow {
  color: $cm-text-tertiary;
  font-size: 40rpx;
}

.theme-light .profile-hero,
.theme-light .profile-stats,
.theme-light .menu-card {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .profile-avatar {
  background: #111827;
  color: #ffffff;
}

.theme-light .profile-badge {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-light .stat-item {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

/* ========== Candy Theme ========== */
.theme-candy .profile-hero,
.theme-candy .profile-stats,
.theme-candy .menu-card {
  background: #ffffff;
  border: 1rpx solid rgba(191, 219, 254, 0.6);
  box-shadow: 0 14rpx 36rpx rgba(37, 99, 235, 0.05);
}

.theme-candy .profile-avatar {
  background: linear-gradient(135deg, #3B82F6 0%, #60A5FA 100%);
  color: #ffffff;
}

.theme-candy .profile-badge {
  background: rgba(59, 130, 246, 0.08);
  color: #2563EB;
}

.theme-candy .stat-item {
  background: linear-gradient(180deg, #F8FAFC 0%, #EFF6FF 100%);
  border: 1rpx solid rgba(219, 234, 254, 0.8);
}

.theme-candy .menu-title,
.theme-candy .stat-value {
  color: #1E3A8A;
}

.theme-candy .menu-arrow {
  color: #93C5FD;
}


/* ========== Orange Theme ========== */
.theme-orange .profile-hero,
.theme-orange .profile-stats,
.theme-orange .menu-card {
  background: #ffffff;
  border: 1rpx solid rgba(254, 215, 170, 0.6);
  box-shadow: 0 14rpx 36rpx rgba(234, 88, 12, 0.05);
}

.theme-orange .profile-avatar {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  color: #ffffff;
}

.theme-orange .profile-badge {
  background: rgba(249, 115, 22, 0.08);
  color: #EA580C;
}

.theme-orange .stat-item {
  background: linear-gradient(180deg, #FFFBF5 0%, #FFFBF5 100%);
  border: 1rpx solid rgba(255, 237, 213, 0.8);
}

.theme-orange .menu-title,
.theme-orange .stat-value {
  color: #9A3412;
}

.theme-orange .menu-arrow {
  color: #FDBA74;
}

/* ========== Red Theme ========== */
.theme-red .profile-hero,
.theme-red .profile-stats,
.theme-red .menu-card {
  background: #ffffff;
  border: 1rpx solid rgba(255, 205, 210, 0.6);
  box-shadow: 0 14rpx 36rpx rgba(229, 57, 53, 0.05);
}

.theme-red .profile-avatar {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  color: #ffffff;
}

.theme-red .profile-badge {
  background: rgba(239, 83, 80, 0.08);
  color: #E53935;
}

.theme-red .stat-item {
  background: linear-gradient(180deg, #FFFBFA 0%, #FFEBEE 100%);
  border: 1rpx solid rgba(255, 235, 238, 0.8);
}

.theme-red .menu-title,
.theme-red .stat-value {
  color: #B71C1C;
}

.theme-red .menu-arrow {
  color: #FECDD3;
}
</style>
