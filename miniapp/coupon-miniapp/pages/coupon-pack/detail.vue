<template>
  <view :class="['cm-page', 'pack-detail-page', themeClass]">
    <view class="pack-hero">
      <view class="pack-hero-bg" :style="heroBackgroundStyle"></view>
      <view class="pack-hero-mask"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container pack-hero-content">
        <view class="pack-topbar">
          <view class="pack-back" @click="goBack">‹ 返回</view>
          <view class="pack-status">{{ saleStatusText }}</view>
        </view>

        <view class="pack-head-copy">
          <text class="pack-eyebrow">COUPON PACK</text>
          <text class="pack-title">{{ packDetail.name }}</text>
          <text class="pack-subtitle">{{ packSubtitle }}</text>
        </view>

        <view class="hero-tags">
          <text class="hero-tag">组合权益</text>
          <text class="hero-tag">支付后自动入包</text>
          <text class="hero-tag">ERP 扫码核销</text>
        </view>
      </view>
    </view>

    <view class="cm-container pack-body">
      <view class="price-card cm-card">
        <view class="price-main">
          <text class="price-label">券包售价</text>
          <view class="price-row">
            <text class="price-unit">¥</text>
            <text class="price-value">{{ packDetail.salePrice }}</text>
          </view>
          <text class="price-note">每人限购 {{ packDetail.perUserLimit || 1 }} 份，支付后自动发放。</text>
        </view>
        <view class="price-badge">精选券包</view>
      </view>

      <view class="pack-overview cm-card">
        <view class="overview-item" v-for="item in overviewStats" :key="item.label">
          <text class="overview-value">{{ item.value }}</text>
          <text class="overview-label">{{ item.label }}</text>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="权益内容" title="券包包含" subtitle="购买后可获得" />
        <view class="benefit-stack">
          <view class="benefit-card cm-card" v-for="item in benefits" :key="item.couponTemplateId || item.id">
            <view class="benefit-top">
              <text class="benefit-type">{{ item.type }}</text>
              <text class="benefit-count">×{{ item.count }}</text>
            </view>
            <text class="benefit-title">{{ item.title }}</text>
            <text class="benefit-desc">{{ item.desc }}</text>
            <text class="benefit-meta">{{ item.meta }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="购买说明" title="购买信息" subtitle="下单前确认" />
        <view class="info-stack">
          <view class="info-card cm-card" v-for="item in purchaseInfo" :key="item.label">
            <text class="info-label">{{ item.label }}</text>
            <text class="info-value">{{ item.value }}</text>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="购买流程" title="下单后会发生什么" subtitle="支付后自动发放" />
        <view class="step-stack">
          <view class="step-card cm-card" v-for="item in steps" :key="item.step">
            <text class="step-index">{{ item.step }}</text>
            <view class="step-copy">
              <text class="step-title">{{ item.title }}</text>
              <text class="step-desc">{{ item.desc }}</text>
            </view>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="使用规则" title="发放与核销规则" subtitle="每张券按各自规则使用" />
        <view class="rule-stack">
          <view class="rule-card cm-card" v-for="(rule, index) in rules" :key="rule">
            <text class="rule-index">0{{ index + 1 }}</text>
            <text class="rule-text">{{ rule }}</text>
          </view>
        </view>
      </view>

      <view class="buy-bar cm-card">
        <view class="buy-left">
          <text class="buy-title">{{ packDetail.name }}</text>
          <text class="buy-summary">共 {{ benefits.length }} 类权益，支付完成后自动进入券包。</text>
        </view>
        <view class="buy-right">
          <text class="buy-price">¥{{ packDetail.salePrice }}</text>
          <view class="buy-button" :class="{ disabled: buying }" @click="handleBuy">
            {{ buyButtonText }}
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
import { ensureMiniProgramLogin, ensurePhoneReady } from '@/api/auth'
import {
  completeMiniAppOrderPayment,
  createMiniAppOrder,
  createMiniAppOrderPayment,
  getMiniAppCouponPackDetail
} from '@/api/miniapp'
import { useSessionStore } from '@/store/session'

const session = useSessionStore()
const { themeClass } = useTheme()
const saleStatusText = computed(() => {
  const now = new Date()
  const start = packDetail.value.saleStartTime ? new Date(packDetail.value.saleStartTime) : null
  const end = packDetail.value.saleEndTime ? new Date(packDetail.value.saleEndTime) : null
  if (start && start > now) {
    return '即将开售'
  }
  if (end && end < now) {
    return '已结束'
  }
  return '热销中'
})

const heroBackgroundStyle = computed(() => {
  const url = packDetail.value.imageUrl || packDetail.value.mainImageUrl || ''
  if (!url) {
    return {}
  }
  return { backgroundImage: `url(${url})` }
})

const buying = ref(false)
const packDetail = ref({
  id: 0,
  name: '春日焕新券包',
  salePrice: '29.9',
  perUserLimit: 1,
  remark: '',
  saleStartTime: '',
  saleEndTime: ''
})

const benefits = ref([
  {
    id: 1,
    type: '无门槛券',
    count: 2,
    title: '门店通用券',
    desc: '消费即可抵扣 5 元，适合日常到店使用',
    meta: '领取后立即生效 · 全部门店可用'
  },
  {
    id: 2,
    type: '满减券',
    count: 3,
    title: '春日满减券',
    desc: '满 100 元减 20 元，适合组合消费场景',
    meta: '领取后 7 天内有效'
  },
  {
    id: 3,
    type: '指定商品券',
    count: 1,
    title: '精品专区券',
    desc: '指定商品立减 30 元，适用于精品专区',
    meta: '按商品范围核销'
  }
])

const scenes = [
  { title: '首购拉新', desc: '适合首次到店。' },
  { title: '门店复购', desc: '适合多次到店使用。' },
  { title: '礼赠带客', desc: '适合作为活动礼赠。' }
]

const steps = [
  { step: '01', title: '完成支付', desc: '确认后完成支付。' },
  { step: '02', title: '自动入包', desc: '自动发放到券包。' },
  { step: '03', title: '到店核销', desc: '到店出示二维码核销。' }
]

const rules = [
  '券包一经支付成功，系统将自动拆分并发放对应卡券，不支持手动拆包。',
  '券包内各张券以单券规则独立计算有效期、门店范围和商品范围。',
  '部分券为指定商品券，下单前请先确认商品是否在可用范围内。',
  '如发生退款，以订单状态和门店规则为准，已核销券不再返还。'
]

const buyButtonText = computed(() => (buying.value ? '正在提交...' : '立即购买'))

const overviewStats = computed(() => {
  const totalCoupons = benefits.value.reduce((sum, item) => sum + Number(item.count || 0), 0)
  const discountTotal = benefits.value.reduce((sum, item) => {
    const matched = String(item.desc || '').match(/减\s*(\d+(?:\.\d+)?)/)
    return sum + Number(matched?.[1] || 0) * Number(item.count || 0)
  }, 0)

  return [
    { label: '总券张数', value: `${totalCoupons || benefits.value.length} 张` },
    { label: '权益价值', value: `¥${discountTotal || packDetail.value.salePrice}` },
    { label: '购买限制', value: `每人 ${packDetail.value.perUserLimit || 1} 份` }
  ]
})

const purchaseInfo = computed(() => ([
  { label: '适用人群', value: session.userId ? '已完成授权用户' : '首次购买将自动完成用户建档' },
  { label: '发放方式', value: '支付成功后自动发放至我的券包' },
  { label: '限购规则', value: `每位用户限购 ${packDetail.value.perUserLimit || 1} 份` },
  { label: '使用方式', value: '到店出示券详情二维码，由 ERP 扫码核销' }
]))

const goBack = () => {
  uni.navigateBack({ delta: 1 })
}

function formatCouponType(templateType) {
  return ({ 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }[templateType] || '优惠券')
}

function navigateToResult(orderId) {
  const suffix = orderId ? `?orderId=${orderId}` : ''
  uni.navigateTo({ url: `/pages/order/result${suffix}` })
}

async function handleBuy() {
  if (buying.value) {
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
      redirect: `/pages/coupon-pack/detail?id=${packDetail.value.id}`
    })
    if (!ready) {
      return
    }

    const order = await createMiniAppOrder({
      couponPackId: packDetail.value.id
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
    console.warn('[coupon-pack-detail] handleBuy failed', error)
    const message = error?.errMsg?.includes('cancel')
      ? '已取消支付'
      : (error?.message || '购买失败，请稍后重试')
    uni.showToast({ title: message, icon: 'none' })
  } finally {
    uni.hideLoading()
    buying.value = false
  }
}

async function loadPackDetail(id) {
  if (!id) {
    return
  }

  try {
    const result = await getMiniAppCouponPackDetail(id)
    if (result) {
      packDetail.value = result
      if (Array.isArray(result.items) && result.items.length) {
        benefits.value = result.items.map((item) => ({
          couponTemplateId: item.couponTemplateId,
          type: formatCouponType(item.templateType),
          count: item.quantity,
          title: item.couponTemplateName,
          desc: item.thresholdAmount ? `满 ${item.thresholdAmount} 元减 ${item.discountAmount || 0} 元` : `立减 ${item.discountAmount || 0} 元`,
          meta: item.isAllStores ? '全门店可用' : '指定门店可用'
        }))
      }
    }
  } catch (error) {
    console.warn('[coupon-pack-detail] loadPackDetail failed', error)
  }
}

onLoad((options) => {
  loadPackDetail(options?.id)
})
</script>

<style lang="scss" scoped>
.pack-detail-page {
  background: linear-gradient(180deg, #f7f2e8 0%, #f3ede2 44%, #f8f6f1 100%);
}
.pack-hero {
  position: relative;
  overflow: hidden;
  min-height: 468rpx;
  background: linear-gradient(135deg, #264337 0%, #4b6650 52%, #aa9664 100%);
  color: #fffaf4;
  border-bottom-left-radius: 42rpx;
  border-bottom-right-radius: 42rpx;
}
.pack-hero-bg,
.pack-hero-mask {
  position: absolute;
  inset: 0;
}
.pack-hero-bg {
  background-size: cover;
  background-position: center;
  opacity: 0.2;
  transform: scale(1.04);
}
.pack-hero-mask {
  background:
    linear-gradient(180deg, rgba(25, 44, 35, 0.08) 0%, rgba(25, 44, 35, 0.55) 100%),
    radial-gradient(circle at top right, rgba(255, 248, 229, 0.22), transparent 28%),
    radial-gradient(circle at left bottom, rgba(255, 255, 255, 0.1), transparent 24%);
}
.pack-hero-content {
  position: relative;
  display: grid;
  gap: 30rpx;
  padding-top: 18rpx;
  padding-bottom: 46rpx;
}
.pack-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.pack-back,
.pack-status {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 56rpx;
  padding: 0 20rpx;
  border-radius: 999rpx;
  background: rgba(255, 255, 255, 0.14);
  font-size: 24rpx;
}
.pack-head-copy {
  display: grid;
  gap: 14rpx;
}
.pack-eyebrow {
  color: #f1e1b8;
  font-size: 22rpx;
  letter-spacing: 4rpx;
}
.pack-title {
  font-size: 54rpx;
  font-weight: 700;
  line-height: 1.2;
}
.pack-subtitle {
  max-width: 640rpx;
  font-size: 25rpx;
  line-height: 1.8;
  opacity: 0.92;
}
.hero-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 14rpx;
}
.hero-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 48rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  background: rgba(255, 255, 255, 0.12);
  font-size: 22rpx;
}
.pack-body {
  margin-top: -26rpx;
  padding-bottom: 32rpx;
}
.price-card {
  position: relative;
  z-index: 2;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24rpx;
  padding: 30rpx;
}
.price-main {
  display: grid;
  gap: 10rpx;
}
.price-label {
  color: $cm-text-secondary;
  font-size: 24rpx;
}
.price-row {
  display: flex;
  align-items: flex-end;
  gap: 8rpx;
}
.price-unit {
  color: $cm-primary-strong;
  font-size: 30rpx;
  font-weight: 700;
  line-height: 1.1;
}
.price-value {
  color: $cm-primary-strong;
  font-size: 54rpx;
  font-weight: 700;
  line-height: 1;
}
.price-note {
  color: $cm-text-secondary;
  font-size: 22rpx;
  line-height: 1.7;
}
.price-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 52rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
  font-size: 22rpx;
}
.pack-overview {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14rpx;
  margin-top: 18rpx;
  padding: 18rpx;
}
.overview-item {
  display: grid;
  gap: 8rpx;
  padding: 18rpx 12rpx;
  border-radius: 24rpx;
  background: rgba(45, 91, 72, 0.05);
  text-align: center;
}
.overview-value {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}
.overview-label {
  color: $cm-text-secondary;
  font-size: 22rpx;
}
.benefit-stack,
.info-stack,
.step-stack,
.rule-stack {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
}
.benefit-card,
.info-card,
.step-card,
.rule-card {
  display: grid;
  gap: 10rpx;
  padding: 24rpx 26rpx;
}
.benefit-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}
.benefit-type,
.rule-index {
  color: $cm-accent-gold;
  font-size: 22rpx;
}
.benefit-count {
  color: $cm-primary;
  font-size: 22rpx;
}
.benefit-title,
.step-title,
.buy-title {
  color: $cm-text-primary;
  font-size: 30rpx;
  font-weight: 700;
}
.benefit-desc,
.benefit-meta,
.info-value,
.step-desc,
.rule-text,
.buy-summary {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}
.info-label {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}
.step-card {
  grid-template-columns: 80rpx 1fr;
  align-items: start;
}
.step-index {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 72rpx;
  height: 72rpx;
  border-radius: 24rpx;
  background: rgba(45, 91, 72, 0.08);
  color: $cm-primary;
  font-size: 24rpx;
  font-weight: 700;
}
.step-copy {
  display: grid;
  gap: 8rpx;
}
.buy-bar {
  position: sticky;
  bottom: 24rpx;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20rpx;
  margin-top: 22rpx;
  padding: 22rpx 24rpx;
  backdrop-filter: blur(18rpx);
}
.buy-left,
.buy-right {
  display: grid;
  gap: 8rpx;
}
.buy-right {
  justify-items: end;
}
.buy-price {
  color: $cm-primary-strong;
  font-size: 38rpx;
  font-weight: 700;
}
.buy-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 220rpx;
  min-height: 84rpx;
  padding: 0 28rpx;
  border-radius: 999rpx;
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf8;
  font-size: 26rpx;
}
.buy-button.disabled {
  opacity: 0.72;
}

