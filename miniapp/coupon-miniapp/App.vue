<script>
import { getMiniAppSettings } from '@/api/miniapp'
import { ensureMiniProgramLogin, shouldRequirePhone, openPhoneOnboarding } from '@/api/auth'
import { hydrateSessionStore } from '@/store/session'
import { setThemeCode } from '@/store/session'

export default {
  async onLaunch() {
    hydrateSessionStore()
    getMiniAppSettings()
      .then((settings) => {
        setThemeCode(settings?.themeCode)
      })
      .catch((error) => {
        console.warn('[coupon-miniapp] load theme settings failed', error)
      })
    ensureMiniProgramLogin().then(() => {
      if (shouldRequirePhone()) {
        openPhoneOnboarding({ redirect: '/pages/index/index', force: false })
      }
    }).catch((error) => {
      console.warn('[coupon-miniapp] mini login on launch failed', error)
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
  --cm-theme-primary: #2d5b48;
  --cm-theme-primary-strong: #23493b;
  --cm-theme-primary-soft: #6f8a6d;
  --cm-theme-action-bg: rgba(45, 91, 72, 0.08);
  --cm-theme-action-color: #23493b;
  --cm-theme-action-border: rgba(45, 91, 72, 0.12);
  --cm-theme-card-bg: rgba(255, 252, 246, 0.94);
  --cm-theme-card-border: rgba(103, 120, 97, 0.16);
  --cm-theme-card-shadow: 0 20rpx 50rpx rgba(82, 67, 40, 0.08);
  --cm-theme-pill-bg: rgba(61, 95, 79, 0.08);
  --cm-theme-pill-color: #2d5b48;
  --cm-coupon-side-bg: linear-gradient(180deg, rgba(45, 91, 72, 0.94) 0%, rgba(76, 112, 92, 0.92) 100%);
  --cm-coupon-button-bg: rgba(255, 255, 255, 0.16);
  --cm-coupon-button-color: #fffdf9;
  --cm-coupon-button-border: rgba(255, 255, 255, 0.18);
  --cm-pack-cover-bg: linear-gradient(135deg, rgba(44, 74, 58, 0.92) 0%, rgba(95, 116, 83, 0.88) 54%, rgba(191, 167, 117, 0.72) 100%);
  --cm-pack-action-bg: linear-gradient(135deg, #325d49 0%, #5f7453 100%);
  --cm-pack-action-color: #fffdf8;
  --cm-pack-action-shadow: none;
}

.theme-light.cm-page {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  --cm-theme-primary: #334155;
  --cm-theme-primary-strong: #0f172a;
  --cm-theme-primary-soft: #64748b;
  --cm-theme-action-bg: rgba(15, 23, 42, 0.05);
  --cm-theme-action-color: #0f172a;
  --cm-theme-action-border: rgba(148, 163, 184, 0.18);
  --cm-theme-card-bg: #ffffff;
  --cm-theme-card-border: rgba(148, 163, 184, 0.24);
  --cm-theme-card-shadow: 0 14rpx 36rpx rgba(15, 23, 42, 0.05);
  --cm-theme-pill-bg: rgba(15, 23, 42, 0.05);
  --cm-theme-pill-color: #334155;
}

.cm-container {
  padding: 32rpx 28rpx 40rpx;
}

.cm-nav-spacer {
  height: calc(var(--status-bar-height) + 88rpx);
}

.cm-card {
  background: var(--cm-theme-card-bg, $cm-surface-card);
  border: 1rpx solid var(--cm-theme-card-border, $cm-border-soft);
  border-radius: $cm-radius-xl;
  box-shadow: var(--cm-theme-card-shadow, $cm-shadow-soft);
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
  background: var(--cm-theme-pill-bg, rgba(61, 95, 79, 0.08));
  color: var(--cm-theme-pill-color, $cm-primary);
  font-size: 22rpx;
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

/* Candy Theme (方案 A) 全局配置 */
.theme-candy.cm-page {
  background: linear-gradient(180deg, #f0f4ff 0%, #f8fafc 100%);
  --cm-theme-primary: #2563eb;
  --cm-theme-primary-strong: #1d4ed8;
  --cm-theme-primary-soft: #60a5fa;
  --cm-theme-action-bg: rgba(59, 130, 246, 0.12);
  --cm-theme-action-color: #1d4ed8;
  --cm-theme-action-border: rgba(59, 130, 246, 0.18);
  --cm-theme-card-bg: #ffffff;
  --cm-theme-card-border: rgba(59, 130, 246, 0.12);
  --cm-theme-card-shadow: 0 16rpx 40rpx rgba(59, 130, 246, 0.08);
  --cm-theme-pill-bg: rgba(59, 130, 246, 0.1);
  --cm-theme-pill-color: #2563eb;
  --cm-coupon-side-bg: linear-gradient(180deg, rgba(29, 78, 216, 0.96) 0%, rgba(59, 130, 246, 0.94) 100%);
  --cm-coupon-button-bg: rgba(239, 246, 255, 0.16);
  --cm-coupon-button-color: #eff6ff;
  --cm-coupon-button-border: rgba(255, 255, 255, 0.22);
  --cm-pack-cover-bg:
    radial-gradient(150rpx 86rpx at 78% 74%, rgba(255, 255, 255, 0.78) 0%, rgba(255, 255, 255, 0) 72%),
    radial-gradient(120rpx 68rpx at 22% 86%, rgba(255, 255, 255, 0.62) 0%, rgba(255, 255, 255, 0) 72%),
    radial-gradient(170rpx 96rpx at 58% 22%, rgba(255, 255, 255, 0.48) 0%, rgba(255, 255, 255, 0) 74%),
    radial-gradient(90rpx 56rpx at 92% 28%, rgba(255, 255, 255, 0.55) 0%, rgba(255, 255, 255, 0) 74%),
    linear-gradient(180deg, #2563eb 0%, #3b82f6 55%, #60a5fa 100%);
  --cm-pack-action-bg: linear-gradient(135deg, #2563eb 0%, #60a5fa 100%);
  --cm-pack-action-color: #eff6ff;
  --cm-pack-action-shadow: 0 14rpx 32rpx rgba(37, 99, 235, 0.2);
}

.theme-candy .home-hero,
.theme-candy .detail-hero,
.theme-candy .pack-hero {
  background: linear-gradient(180deg, #EFF6FF 0%, #FAFAF9 100%);
}

.theme-candy .hero-mask,
.theme-candy .hero-overlay,
.theme-candy .pack-hero-mask,
.theme-candy .banner-overlay {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.1) 0%, rgba(248, 250, 252, 0.3) 100%);
}

.theme-candy .hero-chip,
.theme-candy .detail-status,
.theme-candy .pack-status,
.theme-candy .profile-badge,
.theme-candy .product-tag {
  background: rgba(59, 130, 246, 0.08);
  color: #2563EB;
}

/* ============================================================ */
/* Orange Theme（活力橙 · 第 4 套主题） ---------------------- */
/* 主色 #F97316 · 纹理：糖果圆点 */
/* ============================================================ */
.theme-orange.cm-page {
  background:
    radial-gradient(circle at 12% 18%, rgba(249, 115, 22, 0.05) 4rpx, transparent 6rpx),
    radial-gradient(circle at 78% 42%, rgba(251, 146, 60, 0.05) 5rpx, transparent 7rpx),
    radial-gradient(circle at 35% 82%, rgba(249, 115, 22, 0.04) 4rpx, transparent 6rpx),
    radial-gradient(circle at 92% 88%, rgba(251, 146, 60, 0.05) 5rpx, transparent 7rpx),
    linear-gradient(180deg, #FFF7ED 0%, #FFFBF5 48%, #FFFFFF 100%);
  background-size: 260rpx 260rpx, 320rpx 320rpx, 300rpx 300rpx, 240rpx 240rpx, 100% 100%;
  --cm-theme-primary: #F97316;
  --cm-theme-primary-strong: #EA580C;
  --cm-theme-primary-soft: #FB923C;
  --cm-theme-action-bg: rgba(249, 115, 22, 0.10);
  --cm-theme-action-color: #EA580C;
  --cm-theme-action-border: rgba(249, 115, 22, 0.18);
  --cm-theme-card-bg: #FFFFFF;
  --cm-theme-card-border: rgba(249, 115, 22, 0.10);
  --cm-theme-card-shadow: 0 16rpx 40rpx rgba(249, 115, 22, 0.08);
  --cm-theme-pill-bg: rgba(249, 115, 22, 0.10);
  --cm-theme-pill-color: #EA580C;
  --cm-coupon-side-bg:
    repeating-linear-gradient(135deg,
      rgba(255, 255, 255, 0.22) 0 4rpx,
      rgba(255, 255, 255, 0) 4rpx 18rpx),
    linear-gradient(180deg, #F97316 0%, #FB923C 100%);
  --cm-coupon-button-bg: rgba(255, 255, 255, 0.22);
  --cm-coupon-button-color: #FFFBF5;
  --cm-coupon-button-border: rgba(255, 255, 255, 0.32);
  --cm-pack-cover-bg:
    repeating-linear-gradient(135deg,
      rgba(255, 255, 255, 0.18) 0 6rpx,
      rgba(255, 255, 255, 0) 6rpx 28rpx),
    repeating-linear-gradient(45deg,
      rgba(255, 255, 255, 0.10) 0 4rpx,
      rgba(255, 255, 255, 0) 4rpx 22rpx),
    linear-gradient(135deg, #F97316 0%, #FB923C 55%, #FDBA74 100%);
  --cm-pack-action-bg: linear-gradient(135deg, #EA580C 0%, #F97316 100%);
  --cm-pack-action-color: #FFFBF5;
  --cm-pack-action-shadow: 0 14rpx 32rpx rgba(249, 115, 22, 0.24);
}

.theme-orange .home-hero,
.theme-orange .detail-hero,
.theme-orange .pack-hero {
  background:
    radial-gradient(160rpx 96rpx at 85% 20%, rgba(251, 146, 60, 0.22) 0%, rgba(255, 255, 255, 0) 72%),
    radial-gradient(120rpx 72rpx at 12% 78%, rgba(253, 186, 116, 0.18) 0%, rgba(255, 255, 255, 0) 74%),
    linear-gradient(180deg, #FFF7ED 0%, #FFFBF5 100%);
}

.theme-orange .hero-mask,
.theme-orange .hero-overlay,
.theme-orange .pack-hero-mask,
.theme-orange .banner-overlay {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.10) 0%, rgba(255, 247, 237, 0.36) 100%);
}

.theme-orange .hero-chip,
.theme-orange .detail-status,
.theme-orange .pack-status,
.theme-orange .profile-badge,
.theme-orange .product-tag {
  background: rgba(249, 115, 22, 0.10);
  color: #EA580C;
}

/* ============================================================ */
/* Red Theme（珊瑚红 · 第 5 套主题） ------------------------- */
/* 主色 #EF5350 · 纹理：中式波纹 + 斜向细纹 */
/* ============================================================ */
.theme-red.cm-page {
  background:
    repeating-linear-gradient(135deg, transparent 0 28rpx, rgba(239, 83, 80, 0.025) 28rpx 30rpx),
    radial-gradient(240rpx 140rpx at 82% 12%, rgba(239, 83, 80, 0.08) 0%, rgba(255, 255, 255, 0) 62%),
    radial-gradient(200rpx 120rpx at 10% 88%, rgba(244, 128, 128, 0.06) 0%, rgba(255, 255, 255, 0) 66%),
    linear-gradient(180deg, #FFF5F5 0%, #FFFBFA 48%, #FFFFFF 100%);
  background-size: 180rpx 180rpx, 100% 100%, 100% 100%, 100% 100%;
  --cm-theme-primary: #EF5350;
  --cm-theme-primary-strong: #E53935;
  --cm-theme-primary-soft: #F48080;
  --cm-theme-action-bg: rgba(239, 83, 80, 0.10);
  --cm-theme-action-color: #E53935;
  --cm-theme-action-border: rgba(239, 83, 80, 0.20);
  --cm-theme-card-bg: #FFFFFF;
  --cm-theme-card-border: rgba(239, 83, 80, 0.12);
  --cm-theme-card-shadow: 0 16rpx 40rpx rgba(239, 83, 80, 0.08);
  --cm-theme-pill-bg: rgba(239, 83, 80, 0.10);
  --cm-theme-pill-color: #E53935;
  --cm-coupon-side-bg:
    repeating-linear-gradient(135deg, transparent 0 18rpx, rgba(255, 255, 255, 0.08) 18rpx 20rpx),
    radial-gradient(120rpx 72rpx at 82% 72%, rgba(255, 255, 255, 0.36) 0%, rgba(255, 255, 255, 0) 72%),
    linear-gradient(180deg, #EF5350 0%, #F48080 100%);
  --cm-coupon-button-bg: rgba(255, 255, 255, 0.22);
  --cm-coupon-button-color: #FFFBF5;
  --cm-coupon-button-border: rgba(255, 255, 255, 0.32);
  --cm-pack-cover-bg:
    repeating-linear-gradient(135deg, transparent 0 28rpx, rgba(255, 255, 255, 0.06) 28rpx 30rpx),
    radial-gradient(180rpx 96rpx at 78% 74%, rgba(255, 255, 255, 0.42) 0%, rgba(255, 255, 255, 0) 72%),
    radial-gradient(140rpx 76rpx at 22% 86%, rgba(255, 255, 255, 0.32) 0%, rgba(255, 255, 255, 0) 72%),
    linear-gradient(135deg, #EF5350 0%, #F48080 55%, #FDE7E7 100%);
  --cm-pack-action-bg: linear-gradient(135deg, #E53935 0%, #EF5350 100%);
  --cm-pack-action-color: #FFFBF5;
  --cm-pack-action-shadow: 0 14rpx 32rpx rgba(239, 83, 80, 0.26);
}

.theme-red .home-hero,
.theme-red .detail-hero,
.theme-red .pack-hero {
  background:
    radial-gradient(160rpx 96rpx at 85% 20%, rgba(244, 128, 128, 0.20) 0%, rgba(255, 255, 255, 0) 70%),
    radial-gradient(120rpx 72rpx at 12% 78%, rgba(253, 231, 231, 0.45) 0%, rgba(255, 255, 255, 0) 74%),
    linear-gradient(180deg, #FFF5F5 0%, #FFFBFA 100%);
}

.theme-red .hero-mask,
.theme-red .hero-overlay,
.theme-red .pack-hero-mask,
.theme-red .banner-overlay {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.10) 0%, rgba(255, 245, 245, 0.38) 100%);
}

.theme-red .hero-chip,
.theme-red .detail-status,
.theme-red .pack-status,
.theme-red .profile-badge,
.theme-red .product-tag {
  background: rgba(239, 83, 80, 0.10);
  color: #E53935;
}

</style>
