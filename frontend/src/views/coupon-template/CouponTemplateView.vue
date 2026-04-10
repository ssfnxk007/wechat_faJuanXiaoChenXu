<template>
  <div class="business-page page-v2 coupon-template-page">
    <section class="hero-panel template-hero">
      <div class="hero-copy">
        <span class="page-kicker">发券运营</span>
        <h2>券模板管理</h2>
        <p>
          统一维护新人券、无门槛券、指定商品券与满减券规则，支撑公开领取、定向发放、券包售卖与门店核销的全流程配置。
        </p>
        <div class="hero-tags">
          <span class="badge info">模板规则归档</span>
          <span class="badge success">支持固定日期与领后生效</span>
          <span class="badge warning">可配置门店范围</span>
        </div>
      </div>
      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight">
          <span class="quick-card-label">当前概览</span>
          <strong>{{ totalCount }}</strong>
          <p>已建立的券模板档案总数，覆盖当前查询范围。</p>
        </article>
        <div class="hero-side-grid">
          <article class="quick-card compact">
            <span class="quick-card-label">启用模板</span>
            <strong>{{ enabledCount }}</strong>
            <p>可参与发券的模板数</p>
          </article>
          <article class="quick-card compact">
            <span class="quick-card-label">新人券</span>
            <strong>{{ newUserTemplateCount }}</strong>
            <p>仅限成为用户后领取一次</p>
          </article>
        </div>
      </div>
    </section>

    <div class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">模板总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">当前筛选条件下的模板记录数</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">当前页码</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">启用状态</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页可投放模板</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">新人券模板</span>
        <strong class="stat-value">{{ newUserTemplateCount }}</strong>
        <span class="stat-footnote">仅允许单用户一次领取</span>
      </article>
    </div>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">业务操作台</span>
          <h3>模板筛选与管理动作</h3>
          <p class="section-tip">按模板名称快速定位配置档案，结合分页与新增入口进行统一维护。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新增券模板</button>
        </div>
      </div>

      <div class="filter-panel-grid">
        <label class="field-card filter-field">
          <span class="field-label">模板名称</span>
          <input v-model.trim="query.keyword" type="text" placeholder="输入模板名称后回车检索" @keyup.enter="handleSearch" />
        </label>
        <label class="field-card filter-field compact-field">
          <span class="field-label">分页条数</span>
          <select v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">每页 10 条</option>
            <option :value="20">每页 20 条</option>
            <option :value="50">每页 50 条</option>
          </select>
        </label>
        <div class="field-card summary-field">
          <span class="field-label">当前筛选</span>
          <strong>{{ querySummary }}</strong>
          <p>基于服务端分页查询，不做前端全量缓存筛选。</p>
        </div>
      </div>
    </section>

    <section class="card card-v2 data-card archive-card">
      <div class="section-head">
        <div class="section-head-main">
          <span class="section-kicker">模板档案</span>
          <h3>券模板列表</h3>
          <p class="section-tip">集中展示模板类型、有效期、优惠力度、适用商品和状态，支持弹窗编辑与删除。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">当前页 {{ items.length }} 条</span>
          <span class="badge warning">每页 {{ pageSize }} 条</span>
        </div>
      </div>

      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>模板信息</th>
              <th>有效期</th>
              <th>优惠规则</th>
              <th>适用范围</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="table-primary-cell template-cell">
                  <strong>{{ item.name }}</strong>
                  <div class="template-meta-row">
                    <span class="badge info">{{ typeMap[item.templateType] || '-' }}</span>
                    <span v-if="item.isNewUserOnly" class="badge warning">新人专享</span>
                    <span :class="['badge', item.isAllStores ? 'success' : 'warning']">
                      {{ item.isAllStores ? '全部门店可用' : '指定门店可用' }}
                    </span>
                  </div>
                </div>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ formatValidity(item) }}</strong>
                  <span>{{ validPeriodTypeMap[item.validPeriodType] || '-' }}</span>
                </div>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ formatDiscount(item) }}</strong>
                  <span>每用户限领 {{ item.perUserLimit }} 张</span>
                </div>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ formatProductIds(item.productIds) }}</strong>
                  <span>{{ item.remark || '未设置补充说明' }}</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">
                  {{ item.isEnabled ? '启用' : '停用' }}
                </span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <div class="table-actions">
                  <button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button>
                  <button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="8" class="empty-text">当前没有符合条件的券模板记录</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager pager-v2">
        <div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条记录</div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 template-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">模板表单</span>
            <h3>{{ editingId ? '编辑券模板' : '新增券模板' }}</h3>
            <p>{{ editingId ? '调整已有模板的投放规则与有效期设置。' : '建立新的发券模板，支持后续发放、领取和核销。' }}</p>
          </div>
        </div>

        <div class="grid-form dialog-form template-form-grid">
          <label>
            <span>模板名称</span>
            <input v-model.trim="form.name" type="text" placeholder="例如：新人欢迎礼券" />
          </label>
          <label>
            <span>模板类型</span>
            <select v-model.number="form.templateType">
              <option :value="1">新人券</option>
              <option :value="2">无门槛券</option>
              <option :value="3">指定商品券</option>
              <option :value="4">满减券</option>
            </select>
          </label>
          <label>
            <span>有效期类型</span>
            <select v-model.number="form.validPeriodType">
              <option :value="1">固定日期范围</option>
              <option :value="2">领取后 N 天有效</option>
            </select>
          </label>
          <label>
            <span>优惠金额</span>
            <input v-model.number="form.discountAmount" type="number" min="0" step="0.01" placeholder="例如：5" />
          </label>
          <label>
            <span>门槛金额</span>
            <input v-model.number="form.thresholdAmount" type="number" min="0" step="0.01" placeholder="满减券可填写门槛" />
          </label>
          <label>
            <span>每用户限领</span>
            <input v-model.number="form.perUserLimit" type="number" min="1" step="1" />
          </label>
          <label v-if="form.validPeriodType === 2">
            <span>领取后有效天数</span>
            <input v-model.number="form.validDays" type="number" min="1" step="1" />
          </label>
          <label v-if="form.validPeriodType === 1">
            <span>开始时间</span>
            <input v-model="validFromLocal" type="datetime-local" />
          </label>
          <label v-if="form.validPeriodType === 1">
            <span>结束时间</span>
            <input v-model="validToLocal" type="datetime-local" />
          </label>
          <label class="field-span-3">
            <span>指定商品 ID</span>
            <input v-model.trim="form.productIdsText" type="text" placeholder="多个商品 ID 用逗号、空格或换行分隔" />
          </label>
          <label class="field-span-3">
            <span>备注说明</span>
            <input v-model.trim="form.remark" type="text" placeholder="用于补充适用门店、活动场景等说明" />
          </label>
          <label class="checkbox-field checkbox-card">
            <input v-model="form.isNewUserOnly" type="checkbox" />
            <span>仅限新人领取一次</span>
          </label>
          <label class="checkbox-field checkbox-card">
            <input v-model="form.isAllStores" type="checkbox" />
            <span>全部门店可用</span>
          </label>
          <label class="checkbox-field checkbox-card">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用模板</span>
          </label>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeDialog">取消</button>
          <button v-if="editingId ? canEdit : canCreate" type="button" class="primary-button" @click="submit">
            {{ editingId ? '保存修改' : '保存新增' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createCouponTemplate, deleteCouponTemplate, getCouponTemplateList, updateCouponTemplate } from '@/api/coupon-template'
import type { CouponTemplateListItemDto, SaveCouponTemplateRequest } from '@/types/coupon'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

type CouponTemplateForm = SaveCouponTemplateRequest & {
  productIdsText: string
}

const items = ref<CouponTemplateListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const validFromLocal = ref('')
const validToLocal = ref('')

const query = reactive({
  keyword: '',
})

const typeMap: Record<number, string> = {
  1: '新人券',
  2: '无门槛券',
  3: '指定商品券',
  4: '满减券',
}

const validPeriodTypeMap: Record<number, string> = {
  1: '固定日期范围',
  2: '领取后生效',
}

const createEmptyForm = (): CouponTemplateForm => ({
  name: '',
  templateType: 2,
  validPeriodType: 2,
  discountAmount: undefined,
  thresholdAmount: undefined,
  validDays: 7,
  validFrom: undefined,
  validTo: undefined,
  isNewUserOnly: false,
  isAllStores: true,
  perUserLimit: 1,
  isEnabled: true,
  remark: '',
  productIds: [],
  productIdsText: '',
})

const form = reactive<CouponTemplateForm>(createEmptyForm())

const canCreate = authStorage.hasPermission('coupon-template.create')
const canEdit = authStorage.hasPermission('coupon-template.edit')
const canDelete = authStorage.hasPermission('coupon-template.delete')

const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const newUserTemplateCount = computed(() => items.value.filter((item) => item.isNewUserOnly).length)
const querySummary = computed(() => {
  const keyword = query.keyword || '全部模板'
  return `关键词：${keyword} / 每页 ${pageSize.value} 条`
})

watch(validFromLocal, (value) => {
  form.validFrom = toServerDateTime(value)
})

watch(validToLocal, (value) => {
  form.validTo = toServerDateTime(value)
})

const resetForm = () => {
  Object.assign(form, createEmptyForm())
  validFromLocal.value = ''
  validToLocal.value = ''
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

const toDateTimeLocal = (value?: string) => (value ? value.slice(0, 16).replace(' ', 'T') : '')

const toServerDateTime = (value?: string) => (value ? value.replace('T', ' ') : undefined)

const parseProductIds = (value?: string) => {
  if (!value) return [] as number[]

  return Array.from(
    new Set(
      value
        .split(/[\s,]+/)
        .map((item) => Number(item.trim()))
        .filter((item) => Number.isInteger(item) && item > 0),
    ),
  )
}

const formatProductIds = (productIds?: number[]) => {
  if (!productIds || productIds.length === 0) {
    return '全部商品'
  }

  if (productIds.length <= 3) {
    return productIds.join(', ')
  }

  return `${productIds.slice(0, 3).join(', ')} 等 ${productIds.length} 项`
}

const formatAmount = (value?: number) => (typeof value === 'number' ? `¥${value.toFixed(2)}` : '-')

const formatDiscount = (item: CouponTemplateListItemDto) => {
  if (item.templateType === 4) {
    const threshold = formatAmount(item.thresholdAmount)
    const discount = formatAmount(item.discountAmount)
    return `${threshold} 减 ${discount}`
  }

  return formatAmount(item.discountAmount)
}

const formatValidity = (item: CouponTemplateListItemDto) => {
  if (item.validPeriodType === 1) {
    return `${formatDate(item.validFrom)} ~ ${formatDate(item.validTo)}`
  }

  return `领取后 ${item.validDays || 0} 天内有效`
}

const loadData = async () => {
  try {
    const response = await getCouponTemplateList({
      keyword: query.keyword || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })

    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载券模板列表失败'))
  }
}

const handleSearch = async () => {
  pageIndex.value = 1
  await loadData()
}

const resetQuery = async () => {
  query.keyword = ''
  pageSize.value = 10
  pageIndex.value = 1
  await loadData()
  notify.info('已重置券模板筛选条件')
}

const handlePageSizeChange = async () => {
  pageIndex.value = 1
  await loadData()
}

const goPrevPage = async () => {
  if (pageIndex.value <= 1) return
  pageIndex.value -= 1
  await loadData()
}

const goNextPage = async () => {
  if (pageIndex.value >= totalPages.value) return
  pageIndex.value += 1
  await loadData()
}

const openCreateDialog = () => {
  resetForm()
  editingId.value = null
  dialogVisible.value = true
}

const openEditDialog = (item: CouponTemplateListItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    templateType: item.templateType,
    validPeriodType: item.validPeriodType,
    discountAmount: item.discountAmount,
    thresholdAmount: item.thresholdAmount,
    validDays: item.validDays,
    validFrom: item.validFrom,
    validTo: item.validTo,
    isNewUserOnly: item.isNewUserOnly,
    isAllStores: item.isAllStores,
    perUserLimit: item.perUserLimit,
    isEnabled: item.isEnabled,
    remark: item.remark,
    productIds: item.productIds || [],
    productIdsText: (item.productIds || []).join(', '),
  })
  validFromLocal.value = toDateTimeLocal(item.validFrom)
  validToLocal.value = toDateTimeLocal(item.validTo)
  dialogVisible.value = true
}