.theme-light .pack-hero {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  color: #0f172a;
}

.theme-light .pack-status,
.theme-light .hero-tag,
.theme-light .pack-tag {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-light .overview-item,
.theme-light .step-index {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .buy-price,
.theme-light .benefit-count {
  color: #111827;
}

.theme-light .buy-button {
  background: #111827;
  color: #ffffff;
}

.theme-light .buy-bar {
  background: rgba(255, 255, 255, 0.92);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
  box-shadow: 0 18rpx 44rpx rgba(15, 23, 42, 0.06);
}

.theme-candy.pack-detail-page {
  background: linear-gradient(180deg, #eff6ff 0%, #f8fbff 44%, #ffffff 100%);
}

.theme-candy .pack-hero {
  background: linear-gradient(135deg, #e0e7ff 0%, #dbeafe 52%, #bfdbfe 100%);
  color: #1e3a8a;
}

.theme-candy .pack-hero-mask {
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.08) 0%, rgba(191, 219, 254, 0.28) 100%),
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.88), transparent 30%),
    radial-gradient(circle at left bottom, rgba(219, 234, 254, 0.72), transparent 24%);
}

.theme-candy .pack-eyebrow,
.theme-candy .pack-subtitle {
  color: #3b82f6;
}

.theme-candy .pack-back,
.theme-candy .pack-status,
.theme-candy .hero-tag,
.theme-candy .price-badge {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(191, 219, 254, 0.7);
  color: #2563eb;
}

