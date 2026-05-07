<template>
  <div class="business-page page-v2 miniapp-theme-page">
    <section class="hero-panel miniapp-theme-hero">
      <div class="hero-copy">
        <span class="page-kicker">系统管理</span>
        <h2>小程序主题</h2>
        <p>统一配置小程序当前启用的视觉主题。保存后首页与业务页面会按主题切换展示，无需再改小程序代码。</p>
        <div class="hero-tags">
          <span class="badge info">后台配置</span>
          <span class="badge success">多主题切换</span>
          <span class="badge warning">保存后生效</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">当前主题</span>
          <strong>{{ activeThemeLabel }}</strong>
          <p>首页、券详情、券包页等统一跟随该主题展示</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">可用方案</span>
          <strong>{{ themeOptions.length }} 套</strong>
          <p>按业务氛围选择最合适的视觉风格</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">切换方式</span>
          <strong>后台配置</strong>
          <p>无需改小程序代码，保存后重新进入即可查看</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前状态</span>
          <strong>{{ loading ? '加载中' : (submitting ? '保存中' : '可配置') }}</strong>
          <p>主题管理权限由后台按钮权限控制</p>
        </article>
      </div>
    </section>

    <nav class="tab-bar">
      <button type="button" class="tab-btn" :class="{ active: activeTab === 'theme' }" @click="switchTab('theme')">主题设置</button>
      <button type="button" class="tab-btn" :class="{ active: activeTab === 'pay' }" @click="switchTab('pay')">支付参数</button>
    </nav>

    <div v-if="activeTab === 'theme'">
    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">当前主题</span>
        <strong class="stat-value stat-value-text">{{ activeThemeLabel }}</strong>
        <span class="stat-footnote">小程序首页、券详情、券包页等统一跟随该主题展示</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">可用方案</span>
        <strong class="stat-value">{{ themeOptions.length }}</strong>
        <span class="stat-footnote">蓝色风 / 新中式绿意 / 极简纯白 / 活力橙 / 珊瑚红</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">切换方式</span>
        <strong class="stat-value stat-value-text">后台配置</strong>
        <span class="stat-footnote">无需改小程序代码即可切换主题</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">保存提示</span>
        <strong class="stat-value stat-value-text">检查关键页</strong>
        <span class="stat-footnote">建议重点查看首页、券包详情、我的券包与个人中心</span>
      </article>
    </section>

    <section class="miniapp-theme-content-grid">
      <article class="card toolbar-card card-v2">
        <div class="toolbar-row">
          <div class="toolbar-title">
            <span class="section-kicker">主题选择</span>
            <h3>选择小程序视觉主题</h3>
            <p class="section-tip">建议保存后重点检查首页、券包详情、我的券包与个人中心的视觉一致性。</p>
          </div>
          <div class="toolbar-actions">
            <button type="button" class="ghost-button" :disabled="loading || submitting" @click="loadData">刷新配置</button>
            <button v-if="canManage" type="button" class="primary-button" :disabled="loading || submitting" @click="submit">{{ submitting ? '保存中...' : '保存主题' }}</button>
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
      </article>

      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">维护要点</span>
            <h3>主题切换规则</h3>
            <p class="section-tip">把主题管理真正需要记住的边界固定下来，降低切换后的回归成本。</p>
          </div>
        </div>
        <div class="guide-list">
          <div class="guide-item">
            <strong>主题影响整站视觉，不改业务逻辑</strong>
            <p>主题切换只改变小程序展示风格，不应改变页面功能、接口或业务规则。</p>
          </div>
          <div class="guide-item">
            <strong>保存后重点回看关键页</strong>
            <p>建议保存后优先检查首页、券包详情、我的券包和个人中心，确认主色与层级是否一致。</p>
          </div>
          <div class="guide-item">
            <strong>运营场景决定主题选择</strong>
            <p>活动氛围强时可选橙/红，强调内容时可选纯白或蓝色，主题应服务当前运营目标。</p>
          </div>
        </div>
      </article>
    </section>
    </div>

    <div v-if="activeTab === 'pay'" class="pay-tab">
      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">微信支付</span>
            <h3>支付参数配置</h3>
            <p class="section-tip">保存后即时生效；敏感字段留空表示保留原值。</p>
          </div>
          <div class="section-meta">
            <span class="badge" :class="payStatus.isConfigured ? 'success' : 'warning'">{{ payConfiguredLabel }}</span>
            <span class="muted">最后修改：{{ payUpdatedAtLabel }}</span>
          </div>
        </div>

        <div class="form-grid">
          <label class="form-field">
            <span>小程序 AppId</span>
            <input v-model="payForm.appId" :disabled="!canManagePay || paySubmitting" placeholder="wx开头的 AppId" />
          </label>
          <label class="form-field">
            <span>商户号</span>
            <input v-model="payForm.merchantId" :disabled="!canManagePay || paySubmitting" placeholder="1600000000" />
          </label>
          <label class="form-field">
            <span>商户证书序列号</span>
            <input v-model="payForm.merchantSerialNo" :disabled="!canManagePay || paySubmitting" placeholder="40 位左右的大写十六进制" />
          </label>
          <label class="form-field form-field-wide">
            <span>商户私钥 PEM</span>
            <textarea v-model="payForm.privateKeyPem" :disabled="!canManagePay || paySubmitting" rows="6"
              :placeholder="payStatus.privateKeyDisplay ? '已配置（留空表示保留原值）' : '把整段 -----BEGIN PRIVATE KEY----- ... -----END PRIVATE KEY----- 贴到这里'"></textarea>
          </label>
          <label class="form-field">
            <span>APIv3 密钥</span>
            <input v-model="payForm.apiV3Key" type="password" :disabled="!canManagePay || paySubmitting"
              :placeholder="payStatus.apiV3KeyDisplay ? '已配置（留空表示保留原值）' : '32 位 APIv3 密钥'" />
          </label>
          <label class="form-field form-field-wide">
            <span>支付回调地址</span>
            <input v-model="payForm.notifyUrl" :disabled="!canManagePay || paySubmitting" placeholder="https://your-domain/api/payments/callback" />
          </label>
          <label class="form-field form-field-wide form-checkbox">
            <input type="checkbox" v-model="payForm.enableMockFallback" :disabled="!canManagePay || paySubmitting" />
            <span>未配置时回落模拟支付（开发/测试用）</span>
          </label>
        </div>

        <div class="toolbar-actions pay-actions">
          <button type="button" class="ghost-button" :disabled="payLoading || paySubmitting" @click="loadPayData">刷新配置</button>
          <button v-if="canManagePay" type="button" class="primary-button" :disabled="payLoading || paySubmitting" @click="submitPay">{{ paySubmitting ? '保存中...' : '保存支付参数' }}</button>
        </div>
      </article>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { getMiniAppThemeSettings, updateMiniAppThemeSettings } from '@/api/miniapp-setting'
