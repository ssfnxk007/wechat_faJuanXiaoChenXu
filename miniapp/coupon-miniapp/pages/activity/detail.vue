<template>
  <view :class="['cm-page', themeClass]">
    <view class="activity-hero">
      <view class="activity-hero-mask"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container activity-hero-content">
        <view class="activity-topbar">
          <view class="activity-back" @click="goBack">返回</view>
          <view class="activity-chip">活动详情</view>
        </view>
        <text class="activity-eyebrow">ACTIVITY</text>
        <text class="activity-title">{{ activity.title }}</text>
        <text class="activity-subtitle">{{ activity.subtitle }}</text>
      </view>
    </view>

    <view class="cm-container activity-body">
      <view class="action-bar cm-card">
        <view class="action-copy">
          <text class="action-title">{{ activity.ctaTitle }}</text>
          <text class="action-desc">{{ activity.ctaDesc }}</text>
        </view>
        <view class="action-buttons">
          <view class="primary-action" @click="handlePrimaryAction">{{ activity.ctaText }}</view>
          <view class="ghost-action" @click="goBack">稍后再看</view>
        </view>
      </view>

      <view class="activity-brief cm-card">
        <view class="brief-item" v-for="item in activity.briefs" :key="item.label">
          <text class="brief-label">{{ item.label }}</text>
          <text class="brief-value">{{ item.value }}</text>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="ACTIVITY INFO" title="活动说明" subtitle="帮助用户快速了解当前活动内容" />
        <view class="desc-card cm-card">
          <text class="desc-text">{{ activity.description }}</text>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="PROCESS" title="参与流程" subtitle="按步骤完成领取或使用" />
        <view class="step-stack">
          <view class="step-card cm-card" v-for="item in activity.steps" :key="item.title">
            <text class="step-index">{{ item.index }}</text>
            <view class="step-copy">
              <text class="step-title">{{ item.title }}</text>
              <text class="step-desc">{{ item.desc }}</text>
            </view>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="RULES" title="活动规则" subtitle="请以活动页展示和后台配置为准" />
        <view class="rule-stack">
          <view class="rule-card cm-card" v-for="(item, index) in activity.rules" :key="item">
            <text class="rule-index">0{{ index + 1 }}</text>
            <text class="rule-text">{{ item }}</text>
          </view>
        </view>
      </view>

    </view>

    <view class="sticky-cta">
      <view class="sticky-primary" @click="handlePrimaryAction">{{ activity.ctaText }}</view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onLoad, onShareAppMessage } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import { useTheme } from '@/composables/use-theme'
import { useSessionStore } from '@/store/session'
import { createEventKey, createShareId, getOrCreateVisitorKey, normalizeSceneValue, reportShareTrackingEvent } from '@/utils/share-tracking'

const { themeClass } = useTheme()
const session = useSessionStore()

