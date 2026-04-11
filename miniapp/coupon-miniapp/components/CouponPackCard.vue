<template>
  <view class="pack-card cm-card">
    <view class="pack-cover">
      <text class="pack-badge">主推券包</text>
      <text class="pack-cover-title">{{ title }}</text>
      <text class="pack-cover-subtitle">{{ subtitle }}</text>
    </view>
    <view class="pack-body">
      <text class="pack-price">¥{{ price }}</text>
      <text class="pack-desc">{{ desc }}</text>
      <view class="pack-footer">
        <text class="pack-meta">{{ meta }}</text>
        <view class="pack-action">立即购买</view>
      </view>
    </view>
  </view>
</template>

<script setup>
defineProps({
  title: { type: String, default: '春日焕新券包' },
  subtitle: { type: String, default: '组合权益 · 门店可核销' },
  price: { type: [String, Number], default: '29.9' },
  desc: { type: String, default: '含 2 张无门槛券 + 3 张满减券 + 1 张指定商品券' },
  meta: { type: String, default: '限购 1 份' }
})
</script>

<style lang="scss" scoped>
.pack-card {
  overflow: hidden;
}
.pack-cover {
  display: grid;
  gap: 10rpx;
  min-height: 240rpx;
  padding: 28rpx;
  background: linear-gradient(135deg, rgba(44, 74, 58, 0.92) 0%, rgba(95, 116, 83, 0.88) 54%, rgba(191, 167, 117, 0.72) 100%);
  color: #fffaf3;
}
.pack-badge {
  width: fit-content;
  padding: 8rpx 18rpx;
  border-radius: 999rpx;
  background: rgba(255, 255, 255, 0.12);
  font-size: 20rpx;
}
.pack-cover-title {
  font-size: 38rpx;
  font-weight: 700;
}
.pack-cover-subtitle {
  font-size: 24rpx;
  opacity: 0.88;
}
.pack-body {
  display: grid;
  gap: 14rpx;
  padding: 24rpx 28rpx 28rpx;
}
.pack-price {
  color: $cm-primary-strong;
  font-size: 44rpx;
  font-weight: 700;
}
.pack-desc,
.pack-meta {
  color: $cm-text-secondary;
  font-size: 24rpx;
}
.pack-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
}
.pack-action {
  min-width: 152rpx;
  padding: 18rpx 24rpx;
  border-radius: 999rpx;
  background: linear-gradient(135deg, #325d49 0%, #5f7453 100%);
  color: #fffdf8;
  text-align: center;
  font-size: 24rpx;
}
</style>
