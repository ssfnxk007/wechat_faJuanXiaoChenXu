<template>
  <div class="admin-page coupon-pack-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="cp-keyword">券包名称</label>
          <input
            id="cp-keyword"
            v-model.trim="query.keyword"
            type="text"
            placeholder="输入券包名称后回车检索"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="field">
          <label for="cp-page-size">每页条数</label>
          <select id="cp-page-size" v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">10 条</option>
            <option :value="20">20 条</option>
            <option :value="50">50 条</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="handleSearch">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button v-if="canCreate" type="button" class="primary-button compact" @click="openCreateDialog">+ 新增券包</button>
        <button type="button" class="ghost-button compact" @click="loadData">刷新</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>券包列表</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="data-card-meta">{{ querySummary }}</div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 220px;">券包信息</th>
              <th style="min-width: 130px;">售价 / 限购</th>
              <th style="min-width: 84px;">状态</th>
              <th style="min-width: 200px;">售卖时间</th>
              <th style="min-width: 156px;">创建时间</th>
              <th style="min-width: 110px;">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td>{{ item.id }}</td>
              <td>
                <div class="cell-stack">
                  <strong>{{ item.name }}</strong>
                  <span class="muted-line">{{ item.remark || '未设置备注说明' }}</span>
                </div>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ formatAmount(item.salePrice) }}</strong>
                  <span class="muted-line">每人限购 {{ item.perUserLimit }} 次</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', item.status === 1 ? 'success' : 'danger']">
                  {{ item.status === 1 ? '启用' : '停用' }}
                </span>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ formatDate(item.saleStartTime) }}</strong>
                  <span class="muted-line">至 {{ formatDate(item.saleEndTime) }}</span>
                </div>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <button v-if="canEdit" type="button" class="cell-link" @click="openEditDialog(item)">编辑</button>
                <button v-if="canDelete" type="button" class="cell-link danger" @click="removeItem(item)">删除</button>
              </td>
            </tr>
            <tr v-if="items.length === 0" class="empty-row">
              <td colspan="7">当前没有符合条件的券包记录</td>
            </tr>
          </tbody>
        </table>
      </div>

      <footer class="pager-compact">
        <div class="pager-info">第 {{ pageIndex }} / {{ totalPages }} 页 · 共 {{ totalCount }} 条</div>
        <div class="pager-actions">
          <button type="button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </footer>
    </section>

    <MainDetailDialog
      v-if="dialogVisible"
      :title="editingId ? '编辑券包' : '新增券包'"
      :sub="editingId ? '在一个窗口内同时调整券包资料和券包明细' : '先填写券包资料，再直接配置包含的券模板组合'"
      size="xl"
      @close="closeDialog"
    >
      <section class="detail-section">
        <header class="detail-section-head">
          <h4>券包属性</h4>
          <span class="detail-section-tip">名称、售价、状态和售卖时间都在这里维护</span>
        </header>
        <div class="dialog-form-grid">
          <label class="dialog-field">
            <span>券包名称</span>
            <input v-model.trim="form.name" type="text" placeholder="输入券包名称" />
          </label>
          <label class="dialog-field">
            <span>售价</span>
            <input v-model.number="form.salePrice" type="number" min="0.01" step="0.01" placeholder="输入售价" />
          </label>
          <label class="dialog-field">
            <span>每人限购</span>
            <input v-model.number="form.perUserLimit" type="number" min="1" placeholder="输入限购次数" />
          </label>
          <label class="dialog-field">
            <span>状态</span>
            <select v-model.number="form.status">
              <option :value="1">启用</option>
              <option :value="0">停用</option>
            </select>
          </label>
          <label class="dialog-field">
            <span>销售开始时间</span>
            <input v-model="form.saleStartTime" type="datetime-local" />
          </label>
          <label class="dialog-field">
            <span>销售结束时间</span>
            <input v-model="form.saleEndTime" type="datetime-local" />
          </label>
          <label class="dialog-field field-span-2">
            <span>备注</span>
            <input v-model.trim="form.remark" type="text" placeholder="补充售卖规则或投放说明" />
          </label>
        </div>
      </section>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>券包明细</h4>
          <span class="detail-section-tip">条目 {{ packItems.length }} · 总数量 {{ totalPackItemQuantity }}</span>
          <div class="head-actions">
            <button type="button" class="ghost-button compact" @click="appendPackItem">新增一条明细</button>
          </div>
        </header>
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
            <button type="button" class="cell-link danger" @click="removePackItem(index)">删除</button>
          </div>
          <div v-if="packItems.length === 0" class="detail-empty">
            当前还没有配置券包明细。至少添加 1 条券模板，支付成功后才能正常发券。
          </div>
        </div>
      </section>

      <template #footer>
        <button type="button" class="ghost-button compact" :disabled="submitting || deleting" @click="closeDialog">取消</button>
        <button
          v-if="editingId ? canEdit : canCreate"
          type="button"
          class="primary-button compact"
          :disabled="submitting || deleting"
          @click="submit"
        >{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button>
      </template>
    </MainDetailDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import RemoteSelectField from '@/components/RemoteSelectField.vue'
import MainDetailDialog from '@/components/MainDetailDialog.vue'
import { createCouponPack, deleteCouponPack, getCouponPackList, updateCouponPack } from '@/api/coupon-pack'
import { getCouponTemplateList } from '@/api/coupon-template'
import { deleteCouponPackItem, getCouponPackItemList, saveCouponPackItem, updateCouponPackItem } from '@/api/coupon-pack-item'
import type { CouponTemplateListItemDto } from '@/types/coupon'
import type { CouponPackListItemDto, SaveCouponPackRequest } from '@/types/coupon-pack'
import type { CouponPackItemDto } from '@/types/coupon-pack-item'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils/auth'
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

const query = reactive({ keyword: '' })

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
const templateTypeMap: Record<number, string> = { 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }
const querySummary = computed(() => `关键词：${query.keyword || '全部'} · 每页 ${pageSize.value} 条`)
const totalPackItemQuantity = computed(() => packItems.value.reduce((sum, item) => sum + Number(item.quantity || 0), 0))
const couponTemplateSelectOptions = computed(() => couponTemplateOptions.value.map((template) => ({
  value: template.id,
  label: `${template.name} / ${template.isSystemProductVoucher ? '商品提货券' : (templateTypeMap[template.templateType] || '券模板')}`,
})))

const normalizeDateTime = (value?: string) => {
  const normalized = value?.trim()
  return normalized || undefined
}
const toDateTimeLocal = (value?: string) => (value ? value.slice(0, 16).replace(' ', 'T') : undefined)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
  packItems.value = []
  originalPackItems.value = []
}