const activityMap = {
  newcomer: {
    title: '新人有礼',
    subtitle: '成为新用户后可领取一次新人券',
    description: '新用户完成微信授权后，即可领取新人专享优惠券。领取成功后会进入券包，可在有效期内到店核销使用。',
    briefs: [
      { label: '活动对象', value: '新用户' },
      { label: '领取次数', value: '仅限一次' },
      { label: '领取后去向', value: '进入券包' },
    ],
    steps: [
      { index: '01', title: '完成授权', desc: '首次进入小程序后完成微信授权并建立用户档案。' },
      { index: '02', title: '进入领券页', desc: '在首页或活动入口中点击免费领取。' },
      { index: '03', title: '查看券包', desc: '领取成功后可在券包中查看券详情和使用规则。' },
    ],
    rules: ['新人券仅限新用户领取一次。', '有效期以后台配置为准。', '核销时请以券详情页规则和门店受理范围为准。'],
    ctaTitle: '立即领取新人券',
    ctaDesc: '领取成功后会直接进入你的券包。',
    ctaText: '去领券',
    action: 'coupon-center',
  },
  free: {
    title: '免费领券活动',
    subtitle: '当前可领取的无门槛券、满减券、商品券',
    description: '进入活动后，可查看当前开放领取的优惠券列表。领取成功后进入券包，后续可到店出示二维码核销。',
    briefs: [
      { label: '活动类型', value: '免费领取' },
      { label: '券类型', value: '无门槛 / 满减 / 商品券' },
      { label: '使用方式', value: '券包查看后到店核销' },
    ],
    steps: [
      { index: '01', title: '查看券列表', desc: '进入活动页后查看当前可领取优惠券。' },
      { index: '02', title: '立即领取', desc: '符合条件即可领取至券包。' },
      { index: '03', title: '到店使用', desc: '到店出示二维码，由 ERP 扫码核销。' },
    ],
    rules: ['每种券的有效期与使用门店以券详情页为准。', '部分券有领取上限或活动时间限制。', '领取后请尽快在有效期内使用。'],
    ctaTitle: '去免费领券',
    ctaDesc: '立即查看当前开放领取的优惠券。',
    ctaText: '去领券中心',
    action: 'coupon-center',
  },
  writeoff: {
    title: '到店核销说明',
    subtitle: '领券后出示二维码，由 ERP 完成核销',
    description: '领取或购买券包后，券会进入券包。到店后打开券详情页出示二维码，由 ERP 扫码并调用接口完成核销。',
    briefs: [
      { label: '核销方式', value: 'ERP 扫码' },
      { label: '出示内容', value: '券详情二维码' },
      { label: '核销结果', value: '实时更新券状态' },
    ],
    steps: [
      { index: '01', title: '进入券包', desc: '找到待使用的券或已购买券包发放的券。' },
      { index: '02', title: '打开券详情', desc: '出示券详情页二维码给门店 ERP 扫码。' },
      { index: '03', title: '完成核销', desc: '核销成功后券状态会立即变更。' },
    ],
    rules: ['二维码仅用于门店核销，请勿重复截图转发。', '已核销的券不可恢复。', '若门店网络异常，请稍后刷新券状态。'],
    ctaTitle: '去查看券包',
    ctaDesc: '从券包进入券详情页，查看二维码和可用状态。',
    ctaText: '打开券包',
    action: 'coupon-center',
  },
}

const activity = ref(activityMap.newcomer)
const activityKey = ref('newcomer')
const hasReportedOpen = ref(false)
const pagePath = '/pages/activity/detail'

onLoad((options = {}) => {
  const key = String(options.key || 'newcomer')
  activityKey.value = Object.prototype.hasOwnProperty.call(activityMap, key) ? key : 'newcomer'
  activity.value = activityMap[activityKey.value]

  if (options.shareId && !hasReportedOpen.value) {
    hasReportedOpen.value = true
    reportShareTrackingEvent({
      eventType: 'open',
      eventKey: createEventKey('open', String(options.shareId), session.userId ? `u:${session.userId}` : getOrCreateVisitorKey(), pagePath),
      shareId: String(options.shareId),
      targetType: 'activity',
      targetKey: activityKey.value,
      pagePath,
      visitorKey: session.userId ? null : getOrCreateVisitorKey(),
      scene: normalizeSceneValue(),
      query: options,
    })
  }
})

onShareAppMessage(() => {
  const shareId = createShareId()
  reportShareTrackingEvent({
    eventType: 'shareIntent',
    eventKey: createEventKey('shareIntent', shareId, '', pagePath),
    shareId,
    targetType: 'activity',
    targetKey: activityKey.value,
    pagePath,
    scene: normalizeSceneValue(),
    query: { key: activityKey.value, shareId },
  })

  const activityTitle = activity.value?.title || '活动详情'
  return {
    title: `邀你查看：${activityTitle}`,
    path: `${pagePath}?key=${encodeURIComponent(activityKey.value)}&shareId=${shareId}`,
  }
})

function goBack() {
  uni.navigateBack({ delta: 1 })
}

function handlePrimaryAction() {
  if (activity.value.action === 'coupon-center') {
    uni.switchTab({ url: '/pages/coupon/index' })
    return
  }
  uni.switchTab({ url: '/pages/index/index' })
}
</script>

<style lang="scss" scoped>
.activity-hero {
  position: relative;
  overflow: hidden;
  min-height: 320rpx;
  border-bottom-left-radius: 48rpx;
  border-bottom-right-radius: 48rpx;
  background: linear-gradient(135deg, #274b3a 0%, #4d6956 58%, #a28a5c 100%);
  color: #fffaf4;
}

.activity-hero-mask {
  position: absolute;
  inset: 0;
  background: radial-gradient(circle at right top, rgba(255, 243, 214, 0.2), transparent 34%);
}

.activity-hero-content,
.action-bar {
  position: relative;
  display: grid;
  gap: 20rpx;
}

.activity-hero-content {
  padding-top: 18rpx;
  padding-bottom: 42rpx;
}

.activity-topbar,
.brief-item,
.step-card,
.action-buttons {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
}

.activity-back,
.activity-chip,
.primary-action,
.ghost-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 48rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  font-size: 22rpx;
}

