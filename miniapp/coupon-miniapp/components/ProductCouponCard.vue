<template>
  <view :class="['product-sale-card', 'cm-card', themeClass]">
    <view class="product-sale-top">
      <view class="product-copy">
        <view class="product-tag-row">
          <text class="product-badge">{{ subtitle }}</text>
          <text class="product-summary">{{ productSummary }}</text>
        </view>
        <text class="product-title">{{ title }}</text>
        <text class="product-desc">{{ desc }}</text>
      </view>
      <view class="product-side">
        <text class="product-price-label">售价</text>
        <text class="product-price">¥{{ price }}</text>
      </view>
    </view>
    <view class="product-sale-bottom">
      <text class="product-hint">{{ fulfillmentHint }}</text>
      <view class="product-action">查看详情</view>
    </view>
  </view>
</template>

<script setup>
defineProps({
  title: { type: String, default: '商品券' },
  subtitle: { type: String, default: '商品券' },
  desc: { type: String, default: '支付成功后发放商品券，待后续履约。' },
  price: { type: [String, Number], default: '0' },
  productSummary: { type: String, default: '目标商品' },
  fulfillmentHint: { type: String, default: '支付成功后待 ERP 处理' },
  themeClass: { type: String, default: '' }
})
</script>

<style lang="scss" scoped>
.product-sale-card {
  padding: 18rpx 20rpx;
  background:
    radial-gradient(circle at top right, rgba(183, 155, 99, 0.14), transparent 26%),
    linear-gradient(180deg, rgba(255, 250, 241, 1) 0%, rgba(246, 241, 231, 0.96) 100%);
  border: 1rpx solid rgba(183, 155, 99, 0.34);
}

.product-sale-top,
.product-tag-row,
.product-sale-bottom {
  display: flex;
  justify-content: space-between;
  gap: 14rpx;
}

.product-sale-top {
  align-items: flex-start;
}

.product-copy {
  flex: 1;
  display: grid;
  gap: 8rpx;
  min-width: 0;
}

.product-side {
  display: grid;
  justify-items: end;
  gap: 8rpx;
  min-width: 128rpx;
}

.product-tag-row {
  align-items: center;
  flex-wrap: wrap;
}

.product-badge,
.product-summary {
  display: inline-flex;
  align-items: center;
  min-height: 34rpx;
  padding: 0 12rpx;
  border-radius: 999rpx;
  font-size: 18rpx;
}

.product-badge {
  background: rgba(183, 155, 99, 0.14);
  color: $cm-accent-gold;
}

.product-summary {
  background: rgba(36, 69, 54, 0.08);
  color: $cm-primary-strong;
}

.product-title {
  color: $cm-text-primary;
  font-size: 28rpx;
  font-weight: 700;
}

.product-desc,
.product-price-label,
.product-hint {
  color: $cm-text-secondary;
  font-size: 20rpx;
  line-height: 1.7;
}

.product-price {
  color: $cm-accent-gold;
  font-size: 34rpx;
  font-weight: 700;
}

.product-sale-bottom {
  align-items: center;
  margin-top: 12rpx;
}

.product-hint {
  flex: 1;
}

.product-action {
  min-width: 120rpx;
  padding: 12rpx 16rpx;
  border-radius: 999rpx;
  background: linear-gradient(135deg, #b79b63 0%, #d2b276 100%);
  color: #fffdf8;
  text-align: center;
  font-size: 20rpx;
  font-weight: 600;
}

.theme-candy .product-sale-card,
.theme-candy.product-sale-card {
  background:
    radial-gradient(circle at top right, rgba(96, 165, 250, 0.14), transparent 26%),
    linear-gradient(180deg, #ffffff 0%, #eff6ff 100%);
  border-color: rgba(147, 197, 253, 0.65);
}

.theme-candy .product-badge,
.theme-candy.product-sale-card .product-badge {
  background: rgba(96, 165, 250, 0.12);
  color: #2563eb;
}

.theme-candy .product-summary,
.theme-candy.product-sale-card .product-summary {
  background: rgba(59, 130, 246, 0.08);
  color: #3b82f6;
}

.theme-candy .product-price,
.theme-candy.product-sale-card .product-price {
  color: #2563eb;
}

.theme-candy .product-action,
.theme-candy.product-sale-card .product-action {
  background: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
}

.theme-light .product-sale-card,
.theme-light.product-sale-card {
  background: #ffffff;
  border-color: rgba(226, 232, 240, 0.9);
}

.theme-light .product-badge,
.theme-light.product-sale-card .product-badge {
  background: rgba(15, 23, 42, 0.06);
  color: #475569;
}

.theme-light .product-summary,
.theme-light.product-sale-card .product-summary {
  background: rgba(15, 23, 42, 0.06);
  color: #334155;
}

.theme-light .product-price,
.theme-light.product-sale-card .product-price {
  color: #111827;
}

.theme-light .product-action,
.theme-light.product-sale-card .product-action {
  background: #111827;
}

.theme-orange .product-sale-card,
.theme-orange.product-sale-card {
  background:
    radial-gradient(circle at top right, rgba(251, 146, 60, 0.16), transparent 26%),
    linear-gradient(180deg, #ffffff 0%, #fffbf5 100%);
  border-color: rgba(254, 215, 170, 0.8);
}

.theme-orange .product-badge,
.theme-orange.product-sale-card .product-badge {
  background: rgba(251, 146, 60, 0.14);
  color: #ea580c;
}

.theme-orange .product-summary,
.theme-orange.product-sale-card .product-summary {
  background: rgba(249, 115, 22, 0.08);
  color: #f97316;
}

.theme-orange .product-price,
.theme-orange.product-sale-card .product-price {
  color: #ea580c;
}

.theme-orange .product-action,
.theme-orange.product-sale-card .product-action {
  background: linear-gradient(135deg, #f97316 0%, #fb923c 100%);
}

.theme-red .product-sale-card,
.theme-red.product-sale-card {
  background:
    radial-gradient(circle at top right, rgba(244, 128, 128, 0.16), transparent 26%),
    linear-gradient(180deg, #ffffff 0%, #ffebee 100%);
  border-color: rgba(255, 205, 210, 0.8);
}

.theme-red .product-badge,
.theme-red.product-sale-card .product-badge {
  background: rgba(244, 128, 128, 0.14);
  color: #e53935;
}

.theme-red .product-summary,
.theme-red.product-sale-card .product-summary {
  background: rgba(239, 83, 80, 0.08);
  color: #ef5350;
}

.theme-red .product-price,
.theme-red.product-sale-card .product-price {
  color: #e53935;
}

.theme-red .product-action,
.theme-red.product-sale-card .product-action {
  background: linear-gradient(135deg, #ef5350 0%, #f48080 100%);
}
</style>
