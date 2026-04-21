<template>
  <view :class="['sale-card', 'cm-card', themeClass]">
    <view class="sale-card-main">
      <view class="sale-copy">
        <view class="sale-badge-row">
          <text class="sale-badge">{{ subtitle }}</text>
          <text v-if="fulfillmentHint" class="sale-hint">{{ fulfillmentHint }}</text>
        </view>
        <text class="sale-title">{{ title }}</text>
        <text class="sale-desc">{{ desc }}</text>
        <text class="sale-meta">{{ meta }}</text>
      </view>
      <view class="sale-side">
        <text class="sale-price-label">售价</text>
        <text class="sale-price">¥{{ price }}</text>
        <view class="sale-action">去购买</view>
      </view>
    </view>
  </view>
</template>

<script setup>
defineProps({
  title: { type: String, default: '单张售卖券' },
  subtitle: { type: String, default: '单张售卖券' },
  desc: { type: String, default: '支付后自动发放至我的券包。' },
  meta: { type: String, default: '无门槛使用' },
  price: { type: [String, Number], default: '0' },
  fulfillmentHint: { type: String, default: '' },
  themeClass: { type: String, default: '' }
})
</script>

<style lang="scss" scoped>
.sale-card {
  padding: 18rpx 20rpx;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(246, 241, 231, 0.92) 100%);
  border: 1rpx solid rgba(207, 194, 166, 0.52);
}

.sale-card-main,
.sale-badge-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14rpx;
}

.sale-copy {
  flex: 1;
  display: grid;
  gap: 8rpx;
  min-width: 0;
}

.sale-side {
  display: grid;
  justify-items: end;
  gap: 8rpx;
  min-width: 132rpx;
}

.sale-badge,
.sale-hint {
  display: inline-flex;
  align-items: center;
  min-height: 34rpx;
  padding: 0 12rpx;
  border-radius: 999rpx;
  font-size: 18rpx;
}

.sale-badge {
  background: rgba(36, 69, 54, 0.08);
  color: $cm-primary-strong;
}

.sale-hint {
  background: rgba(183, 155, 99, 0.12);
  color: $cm-accent-gold;
}

.sale-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}

.sale-desc,
.sale-meta,
.sale-price-label {
  color: $cm-text-secondary;
  font-size: 20rpx;
  line-height: 1.7;
}

.sale-price {
  color: $cm-primary-strong;
  font-size: 34rpx;
  font-weight: 700;
}

.sale-action {
  min-width: 112rpx;
  padding: 12rpx 16rpx;
  border-radius: 999rpx;
  background: linear-gradient(135deg, #325d49 0%, #5f7453 100%);
  color: #fffdf8;
  text-align: center;
  font-size: 20rpx;
  font-weight: 600;
}

.theme-candy .sale-card,
.theme-candy.sale-card {
  background: linear-gradient(180deg, #ffffff 0%, #eff6ff 100%);
  border-color: rgba(191, 219, 254, 0.85);
  box-shadow: 0 16rpx 40rpx rgba(37, 99, 235, 0.08);
}

.theme-candy .sale-badge,
.theme-candy.sale-card .sale-badge {
  background: rgba(59, 130, 246, 0.08);
  color: #2563eb;
}

.theme-candy .sale-hint,
.theme-candy.sale-card .sale-hint {
  background: rgba(96, 165, 250, 0.12);
  color: #3b82f6;
}

.theme-candy .sale-price,
.theme-candy.sale-card .sale-price {
  color: #2563eb;
}

.theme-candy .sale-action,
.theme-candy.sale-card .sale-action {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
}

.theme-light .sale-card,
.theme-light.sale-card {
  background: #ffffff;
  border-color: rgba(226, 232, 240, 0.9);
}

.theme-light .sale-badge,
.theme-light.sale-card .sale-badge {
  background: rgba(15, 23, 42, 0.06);
  color: #334155;
}

.theme-light .sale-hint,
.theme-light.sale-card .sale-hint {
  background: rgba(15, 23, 42, 0.08);
  color: #475569;
}

.theme-light .sale-price,
.theme-light.sale-card .sale-price {
  color: #111827;
}

.theme-light .sale-action,
.theme-light.sale-card .sale-action {
  background: #111827;
}

.theme-orange .sale-card,
.theme-orange.sale-card {
  background: linear-gradient(180deg, #ffffff 0%, #fffbf5 100%);
  border-color: rgba(254, 215, 170, 0.85);
  box-shadow: 0 16rpx 40rpx rgba(234, 88, 12, 0.08);
}

.theme-orange .sale-badge,
.theme-orange.sale-card .sale-badge {
  background: rgba(249, 115, 22, 0.08);
  color: #ea580c;
}

.theme-orange .sale-hint,
.theme-orange.sale-card .sale-hint {
  background: rgba(251, 146, 60, 0.12);
  color: #f97316;
}

.theme-orange .sale-price,
.theme-orange.sale-card .sale-price {
  color: #ea580c;
}

.theme-orange .sale-action,
.theme-orange.sale-card .sale-action {
  background: linear-gradient(135deg, #f97316 0%, #fb923c 100%);
}

.theme-red .sale-card,
.theme-red.sale-card {
  background: linear-gradient(180deg, #ffffff 0%, #ffebee 100%);
  border-color: rgba(255, 205, 210, 0.85);
  box-shadow: 0 16rpx 40rpx rgba(229, 57, 53, 0.08);
}

.theme-red .sale-badge,
.theme-red.sale-card .sale-badge {
  background: rgba(239, 83, 80, 0.08);
  color: #e53935;
}

.theme-red .sale-hint,
.theme-red.sale-card .sale-hint {
  background: rgba(244, 128, 128, 0.12);
  color: #ef5350;
}

.theme-red .sale-price,
.theme-red.sale-card .sale-price {
  color: #e53935;
}

.theme-red .sale-action,
.theme-red.sale-card .sale-action {
  background: linear-gradient(135deg, #ef5350 0%, #f48080 100%);
}
</style>