.activity-back,
.activity-chip {
  background: rgba(255, 255, 255, 0.14);
}

.activity-eyebrow {
  font-size: 22rpx;
  letter-spacing: 4rpx;
  opacity: 0.82;
}

.activity-title {
  font-size: 42rpx;
  font-weight: 700;
}

.activity-subtitle,
.desc-text,
.step-desc,
.rule-text,
.action-desc,
.brief-label {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.7;
}

.activity-body {
  display: grid;
  gap: 22rpx;
  padding-top: 24rpx;
  padding-bottom: 180rpx;
}

.activity-brief,
.desc-card,
.step-card,
.rule-card,
.action-bar {
  padding: 24rpx;
}

.activity-brief,
.step-stack,
.rule-stack {
  display: grid;
  gap: 18rpx;
}

.brief-value,
.step-title,
.action-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}

.step-index,
.rule-index {
  width: 68rpx;
  min-width: 68rpx;
  height: 68rpx;
  border-radius: 50%;
  background: rgba(44, 92, 73, 0.1);
  color: $cm-primary;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
}

.step-copy {
  flex: 1;
  display: grid;
  gap: 8rpx;
}

.primary-action {
  background: $cm-primary;
  color: #fff;
}

.ghost-action {
  background: rgba(15, 23, 42, 0.06);
  color: $cm-text-primary;
}

.theme-light .activity-hero {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  color: #111827;
}

