<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>小程序主题</h2>
        <p>统一配置小程序当前启用的视觉主题，保存后首页与业务页面会按主题切换展示。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" :disabled="loading || submitting" @click="loadData">刷新配置</button>
        <button v-if="canManage" type="button" class="primary-button" :disabled="loading || submitting" @click="submit">{{ submitting ? '保存中...' : '保存主题' }}</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">当前主题</span>
        <strong class="stat-value">{{ activeThemeLabel }}</strong>
        <span class="stat-footnote">小程序首页、券详情、券包页等统一跟随该主题展示</span>
      </article>
      <article class="stat-card">
        <span class="label">可用方案</span>
        <strong class="stat-value">5 套</strong>
        <span class="stat-footnote">蓝色风 / 新中式绿意 / 极简纯白 / 活力橙 / 珊瑚红，按业务氛围任选</span>
      </article>
      <article class="stat-card">
        <span class="label">切换方式</span>
        <strong class="stat-value">后台配置</strong>
        <span class="stat-footnote">无需改小程序代码，保存后重新进入即可查看最新主题</span>
      </article>
    </div>

    <div class="data-card option-panel">
      <div class="section-head">
        <div>
          <h3>主题选择</h3>
          <p>建议保存后重点检查首页、券包详情、我的券包与个人中心的视觉一致性。</p>
        </div>
      </div>

      <div class="theme-grid">
        <label v-for="item in themeOptions" :key="item.code" class="theme-option" :class="{ active: form.themeCode === item.code }">
          <input v-model="form.themeCode" type="radio" name="themeCode" :value="item.code" :disabled="!canManage || submitting" />
          <div class="theme-preview" :class="`theme-preview-${item.code}`">
            <div class="theme-preview-top"></div>
            <div class="theme-preview-banner"></div>
            <div class="theme-preview-cards">
              <span></span>
              <span></span>
              <span></span>
            </div>
            <div class="theme-preview-list">
              <span></span>
              <span></span>
            </div>
          </div>
          <div class="theme-copy">
            <strong>{{ item.label }}</strong>
            <p>{{ item.description }}</p>
          </div>
        </label>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { getMiniAppThemeSettings, updateMiniAppThemeSettings } from '@/api/miniapp-setting'
import { notify } from '@/utils/notify'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'

type ThemeCode = 'green' | 'light' | 'candy' | 'orange' | 'red'

const canManage = authStorage.hasPermission('miniapp.theme.manage')
const loading = ref(false)
const submitting = ref(false)
const form = reactive<{ themeCode: ThemeCode }>({ themeCode: 'candy' })

const themeOptions: Array<{ code: ThemeCode; label: string; description: string }> = [
  { code: 'candy', label: '蓝色风', description: '以亮蓝色为主色调，卡片干净、层次分明，突出运营按钮与关键数据，适合主推活动与券包。' },
  { code: 'orange', label: '活力橙', description: '暖米白底 + 活力橙 + 糖果圆点纹理，视觉热闹、情绪高昂，适合电商、本地生活、运动健身类。' },
  { code: 'red', label: '珊瑚红', description: '浅粉米底 + 珊瑚红 + 中式斜纹 + 柔光波纹，节日氛围浓但不刺眼，适合餐饮、礼赠、节庆促销。' },
  { code: 'green', label: '新中式绿意', description: '保留当前绿意底色、卡片层次和轻运营氛围，适合活动感更强的首页。' },
  { code: 'light', label: '极简纯白风', description: '纯白背景、弱化渐变与装饰，更克制、更简洁，适合强调内容与商品本身。' },
]

const activeThemeLabel = computed(() => themeOptions.find((item) => item.code === form.themeCode)?.label || '未设置')

const loadData = async () => {
  loading.value = true
  try {
    const response = await getMiniAppThemeSettings()
    form.themeCode = (response.data.themeCode || 'candy') as ThemeCode
  } catch (error) {
    notify.error(getErrorMessage(error, '加载小程序主题配置失败'))
  } finally {
    loading.value = false
  }
}

