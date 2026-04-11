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
        <strong class="stat-value">2 套</strong>
        <span class="stat-footnote">保留现有新中式绿意，同时新增极简纯白风</span>
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

type ThemeCode = 'green' | 'light'

const canManage = authStorage.hasPermission('miniapp.theme.manage')
const loading = ref(false)
const submitting = ref(false)
const form = reactive<{ themeCode: ThemeCode }>({ themeCode: 'green' })

const themeOptions: Array<{ code: ThemeCode; label: string; description: string }> = [
  { code: 'green', label: '新中式绿意', description: '保留当前绿意底色、卡片层次和轻运营氛围，适合活动感更强的首页。' },
  { code: 'light', label: '极简纯白风', description: '纯白背景、弱化渐变与装饰，更克制、更简洁，适合强调内容与商品本身。' },
]

const activeThemeLabel = computed(() => themeOptions.find((item) => item.code === form.themeCode)?.label || '未设置')

const loadData = async () => {
  loading.value = true
  try {
    const response = await getMiniAppThemeSettings()
    form.themeCode = (response.data.themeCode || 'green') as ThemeCode
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
    form.themeCode = (response.data.themeCode || 'green') as ThemeCode
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
  grid-template-columns: repeat(2, minmax(0, 1fr));
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