.theme-light .activity-back,
.theme-light .activity-chip {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-candy .activity-hero {
  background: linear-gradient(135deg, #e0e7ff 0%, #dbeafe 52%, #bfdbfe 100%);
  color: #1e3a8a;
}

.theme-candy .activity-hero-mask {
  background:
    radial-gradient(circle at right top, rgba(255, 255, 255, 0.88), transparent 34%),
    radial-gradient(circle at left bottom, rgba(219, 234, 254, 0.7), transparent 28%);
}

.theme-candy .activity-back,
.theme-candy .activity-chip {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(191, 219, 254, 0.7);
  color: #2563eb;
}

.theme-candy .activity-subtitle {
  color: #3b82f6;
}

.theme-candy .activity-brief,
.theme-candy .step-card,
.theme-candy .rule-card,
.theme-candy .action-bar {
  background: linear-gradient(180deg, #ffffff 0%, #eff6ff 100%);
  border: 1rpx solid rgba(191, 219, 254, 0.7);
  box-shadow: 0 18rpx 44rpx rgba(37, 99, 235, 0.06);
}

.theme-candy .brief-value,
.theme-candy .step-title,
.theme-candy .action-title {
  color: #1e3a8a;
}

.theme-candy .step-index,
.theme-candy .rule-index {
  background: rgba(59, 130, 246, 0.1);
  color: #2563eb;
}

.theme-candy .action-desc {
  color: #64748b;
}

.theme-candy .primary-action {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
  color: #ffffff;
  box-shadow: 0 18rpx 36rpx rgba(37, 99, 235, 0.18);
}

.theme-candy .ghost-action {
  background: rgba(59, 130, 246, 0.05);
  border: 1rpx solid rgba(191, 219, 254, 0.65);
  color: #2563eb;
}

/* ========== Orange Theme ========== */
.theme-orange .activity-hero {
  background: linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 52%, #FED7AA 100%);
  color: #9A3412;
}

.theme-orange .activity-hero-mask {
  background:
    radial-gradient(circle at right top, rgba(255, 255, 255, 0.88), transparent 34%),
    radial-gradient(circle at left bottom, rgba(255, 237, 213, 0.7), transparent 28%);
}

.theme-orange .activity-back,
.theme-orange .activity-chip {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(254, 215, 170, 0.7);
  color: #EA580C;
}

.theme-orange .activity-subtitle {
  color: #F97316;
}

.theme-orange .activity-brief,
.theme-orange .step-card,
.theme-orange .rule-card,
.theme-orange .action-bar {
  background: linear-gradient(180deg, #ffffff 0%, #FFFBF5 100%);
  border: 1rpx solid rgba(254, 215, 170, 0.7);
  box-shadow: 0 18rpx 44rpx rgba(234, 88, 12, 0.06);
}

.theme-orange .brief-value,
.theme-orange .step-title,
.theme-orange .action-title {
  color: #9A3412;
}

.theme-orange .step-index,
.theme-orange .rule-index {
  background: rgba(249, 115, 22, 0.1);
  color: #EA580C;
}

.theme-orange .action-desc {
  color: #64748b;
}

.theme-orange .primary-action {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  color: #ffffff;
  box-shadow: 0 18rpx 36rpx rgba(234, 88, 12, 0.18);
}

.theme-orange .ghost-action {
  background: rgba(249, 115, 22, 0.05);
  border: 1rpx solid rgba(254, 215, 170, 0.65);
  color: #EA580C;
}

/* ========== Red Theme ========== */
.theme-red .activity-hero {
  background: linear-gradient(135deg, #FFEBEE 0%, #FFCDD2 52%, #FFCDD2 100%);
  color: #B71C1C;
}

.theme-red .activity-hero-mask {
  background:
    radial-gradient(circle at right top, rgba(255, 255, 255, 0.88), transparent 34%),
    radial-gradient(circle at left bottom, rgba(255, 235, 238, 0.7), transparent 28%);
}

.theme-red .activity-back,
.theme-red .activity-chip {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(255, 205, 210, 0.7);
  color: #E53935;
}

.theme-red .activity-subtitle {
  color: #EF5350;
}

.theme-red .activity-brief,
.theme-red .step-card,
.theme-red .rule-card,
.theme-red .action-bar {
  background: linear-gradient(180deg, #ffffff 0%, #FFEBEE 100%);
  border: 1rpx solid rgba(255, 205, 210, 0.7);
  box-shadow: 0 18rpx 44rpx rgba(229, 57, 53, 0.06);
}

.theme-red .brief-value,
.theme-red .step-title,
.theme-red .action-title {
  color: #B71C1C;
}

.theme-red .step-index,
.theme-red .rule-index {
  background: rgba(239, 83, 80, 0.1);
  color: #E53935;
}

.theme-red .action-desc {
  color: #64748b;
}

.theme-red .primary-action {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  color: #ffffff;
  box-shadow: 0 18rpx 36rpx rgba(229, 57, 53, 0.18);
}

.theme-red .ghost-action {
  background: rgba(239, 83, 80, 0.05);
  border: 1rpx solid rgba(255, 205, 210, 0.65);
  color: #E53935;
}

/* ========== 强化首屏主行动 CTA ========== */
.action-bar .action-buttons {
  justify-content: flex-start;
}

.action-bar .primary-action {
  flex: 1;
  min-height: 80rpx;
  font-size: 28rpx;
  font-weight: 700;
  padding: 0 32rpx;
}

.action-bar .ghost-action {
  min-height: 80rpx;
  padding: 0 28rpx;
  font-size: 24rpx;
}

/* ========== 吸底快速操作 ========== */
.sticky-cta {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 50;
  display: flex;
  padding: 14rpx 24rpx calc(14rpx + env(safe-area-inset-bottom)) 24rpx;
  background: rgba(255, 255, 255, 0.94);
  backdrop-filter: blur(14rpx);
  border-top: 1rpx solid rgba(15, 23, 42, 0.08);
}

.sticky-primary {
  flex: 1;
  min-height: 88rpx;
  border-radius: 999rpx;
  background: $cm-primary;
  color: #ffffff;
  font-size: 30rpx;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 14rpx 28rpx rgba(39, 75, 58, 0.18);
}

.theme-light .sticky-cta {
  background: rgba(255, 255, 255, 0.94);
  border-top-color: rgba(15, 23, 42, 0.08);
}

.theme-candy .sticky-cta {
  background: rgba(239, 246, 255, 0.92);
  border-top-color: rgba(191, 219, 254, 0.7);
}

.theme-candy .sticky-primary {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
  box-shadow: 0 14rpx 28rpx rgba(37, 99, 235, 0.2);
}

.theme-orange .sticky-cta {
  background: rgba(255, 251, 245, 0.92);
  border-top-color: rgba(254, 215, 170, 0.7);
}

.theme-orange .sticky-primary {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  box-shadow: 0 14rpx 28rpx rgba(234, 88, 12, 0.2);
}

.theme-red .sticky-cta {
  background: rgba(255, 235, 238, 0.92);
  border-top-color: rgba(255, 205, 210, 0.7);
}

.theme-red .sticky-primary {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  box-shadow: 0 14rpx 28rpx rgba(229, 57, 53, 0.2);
}
</style>
