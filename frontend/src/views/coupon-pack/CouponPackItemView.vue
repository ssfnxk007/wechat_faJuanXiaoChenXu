<template>
  <div class="business-page page-v2 coupon-pack-item-page">
    <section class="hero-panel item-hero">
      <div class="hero-copy">
        <span class="page-kicker">券包配置</span>
        <h2>券包明细</h2>
        <p>维护券包内包含的券模板与数量，确保销售后的发券内容清晰、稳定、可追踪。</p>
        <div class="hero-tags">
          <span class="badge info">按券包维度维护</span>
          <span class="badge success">支持数量配置</span>
          <span class="badge warning">影响券包发放内容</span>
        </div>
      </div>
      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight">
          <span class="quick-card-label">当前明细数</span>
          <strong>{{ items.length }}</strong>
          <p>当前券包下已配置的模板条目数量。</p>
        </article>
        <div class="hero-side-grid">
          <article class="quick-card compact">
            <span class="quick-card-label">当前券包</span>
            <strong>{{ selectedCouponPackName || '-' }}</strong>
            <p>列表与表单均围绕当前券包维护。</p>
          </article>
          <article class="quick-card compact">
            <span class="quick-card-label">总数量</span>
            <strong>{{ totalQuantity }}</strong>
            <p>当前券包内各券模板张数合计。</p>
          </article>
        </div>
      </div>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">配置入口</span>
          <h3>券包明细筛选与维护</h3>
          <p class="section-tip">优先选择券包，再维护券模板与数量，减少直接填写编号带来的配置误差。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新增明细</button>
        </div>
      </div>

      <div class="filter-panel-grid">
        <label class="field-card filter-field compact-field">
          <span class="field-label">券包</span>
          <RemoteSelectField v-model="couponPackId" v-model:keyword="selectorQuery.couponPackKeyword" placeholder="输入券包名称后搜索" empty-label="请选择券包" :options="couponPackSelectOptions" @search="searchCouponPacks" />
        </label>
        <div class="field-card summary-field">
          <span class="field-label">当前说明</span>
          <strong>{{ selectedCouponPackName || '请选择券包' }}</strong>
          <p>明细列表会按当前券包查询，不混合展示其他券包的配置。</p>
        </div>
      </div>
    </section>

    <section class="card card-v2 data-card archive-card">
      <div class="section-head">
        <div class="section-head-main">
          <span class="section-kicker">明细档案</span>
          <h3>券包明细列表</h3>
          <p class="section-tip">展示券模板名称、发放数量与所属券包，便于运营核对券包内容。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">条目 {{ items.length }}</span>
          <span class="badge warning">总数量 {{ totalQuantity }}</span>
        </div>
      </div>

      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>券模板信息</th>
              <th>数量</th>
              <th>所属券包</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ item.couponTemplateName }}</strong>
                  <span>券模板配置</span>
                </div>
              </td>
              <td><span class="badge success">{{ item.quantity }} 张</span></td>
              <td>{{ selectedCouponPackName || `券包 #${item.couponPackId}` }}</td>
              <td>
                <div class="table-actions">
                  <button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button>
                  <button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="5" class="empty-text">当前券包没有明细记录</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 item-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">明细表单</span>
            <h3>{{ editingId ? '编辑券包明细' : '新增券包明细' }}</h3>
            <p>{{ editingId ? '调整券模板与数量配置。' : '为当前券包新增券模板配置。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form item-form-grid">
          <label>
            <span>所属券包</span>
            <RemoteSelectField v-model="form.couponPackId" v-model:keyword="selectorQuery.couponPackKeyword" placeholder="输入券包名称后搜索" empty-label="请选择券包" :options="couponPackSelectOptions" @search="searchCouponPacks" />
          </label>
          <label>
            <span>券模板</span>
            <RemoteSelectField v-model="form.couponTemplateId" v-model:keyword="selectorQuery.couponTemplateKeyword" placeholder="输入模板名称后搜索" empty-label="请选择券模板" :options="couponTemplateSelectOptions" @search="searchCouponTemplates" />
          </label>
          <label>
            <span>数量</span>
            <input v-model.number="form.quantity" type="number" min="1" step="1" placeholder="请输入数量" />
          </label>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">取消</button>
          <button type="button" class="primary-button" :disabled="submitting || deleting" @click="submit">{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import RemoteSelectField from '@/components/RemoteSelectField.vue'
import { getCouponPackList } from '@/api/coupon-pack'
import { deleteCouponPackItem, getCouponPackItemList, saveCouponPackItem, updateCouponPackItem } from '@/api/coupon-pack-item'
import { getCouponTemplateList } from '@/api/coupon-template'
import type { CouponTemplateListItemDto } from '@/types/coupon'
import type { CouponPackListItemDto } from '@/types/coupon-pack'
import type { CouponPackItemDto, SaveCouponPackItemRequest } from '@/types/coupon-pack-item'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<CouponPackItemDto[]>([])
const couponPackId = ref(0)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive<SaveCouponPackItemRequest>({ couponPackId: 0, couponTemplateId: 0, quantity: 1 })
const couponPackOptions = ref<CouponPackListItemDto[]>([])
const couponTemplateOptions = ref<CouponTemplateListItemDto[]>([])
const selectorQuery = reactive({ couponPackKeyword: '', couponTemplateKeyword: '' })
const submitting = ref(false)
const deleting = ref(false)

const canCreate = authStorage.hasPermission('coupon-pack-item.create')
const canEdit = authStorage.hasPermission('coupon-pack-item.edit')
const canDelete = authStorage.hasPermission('coupon-pack-item.delete')
const templateTypeMap: Record<number, string> = { 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }
const couponPackSelectOptions = computed(() => couponPackOptions.value.map((pack) => ({ value: pack.id, label: `${pack.name} / ¥${Number(pack.salePrice || 0).toFixed(2)}` })))
const couponTemplateSelectOptions = computed(() => couponTemplateOptions.value.map((template) => ({ value: template.id, label: `${template.name} / ${templateTypeMap[template.templateType] || '券模板'}` })))
const totalQuantity = computed(() => items.value.reduce((sum, item) => sum + Number(item.quantity || 0), 0))
const selectedCouponPackName = computed(() => couponPackOptions.value.find((item) => item.id === couponPackId.value)?.name || '')

const resetForm = () => {
  form.couponPackId = couponPackId.value || 0
  form.couponTemplateId = 0
  form.quantity = 1
}

const loadCouponPackOptions = async () => {
  const response = await getCouponPackList({ keyword: selectorQuery.couponPackKeyword || undefined, pageIndex: 1, pageSize: 50 })
  couponPackOptions.value = response.data.items
}

const loadCouponTemplateOptions = async () => {
  const response = await getCouponTemplateList({ keyword: selectorQuery.couponTemplateKeyword || undefined, pageIndex: 1, pageSize: 50 })
  couponTemplateOptions.value = response.data.items
}

const searchCouponPacks = async () => { await loadCouponPackOptions() }
const searchCouponTemplates = async () => { await loadCouponTemplateOptions() }

const loadData = async () => {
  if (!couponPackId.value || couponPackId.value <= 0) {
    items.value = []
    return
  }

  try {
    const response = await getCouponPackItemList(couponPackId.value)
    items.value = response.data
  } catch (error) {
    notify.error(getErrorMessage(error, '加载券包明细失败'))
  }
}

const openCreateDialog = () => {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

const openEditDialog = (item: CouponPackItemDto) => {
  editingId.value = item.id
  form.couponPackId = item.couponPackId
  form.couponTemplateId = item.couponTemplateId
  form.quantity = item.quantity
  dialogVisible.value = true
}

const closeDialog = () => {
  dialogVisible.value = false
  editingId.value = null
  resetForm()
}

const submit = async () => {
  if (form.couponPackId <= 0) return notify.info('请选择券包')
  if (form.couponTemplateId <= 0) return notify.info('请选择券模板')
  if (form.quantity <= 0) return notify.info('数量必须大于 0')
  if (submitting.value) return
  submitting.value = true

  try {
    if (editingId.value) {
      await updateCouponPackItem(editingId.value, { ...form })
      notify.success('券包明细已更新')
    } else {
      await saveCouponPackItem({ ...form })
      notify.success('券包明细已创建')
    }

    couponPackId.value = form.couponPackId
    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '保存券包明细失败' : '新增券包明细失败'))
  } finally {
    submitting.value = false
  }
}

const removeItem = async (item: CouponPackItemDto) => {
  if (!window.confirm(`确认删除模板“${item.couponTemplateName}”的明细吗？`)) return
  if (deleting.value) return
  deleting.value = true

  try {
    await deleteCouponPackItem(item.id)
    notify.success('券包明细已删除')
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '删除券包明细失败'))
  } finally {
    deleting.value = false
  }
}