import { getMiniAppPaySettings, updateMiniAppPaySettings } from '@/api/miniapp-pay-setting'
import type { SaveAdminWeChatPaySettingsRequest } from '@/types/miniapp-pay-setting'
import { notify } from '@/utils/notify'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils/auth'

type ThemeCode = 'green' | 'light' | 'candy' | 'orange' | 'red'
type TabKey = 'theme' | 'pay'

const canManage = authStorage.hasPermission('miniapp.theme.manage')
const canManagePay = authStorage.hasPermission('miniapp.pay.manage')
const loading = ref(false)
const submitting = ref(false)
const activeTab = ref<TabKey>('theme')
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
  if (!canManage || submitting.value) return

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

const payLoading = ref(false)
const paySubmitting = ref(false)
const payLoaded = ref(false)
const payForm = reactive<SaveAdminWeChatPaySettingsRequest>({
  appId: '',
  merchantId: '',
  merchantSerialNo: '',
  privateKeyPem: '',
  apiV3Key: '',
  notifyUrl: '',
  enableMockFallback: true,
})
const payStatus = reactive({
  privateKeyDisplay: '',
  apiV3KeyDisplay: '',
  isConfigured: false,
  updatedAt: null as string | null,
})

const payUpdatedAtLabel = computed(() =>
  payStatus.updatedAt ? new Date(payStatus.updatedAt).toLocaleString() : '从未保存',
)
const payConfiguredLabel = computed(() => (payStatus.isConfigured ? '已配置' : '未配置完整'))