const closeDialog = () => {
  dialogVisible.value = false
  editingId.value = null
  resetForm()
}

const buildPayload = (): SaveCouponTemplateRequest => ({
  name: form.name.trim(),
  templateType: form.templateType,
  validPeriodType: form.validPeriodType,
  discountAmount: form.discountAmount,
  thresholdAmount: form.thresholdAmount,
  validDays: form.validPeriodType === 2 ? form.validDays : undefined,
  validFrom: form.validPeriodType === 1 ? form.validFrom : undefined,
  validTo: form.validPeriodType === 1 ? form.validTo : undefined,
  isNewUserOnly: form.isNewUserOnly,
  isAllStores: form.isAllStores,
  perUserLimit: form.perUserLimit,
  isEnabled: form.isEnabled,
  remark: form.remark?.trim() || undefined,
  productIds: parseProductIds(form.productIdsText),
})

const submit = async () => {
  if (!form.name.trim()) {
    notify.info('请输入模板名称')
    return
  }

  if ((form.discountAmount ?? 0) <= 0) {
    notify.info('请输入有效的优惠金额')
    return
  }

  if (form.templateType === 4 && (form.thresholdAmount ?? 0) <= 0) {
    notify.info('满减券必须填写门槛金额')
    return
  }

  if (form.validPeriodType === 1 && (!form.validFrom || !form.validTo)) {
    notify.info('固定日期范围必须填写开始和结束时间')
    return
  }

  if (form.validPeriodType === 2 && (form.validDays ?? 0) <= 0) {
    notify.info('领取后有效天数必须大于 0')
    return
  }

  try {
    const payload = buildPayload()
    if (editingId.value) {
      await updateCouponTemplate(editingId.value, payload)
      notify.success('券模板已更新')
    } else {
      await createCouponTemplate(payload)
      notify.success('券模板已创建')
    }

    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '保存券模板失败' : '新增券模板失败'))
  }
}

