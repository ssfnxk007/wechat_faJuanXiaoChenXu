<template>
  <view :class="['cm-page', themeClass]">
    <CmPullRefresh :refreshing="refreshing" @refresh="handleRefresh">
      <view class="cm-container">
        <view class="cm-nav-spacer"></view>
        <SectionHeader eyebrow="MALL" title="券包商城" subtitle="券包、单张券、商品券与商品一页浏览" />

        <view class="search-card cm-card cm-section">
          <text class="search-placeholder">搜索券包、商品或活动</text>
          <text class="search-action">上拉查看更多商品</text>
        </view>

        <view class="cm-section">
          <SectionHeader title="主推券包" subtitle="在售券包" action-text="更多" @action-click="goPackDetail(packs[0]?.id)" />
          <view class="page-stack">
            <view v-for="item in packs" :key="item.id" @click="goPackDetail(item.id)">
              <CouponPackCard :title="item.title" :subtitle="item.subtitle" :price="item.price" :desc="item.desc" :meta="item.meta" />
            </view>
          </view>
        </view>

        <view class="cm-section" v-if="standaloneCoupons.length">
          <SectionHeader title="单张售卖券" subtitle="轻量购买，支付后立即发券" />
          <view class="coupon-stack">
            <view v-for="item in standaloneCoupons" :key="item.id" @click="goSaleCouponDetail(item.id)">
              <StandaloneCouponCard
                :title="item.title"
                :subtitle="item.subtitle"
                :price="item.price"
                :desc="item.desc"
                :meta="item.meta"
                :fulfillment-hint="item.fulfillmentHint"
                :theme-class="themeClass"
              />
            </view>
          </view>
        </view>

        <view class="cm-section" v-if="productCoupons.length">
          <SectionHeader title="商品券专区" subtitle="先支付发券，当前阶段显示待履约" />
          <view class="coupon-stack">
            <view v-for="item in productCoupons" :key="item.id" @click="goSaleCouponDetail(item.id)">
              <ProductCouponCard
                :title="item.title"
                :subtitle="item.subtitle"
                :price="item.price"
                :desc="item.desc"
                :product-summary="item.productSummary || item.meta"
                :fulfillment-hint="item.fulfillmentHint"
                :theme-class="themeClass"
              />
            </view>
          </view>
        </view>

        <view class="cm-section">
          <SectionHeader title="精选商品" subtitle="搭配用券更划算" />
          <view class="cm-grid-2 goods-grid">
            <view class="goods-card cm-card" v-for="item in goods" :key="item.id" @click="goProductDetail(item.id)">
              <view class="goods-cover">
                <image v-if="item.imageUrl" class="goods-cover-img" :src="item.imageUrl" mode="aspectFit" />
              </view>
              <text class="goods-title">{{ item.title }}</text>
              <text v-if="item.desc" class="goods-text">{{ item.desc }}</text>
              <view class="goods-footer">
                <text class="goods-price">¥{{ item.price }}</text>
                <text v-if="item.barcodeText" class="goods-barcode">{{ item.barcodeText }}</text>
              </view>
            </view>
          </view>
          <view v-if="goods.length" class="goods-loading-row">
            <text v-if="goodsLoadingMore" class="goods-loading-text">正在加载更多商品...</text>
            <text v-else-if="goodsFinished" class="goods-loading-text">已经到底了</text>
            <text v-else class="goods-loading-text">上拉继续加载更多商品</text>
          </view>
        </view>
      </view>
    </CmPullRefresh>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onShow, onReachBottom } from '@dcloudio/uni-app'
import SectionHeader from '@/components/SectionHeader.vue'
import CouponPackCard from '@/components/CouponPackCard.vue'
import StandaloneCouponCard from '@/components/StandaloneCouponCard.vue'
import ProductCouponCard from '@/components/ProductCouponCard.vue'
import { useTheme } from '@/composables/use-theme'
import { fetchMallPageData, fetchMiniAppProductList } from '@/api/mall'
import CmPullRefresh from '@/components/CmPullRefresh.vue'

const packs = ref([])
const standaloneCoupons = ref([])
const productCoupons = ref([])
const goods = ref([])
const { themeClass } = useTheme()

const refreshing = ref(false)
const goodsLoadingMore = ref(false)
const goodsPageIndex = ref(1)
const goodsPageSize = ref(8)
const goodsTotalPages = ref(1)
const goodsFinished = ref(false)

async function loadGoods(reset = false) {
  if (goodsLoadingMore.value) return
  if (!reset && goodsFinished.value) return

  goodsLoadingMore.value = true
  try {
    const targetPage = reset ? 1 : goodsPageIndex.value + 1
    const result = await fetchMiniAppProductList({
      pageIndex: targetPage,
      pageSize: goodsPageSize.value
    })

    goodsPageIndex.value = result.pageIndex
    goodsPageSize.value = result.pageSize
    goodsTotalPages.value = result.totalPages || 1
    goodsFinished.value = goodsPageIndex.value >= goodsTotalPages.value
    goods.value = reset ? result.items : goods.value.concat(result.items)
  } finally {
    goodsLoadingMore.value = false
  }
}

async function loadMallData() {
  const result = await fetchMallPageData()
  packs.value = result.packs
  standaloneCoupons.value = result.standaloneCoupons
  productCoupons.value = result.productCoupons
  await loadGoods(true)
}