onMounted(async () => {
  try {
    await Promise.all([loadCouponPackOptions(), loadCouponTemplateOptions()])
  } catch (error) {
    notify.error(getErrorMessage(error, '加载业务选项失败'))
  }

  resetForm()
})
</script>

<style scoped>
.item-hero { background: radial-gradient(circle at top right, rgba(20,184,166,.12), transparent 28%), linear-gradient(135deg, #ffffff 0%, #f7fffd 52%, #f4f8fb 100%); }
.hero-side-stack { align-content: stretch; }
.hero-side-grid { display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 12px; }
.quick-card-spotlight { min-height: 148px; background: linear-gradient(135deg, rgba(20,184,166,.08), rgba(59,130,246,.03)); border: 1px solid rgba(20,184,166,.14); }
.quick-card.compact { min-height: 112px; }
.quick-card-label { display: inline-flex; width: fit-content; padding: 4px 10px; border-radius: 999px; background: rgba(37,99,235,.08); color: var(--primary); font-size: 12px; font-weight: 700; }
.filter-panel-grid { display: grid; grid-template-columns: .8fr 1.2fr; gap: 14px; }
.field-card { display: grid; gap: 10px; padding: 16px; border-radius: 18px; border: 1px solid rgba(226,232,240,.96); background: linear-gradient(180deg,#fff 0%,#fbfdff 100%); }
.field-label { font-size: 12px; font-weight: 700; color: #475467; letter-spacing: .04em; }
.summary-field strong { font-size: 16px; }
.summary-field p { margin: 0; color: var(--muted); font-size: 13px; line-height: 1.6; }
.item-dialog { width: min(760px, calc(100vw - 48px)); }
.item-form-grid { grid-template-columns: repeat(3, minmax(0,1fr)); }
.dialog-form>label { display: grid; gap: 8px; }
.dialog-form>label>span { font-size: 13px; font-weight: 700; color: #344054; }
.dialog-form input,
.dialog-form select { width: 100%; height: 44px; padding: 0 14px; border: 1px solid var(--line-strong); border-radius: 12px; background: #fff; }
@media (max-width:1100px){ .filter-panel-grid,.item-form-grid,.hero-side-grid{ grid-template-columns: repeat(2, minmax(0,1fr)); } }
@media (max-width:820px){ .filter-panel-grid,.item-form-grid,.hero-side-grid{ grid-template-columns: 1fr; } }
</style>
