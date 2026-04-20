<template>
  <div class="business-page page-v2 coupon-pack-page-v2">
    <section class="hero-panel coupon-pack-hero">
      <div class="hero-copy">
        <span class="page-kicker">售卖配置</span>
        <h2>券包管理</h2>
        <p>围绕可售卖券包的配置、组合与状态管理搭建工作台，先看筛选与结果，再进入弹窗编辑，不让展示型内容喧宾夺主。</p>
        <div class="hero-tags">
          <span class="badge info">服务端分页</span>
          <span class="badge success">券包 + 明细联编</span>
          <span class="badge warning">支持弹窗编辑</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">券包总数</span>
          <strong>{{ totalCount }}</strong>
          <p>当前服务端分页统计总数</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">启用券包</span>
          <strong>{{ enabledCount }}</strong>
          <p>当前页处于启用状态的券包数</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前页码</span>
          <strong>{{ pageIndex }} / {{ totalPages }}</strong>
          <p>支持关键字检索与翻页浏览</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">平均售价</span>
          <strong>¥{{ averagePrice }}</strong>
          <p>当前页券包平均售价</p>
        </article>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">券包总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">服务端分页统计结果</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">当前页码</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">启用券包</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页启用中的券包</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">平均售价</span>
        <strong class="stat-value">¥{{ averagePrice }}</strong>
        <span class="stat-footnote">当前页券包平均售价</span>
      </article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">检索与动作</span>
          <h3>券包筛选与维护入口</h3>
          <p class="section-tip">统一在筛选区完成查询、翻页和新增动作；编辑时在同一弹窗处理基础资料与券包明细。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetQuery">重置筛选</button>
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新增券包</button>
        </div>
      </div>

      <div class="filter-panel-grid coupon-pack-filter-grid">
        <label class="field-card filter-field">
          <span class="field-label">券包名称</span>
          <input v-model.trim="query.keyword" type="text" placeholder="输入券包名称后回车检索" @keyup.enter="handleSearch" />
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
          <p>券包是售卖链路上游，编辑时会同步处理券包明细。</p>
        </div>
      </div>
    </section>

    <section class="card card-v2 data-card archive-card">
      <div class="section-head">
        <div class="section-head-main">
          <span class="section-kicker">售卖档案</span>
          <h3>券包列表</h3>
          <p class="section-tip">展示售价、状态、限购和时间信息；进入编辑弹窗后可同时维护券包组合内容。</p>
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
              <th>券包信息</th>
              <th>售价 / 限购</th>
              <th>状态</th>
              <th>售卖时间</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ item.name }}</strong>
                  <span>{{ item.remark || '未设置备注说明' }}</span>
                </div>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ formatAmount(item.salePrice) }}</strong>
                  <span>每人限购 {{ item.perUserLimit }} 次</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', item.status === 1 ? 'success' : 'danger']">
                  {{ item.status === 1 ? '启用' : '停用' }}
                </span>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ formatDate(item.saleStartTime) }}</strong>
                  <span>至 {{ formatDate(item.saleEndTime) }}</span>
                </div>
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
              <td colspan="7" class="empty-text">当前没有符合条件的券包记录</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager pager-v2">
        <div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条</div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 pack-dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">券包表单</span>
            <h3>{{ editingId ? '编辑券包' : '新增券包' }}</h3>
            <p>{{ editingId ? '在一个窗口内同时调整券包资料和券包明细。' : '先填写券包资料，再直接配置包含的券模板组合。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <section class="dialog-section">
          <div class="section-head compact">
            <div class="section-head-main">
              <span class="section-kicker">基础资料</span>
              <h4>券包属性</h4>
              <p class="section-tip">名称、售价、状态和售卖时间都在这里维护。</p>
            </div>
          </div>

          <div class="grid-form dialog-form">
            <label><span>券包名称</span><input v-model.trim="form.name" type="text" placeholder="输入券包名称" /></label>
            <label><span>售价</span><input v-model.number="form.salePrice" type="number" min="0.01" step="0.01" placeholder="输入售价" /></label>
            <label><span>每人限购</span><input v-model.number="form.perUserLimit" type="number" min="1" placeholder="输入限购次数" /></label>
            <label><span>状态</span><select v-model.number="form.status"><option :value="1">启用</option><option :value="0">停用</option></select></label>
            <label><span>销售开始时间</span><input v-model="form.saleStartTime" type="datetime-local" /></label>
            <label><span>销售结束时间</span><input v-model="form.saleEndTime" type="datetime-local" /></label>
            <label class="field-span-2"><span>备注</span><input v-model.trim="form.remark" type="text" placeholder="补充售卖规则或投放说明" /></label>
          </div>
        </section>

        <section class="dialog-section">
          <div class="section-head compact">
            <div class="section-head-main">
              <span class="section-kicker">券包组合</span>
              <h4>券包明细</h4>
              <p class="section-tip">直接选择券模板并填写数量，保存时自动新增、更新和删除对应明细。</p>
            </div>
            <div class="inline-metrics">
              <span class="badge info">条目 {{ packItems.length }}</span>
              <span class="badge success">总数量 {{ totalPackItemQuantity }}</span>
            </div>
          </div>

          <div class="pack-item-toolbar">
            <div class="pack-item-summary">
              <strong>券模板池</strong>
              <span>先搜索模板，再把模板加入当前券包组合。</span>
            </div>
            <button type="button" class="ghost-button" @click="appendPackItem">新增一条明细</button>
          </div>

          <div class="pack-item-list">
            <div v-for="(item, index) in packItems" :key="item.key" class="pack-item-row">
              <div class="pack-item-index">{{ index + 1 }}</div>
              <div class="pack-item-template">
                <RemoteSelectField
                  v-model="item.couponTemplateId"
                  v-model:keyword="templateKeyword"
                  placeholder="输入模板名称后搜索"
                  empty-label="请选择券模板"
                  :options="couponTemplateSelectOptions"
                  @search="searchCouponTemplates"
                />
              </div>
              <input v-model.number="item.quantity" class="pack-item-quantity" type="number" min="1" step="1" placeholder="数量" />
              <button type="button" class="action-button danger" @click="removePackItem(index)">删除</button>
            </div>
            <div v-if="packItems.length === 0" class="empty-pack-items">
              当前还没有配置券包明细。至少添加 1 条券模板，支付成功后才能正常发券。
            </div>
          </div>
        </section>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">取消</button>
          <button v-if="editingId ? canEdit : canCreate" type="button" class="primary-button" :disabled="submitting || deleting" @click="submit">{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import RemoteSelectField from '@/components/RemoteSelectField.vue'
import { createCouponPack, deleteCouponPack, getCouponPackList, updateCouponPack } from '@/api/coupon-pack'
import { getCouponTemplateList } from '@/api/coupon-template'
import { deleteCouponPackItem, getCouponPackItemList, saveCouponPackItem, updateCouponPackItem } from '@/api/coupon-pack-item'
import type { CouponTemplateListItemDto } from '@/types/coupon'
import type { CouponPackListItemDto, SaveCouponPackRequest } from '@/types/coupon-pack'
import type { CouponPackItemDto } from '@/types/coupon-pack-item'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<CouponPackListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const submitting = ref(false)
const deleting = ref(false)
const couponTemplateOptions = ref<CouponTemplateListItemDto[]>([])
const templateKeyword = ref('')

const query = reactive({
  keyword: '',
})

interface EditablePackItem {
  key: string
  id?: number
  couponTemplateId: number
  quantity: number
}

let packItemKeySeed = 0
const createEditablePackItem = (item?: Partial<CouponPackItemDto>): EditablePackItem => ({
  key: `pack-item-${packItemKeySeed += 1}`,
  id: item?.id,
  couponTemplateId: Number(item?.couponTemplateId || 0),
  quantity: Number(item?.quantity || 1),
})

const createEmptyForm = (): SaveCouponPackRequest => ({
  name: '',
  salePrice: 0,
  status: 1,
  perUserLimit: 1,
  saleStartTime: undefined,
  saleEndTime: undefined,
  remark: '',
})

const form = reactive<SaveCouponPackRequest>(createEmptyForm())
const packItems = ref<EditablePackItem[]>([])
const originalPackItems = ref<CouponPackItemDto[]>([])

const canCreate = authStorage.hasPermission('coupon-pack.create')
const canEdit = authStorage.hasPermission('coupon-pack.edit')
const canDelete = authStorage.hasPermission('coupon-pack.delete')
const enabledCount = computed(() => items.value.filter((item) => item.status === 1).length)
const templateTypeMap: Record<number, string> = { 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }
const averagePrice = computed(() => {
  if (!items.value.length) return '0.00'
  const total = items.value.reduce((sum, item) => sum + Number(item.salePrice || 0), 0)
  return (total / items.value.length).toFixed(2)
})
const querySummary = computed(() => `关键词：${query.keyword || '全部'} / 每页：${pageSize.value}`)
const totalPackItemQuantity = computed(() => packItems.value.reduce((sum, item) => sum + Number(item.quantity || 0), 0))
const couponTemplateSelectOptions = computed(() => couponTemplateOptions.value.map((template) => ({
  value: template.id,
  label: `${template.name} / ${templateTypeMap[template.templateType] || '券模板'}`,
})))

const normalizeDateTime = (value?: string) => (value ? value.replace('T', ' ') : undefined)
const toDateTimeLocal = (value?: string) => (value ? value.slice(0, 16).replace(' ', 'T') : undefined)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
  packItems.value = []
  originalPackItems.value = []
}

const loadCouponTemplateOptions = async () => {
  const response = await getCouponTemplateList({ keyword: templateKeyword.value || undefined, pageIndex: 1, pageSize: 50 })
  couponTemplateOptions.value = response.data.items
}

const searchCouponTemplates = async () => {
  await loadCouponTemplateOptions()
}

const appendPackItem = () => {
  packItems.value.push(createEditablePackItem())
}

const removePackItem = (index: number) => {
  packItems.value.splice(index, 1)
}

const loadPackItems = async (couponPackId: number) => {
  const response = await getCouponPackItemList(couponPackId)
  originalPackItems.value = response.data
  packItems.value = response.data.map((item) => createEditablePackItem(item))
}

const validatePackItems = (): EditablePackItem[] => {
  const cleanedItems = packItems.value
    .map((item) => ({ ...item, couponTemplateId: Number(item.couponTemplateId || 0), quantity: Number(item.quantity || 0) }))
    .filter((item) => item.couponTemplateId > 0 && item.quantity > 0)

  if (cleanedItems.length === 0) {
    throw new Error('请至少配置 1 条券包明细')
  }

  const templateIds = new Set<number>()
  for (const item of cleanedItems) {
    if (templateIds.has(item.couponTemplateId)) {
      throw new Error('同一券模板请只配置一条明细')
    }
    templateIds.add(item.couponTemplateId)
  }

  return cleanedItems
}

const syncPackItems = async (couponPackId: number, cleanedItems: EditablePackItem[]) => {
  const originalMap = new Map(originalPackItems.value.map((item) => [item.id, item]))
  const nextIds = new Set<number>()

  for (const item of cleanedItems) {
    if (item.id) {
      nextIds.add(item.id)
      const original = originalMap.get(item.id)
      if (!original || original.couponTemplateId !== item.couponTemplateId || original.quantity !== item.quantity) {
        await updateCouponPackItem(item.id, { couponPackId, couponTemplateId: item.couponTemplateId, quantity: item.quantity })
      }
      continue
    }

    await saveCouponPackItem({ couponPackId, couponTemplateId: item.couponTemplateId, quantity: item.quantity })
  }

  for (const item of originalPackItems.value) {
    if (!nextIds.has(item.id)) {
      await deleteCouponPackItem(item.id)
    }
  }
}

const loadData = async () => {
  try {
    const response = await getCouponPackList({
      keyword: query.keyword || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })

    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载券包列表失败'))
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
  notify.info('已重置券包筛选条件')
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
  editingId.value = null
  resetForm()
  appendPackItem()
  dialogVisible.value = true
}

const openEditDialog = async (item: CouponPackListItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    salePrice: item.salePrice,
    status: item.status,
    perUserLimit: item.perUserLimit,
    saleStartTime: toDateTimeLocal(item.saleStartTime),
    saleEndTime: toDateTimeLocal(item.saleEndTime),
    remark: item.remark || '',
  })
  try {
    await loadPackItems(item.id)
  } catch (error) {
    notify.error(getErrorMessage(error, '加载券包明细失败'))
    packItems.value = []
    originalPackItems.value = []
  }
  dialogVisible.value = true
}

const closeDialog = () => {
  dialogVisible.value = false
  editingId.value = null
  resetForm()
}

const buildPayload = (): SaveCouponPackRequest => ({
  ...form,
  saleStartTime: normalizeDateTime(form.saleStartTime),
  saleEndTime: normalizeDateTime(form.saleEndTime),
})

const submit = async () => {
  if (submitting.value) return
  if (!form.name?.trim()) return notify.info('请输入券包名称')
  if (!form.salePrice || Number(form.salePrice) <= 0) return notify.info('售价必须大于 0')
  if (!form.perUserLimit || Number(form.perUserLimit) <= 0) return notify.info('每人限购必须大于 0')

  let cleanedItems: EditablePackItem[]
  try {
    cleanedItems = validatePackItems()
  } catch (error) {
    return notify.info(getErrorMessage(error, '券包明细校验失败'))
  }

  submitting.value = true
  let couponPackId = editingId.value ?? 0
  let packCreated = false
  try {
    const payload = buildPayload()

    if (editingId.value) {
      await updateCouponPack(editingId.value, payload)
    } else {
      const response = await createCouponPack(payload)
      couponPackId = response.data
      packCreated = true
    }

    try {
      await syncPackItems(couponPackId, cleanedItems)
    } catch (syncError) {
      if (packCreated) {
        editingId.value = couponPackId
        originalPackItems.value = []
        notify.error(getErrorMessage(syncError, '券包已创建，但明细同步失败，请在当前窗口补全后再次保存'))
      } else {
        notify.error(getErrorMessage(syncError, '券包明细保存失败，请重试'))
      }
      return
    }

    if (packCreated) {
      pageIndex.value = 1
    }
    notify.success(editingId.value && !packCreated ? '券包修改成功' : '券包创建成功')
    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '券包修改失败' : '券包创建失败'))
  } finally {
    submitting.value = false
  }
}

