<template>
  <view :class="['cm-page', themeClass]">
    <view class="home-hero">
      <view class="hero-mask"></view>
      <view class="cm-nav-spacer"></view>
      <view class="cm-container hero-content">
        <view class="hero-banner">
          <swiper class="banner-swiper" :circular="banners.length > 1" :autoplay="banners.length > 1" interval="3500" duration="450" @change="onBannerChange">
            <swiper-item v-for="item in banners" :key="item.id">
              <view class="banner-card" @click="openBannerLink(item)">
                <view class="banner-copy">
                  <text class="banner-title">{{ item.title }}</text>
                  <text class="banner-subtitle" v-if="item.subtitle">{{ item.subtitle }}</text>
                  <view class="banner-cta">
                    <text class="banner-cta-text">{{ item.ctaText || '立即抢购' }}</text>
                  </view>
                </view>
                <image v-if="item.illustrationUrl" class="banner-illustration" :src="item.illustrationUrl" mode="aspectFit" />
              </view>
            </swiper-item>
          </swiper>
          <view v-if="banners.length > 1" class="banner-dots">
            <view
              v-for="(item, idx) in banners"
              :key="item.id"
              class="banner-dot"
              :class="{ 'banner-dot--active': idx === currentBanner }"
            ></view>
          </view>
        </view>

      </view>
    </view>

    <view class="cm-container home-body">
      <view class="entry-grid">
        <view class="entry-card entry-card--mall" @click="goMall">
          <view class="entry-copy">
            <text class="entry-title">商城购券</text>
            <text class="entry-subtitle">精选券包 组合优惠</text>
            <view class="entry-go">
              <text class="entry-go-text">GO ›</text>
            </view>
          </view>
          <view class="entry-deco">
            <view class="entry-deco-icon entry-deco-icon--mall"></view>
          </view>
        </view>
        <view class="entry-card entry-card--free" @click="handleFreeCouponEntry">
          <view class="entry-copy">
            <text class="entry-title">免费领券</text>
            <text class="entry-subtitle">新人专享 到店核销</text>
            <view class="entry-go">
              <text class="entry-go-text">GO ›</text>
            </view>
          </view>
          <view class="entry-deco">
            <view class="entry-deco-icon entry-deco-icon--free"></view>
          </view>
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
            <view class="product-image">
              <image v-if="item.imageUrl" class="product-image-img" :src="item.imageUrl" mode="aspectFit" />
            </view>
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
import { ref } from 'vue'
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
  {
    id: 1,
    title: '购买券包',
    subtitle: '更优惠',
    ctaText: '立即抢购',
    illustrationUrl: '',
    linkUrl: '/pages/mall/index'
  },
  {
    id: 2,
    title: '免费领券',
    subtitle: '新人专享',
    ctaText: '立即领取',
    illustrationUrl: '',
    linkUrl: '/pages/activity/detail?key=free'
  }
])
const directCoupons = ref([])
const featuredPacks = ref([])
const products = ref([])
const currentBanner = ref(0)

function onBannerChange(e) {
  currentBanner.value = e?.detail?.current ?? 0
}

