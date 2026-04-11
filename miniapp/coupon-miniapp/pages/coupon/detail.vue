<template>
  <view :class="['cm-page', 'detail-page', themeClass]">
    <view class="detail-hero">
      <view class="hero-overlay"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container detail-hero-content">
        <view class="detail-topbar">
          <view class="detail-back" @click="goBack">‹ 返回</view>
          <view class="detail-status">{{ statusText }}</view>
        </view>

        <view class="detail-header">
          <text class="detail-type">{{ couponTypeText }}</text>
          <text class="detail-title">{{ couponDetail.couponTemplateName }}</text>
          <text class="detail-desc">{{ couponDesc }}</text>
        </view>
      </view>
    </view>

    <view class="cm-container detail-body">
      <view v-if="isUserCouponMode" class="qr-card cm-card">
        <view class="qr-card-head">
          <text class="qr-card-title">核销二维码</text>
          <text class="qr-card-note">到店出示即可</text>
        </view>
        <view class="qr-box">
          <image v-if="qrImageUrl" class="qr-image" :src="qrImageUrl" mode="aspectFit" />
          <view v-else class="qr-empty">
            <text class="qr-empty-title">二维码加载中</text>
            <text class="qr-empty-desc">稍后可重新进入本页。</text>
          </view>
        </view>
        <text class="coupon-code">券码：{{ couponDetail.couponCode }}</text>
        <view class="qr-actions">
          <view class="qr-action primary" @click="goProfile">查看核销记录</view>
          <view class="qr-action secondary" @click="goCouponList">返回卡包</view>
        </view>
      </view>

      <view v-else class="claim-card cm-card">
        <view class="claim-head">
          <text class="claim-title">立即领取</text>
          <text class="claim-note">领取后自动进入券包</text>
        </view>
        <view class="claim-body">
          <view class="claim-stat">
            <text class="claim-stat-label">每人限领</text>
            <text class="claim-stat-value">{{ couponDetail.perUserLimit || 1 }} 张</text>
          </view>
          <view class="claim-stat">
            <text class="claim-stat-label">已领取</text>
            <text class="claim-stat-value">{{ couponDetail.claimedCount || 0 }} 张</text>
          </view>
        </view>
        <view class="claim-actions">
          <view class="qr-action secondary" @click="goBack">稍后再说</view>
          <view class="qr-action primary" :class="{ disabled: claiming || !couponDetail.canClaim }" @click="handleClaim">
            {{ claimButtonText }}
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="VALIDITY" title="使用信息" subtitle="有效期与适用范围" />
        <view class="info-stack">
          <view class="info-card cm-card" v-for="item in infoBlocks" :key="item.label">
            <text class="info-label">{{ item.label }}</text>
            <text class="info-value">{{ item.value }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="RULES" title="使用规则" subtitle="使用前请查看" />
        <view class="rule-stack">
          <view class="rule-card cm-card" v-for="(rule, index) in rules" :key="rule">
            <text class="rule-index">0{{ index + 1 }}</text>
            <text class="rule-text">{{ rule }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="RELATED GOODS" title="推荐搭配商品" subtitle="可搭配商品" action-text="查看全部" @action-click="goMall" />
        <view class="cm-grid-2 goods-grid">
          <view class="goods-card cm-card" v-for="item in relatedGoods" :key="item.id" @click="goMall">
            <view class="goods-cover"></view>
            <text class="goods-title">{{ item.title }}</text>
            <text class="goods-desc">{{ item.desc }}</text>
            <view class="goods-footer">
              <text class="goods-price">¥{{ item.price }}</text>
              <text class="goods-tag">{{ item.tag }}</text>
            </view>
          </view>
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
import { ensureMiniProgramLogin } from '@/api/auth'
import { claimMiniAppCouponTemplate, getMiniAppCouponTemplateDetail, getMiniAppUserCouponDetail } from '@/api/miniapp'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const claiming = ref(false)
const isUserCouponMode = ref(true)
const couponDetail = ref({
  id: 0,
  couponTemplateId: 0,
  couponTemplateName: '春日满减券',
  couponCode: '',
  templateType: 4,
  thresholdAmount: 100,
  discountAmount: 20,
  effectiveAt: '',
  expireAt: '',
  validFrom: '',
  validTo: '',
  validDays: 0,
  validPeriodType: 1,
  isAllStores: true,
  isNewUserOnly: false,
  perUserLimit: 1,
  claimedCount: 0,
  canClaim: true,
  templateRemark: '到店结算时请出示二维码，由 ERP 端扫码核销。',
  writeOffRecords: []
})

const qrImageUrl = computed(() => {
  if (!isUserCouponMode.value || !couponDetail.value.id || !session.userId) {
    return ''
  }

  const baseUrl = String(session.apiBaseUrl || '').replace(/\/+$/, '')
  if (!baseUrl) {
    return ''
  }

  return `${baseUrl}/api/miniapp/users/${session.userId}/coupons/${couponDetail.value.id}/qrcode`
})

const couponTypeText = computed(() => ({ 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }[couponDetail.value.templateType] || '优惠券'))
const statusText = computed(() => {
  if (!isUserCouponMode.value) {
    return couponDetail.value.canClaim ? '可领取' : '已领完'
  }
  return '可使用'
})
const claimButtonText = computed(() => {
  if (!couponDetail.value.canClaim) {
    return '领取上限已满'
  }
  return claiming.value ? '领取中...' : '立即领取'
})
const couponDesc = computed(() => {
  const amountText = couponDetail.value.thresholdAmount
    ? `满 ${couponDetail.value.thresholdAmount} 元减 ${couponDetail.value.discountAmount || 0} 元`
    : `立减 ${couponDetail.value.discountAmount || 0} 元`
  return `${amountText} · ${couponDetail.value.isAllStores ? '门店通用' : '指定门店'} · ${isUserCouponMode.value ? 'ERP 扫码核销' : '领取后进入我的券包'}`
})
const effectiveDateText = computed(() => {
  if (isUserCouponMode.value && couponDetail.value.effectiveAt && couponDetail.value.expireAt) {
    return `${String(couponDetail.value.effectiveAt).slice(0, 10)} 至 ${String(couponDetail.value.expireAt).slice(0, 10)}`
  }
  if (Number(couponDetail.value.validPeriodType) === 1 && couponDetail.value.validFrom && couponDetail.value.validTo) {
    return `${String(couponDetail.value.validFrom).slice(0, 10)} 至 ${String(couponDetail.value.validTo).slice(0, 10)}`
  }
  if (Number(couponDetail.value.validPeriodType) === 2 && couponDetail.value.validDays) {
    return `领取后 ${couponDetail.value.validDays} 天有效`
  }
  return '以下发时规则为准'
})
const infoBlocks = computed(() => ([
  { label: '优惠内容', value: couponDetail.value.thresholdAmount ? `满 ${couponDetail.value.thresholdAmount} 元减 ${couponDetail.value.discountAmount || 0} 元` : `立减 ${couponDetail.value.discountAmount || 0} 元` },
  { label: isUserCouponMode.value ? '有效期' : '领取后有效期', value: effectiveDateText.value },
  { label: '适用门店', value: couponDetail.value.isAllStores ? '全部门店可用' : '指定门店可用' },
  { label: '适用说明', value: couponDetail.value.templateRemark || '请以活动规则为准' }
]))
const rules = computed(() => {
  const baseRules = [
    '同一张券只可核销一次，核销后不可恢复。',
    '如为指定商品券，请在结算时确认商品属于可用范围。',
    '券有效期以页面展示为准，过期后不可使用。'
  ]
  if (!isUserCouponMode.value) {
    baseRules.unshift('领取成功后将自动进入我的券包，后续在券详情页展示二维码。')
  }
  if (couponDetail.value.isNewUserOnly) {
    baseRules.unshift('该券为新人专享券，成为用户后仅可领取一次。')
  }
  return baseRules
})

const relatedGoods = [
  { id: 1, title: '东方香礼', desc: '适合搭配满减券下单', price: '168', tag: '满减可用' },
  { id: 2, title: '云锦礼盒', desc: '门店通用，适合到店核销', price: '128', tag: '门店核销' }
]

const goBack = () => {
  uni.navigateBack({ delta: 1 })
}

const goProfile = () => {
  uni.switchTab({ url: '/pages/profile/index' })
}

const goCouponList = () => {
  uni.switchTab({ url: '/pages/coupon/index' })
}

const goMall = () => {
  uni.switchTab({ url: '/pages/mall/index' })
}

async function loadUserCouponDetail(id) {
  if (!id || !session.userId) {
    return
  }

  const result = await getMiniAppUserCouponDetail(session.userId, id)
  if (result) {
    couponDetail.value = result
    isUserCouponMode.value = true
  }
}

async function loadTemplateDetail(templateId) {
  const params = session.userId ? { userId: session.userId } : undefined
  const result = await getMiniAppCouponTemplateDetail(templateId, params)
  if (result) {
    couponDetail.value = {
      ...couponDetail.value,
      ...result,
      couponTemplateId: result.id,
      couponTemplateName: result.name,
      id: 0,
      couponCode: ''
    }
    isUserCouponMode.value = false
  }
}

async function handleClaim() {
  if (claiming.value || !couponDetail.value.canClaim) {
    return
  }

  try {
    claiming.value = true
    await ensureMiniProgramLogin()
    if (!session.userId) {
      throw new Error('请先完成微信授权后再领取')
    }

    const result = await claimMiniAppCouponTemplate(couponDetail.value.couponTemplateId || couponDetail.value.id, {
      userId: session.userId
    })

    uni.showToast({ title: '领取成功', icon: 'success' })
    uni.redirectTo({ url: `/pages/coupon/detail?id=${result.userCouponId}` })
  } catch (error) {
    console.warn('[coupon-detail] handleClaim failed', error)
    uni.showToast({ title: error?.message || '领取失败，请稍后再试', icon: 'none' })
  } finally {
    claiming.value = false
  }
}

onLoad(async (options) => {
  try {
    await ensureMiniProgramLogin()
    if (options?.templateId) {
      await loadTemplateDetail(options.templateId)
      return
    }
    await loadUserCouponDetail(options?.id)
  } catch (error) {
    console.warn('[coupon-detail] onLoad failed', error)
  }
})
</script>

<style lang="scss" scoped>
.detail-page {
  background: linear-gradient(180deg, #f7f2e8 0%, #f3ede2 42%, #f8f6f1 100%);
}
.detail-hero {
  position: relative;
  overflow: hidden;
  min-height: 420rpx;
  background: linear-gradient(135deg, #233d33 0%, #48634f 56%, #9f9068 100%);
  color: #fffaf4;
  border-bottom-left-radius: 42rpx;
  border-bottom-right-radius: 42rpx;
}
.hero-overlay {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at top right, rgba(255, 248, 229, 0.2), transparent 28%),
    radial-gradient(circle at left center, rgba(255, 255, 255, 0.1), transparent 24%);
}
.detail-hero-content {
  position: relative;
  display: grid;
  gap: 34rpx;
  padding-top: 18rpx;
  padding-bottom: 36rpx;
}
.detail-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.detail-back,
.detail-status {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 56rpx;
  padding: 0 20rpx;
  border-radius: 999rpx;
  background: rgba(255, 255, 255, 0.12);
  font-size: 24rpx;
}
.detail-header {
  display: grid;
  gap: 14rpx;
}
.detail-type {
  color: #f1e1b8;
  font-size: 24rpx;
}
.detail-title {
  font-size: 52rpx;
  font-weight: 700;
}
.detail-desc {
  max-width: 620rpx;
  font-size: 25rpx;
  line-height: 1.8;
  opacity: 0.9;
}
.detail-body {
  margin-top: -28rpx;
}
.qr-card,
.claim-card {
  position: relative;
  z-index: 2;
  display: grid;
  gap: 20rpx;
  padding: 30rpx;
}
.qr-card-head,
.claim-head {
  display: grid;
  gap: 8rpx;
}
.qr-card-title,
.claim-title {
  color: $cm-text-primary;
  font-size: 34rpx;
  font-weight: 700;
}
.qr-card-note,
.claim-note {
  color: $cm-text-secondary;
  font-size: 24rpx;
}
.qr-box {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 360rpx;
  padding: 30rpx;
  border-radius: 28rpx;
  background: linear-gradient(180deg, #fffdf9 0%, #f8f3e8 100%);
}
.qr-image {
  width: 320rpx;
  height: 320rpx;
}
.qr-empty {
  display: grid;
  gap: 10rpx;
  text-align: center;
}
.qr-empty-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}
.qr-empty-desc {
  color: $cm-text-secondary;
  font-size: 22rpx;
}
.coupon-code {
  color: $cm-text-secondary;
  font-size: 24rpx;
  text-align: center;
  letter-spacing: 1rpx;
}
.claim-body {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16rpx;
}
.claim-stat {
  display: grid;
  gap: 8rpx;
  padding: 22rpx;
  border-radius: 24rpx;
  background: rgba(45, 91, 72, 0.06);
}
.claim-stat-label {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}
.claim-stat-value {
  color: $cm-primary-strong;
  font-size: 30rpx;
  font-weight: 700;
}
.qr-actions,
.claim-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16rpx;
}
.qr-action {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 82rpx;
  border-radius: 999rpx;
  font-size: 26rpx;
}
.qr-action.primary {
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf8;
}
.qr-action.secondary {
  background: rgba(45, 91, 72, 0.08);
  color: $cm-primary;
}
.qr-action.disabled {
  opacity: 0.6;
}
.info-stack,
.rule-stack {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
}
.info-card,
.rule-card {
  display: grid;
  gap: 10rpx;
  padding: 24rpx 26rpx;
}
.info-label {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}
.info-value {
  color: $cm-text-primary;
  font-size: 28rpx;
  line-height: 1.7;
}
.rule-index {
  color: $cm-accent-gold;
  font-size: 22rpx;
}
.rule-text {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}
.goods-grid {
  margin-top: 18rpx;
}
.goods-card {
  display: grid;
  gap: 12rpx;
  padding: 18rpx;
}
.goods-cover {
  height: 210rpx;
  border-radius: 20rpx;
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.12) 0%, rgba(183, 155, 99, 0.18) 100%);
}
.goods-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}
.goods-desc {
  color: $cm-text-secondary;
  font-size: 22rpx;
  line-height: 1.7;
}
.goods-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}
.goods-price {
  color: $cm-primary-strong;
  font-size: 28rpx;
  font-weight: 700;
}
.goods-tag {
  padding: 8rpx 12rpx;
  border-radius: 999rpx;
  background: rgba(183, 155, 99, 0.12);
  color: $cm-accent-gold;
  font-size: 20rpx;
}

.theme-light .detail-hero {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  color: #0f172a;
}

.theme-light .detail-status,
.theme-light .qr-action.secondary,
.theme-light .goods-tag {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-light .claim-stat {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .qr-action.primary {
  background: #111827;
  color: #ffffff;
}

.theme-light .goods-cover {
  background: linear-gradient(180deg, #f8fafc 0%, #eef2f7 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .goods-price,
.theme-light .claim-stat-value {
  color: #111827;
}
</style>
