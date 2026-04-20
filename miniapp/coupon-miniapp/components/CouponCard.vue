<template>
  <view class="coupon-card cm-card">
    <view class="coupon-main">
      <text class="coupon-tag">{{ tag }}</text>
      <text class="coupon-title">{{ title }}</text>
      <text class="coupon-desc">{{ desc }}</text>
      <text class="coupon-date">{{ date }}</text>
    </view>
    <view class="coupon-side">
      <text class="coupon-prefix">¥</text>
      <text class="coupon-amount">{{ amount }}</text>
      <text class="coupon-unit">元</text>
      <view class="coupon-button">{{ actionText }}</view>
    </view>
  </view>
</template>

<script setup>
defineProps({
  tag: { type: String, default: '门店通用' },
  title: { type: String, default: '满减券' },
  desc: { type: String, default: '满 100 元减 20 元' },
  date: { type: String, default: '领取后 7 天内有效' },
  amount: { type: [String, Number], default: '20' },
  actionText: { type: String, default: '免费领取' }
})
</script>

<style lang="scss" scoped>
.coupon-card {
  display: grid;
  grid-template-columns: 1fr 184rpx;
  overflow: hidden;
}
.coupon-main {
  display: grid;
  gap: 10rpx;
  padding: 28rpx;
}
.coupon-side {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6rpx;
  background: var(--cm-coupon-side-bg, linear-gradient(180deg, rgba(45, 91, 72, 0.94) 0%, rgba(76, 112, 92, 0.92) 100%));
  color: #fffdf9;
}
.coupon-tag {
  color: $cm-accent-gold;
  font-size: 22rpx;
}
.coupon-title {
  color: $cm-text-primary;
  font-size: 34rpx;
  font-weight: 700;
}
.coupon-desc,
.coupon-date {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.7;
}
.coupon-prefix {
  font-size: 22rpx;
  line-height: 1;
  opacity: 0.88;
}
.coupon-amount {
  font-size: 60rpx;
  font-weight: 700;
  line-height: 1;
}
.coupon-unit {
  font-size: 22rpx;
  opacity: 0.9;
}
.coupon-button {
  margin-top: 10rpx;
  padding: 10rpx 20rpx;
  border: 1rpx solid var(--cm-coupon-button-border, rgba(255, 255, 255, 0.18));
  border-radius: 999rpx;
  background: var(--cm-coupon-button-bg, rgba(255, 255, 255, 0.16));
  color: var(--cm-coupon-button-color, #fffdf9);
  font-size: 22rpx;
  font-weight: 600;
}
</style>
