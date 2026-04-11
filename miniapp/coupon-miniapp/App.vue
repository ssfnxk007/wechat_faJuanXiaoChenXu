<script>
import { getMiniAppSettings } from '@/api/miniapp'
import { hydrateSessionStore } from '@/store/session'
import { setThemeCode } from '@/store/session'

export default {
  onLaunch() {
    hydrateSessionStore()
    getMiniAppSettings()
      .then((settings) => {
        setThemeCode(settings?.themeCode)
      })
      .catch((error) => {
        console.warn('[coupon-miniapp] load theme settings failed', error)
      })
    console.log('Coupon MiniApp Launch')
  }
}
</script>

<style lang="scss">
@import './uni.scss';

page {
  background: $cm-bg-page;
  color: $cm-text-primary;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
}

view,
text,
button,
input,
textarea {
  box-sizing: border-box;
}

button {
  margin: 0;
  padding: 0;
  background: transparent;
  border: none;
  line-height: 1;
}

button::after {
  border: none;
}

.cm-page {
  min-height: 100vh;
  background: linear-gradient(180deg, #f7f2e8 0%, #f4efe5 48%, #f8f6f1 100%);
}

.theme-light.cm-page {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.cm-container {
  padding: 32rpx 28rpx 40rpx;
}

.cm-nav-spacer {
  height: calc(var(--status-bar-height) + 88rpx);
}

.cm-card {
  background: $cm-surface-card;
  border: 1rpx solid $cm-border-soft;
  border-radius: $cm-radius-xl;
  box-shadow: $cm-shadow-soft;
}

.theme-light .cm-card {
  background: #ffffff;
  border-color: rgba(148, 163, 184, 0.24);
  box-shadow: 0 14rpx 36rpx rgba(15, 23, 42, 0.05);
}

.cm-section {
  margin-top: 28rpx;
}

.cm-grid-2 {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 20rpx;
}

.cm-pill {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 44rpx;
  padding: 0 20rpx;
  border-radius: 999rpx;
  background: rgba(61, 95, 79, 0.08);
  color: $cm-primary;
  font-size: 22rpx;
}

.theme-light .cm-pill {
  background: rgba(15, 23, 42, 0.05);
  color: #334155;
}

.theme-light .home-hero,
.theme-light .detail-hero,
.theme-light .pack-hero {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.theme-light .hero-mask,
.theme-light .hero-overlay,
.theme-light .pack-hero-mask,
.theme-light .banner-overlay {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.08) 0%, rgba(248, 250, 252, 0.26) 100%);
}

.theme-light .hero-chip,
.theme-light .detail-status,
.theme-light .pack-status,
.theme-light .profile-badge,
.theme-light .product-tag {
  background: rgba(15, 23, 42, 0.06);
  color: #334155;
}
</style>