.theme-candy .price-unit,
.theme-candy .price-value,
.theme-candy .buy-price {
  color: #2563eb;
}

.theme-candy .overview-item,
.theme-candy .step-index {
  background: linear-gradient(180deg, #ffffff 0%, #eff6ff 100%);
  border: 1rpx solid rgba(191, 219, 254, 0.7);
}

.theme-candy .buy-title {
  color: #1e3a8a;
}

.theme-candy .buy-summary {
  color: #64748b;
}

.theme-candy .buy-button {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
  box-shadow: 0 18rpx 36rpx rgba(37, 99, 235, 0.22);
}

.theme-candy .buy-button.disabled {
  box-shadow: none;
}

.theme-candy .buy-bar {
  background: rgba(255, 255, 255, 0.94);
  border: 1rpx solid rgba(191, 219, 254, 0.8);
  box-shadow: 0 18rpx 44rpx rgba(37, 99, 235, 0.08);
}

/* ========== Orange Theme ========== */
.theme-orange.pack-detail-page {
  background: linear-gradient(180deg, #FFFBF5 0%, #f8fbff 44%, #ffffff 100%);
}

.theme-orange .pack-hero {
  background: linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 52%, #FED7AA 100%);
  color: #9A3412;
}

.theme-orange .pack-hero-mask {
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.08) 0%, rgba(254, 215, 170, 0.28) 100%),
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.88), transparent 30%),
    radial-gradient(circle at left bottom, rgba(255, 237, 213, 0.72), transparent 24%);
}

