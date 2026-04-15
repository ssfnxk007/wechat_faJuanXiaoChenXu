<template>
  <view :class="['cm-page', 'cm-container', themeClass]">
    <view class="cm-nav-spacer"></view>
    <SectionHeader eyebrow="MALL" title="券包商城" subtitle="可购券包与商品" />

    <view class="search-card cm-card cm-section">
      <text class="search-placeholder">搜索券包、商品或活动</text>
      <text class="search-action">筛选</text>
    </view>

    <view class="cm-section">
      <SectionHeader title="主推券包" subtitle="在售券包" action-text="更多" @action-click="goPackDetail(packs[0]?.id)" />
      <view class="page-stack">
        <view v-for="item in packs" :key="item.id" @click="goPackDetail(item.id)">
          <CouponPackCard :title="item.title" :subtitle="item.subtitle" :price="item.price" :desc="item.desc" :meta="item.meta" />
        </view>
      </view>
    </view>

    <view class="cm-section">
      <SectionHeader title="精选商品" subtitle="搭配用券更划算" />
      <view class="cm-grid-2 goods-grid">
        <view class="goods-card cm-card" v-for="item in goods" :key="item.id" @click="goProductDetail(item.id)">
          <view class="goods-cover" :style="item.imageUrl ? { backgroundImage: `url(${item.imageUrl})` } : {}"></view>
          <text class="goods-title">{{ item.title }}</text>
          <text class="goods-text">{{ item.desc }}</text>
          <view class="goods-footer">
            <text class="goods-price">¥{{ item.price }}</text>
            <text class="goods-tag">{{ item.tag }}</text>
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
import CouponPackCard from '@/components/CouponPackCard.vue'
import { useTheme } from '@/composables/use-theme'
import { fetchMallPageData } from '@/api/mall'

const packs = ref([])
const goods = ref([])
const { themeClass } = useTheme()

const loadMallData = async () => {
  const result = await fetchMallPageData()
  packs.value = result.packs
  goods.value = result.goods
}

onShow(() => {
  loadMallData()
})

const goPackDetail = (id) => {
  uni.navigateTo({ url: id ? `/pages/coupon-pack/detail?id=${id}` : '/pages/coupon-pack/detail' })
}

const goProductDetail = (id) => {
  uni.navigateTo({ url: id ? `/pages/product/detail?id=${id}` : '/pages/product/detail' })
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
  font-size: 26rpx;
}
.page-stack {
  display: grid;
  gap: 20rpx;
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
  height: 220rpx;
  border-radius: 20rpx;
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.12) 0%, rgba(183, 155, 99, 0.18) 100%);
  background-size: cover;
  background-position: center;
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
.goods-tag {
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

.theme-light .goods-tag {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}
</style>
