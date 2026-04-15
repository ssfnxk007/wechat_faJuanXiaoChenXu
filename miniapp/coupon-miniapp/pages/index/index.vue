<template>
  <view :class="['cm-page', themeClass]">
    <view class="home-hero">
      <view class="hero-mask"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container hero-content">
        <view class="hero-topbar">
          <view>
            <text class="hero-brand">云锦甄选</text>
            <text class="hero-note">领券、购券、到店核销</text>
          </view>
          <view class="hero-chip">轮播位</view>
        </view>

        <view class="hero-banner">
          <swiper class="banner-swiper" circular autoplay interval="3500" duration="450" indicator-dots indicator-color="rgba(255,255,255,0.3)" indicator-active-color="#fffdf8">
            <swiper-item v-for="item in banners" :key="item.id">
              <view class="banner-card" :style="item.imageUrl ? { backgroundImage: `url(${item.imageUrl})` } : {}" @click="openBannerLink(item)">
                <view class="banner-overlay"></view>
                <view class="banner-copy">
                  <text class="banner-title">{{ item.title }}</text>
                  <text class="banner-subtitle">{{ item.subtitle || '点击可跳转活动' }}</text>
                </view>
              </view>
            </swiper-item>
          </swiper>
        </view>

      </view>
    </view>

    <view class="cm-container home-body">
      <view class="feature-strip cm-card">
        <view class="feature-item" @click="openCouponPreview(newcomerCoupon)">
          <text class="feature-label">新人专享</text>
          <text class="feature-value">新人券仅一次</text>
        </view>
        <view class="feature-item" @click="scrollToFreeCoupons">
          <text class="feature-label">免费领取</text>
          <text class="feature-value">多种优惠券可领</text>
        </view>
        <view class="feature-item" @click="goRules">
          <text class="feature-label">到店核销</text>
          <text class="feature-value">ERP 扫码核销</text>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="WELCOME GIFT" title="新人专享" subtitle="新人仅可领取一次" action-text="免费领取" @action-click="openCouponPreview(newcomerCoupon)" />
        <view class="coupon-stack">
          <view @click="openCouponPreview(newcomerCoupon)">
            <CouponCard
              :tag="newcomerCoupon.tag"
              :title="newcomerCoupon.title"
              :desc="newcomerCoupon.desc"
              :date="newcomerCoupon.date"
              :amount="newcomerCoupon.amount"
              action-text="免费领取"
            />
          </view>
        </view>
      </view>

      <view class="cm-section free-coupon-section">
        <SectionHeader eyebrow="FREE COUPONS" title="免费领取" subtitle="领取后进入券包" action-text="查看全部" @action-click="scrollToFreeCoupons" />
        <view class="direct-coupon-grid">
          <view class="direct-coupon-card cm-card" v-for="item in directCoupons" :key="item.id" @click="openCouponPreview(item)">
            <view class="direct-coupon-top">
              <text class="direct-coupon-type">{{ item.type }}</text>
              <text class="direct-coupon-badge">免费领取</text>
            </view>
            <text class="direct-coupon-title">{{ item.title }}</text>
            <text class="direct-coupon-desc">{{ item.desc }}</text>
            <view class="direct-coupon-footer">
              <text class="direct-coupon-meta">{{ item.meta }}</text>
              <view class="direct-coupon-action">免费领取</view>
            </view>
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="FEATURED PACKS" title="主推券包" subtitle="组合权益" action-text="进入商城" @action-click="goMall" />
        <view class="pack-stack">
          <view v-for="item in featuredPacks" :key="item.id" @click="goPackDetail(item.id)">
            <CouponPackCard :title="item.title" :subtitle="item.subtitle" :price="item.price" :desc="item.desc" :meta="item.meta" />
          </view>
        </view>
      </view>

      <view class="cm-section">
        <SectionHeader eyebrow="SELECTED GOODS" title="推荐商品" subtitle="适合搭配用券" action-text="查看更多" @action-click="goMall" />
        <view class="cm-grid-2 product-grid">
          <view class="product-card cm-card" v-for="item in products" :key="item.id" @click="goProductDetail(item.id)">
            <view class="product-image" :style="item.imageUrl ? { backgroundImage: `url(${item.imageUrl})` } : {}"></view>
            <text class="product-title">{{ item.title }}</text>
            <text class="product-desc">{{ item.desc }}</text>
            <view class="product-footer">
              <text class="product-price">¥{{ item.price }}</text>
              <text class="product-tag">{{ item.tag }}</text>
            </view>
          </view>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import CouponCard from '@/components/CouponCard.vue'
import CouponPackCard from '@/components/CouponPackCard.vue'
import { useTheme } from '@/composables/use-theme'
import { fetchHomePageData } from '@/api/home'
import { setThemeCode } from '@/store/session'