.theme-orange .pack-eyebrow,
.theme-orange .pack-subtitle {
  color: #F97316;
}

.theme-orange .pack-back,
.theme-orange .pack-status,
.theme-orange .hero-tag,
.theme-orange .price-badge {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(254, 215, 170, 0.7);
  color: #EA580C;
}

.theme-orange .price-unit,
.theme-orange .price-value,
.theme-orange .buy-price {
  color: #EA580C;
}

.theme-orange .overview-item,
.theme-orange .step-index {
  background: linear-gradient(180deg, #ffffff 0%, #FFFBF5 100%);
  border: 1rpx solid rgba(254, 215, 170, 0.7);
}

.theme-orange .buy-title {
  color: #9A3412;
}

.theme-orange .buy-summary {
  color: #64748b;
}

.theme-orange .buy-button {
  background: linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  box-shadow: 0 18rpx 36rpx rgba(234, 88, 12, 0.22);
}

.theme-orange .buy-button.disabled {
  box-shadow: none;
}

.theme-orange .buy-bar {
  background: rgba(255, 255, 255, 0.94);
  border: 1rpx solid rgba(254, 215, 170, 0.8);
  box-shadow: 0 18rpx 44rpx rgba(234, 88, 12, 0.08);
}

/* ========== Red Theme ========== */
.theme-red.pack-detail-page {
  background: linear-gradient(180deg, #FFEBEE 0%, #f8fbff 44%, #ffffff 100%);
}

.theme-red .pack-hero {
  background: linear-gradient(135deg, #FFEBEE 0%, #FFCDD2 52%, #FFCDD2 100%);
  color: #B71C1C;
}

.theme-red .pack-hero-mask {
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 0.08) 0%, rgba(255, 205, 210, 0.28) 100%),
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.88), transparent 30%),
    radial-gradient(circle at left bottom, rgba(255, 235, 238, 0.72), transparent 24%);
}