const loadPayData = async () => {
  payLoading.value = true
  try {
    const response = await getMiniAppPaySettings()
    const dto = response.data
    payForm.appId = dto.appId
    payForm.merchantId = dto.merchantId
    payForm.merchantSerialNo = dto.merchantSerialNo
    payForm.privateKeyPem = ''
    payForm.apiV3Key = ''
    payForm.notifyUrl = dto.notifyUrl
    payForm.enableMockFallback = dto.enableMockFallback
    payStatus.privateKeyDisplay = dto.privateKeyDisplay
    payStatus.apiV3KeyDisplay = dto.apiV3KeyDisplay
    payStatus.isConfigured = dto.isConfigured
    payStatus.updatedAt = dto.updatedAt
    payLoaded.value = true
  } catch (error) {
    notify.error(getErrorMessage(error, '加载支付参数失败'))
  } finally {
    payLoading.value = false
  }
}

const submitPay = async () => {
  if (!canManagePay || paySubmitting.value) return
  paySubmitting.value = true
  try {
    const payload: SaveAdminWeChatPaySettingsRequest = {
      appId: payForm.appId,
      merchantId: payForm.merchantId,
      merchantSerialNo: payForm.merchantSerialNo,
      notifyUrl: payForm.notifyUrl,
      enableMockFallback: payForm.enableMockFallback,
    }
    if (payForm.privateKeyPem && payForm.privateKeyPem.trim().length > 0) {
      payload.privateKeyPem = payForm.privateKeyPem
    }
    if (payForm.apiV3Key && payForm.apiV3Key.trim().length > 0) {
      payload.apiV3Key = payForm.apiV3Key
    }
    await updateMiniAppPaySettings(payload)
    notify.success('支付参数保存成功')
    await loadPayData()
  } catch (error) {
    notify.error(getErrorMessage(error, '保存支付参数失败'))
  } finally {
    paySubmitting.value = false
  }
}

const switchTab = (tab: TabKey) => {
  activeTab.value = tab
  if (tab === 'pay' && !payLoaded.value) void loadPayData()
}
</script>

<style scoped>
.miniapp-theme-hero {
  background:
    radial-gradient(circle at top right, rgba(59, 130, 246, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%);
}

.miniapp-theme-content-grid {
  display: grid;
  gap: 18px;
  grid-template-columns: minmax(0, 1.6fr) minmax(320px, 1fr);
}

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

.guide-list {
  display: grid;
  gap: 12px;
}

@media (max-width: 1080px) {
  .hero-side-grid,
  .miniapp-theme-content-grid,
  .theme-grid {
    grid-template-columns: 1fr;
  }
}

.tab-bar {
  display: flex;
  gap: 8px;
  margin: 12px 0;
}

.tab-btn {
  padding: 10px 20px;
  border: 1px solid var(--line);
  border-radius: 999px;
  background: #fff;
  color: #475467;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.15s ease;
}

.tab-btn.active {
  background: linear-gradient(135deg, #2563eb, #4f46e5);
  border-color: transparent;
  color: #fff;
  box-shadow: 0 10px 20px rgba(37, 99, 235, 0.18);
}

.pay-tab .form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px 18px;
  margin-top: 18px;
}

.pay-tab .form-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
  color: #334155;
}

.pay-tab .form-field > span {
  font-weight: 500;
}

.pay-tab .form-field input,
.pay-tab .form-field textarea {
  padding: 10px 12px;
  border: 1px solid var(--line);
  border-radius: 10px;
  background: #fff;
  font-family: inherit;
  font-size: 14px;
}

.pay-tab .form-field textarea {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
  line-height: 1.5;
}

.pay-tab .form-field-wide {
  grid-column: 1 / -1;
}

.pay-tab .form-checkbox {
  flex-direction: row;
  align-items: center;
  gap: 10px;
}

.pay-tab .pay-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 18px;
}

.pay-tab .section-meta {
  display: flex;
  align-items: center;
  gap: 14px;
  color: #64748b;
  font-size: 12px;
}

@media (max-width: 1080px) {
  .pay-tab .form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