const { themeClass } = useTheme()

const newcomerCoupon = ref({
  id: 0,
  tag: '新人专享',
  title: '新人礼券',
  desc: '立减 10 元，门店通用。',
  date: '领取后 7 天内有效',
  amount: '10',
  type: '新人券',
  meta: '待领取后进入券包'
})
const banners = ref([
  { id: 1, title: '新人专享', subtitle: '点击可查看活动', linkUrl: '/pages/activity/detail?key=newcomer' },
  { id: 2, title: '免费领取', subtitle: '点击可查看活动', linkUrl: '/pages/activity/detail?key=free' },
  { id: 3, title: '到店核销', subtitle: '点击可查看活动', linkUrl: '/pages/activity/detail?key=writeoff' }
])
const directCoupons = ref([])
const featuredPacks = ref([])
const products = ref([])

async function loadHomeData() {
  const result = await fetchHomePageData()
  setThemeCode(result?.theme?.themeCode)
  banners.value = Array.isArray(result.banners) && result.banners.length ? result.banners : banners.value
  newcomerCoupon.value = result.newcomerCoupon || newcomerCoupon.value
  directCoupons.value = Array.isArray(result.directCoupons) ? result.directCoupons : []
  featuredPacks.value = Array.isArray(result.featuredPacks) ? result.featuredPacks : []
  products.value = Array.isArray(result.products) ? result.products : []
}

function openCouponPreview(item = {}) {
  const templateId = item.id || item.couponTemplateId
  if (!templateId) {
    uni.showToast({ title: '券模板不存在', icon: 'none' })
    return
  }
  uni.navigateTo({ url: `/pages/coupon/detail?templateId=${templateId}` })
}

const tabBarPages = new Set([
  '/pages/index/index',
  '/pages/mall/index',
  '/pages/coupon/index',
  '/pages/profile/index'
])