const removeItem = async (item: CouponTemplateListItemDto) => {
  const confirmed = window.confirm(`确认删除券模板“${item.name}”吗？`)
  if (!confirmed) return

  try {
    await deleteCouponTemplate(item.id)
    notify.success('券模板已删除')
    if (items.value.length === 1 && pageIndex.value > 1) {
      pageIndex.value -= 1
    }
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '删除券模板失败'))
  }
}

onMounted(loadData)
</script>

<style scoped>
.coupon-template-page {
  gap: 22px;
}

.template-hero {
  background:
    radial-gradient(circle at top right, rgba(37, 99, 235, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f7faff 54%, #f3f6fb 100%);
}

.hero-side-stack {
  align-content: stretch;
}

.hero-side-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.quick-card-spotlight {
  min-height: 148px;
  background: linear-gradient(135deg, rgba(37, 99, 235, 0.08), rgba(59, 130, 246, 0.02));
  border: 1px solid rgba(59, 130, 246, 0.16);
}

.quick-card.compact {
  min-height: 112px;
}

.quick-card-label {
  display: inline-flex;
  width: fit-content;
  padding: 4px 10px;
  border-radius: 999px;
  background: rgba(37, 99, 235, 0.08);
  color: var(--primary);
  font-size: 12px;
  font-weight: 700;
}

.operations-card {
  gap: 18px;
}

.filter-panel-grid {
  display: grid;
  grid-template-columns: 1.4fr 0.8fr 1fr;
  gap: 14px;
}

.field-card {
  display: grid;
  gap: 10px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(226, 232, 240, 0.96);
  background: linear-gradient(180deg, #fff 0%, #fbfdff 100%);
}

.field-label {
  font-size: 12px;
  font-weight: 700;
  color: #475467;
  letter-spacing: 0.04em;
}

.summary-field strong {
  font-size: 16px;
}

.summary-field p {
  margin: 0;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.6;
}

.archive-card {
  gap: 18px;
}

.template-cell {
  gap: 8px;
}

.template-meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.template-dialog {
  width: min(920px, calc(100vw - 48px));
}

.template-form-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.dialog-form > label {
  display: grid;
  gap: 8px;
}

.dialog-form > label > span {
  font-size: 13px;
  font-weight: 700;
  color: #344054;
}

.dialog-form input,
.dialog-form select {
  width: 100%;
  height: 44px;
  padding: 0 14px;
  border: 1px solid var(--line-strong);
  border-radius: 12px;
  background: #fff;
}

@media (max-width: 1100px) {
  .filter-panel-grid,
  .template-form-grid,
  .hero-side-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 820px) {
  .filter-panel-grid,
  .template-form-grid,
  .hero-side-grid {
    grid-template-columns: 1fr;
  }
}
</style>