.theme-red .pack-eyebrow,
.theme-red .pack-subtitle {
  color: #EF5350;
}

.theme-red .pack-back,
.theme-red .pack-status,
.theme-red .hero-tag,
.theme-red .price-badge {
  background: rgba(255, 255, 255, 0.78);
  border: 1rpx solid rgba(255, 205, 210, 0.7);
  color: #E53935;
}

.theme-red .price-unit,
.theme-red .price-value,
.theme-red .buy-price {
  color: #E53935;
}

.theme-red .overview-item,
.theme-red .step-index {
  background: linear-gradient(180deg, #ffffff 0%, #FFEBEE 100%);
  border: 1rpx solid rgba(255, 205, 210, 0.7);
}

.theme-red .buy-title {
  color: #B71C1C;
}

.theme-red .buy-summary {
  color: #64748b;
}

.theme-red .buy-button {
  background: linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  box-shadow: 0 18rpx 36rpx rgba(229, 57, 53, 0.22);
}

.theme-red .buy-button.disabled {
  box-shadow: none;
}

.theme-red .buy-bar {
  background: rgba(255, 255, 255, 0.94);
  border: 1rpx solid rgba(255, 205, 210, 0.8);
  box-shadow: 0 18rpx 44rpx rgba(229, 57, 53, 0.08);
}
</style>