async function handleRefresh() {
  if (refreshing.value) return
  refreshing.value = true
  try {
    await loadMallData()
  } catch (error) {
    console.warn('[mall] refresh failed', error)
    uni.showToast({ title: error?.message || '加载失败', icon: 'none' })
  } finally {
    refreshing.value = false
  }
}

onShow(() => {
  loadMallData()
})

onReachBottom(() => {
  loadGoods(false)
})

const goPackDetail = (id) => {
  uni.navigateTo({ url: id ? `/pages/coupon-pack/detail?id=${id}` : '/pages/coupon-pack/detail' })
}

const goProductDetail = (id) => {
  uni.navigateTo({ url: id ? `/pages/product/detail?id=${id}` : '/pages/product/detail' })
}

const goSaleCouponDetail = (id) => {
  uni.navigateTo({ url: id ? `/pages/sale-coupon/detail?id=${id}` : '/pages/sale-coupon/detail' })
}
</script>

<style lang="scss" scoped>
.search-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20rpx;
  padding: 26rpx 28rpx;
}

.search-placeholder {
  color: $cm-text-tertiary;
  font-size: 26rpx;
}

.search-action {
  color: $cm-primary;
  font-size: 24rpx;
}

.page-stack {
  display: grid;
  gap: 20rpx;
  margin-top: 18rpx;
}

.coupon-stack {
  display: grid;
  gap: 18rpx;
  margin-top: 18rpx;
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
  position: relative;
  height: 220rpx;
  border-radius: 20rpx;
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.12) 0%, rgba(183, 155, 99, 0.18) 100%);
  overflow: hidden;
}

.goods-cover-img {
  width: 100%;
  height: 100%;
}

.goods-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}

.goods-text {
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

.goods-barcode {
  color: #f97316;
  font-size: 20rpx;
  font-weight: 700;
}

.goods-loading-row {
  display: flex;
  justify-content: center;
  padding: 24rpx 0 8rpx;
}

.goods-loading-text {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}

.theme-candy .search-card,
.theme-candy .goods-card {
  background: linear-gradient(180deg, #ffffff 0%, #eff6ff 100%);
  border: 1rpx solid rgba(191, 219, 254, 0.85);
  box-shadow: 0 16rpx 40rpx rgba(37, 99, 235, 0.08);
}

.theme-candy .search-placeholder,
.theme-candy .goods-text,
.theme-candy .goods-loading-text {
  color: #64748b;
}

.theme-candy .search-action {
  color: #2563eb;
  font-weight: 700;
}

.theme-candy .goods-cover {
  background: linear-gradient(180deg, #f8fafc 0%, #dbeafe 100%);
  border: 1rpx solid rgba(219, 234, 254, 0.9);
}

.theme-candy .goods-price {
  color: #2563eb;
}

.theme-candy .goods-barcode {
  color: #f97316;
}

.theme-light .search-card {
  background: #ffffff;
  border: 1rpx solid rgba(226, 232, 240, 0.9);
  box-shadow: 0 14rpx 36rpx rgba(15, 23, 42, 0.05);
}

.theme-light .search-action {
  color: #475569;
}

.theme-light .goods-cover {
  background: linear-gradient(180deg, #f8fafc 0%, #eef2f7 100%);
  border: 1rpx solid rgba(226, 232, 240, 0.9);
}

.theme-light .goods-price {
  color: #111827;
}

.theme-light .goods-barcode {
  color: #ea580c;
}

.theme-light .goods-loading-text {
  color: #64748b;
}

.theme-orange .search-card,
.theme-orange .goods-card {
  background: linear-gradient(180deg, #ffffff 0%, #FFFBF5 100%);
  border: 1rpx solid rgba(254, 215, 170, 0.85);
  box-shadow: 0 16rpx 40rpx rgba(234, 88, 12, 0.08);
}

.theme-orange .search-placeholder,
.theme-orange .goods-text,
.theme-orange .goods-loading-text {
  color: #64748b;
}

.theme-orange .search-action {
  color: #EA580C;
  font-weight: 700;
}

.theme-orange .goods-cover {
  background: linear-gradient(180deg, #FFFBF5 0%, #FFEDD5 100%);
  border: 1rpx solid rgba(255, 237, 213, 0.9);
}

.theme-orange .goods-price {
  color: #EA580C;
}

.theme-orange .goods-barcode {
  color: #FB923C;
}

.theme-red .search-card,
.theme-red .goods-card {
  background: linear-gradient(180deg, #ffffff 0%, #FFEBEE 100%);
  border: 1rpx solid rgba(255, 205, 210, 0.85);
  box-shadow: 0 16rpx 40rpx rgba(229, 57, 53, 0.08);
}

.theme-red .search-placeholder,
.theme-red .goods-text,
.theme-red .goods-loading-text {
  color: #64748b;
}

.theme-red .search-action {
  color: #E53935;
  font-weight: 700;
}

.theme-red .goods-cover {
  background: linear-gradient(180deg, #FFFBFA 0%, #FFCDD2 100%);
  border: 1rpx solid rgba(255, 235, 238, 0.9);
}

.theme-red .goods-price {
  color: #E53935;
}

.theme-red .goods-barcode {
  color: #FB7185;
}
</style>