const loadCouponTemplateOptions = async () => {
  const response = await getCouponTemplateList({
    keyword: templateKeyword.value || undefined,
    includeSystemProductVoucher: true,
    pageIndex: 1,
    pageSize: 50,
  })
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

const buildPayload = (): SaveCouponPackRequest => {
  const payload: SaveCouponPackRequest = {
    name: form.name,
    imageAssetId: form.imageAssetId,
    salePrice: form.salePrice,
    status: form.status,
    perUserLimit: form.perUserLimit,
    remark: form.remark,
  }

  const saleStartTime = normalizeDateTime(form.saleStartTime)
  const saleEndTime = normalizeDateTime(form.saleEndTime)

  if (saleStartTime) {
    payload.saleStartTime = saleStartTime
  }

  if (saleEndTime) {
    payload.saleEndTime = saleEndTime
  }

  return payload
}

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
  if (!window.confirm(`确认删除券包"${item.name}"吗？`)) {
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

const notifyColumnSettingsPlaceholder = () => {
  notify.info('列设置功能将在下一版本提供')
}

const formatAmount = (value?: number) => `¥${Number(value || 0).toFixed(2)}`
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
onMounted(loadCouponTemplateOptions)
</script>

<style scoped>
.dialog-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  padding: 12px 14px;
}

.dialog-field {
  display: grid;
  gap: 6px;
}

.dialog-field > span {
  font-size: 13px;
  font-weight: 600;
  color: #344054;
}

.dialog-field input[type='text'],
.dialog-field input[type='number'],
.dialog-field input[type='datetime-local'],
.dialog-field select {
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
  width: 100%;
}

.cell-link.danger {
  margin-left: 12px;
}

.head-actions {
  margin-left: auto;
  display: flex;
  gap: 8px;
}

.pack-item-list {
  display: grid;
  gap: 8px;
  padding: 12px 14px;
}

.pack-item-row {
  display: grid;
  grid-template-columns: 36px minmax(0, 1fr) 110px auto;
  gap: 10px;
  align-items: center;
  padding: 8px 10px;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: #fcfdff;
}

.pack-item-index {
  display: grid;
  place-items: center;
  height: 32px;
  border-radius: 6px;
  background: #f4f7fb;
  font-weight: 700;
  font-size: 13px;
  color: var(--text);
}

.pack-item-quantity {
  width: 100%;
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
}

@media (max-width: 1100px) {
  .dialog-form-grid { grid-template-columns: 1fr; }
}

@media (max-width: 720px) {
  .pack-item-row { grid-template-columns: 1fr; }
}
</style>