const removeItem = async (item: CouponPackListItemDto) => {
  if (!window.confirm(`确认删除券包“${item.name}”吗？`)) {
    return
  }

  if (deleting.value) return
  deleting.value = true

  if (items.value.length === 1 && pageIndex.value > 1) {
    pageIndex.value -= 1
  }

  try {
    await deleteCouponPack(item.id)
    await loadData()
    notify.success('券包删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '券包删除失败'))
  } finally {
    deleting.value = false
  }
}

const formatAmount = (value?: number) => `¥${Number(value || 0).toFixed(2)}`
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
onMounted(loadCouponTemplateOptions)
</script>

<style scoped>
.coupon-pack-page-v2 {
  gap: 22px;
}

.coupon-pack-hero {
  background:
    radial-gradient(circle at top right, rgba(59, 130, 246, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%);
}




.coupon-pack-filter-grid {
  grid-template-columns: 1.4fr 0.8fr 1fr;
}





.pack-dialog-card {
  width: min(1080px, calc(100vw - 48px));
}

.dialog-section {
  display: grid;
  gap: 16px;
}

.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.dialog-form label {
  display: grid;
  gap: 8px;
}

.dialog-form label span {
  font-size: 13px;
  font-weight: 700;
  color: #475467;
}

.section-head.compact {
  margin-bottom: 0;
}

.pack-item-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
}

.pack-item-summary {
  display: grid;
  gap: 4px;
  color: var(--muted);
}

.pack-item-summary strong {
  color: var(--text);
}

.pack-item-list {
  display: grid;
  gap: 12px;
}

.pack-item-row {
  display: grid;
  grid-template-columns: 44px minmax(0, 1fr) 120px auto;
  gap: 12px;
  align-items: start;
  padding: 14px;
  border: 1px solid rgba(228, 231, 236, 0.96);
  border-radius: 16px;
  background: linear-gradient(180deg, #fff 0%, #fbfdff 100%);
}

.pack-item-index {
  display: grid;
  place-items: center;
  height: 44px;
  border-radius: 12px;
  background: #f4f7fb;
  font-weight: 700;
}

.pack-item-quantity {
  width: 100%;
  height: 44px;
  padding: 0 14px;
  border: 1px solid var(--line-strong);
  border-radius: 12px;
  background: #fff;
}

.empty-pack-items {
  padding: 16px;
  border: 1px dashed var(--line-strong);
  border-radius: 16px;
  color: var(--muted);
  background: #f8fafc;
}

@media (max-width: 1100px) {
  .hero-side-grid,
  .coupon-pack-filter-grid,
  .dialog-form {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 960px) {
  .pack-item-row {
    grid-template-columns: 1fr;
  }

  .pack-item-index {
    width: 44px;
  }
}
</style>