function normalizePageUrl(url = '') {
  if (!url) return ''
  if (/^\//.test(url)) return url
  if (/^pages\//.test(url)) return `/${url}`
  return url
}

function openBannerLink(item = {}) {
  const rawLink = String(item.linkUrl || item.url || item.path || '').trim()
  if (!rawLink) {
    uni.showToast({ title: '暂未配置跳转', icon: 'none' })
    return
  }

  const pageUrl = normalizePageUrl(rawLink)
  if (tabBarPages.has(pageUrl)) {
    uni.switchTab({ url: pageUrl })
    return
  }

  if (/^\/pages\//.test(pageUrl)) {
    uni.navigateTo({ url: pageUrl })
    return
  }

  uni.showToast({ title: '仅支持小程序内部页面跳转', icon: 'none' })
}

function goPackDetail(id) {
  uni.navigateTo({ url: id ? `/pages/coupon-pack/detail?id=${id}` : '/pages/coupon-pack/detail' })
}

function goMall() {
  uni.switchTab({ url: '/pages/mall/index' })
}

function goProductDetail(id) {
  uni.navigateTo({ url: id ? `/pages/product/detail?id=${id}` : '/pages/product/detail' })
}

function scrollToFreeCoupons() {
  uni.pageScrollTo({
    selector: '.free-coupon-section',
    duration: 280
  })
}

function goRules() {
  uni.navigateTo({ url: '/pages/rules/index' })
}

onShow(() => {
  loadHomeData()
})
</script>

<style lang="scss" scoped>
.home-hero {
  position: relative;
  overflow: hidden;
  background: linear-gradient(135deg, #264337 0%, #4b6650 52%, #aa9664 100%);
  color: #fffaf4;
  border-bottom-left-radius: 48rpx;
  border-bottom-right-radius: 48rpx;
}
.hero-mask {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at top right, rgba(255, 248, 229, 0.2), transparent 28%),
    radial-gradient(circle at left center, rgba(255, 255, 255, 0.08), transparent 24%);
}
.hero-content {
  position: relative;
  display: grid;
  gap: 28rpx;
  padding-top: 18rpx;
  padding-bottom: 42rpx;
}
.hero-topbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18rpx;
}
.hero-brand {
  display: block;
  font-size: 34rpx;
  font-weight: 700;
}
.hero-note {
  display: block;
  margin-top: 8rpx;
  font-size: 22rpx;
  opacity: 0.88;
}
.hero-chip {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 48rpx;
  padding: 0 18rpx;
  border-radius: 999rpx;
  background: rgba(255, 255, 255, 0.14);
  font-size: 22rpx;
}
.hero-banner {
  position: relative;
}
.banner-swiper {
  height: 280rpx;
}
.banner-card {
  position: relative;
  overflow: hidden;
  height: 280rpx;
  border-radius: 32rpx;
  background: linear-gradient(135deg, rgba(255,255,255,0.12) 0%, rgba(255,248,229,0.08) 100%);
  background-size: cover;
  background-position: center;
  border: 1rpx solid rgba(255,255,255,0.12);
}
.banner-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(90deg, rgba(28,46,38,0.48) 0%, rgba(28,46,38,0.18) 100%);
}
.banner-copy {
  position: relative;
  z-index: 1;
  display: grid;
  gap: 10rpx;
  padding: 36rpx 32rpx;
}
.banner-title {
  font-size: 48rpx;
  font-weight: 700;
  color: #fffdf8;
}
.banner-subtitle {
  font-size: 24rpx;
  color: rgba(255,253,248,0.88);
}
.hero-copy {
  display: grid;
  gap: 14rpx;
}
.hero-title {
  font-size: 52rpx;
  font-weight: 700;
  line-height: 1.2;
}
.hero-subtitle {
  max-width: 640rpx;
  font-size: 25rpx;
  line-height: 1.8;
  opacity: 0.92;
}
.home-body {
  padding-bottom: 36rpx;
}
.feature-strip {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14rpx;
  margin-top: -28rpx;
  padding: 18rpx;
}
.feature-item {
  display: grid;
  gap: 8rpx;
  padding: 18rpx 14rpx;
  border-radius: 24rpx;
  background: rgba(45, 91, 72, 0.05);
}
.feature-label {
  color: $cm-text-secondary;
  font-size: 22rpx;
}
.feature-value {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
  line-height: 1.5;
}
.coupon-stack,
.pack-stack {
  display: grid;
  gap: 20rpx;
}
.direct-coupon-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18rpx;
}
.direct-coupon-card {
  display: grid;
  gap: 12rpx;
  padding: 20rpx;
  min-height: 220rpx;
}
.direct-coupon-top,
.direct-coupon-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}
.direct-coupon-type {
  color: $cm-accent-gold;
  font-size: 22rpx;
}
.direct-coupon-badge,
.direct-coupon-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 44rpx;
  padding: 0 16rpx;
  border-radius: 999rpx;
  font-size: 20rpx;
}
.direct-coupon-badge {
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
}
.direct-coupon-action {
  background: rgba(45, 91, 72, 0.08);
  color: $cm-primary;
}
.direct-coupon-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
  line-height: 1.45;
}
.direct-coupon-desc,
.direct-coupon-meta {
  color: $cm-text-secondary;
  font-size: 22rpx;
  line-height: 1.7;
}
.product-grid {
  gap: 18rpx;
}
.product-card {
  display: grid;
  gap: 12rpx;
  padding: 18rpx;
}
.product-image {
  height: 220rpx;
  border-radius: 20rpx;
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.12) 0%, rgba(183, 155, 99, 0.18) 100%);
  background-size: cover;
  background-position: center;
}
.product-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}
.product-desc {
  color: $cm-text-secondary;
  font-size: 22rpx;
  line-height: 1.7;
}
.product-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}
.product-price {
  color: $cm-primary-strong;
  font-size: 28rpx;
  font-weight: 700;
}
.product-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 44rpx;
  padding: 0 16rpx;
  border-radius: 999rpx;
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
  font-size: 20rpx;
}

.theme-light .home-hero {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  color: #111827;
  border-bottom-left-radius: 40rpx;
  border-bottom-right-radius: 40rpx;
}

.theme-light .hero-mask {
  background:
    radial-gradient(circle at top right, rgba(241, 245, 249, 0.9), transparent 32%),
    radial-gradient(circle at left center, rgba(226, 232, 240, 0.65), transparent 26%);
}

.theme-light .hero-note,
.theme-light .banner-subtitle {
  color: #64748b;
}

.theme-light .hero-chip {
  background: #ffffff;
  border: 1rpx solid rgba(148, 163, 184, 0.26);
  color: #475569;
}

.theme-light .banner-card {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  border: 1rpx solid rgba(203, 213, 225, 0.85);
  box-shadow: 0 18rpx 44rpx rgba(15, 23, 42, 0.06);
}

.theme-light .banner-overlay {
  background: linear-gradient(90deg, rgba(255, 255, 255, 0.78) 0%, rgba(255, 255, 255, 0.22) 100%);
}

.theme-light .banner-title {
  color: #0f172a;
}

.theme-light .feature-strip {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
  box-shadow: 0 18rpx 44rpx rgba(15, 23, 42, 0.05);
}

.theme-light .feature-item {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .direct-coupon-badge,
.theme-light .product-tag {
  background: rgba(15, 23, 42, 0.05);
  color: #475569;
}

.theme-light .direct-coupon-action {
  background: #111827;
  color: #ffffff;
}

.theme-light .product-image {
  background: linear-gradient(180deg, #f8fafc 0%, #eef2f7 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .product-price {
  color: #111827;
}
</style>
