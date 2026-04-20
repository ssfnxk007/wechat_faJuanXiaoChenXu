<template>
  <view :class="['cm-page', 'phone-onboarding-page', themeClass]">
    <view class="cm-nav-spacer"></view>
    <view class="cm-container phone-onboarding-body">
      <view class="phone-onboarding-card cm-card">
        <text class="phone-onboarding-eyebrow">WELCOME</text>
        <text class="phone-onboarding-title">先完成手机号授权</text>
        <text class="phone-onboarding-desc">
          首次进入时尽量完成手机号绑定，方便后续按手机号查找、领券归属、下单支付与售后处理。
        </text>

        <view class="phone-onboarding-points">
          <view class="point-item">
            <text class="point-dot"></text>
            <text class="point-text">使用微信官方手机号能力，用户确认后自动绑定。</text>
          </view>
          <view class="point-item">
            <text class="point-dot"></text>
            <text class="point-text">拒绝后仍可先浏览，但领券、下单、支付前会再次要求补齐。</text>
          </view>
        </view>

        <button
          class="phone-authorize-button"
          open-type="getPhoneNumber"
          :disabled="submitting"
          @getphonenumber="handleGetPhoneNumber"
        >
          {{ submitting ? '绑定中...' : '微信授权手机号' }}
        </button>

        <view class="phone-secondary-actions">
          <view class="secondary-link" @click="handleSkip">先逛逛</view>
          <view class="secondary-tip">后续关键操作前仍需补齐手机号</view>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup>
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { exchangePhoneNumber } from '@/api/auth'
import { useTheme } from '@/composables/use-theme'
import { markPhoneOnboardingSkipped } from '@/store/session'

const { themeClass } = useTheme()
const submitting = ref(false)
const redirect = ref('')
const force = ref(false)

function resolveBack() {
  if (getCurrentPages().length > 1) {
    uni.navigateBack({ delta: 1 })
    return
  }

  if (redirect.value) {
    const decoded = decodeURIComponent(redirect.value)
    if (/^\/pages\//.test(decoded)) {
      uni.reLaunch({ url: decoded })
      return
    }
  }

  uni.switchTab({ url: '/pages/index/index' })
}

async function handleGetPhoneNumber(event) {
  const code = event?.detail?.code
  if (!code) {
    if (event?.detail?.errMsg?.includes('fail')) {
      uni.showToast({ title: '你已取消手机号授权', icon: 'none' })
    }
    return
  }

  try {
    submitting.value = true
    await exchangePhoneNumber(code)
    uni.showToast({ title: '手机号绑定成功', icon: 'success' })
    resolveBack()
  } catch (error) {
    uni.showToast({ title: error?.message || '手机号绑定失败', icon: 'none' })
  } finally {
    submitting.value = false
  }
}

function handleSkip() {
  markPhoneOnboardingSkipped(true)
  if (force.value) {
    uni.showToast({ title: '后续关键操作前仍需授权手机号', icon: 'none' })
  }
  resolveBack()
}

onLoad((options) => {
  redirect.value = String(options?.redirect || '')
  force.value = String(options?.force || '0') === '1'
})
</script>

<style lang="scss" scoped>
.phone-onboarding-page {
  min-height: 100vh;
}

.phone-onboarding-body {
  display: flex;
  align-items: center;
  min-height: calc(100vh - var(--status-bar-height) - 88rpx);
}

.phone-onboarding-card {
  display: grid;
  gap: 24rpx;
  width: 100%;
  padding: 36rpx 32rpx;
}

.phone-onboarding-eyebrow {
  color: $cm-accent-gold;
  font-size: 22rpx;
  letter-spacing: 4rpx;
}

.phone-onboarding-title {
  color: $cm-text-primary;
  font-size: 42rpx;
  font-weight: 700;
}

.phone-onboarding-desc {
  color: $cm-text-secondary;
  font-size: 26rpx;
  line-height: 1.8;
}

.phone-onboarding-points {
  display: grid;
  gap: 16rpx;
}

.point-item {
  display: grid;
  grid-template-columns: 18rpx 1fr;
  gap: 14rpx;
  align-items: start;
}

.point-dot {
  width: 18rpx;
  height: 18rpx;
  margin-top: 10rpx;
  border-radius: 50%;
  background: $cm-primary;
}

.point-text {
  color: $cm-text-secondary;
  font-size: 24rpx;
  line-height: 1.8;
}

.phone-authorize-button {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 92rpx;
  border-radius: 999rpx;
  background: linear-gradient(135deg, #2d5b48 0%, #5f7453 100%);
  color: #fffdf8;
  font-size: 28rpx;
  font-weight: 700;
}

.phone-secondary-actions {
  display: grid;
  gap: 10rpx;
  justify-items: center;
}

.secondary-link {
  color: $cm-primary;
  font-size: 24rpx;
}

.secondary-tip {
  color: $cm-text-tertiary;
  font-size: 22rpx;
}
</style>
