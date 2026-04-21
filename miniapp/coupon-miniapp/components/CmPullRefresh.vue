<template>
  <scroll-view
    class="cm-pull-refresh"
    scroll-y
    :refresher-enabled="true"
    refresher-default-style="none"
    :refresher-triggered="refreshing"
    :refresher-threshold="80"
    lower-threshold="80"
    :scroll-top="scrollTop"
    @refresherrefresh="handleRefresh"
    @refresherpulling="handlePulling"
    @refresherrestore="handleRestore"
    @scrolltolower="handleScrollToLower"
    @scroll="handleScroll"
  >
    <view slot="refresher" class="cm-pull-refresher">
      <image
        src="/static/logo.png"
        :class="['cm-pull-logo', refreshing ? 'is-spinning' : '']"
        mode="aspectFit"
      />
      <text class="cm-pull-text">{{ pullText }}</text>
    </view>

    <slot />

    <view v-if="showLoadMore" class="cm-pull-footer">
      <template v-if="loadingMore">
        <view class="cm-pull-dot"></view>
        <view class="cm-pull-dot"></view>
        <view class="cm-pull-dot"></view>
        <text class="cm-pull-footer-text">加载中…</text>
      </template>
      <text v-else-if="noMore" class="cm-pull-footer-text cm-pull-footer-text--muted">已到底啦</text>
    </view>
  </scroll-view>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
  refreshing: { type: Boolean, default: false },
  loadingMore: { type: Boolean, default: false },
  noMore: { type: Boolean, default: false },
  showLoadMore: { type: Boolean, default: false },
  scrollTop: { type: Number, default: 0 }
})

const emit = defineEmits(['refresh', 'loadmore', 'scroll'])

const pullDistance = ref(0)
const pullText = ref('下拉刷新')

function handlePulling(e) {
  const dy = e?.detail?.dy ?? 0
  pullDistance.value = dy
  if (props.refreshing) {
    pullText.value = '正在刷新…'
  } else if (dy >= 80) {
    pullText.value = '松手刷新'
  } else {
    pullText.value = '下拉刷新'
  }
}

function handleRefresh() {
  pullText.value = '正在刷新…'
  emit('refresh')
}

function handleRestore() {
  pullText.value = '下拉刷新'
  pullDistance.value = 0
}

function handleScrollToLower() {
  if (props.loadingMore || props.noMore || !props.showLoadMore) return
  emit('loadmore')
}

function handleScroll(e) {
  emit('scroll', e)
}
</script>

<style lang="scss" scoped>
.cm-pull-refresh {
  width: 100%;
  height: 100vh;
  box-sizing: border-box;
}

.cm-pull-refresher {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 120rpx;
  gap: 8rpx;
}

.cm-pull-logo {
  width: 48rpx;
  height: 48rpx;
  transition: transform 0.2s;
}

.cm-pull-logo.is-spinning {
  animation: cm-pull-spin 1s linear infinite;
}

.cm-pull-text {
  font-size: 22rpx;
  color: var(--cm-theme-primary-strong, #23493b);
}

.cm-pull-footer {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12rpx;
  height: 80rpx;
  padding: 0 24rpx;
}

.cm-pull-footer-text {
  font-size: 24rpx;
  color: var(--cm-theme-primary-strong, #23493b);
}

.cm-pull-footer-text--muted {
  color: var(--cm-theme-primary-soft, #6f8a6d);
}

.cm-pull-dot {
  width: 8rpx;
  height: 8rpx;
  border-radius: 50%;
  background: var(--cm-theme-primary-strong, #23493b);
  animation: cm-pull-dot-blink 1.2s infinite;
}

.cm-pull-dot:nth-child(2) { animation-delay: 0.2s; }
.cm-pull-dot:nth-child(3) { animation-delay: 0.4s; }

@keyframes cm-pull-spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

@keyframes cm-pull-dot-blink {
  0%, 80%, 100% { opacity: 0.3; }
  40% { opacity: 1; }
}
</style>