async function loadHomeData() {
  const result = await fetchHomePageData()
  setThemeCode(result?.theme?.themeCode)
  const serverBanners = Array.isArray(result.banners) && result.banners.length ? result.banners : []
  if (serverBanners.length) {
    banners.value = serverBanners.map((item, idx) => ({
      id: item.id,
      title: item.title || '',
      subtitle: item.subtitle || '',
      ctaText: item.ctaText || '立即查看',
      illustrationUrl: item.illustrationUrl || '',
      linkUrl: item.linkUrl || ''
    }))
  }
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

function handleFreeCouponEntry() {
  if (newcomerCoupon.value?.id) {
    openCouponPreview(newcomerCoupon.value)
    return
  }
  scrollToFreeCoupons()
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
  gap: 0;
  padding-top: 0;
  padding-bottom: 32rpx;
}
.hero-banner {
  position: relative;
}
.banner-swiper {
  height: 420rpx;
}
.banner-card {
  position: relative;
  overflow: hidden;
  height: 420rpx;
  display: flex;
  align-items: center;
  padding: 40rpx 40rpx;
  background: transparent;
}
.banner-copy {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  gap: 6rpx;
  max-width: 56%;
}
.banner-title {
  font-size: 60rpx;
  font-weight: 800;
  color: #FFFFFF;
  line-height: 1.15;
  letter-spacing: 1rpx;
}
.banner-subtitle {
  margin-top: 8rpx;
  font-size: 42rpx;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.95);
  line-height: 1.2;
}
.banner-cta {
  align-self: flex-start;
  margin-top: 32rpx;
  padding: 14rpx 36rpx;
  border-radius: 999rpx;
  background: #FFFFFF;
  box-shadow: 0 10rpx 22rpx rgba(0, 0, 0, 0.16);
}
.banner-cta-text {
  color: #2d5b48;
  font-size: 28rpx;
  font-weight: 700;
}
.banner-illustration {
  position: absolute;
  right: 12rpx;
  top: 50%;
  transform: translateY(-50%);
  width: 420rpx;
  height: 360rpx;
  pointer-events: none;
  filter: drop-shadow(0 6rpx 16rpx rgba(0, 0, 0, 0.22));
}
.banner-dots {
  display: flex;
  justify-content: center;
  gap: 10rpx;
  margin-top: 14rpx;
  padding-bottom: 4rpx;
}
.banner-dot {
  width: 10rpx;
  height: 10rpx;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.5);
  transition: all 0.3s;
}
.banner-dot--active {
  width: 24rpx;
  border-radius: 5rpx;
  background: #ffffff;
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
.entry-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18rpx;
  margin-top: 32rpx;
}
.entry-card {
  position: relative;
  overflow: hidden;
  height: 200rpx;
  padding: 28rpx 28rpx;
  border-radius: 20rpx;
  box-shadow: 0 10rpx 24rpx rgba(15, 23, 42, 0.06);
  display: flex;
  flex-direction: column;
}
.entry-card--mall {
  background: linear-gradient(135deg, #2d5b48 0%, #3f7a5e 58%, #6ea084 100%);
  color: #fffaf3;
}
.entry-card--free {
  background: linear-gradient(135deg, #b79b63 0%, #d6b879 58%, #f1dfb2 100%);
  color: #3a2d12;
}
.entry-copy {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
}
.entry-title {
  font-size: 34rpx;
  font-weight: 700;
  line-height: 1.3;
}
.entry-subtitle {
  font-size: 22rpx;
  line-height: 1.4;
  opacity: 0.85;
  margin-top: 4rpx;
}
.entry-go {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 2rpx;
  align-self: start;
  margin-top: 18rpx;
  padding: 6rpx 16rpx;
  border-radius: 12rpx;
  background: rgba(255, 255, 255, 0.18);
  backdrop-filter: blur(8rpx);
}
.entry-card--free .entry-go {
  background: rgba(58, 45, 18, 0.12);
  color: #3a2d12;
}
.entry-go-text {
  font-size: 22rpx;
  font-weight: 600;
  line-height: 1.2;
  letter-spacing: 1rpx;
}
.entry-deco {
  position: absolute;
  right: -24rpx;
  bottom: -28rpx;
  z-index: 1;
  opacity: 0.35;
  transform: rotate(-10deg);
}
.entry-deco-icon {
  width: 150rpx;
  height: 150rpx;
  background-size: cover;
  background-repeat: no-repeat;
  background-position: center;
  filter: drop-shadow(0 10rpx 16rpx rgba(0, 0, 0, 0.15));
}
.entry-deco-icon--mall {
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 24 24' fill='white' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M19.16 8h-3.21V6.98c0-2.19-1.78-3.98-3.95-3.98-2.17 0-3.95 1.79-3.95 3.98V8H4.84c-1.12 0-1.95 1.05-1.74 2.15l1.6 8.35A3.94 3.94 0 008.57 22h6.86a3.94 3.94 0 003.87-3.5l1.6-8.35c.21-1.1-.62-2.15-1.74-2.15zM10.15 6.98c0-.98.81-1.8 1.85-1.8.94 0 1.76.73 1.84 1.66l.01.14V8h-3.7V6.98zm-1.18 5.76a1.08 1.08 0 01-1.07-1.09c0-.6.48-1.09 1.07-1.09s1.08.49 1.08 1.09c0 .6-.48 1.09-1.08 1.09zm6.06 0a1.08 1.08 0 01-1.08-1.09c0-.6.48-1.09 1.08-1.09.59 0 1.07.49 1.07 1.09 0 .6-.48 1.09-1.07 1.09z'/%3E%3C/svg%3E");
}
.entry-deco-icon--free {
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 24 24' fill='white' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M21.5 10c-.83 0-1.5-.67-1.5-1.5V6a2 2 0 00-2-2H6a2 2 0 00-2 2v2.5C4 9.33 3.33 10 2.5 10v4c.83 0 1.5.67 1.5 1.5V18a2 2 0 002 2h12a2 2 0 002-2v-2.5c0-.83.67-1.5 1.5-1.5v-4zm-11.5 5H7v-2h3v2zm0-4H7V9h3v2zm5 4h-3v-2h3v2zm0-4h-3V9h3v2z'/%3E%3C/svg%3E");
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
  position: relative;
  height: 220rpx;
  border-radius: 20rpx;
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.12) 0%, rgba(183, 155, 99, 0.18) 100%);
  overflow: hidden;
}
.product-image-img {
  width: 100%;
  height: 100%;
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

.theme-light .banner-subtitle {
  color: #64748b;
}

.theme-light .banner-cta {
  background: linear-gradient(135deg, #1f2937 0%, #334155 100%);
  box-shadow: 0 10rpx 22rpx rgba(15, 23, 42, 0.18);
}

.theme-light .banner-cta-text {
  color: #ffffff;
}

.theme-light .banner-title {
  color: #0f172a;
}

.theme-light .banner-dot {
  background: rgba(15, 23, 42, 0.25);
}

.theme-light .banner-dot--active {
  background: #1f2937;
}

.theme-light .entry-card--mall {
  background: linear-gradient(135deg, #1f2937 0%, #334155 100%);
  color: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .entry-card--free {
  background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
  color: #0f172a;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .entry-card--mall .entry-go {
  background: rgba(255, 255, 255, 0.2);
}

.theme-light .entry-card--free .entry-go {
  background: rgba(15, 23, 42, 0.08);
  color: #0f172a;
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

/* ========== Candy Theme ========== */
.theme-candy .home-hero {
  background: linear-gradient(135deg, #E0E7FF 0%, #DBEAFE 52%, #BFDBFE 100%);
  color: #1E3A8A;
  border-bottom-left-radius: 40rpx;
  border-bottom-right-radius: 40rpx;
}

.theme-candy .hero-mask {
  background:
    radial-gradient(circle at top right, rgba(255, 255, 255, 0.9), transparent 32%),
    radial-gradient(circle at left center, rgba(219, 234, 254, 0.65), transparent 26%);
}

.theme-candy .banner-subtitle {
  color: #3B82F6;
}

.theme-candy .banner-cta {
  background: linear-gradient(135deg, #3B82F6 0%, #2563EB 100%);
  box-shadow: 0 10rpx 22rpx rgba(37, 99, 235, 0.22);
}

.theme-candy .banner-cta-text {
  color: #ffffff;
}

.theme-candy .banner-title {
  color: #1E40AF;
}

.theme-candy .banner-dot {
  background: rgba(37, 99, 235, 0.3);
}

.theme-candy .banner-dot--active {
  background: #2563EB;
}

.theme-candy .entry-card--mall {
  background: linear-gradient(135deg, #3B82F6 0%, #60A5FA 100%);
  color: #ffffff;
  border: 1rpx solid rgba(255, 255, 255, 0.3);
}

.theme-candy .entry-card--free {
  background: linear-gradient(135deg, #0EA5E9 0%, #38BDF8 100%);
  color: #ffffff;
  border: 1rpx solid rgba(255, 255, 255, 0.3);
}

.theme-candy .entry-card--mall .entry-go,
.theme-candy .entry-card--free .entry-go {
  background: rgba(255, 255, 255, 0.18);
  color: #ffffff;
}

.theme-candy .entry-deco {
  opacity: 0.35;
}

.theme-candy .direct-coupon-badge,
.theme-candy .product-tag {
  background: rgba(59, 130, 246, 0.08);
  color: #2563EB;
}

.theme-candy .direct-coupon-action {
  background: linear-gradient(135deg, #3B82F6 0%, #2563EB 100%);
  color: #ffffff;
}

.theme-candy .product-image {
  background: linear-gradient(180deg, #F8FAFC 0%, #DBEAFE 100%);
  border: 1rpx solid rgba(219, 234, 254, 0.9);
}

.theme-candy .product-price {
  color: #2563EB;
}

/* ========== Orange Theme ========== */
.theme-orange .home-hero {
  background:
    radial-gradient(circle at 18% 24%, rgba(255, 255, 255, 0.22) 4rpx, transparent 7rpx),
    radial-gradient(circle at 62% 48%, rgba(255, 255, 255, 0.16) 3rpx, transparent 6rpx),
    radial-gradient(circle at 82% 80%, rgba(255, 255, 255, 0.20) 5rpx, transparent 8rpx),
    radial-gradient(160rpx 100rpx at 88% 14%, rgba(255, 255, 255, 0.42) 0%, rgba(255, 255, 255, 0) 68%),
    radial-gradient(120rpx 80rpx at 8% 72%, rgba(255, 255, 255, 0.32) 0%, rgba(255, 255, 255, 0) 72%),
    linear-gradient(135deg, #F97316 0%, #FB923C 52%, #FDBA74 100%);
  background-size: 220rpx 220rpx, 180rpx 180rpx, 260rpx 260rpx, 100% 100%, 100% 100%, 100% 100%;
  color: #FFFBF5;
  border-bottom-left-radius: 40rpx;
  border-bottom-right-radius: 40rpx;
}

.theme-orange .hero-mask {
  background:
    radial-gradient(circle at top right, rgba(255, 251, 245, 0.18), transparent 34%),
    radial-gradient(circle at left center, rgba(255, 237, 213, 0.14), transparent 28%);
}

.theme-orange .banner-cta-text {
  color: #EA580C;
}

.theme-orange .banner-title {
  color: #FFFBF5;
}

.theme-orange .entry-card--mall {
  background:
    repeating-linear-gradient(135deg, transparent 0 20rpx, rgba(255, 255, 255, 0.06) 20rpx 22rpx),
    linear-gradient(135deg, #F97316 0%, #FB923C 100%);
  color: #FFFBF5;
  border: 1rpx solid rgba(255, 255, 255, 0.30);
}

.theme-orange .entry-card--free {
  background:
    repeating-linear-gradient(135deg, transparent 0 20rpx, rgba(255, 255, 255, 0.06) 20rpx 22rpx),
    linear-gradient(135deg, #EA580C 0%, #F97316 100%);
  color: #FFFBF5;
  border: 1rpx solid rgba(255, 255, 255, 0.30);
}

.theme-orange .entry-card--mall .entry-go,
.theme-orange .entry-card--free .entry-go {
  background: rgba(255, 255, 255, 0.20);
  color: #FFFBF5;
}

.theme-orange .entry-deco {
  opacity: 0.38;
}

.theme-orange .direct-coupon-badge,
.theme-orange .product-tag {
  background: rgba(249, 115, 22, 0.10);
  color: #EA580C;
}

.theme-orange .direct-coupon-action {
  background: linear-gradient(135deg, #F97316 0%, #EA580C 100%);
  color: #FFFBF5;
  box-shadow: 0 10rpx 22rpx rgba(249, 115, 22, 0.24);
}

.theme-orange .product-image {
  background:
    radial-gradient(80rpx 48rpx at 78% 72%, rgba(255, 255, 255, 0.32) 0%, rgba(255, 255, 255, 0) 74%),
    linear-gradient(180deg, #FFF7ED 0%, #FFEDD5 100%);
  border: 1rpx solid rgba(254, 215, 170, 0.9);
}

.theme-orange .product-price {
  color: #EA580C;
}

/* ========== Red Theme ========== */
.theme-red .home-hero {
  background:
    repeating-linear-gradient(135deg, transparent 0 24rpx, rgba(255, 255, 255, 0.08) 24rpx 26rpx),
    radial-gradient(160rpx 100rpx at 88% 14%, rgba(255, 255, 255, 0.40) 0%, rgba(255, 255, 255, 0) 68%),
    radial-gradient(120rpx 80rpx at 8% 72%, rgba(255, 255, 255, 0.28) 0%, rgba(255, 255, 255, 0) 72%),
    linear-gradient(135deg, #EF5350 0%, #F48080 52%, #FDE7E7 100%);
  background-size: 180rpx 180rpx, 100% 100%, 100% 100%, 100% 100%;
  color: #FFFBF5;
  border-bottom-left-radius: 40rpx;
  border-bottom-right-radius: 40rpx;
}

.theme-red .hero-mask {
  background:
    radial-gradient(circle at top right, rgba(255, 251, 245, 0.18), transparent 34%),
    radial-gradient(circle at left center, rgba(255, 235, 238, 0.14), transparent 28%);
}

.theme-red .banner-subtitle {
  color: rgba(255, 251, 245, 0.88);
}

.theme-red .banner-cta-text {
  color: #E53935;
}

.theme-red .banner-title {
  color: #FFFBF5;
}

.theme-red .entry-card--mall {
  background:
    repeating-linear-gradient(135deg, transparent 0 18rpx, rgba(255, 255, 255, 0.08) 18rpx 20rpx),
    linear-gradient(135deg, #EF5350 0%, #F48080 100%);
  color: #FFFBF5;
  border: 1rpx solid rgba(255, 255, 255, 0.30);
}

.theme-red .entry-card--free {
  background:
    repeating-linear-gradient(135deg, transparent 0 18rpx, rgba(255, 255, 255, 0.08) 18rpx 20rpx),
    linear-gradient(135deg, #E53935 0%, #EF5350 100%);
  color: #FFFBF5;
  border: 1rpx solid rgba(255, 255, 255, 0.30);
}

.theme-red .entry-card--mall .entry-go,
.theme-red .entry-card--free .entry-go {
  background: rgba(255, 255, 255, 0.20);
  color: #FFFBF5;
}

.theme-red .entry-deco {
  opacity: 0.38;
}

.theme-red .direct-coupon-badge,
.theme-red .product-tag {
  background: rgba(239, 83, 80, 0.10);
  color: #E53935;
}

.theme-red .direct-coupon-action {
  background: linear-gradient(135deg, #EF5350 0%, #E53935 100%);
  color: #FFFBF5;
  box-shadow: 0 10rpx 22rpx rgba(239, 83, 80, 0.24);
}

.theme-red .product-image {
  background:
    radial-gradient(80rpx 48rpx at 78% 72%, rgba(255, 255, 255, 0.32) 0%, rgba(255, 255, 255, 0) 74%),
    linear-gradient(180deg, #FFF5F5 0%, #FFCDD2 100%);
  border: 1rpx solid rgba(255, 205, 210, 0.9);
}

.theme-red .product-price {
  color: #E53935;
}
</style>