const submit = async () => {
  if (!canManage || submitting.value) {
    return
  }

  submitting.value = true
  try {
    const response = await updateMiniAppThemeSettings({ themeCode: form.themeCode })
    form.themeCode = (response.data.themeCode || 'candy') as ThemeCode
    notify.success('小程序主题保存成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '保存小程序主题失败'))
  } finally {
    submitting.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.theme-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 18px;
}

.theme-option {
  display: grid;
  gap: 14px;
  padding: 18px;
  border: 1px solid var(--line);
  border-radius: 20px;
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
  cursor: pointer;
  transition: all 0.18s ease;
}

.theme-option input {
  margin: 0;
}

.theme-option.active {
  border-color: rgba(37, 99, 235, 0.4);
  box-shadow: 0 14px 28px rgba(37, 99, 235, 0.12);
}

.theme-preview {
  height: 220px;
  border-radius: 18px;
  overflow: hidden;
  border: 1px solid rgba(148, 163, 184, 0.24);
  display: grid;
  grid-template-rows: 56px 76px 46px 1fr;
  padding: 14px;
  gap: 12px;
}

.theme-preview-top,
.theme-preview-banner,
.theme-preview-cards span,
.theme-preview-list span {
  display: block;
  border-radius: 14px;
}

.theme-preview-top {
  width: 42%;
}

.theme-preview-cards {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
}

.theme-preview-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.theme-preview-green {
  background: linear-gradient(180deg, #f6f0e5 0%, #f7f6f2 100%);
}

.theme-preview-green .theme-preview-top {
  background: linear-gradient(135deg, #315d4d, #4e7b69);
}

.theme-preview-green .theme-preview-banner {
  background: linear-gradient(135deg, rgba(45, 91, 72, 0.92), rgba(183, 155, 99, 0.74));
}

.theme-preview-green .theme-preview-cards span,
.theme-preview-green .theme-preview-list span {
  background: rgba(255, 255, 255, 0.92);
  box-shadow: 0 8px 20px rgba(45, 91, 72, 0.08);
}

.theme-preview-light {
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.theme-preview-light .theme-preview-top {
  background: linear-gradient(135deg, #111827, #475467);
}

.theme-preview-light .theme-preview-banner {
  background: linear-gradient(180deg, #ffffff 0%, #f3f4f6 100%);
}

.theme-preview-light .theme-preview-cards span,
.theme-preview-light .theme-preview-list span {
  background: #ffffff;
  border: 1px solid #e5e7eb;
}

.theme-preview-candy {
  background: linear-gradient(180deg, #eff6ff 0%, #ffffff 100%);
}

.theme-preview-candy .theme-preview-top {
  background: linear-gradient(135deg, #1d4ed8, #60a5fa);
}

.theme-preview-candy .theme-preview-banner {
  background: linear-gradient(135deg, rgba(37, 99, 235, 0.92), rgba(96, 165, 250, 0.78));
}

.theme-preview-candy .theme-preview-cards span,
.theme-preview-candy .theme-preview-list span {
  background: #ffffff;
  box-shadow: 0 10px 22px rgba(37, 99, 235, 0.12);
}

/* 活力橙：糖果圆点纹理 */
.theme-preview-orange {
  background:
    radial-gradient(circle at 18% 22%, rgba(249, 115, 22, 0.14) 2px, transparent 3px),
    radial-gradient(circle at 78% 48%, rgba(251, 146, 60, 0.12) 3px, transparent 4px),
    radial-gradient(circle at 42% 82%, rgba(249, 115, 22, 0.10) 2px, transparent 3px),
    linear-gradient(180deg, #fff7ed 0%, #fffbf5 100%);
  background-size: 80px 80px, 96px 96px, 88px 88px, 100% 100%;
}

.theme-preview-orange .theme-preview-top {
  background: linear-gradient(135deg, #ea580c, #f97316);
}

.theme-preview-orange .theme-preview-banner {
  background:
    radial-gradient(60px 36px at 78% 72%, rgba(255, 255, 255, 0.55) 0%, rgba(255, 255, 255, 0) 72%),
    linear-gradient(135deg, #f97316 0%, #fb923c 55%, #fdba74 100%);
}

.theme-preview-orange .theme-preview-cards span,
.theme-preview-orange .theme-preview-list span {
  background: #ffffff;
  box-shadow: 0 10px 22px rgba(249, 115, 22, 0.14);
  border: 1px solid rgba(249, 115, 22, 0.08);
}

/* 珊瑚红：斜纹 + 柔光波纹 */
.theme-preview-red {
  background:
    repeating-linear-gradient(135deg, transparent 0 10px, rgba(239, 83, 80, 0.05) 10px 11px),
    radial-gradient(120px 72px at 82% 12%, rgba(239, 83, 80, 0.14) 0%, rgba(255, 255, 255, 0) 62%),
    linear-gradient(180deg, #fff5f5 0%, #fffbfa 100%);
}

.theme-preview-red .theme-preview-top {
  background: linear-gradient(135deg, #e53935, #ef5350);
}

.theme-preview-red .theme-preview-banner {
  background:
    repeating-linear-gradient(135deg, transparent 0 12px, rgba(255, 255, 255, 0.10) 12px 13px),
    radial-gradient(70px 40px at 78% 72%, rgba(255, 255, 255, 0.55) 0%, rgba(255, 255, 255, 0) 74%),
    linear-gradient(135deg, #ef5350 0%, #f48080 55%, #fde7e7 100%);
}

.theme-preview-red .theme-preview-cards span,
.theme-preview-red .theme-preview-list span {
  background: #ffffff;
  box-shadow: 0 10px 22px rgba(239, 83, 80, 0.14);
  border: 1px solid rgba(239, 83, 80, 0.08);
}

.theme-copy {
  display: grid;
  gap: 6px;
}

.theme-copy strong {
  font-size: 16px;
}

.theme-copy p {
  margin: 0;
  color: var(--muted);
  line-height: 1.7;
}

@media (max-width: 1080px) {
  .theme-grid {
    grid-template-columns: 1fr;
  }
}
</style>
