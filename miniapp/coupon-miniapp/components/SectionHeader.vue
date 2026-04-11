<template>
  <view class="section-header">
    <view>
      <text class="eyebrow" v-if="eyebrow">{{ eyebrow }}</text>
      <view class="title-row">
        <text class="title">{{ title }}</text>
        <text class="subtitle" v-if="subtitle">{{ subtitle }}</text>
      </view>
    </view>
    <text class="action" v-if="actionText" @click="emit('action-click')">{{ actionText }}</text>
  </view>
</template>

<script setup>
const emit = defineEmits(['action-click'])

defineProps({
  eyebrow: { type: String, default: '' },
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  actionText: { type: String, default: '' }
})
</script>

<style lang="scss" scoped>
.section-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20rpx;
}
.eyebrow {
  display: inline-block;
  margin-bottom: 10rpx;
  color: $cm-accent-gold;
  font-size: 22rpx;
  letter-spacing: 2rpx;
}
.title-row {
  display: flex;
  flex-direction: column;
  gap: 8rpx;
}
.title {
  color: $cm-text-primary;
  font-size: 38rpx;
  font-weight: 700;
}
.subtitle {
  color: $cm-text-secondary;
  font-size: 24rpx;
}
.action {
  color: $cm-primary;
  font-size: 24rpx;
}
</style>
