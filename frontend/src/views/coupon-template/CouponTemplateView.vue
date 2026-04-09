<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>券模板管理</h2>
        <p>维护新人券、无门槛券、商品券与满减券规则，支持弹窗编辑、删除和分页。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">新增券模板</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">模板总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">服务端分页统计总记录数</span>
      </article>
      <article class="stat-card">
        <span class="label">当前页</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card">
        <span class="label">启用模板</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页启用状态统计</span>
      </article>
      <article class="stat-card">
        <span class="label">新人券</span>
        <strong class="stat-value">{{ newUserTemplateCount }}</strong>
        <span class="stat-footnote">当前页新人券模板数量</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>筛选与统计</h3>
          <p class="section-tip">使用服务端关键字查询与分页，避免前端全量筛选。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">关键词查询</span>
          <span class="badge success">第 {{ pageIndex }} / {{ totalPages }} 页</span>
        </div>
      </div>
      <div class="filter-grid">
        <input v-model.trim="query.keyword" type="text" placeholder="搜索模板名称" @keyup.enter="handleSearch" />
        <select v-model.number="pageSize" @change="handlePageSizeChange">
          <option :value="10">每页 10 条</option>
          <option :value="20">每页 20 条</option>
          <option :value="50">每页 50 条</option>
        </select>
        <input :value="querySummary" type="text" readonly />
        <div class="toolbar-actions">
          <button type="button" @click="handleSearch">查询</button>
          <button type="button" class="ghost-button" @click="resetQuery">重置</button>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>券模板列表</h3>
          <p class="section-tip">支持新增、编辑、删除，删除后自动刷新当前分页数据。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">当前页 {{ items.length }}</span>
          <span class="badge warning">每页 {{ pageSize }}</span>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>名称</th>
              <th>类型</th>
              <th>有效期</th>
              <th>优惠</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>{{ item.name }}</td>
              <td><span class="badge info">{{ typeMap[item.templateType] }}</span></td>
              <td>{{ formatValidity(item) }}</td>
              <td>{{ formatAmount(item.discountAmount) }}</td>
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
              <td colspan="8" class="empty-text">暂无数据</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager">
        <div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条</div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </div>
    </div>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>{{ editingId ? '编辑券模板' : '新增券模板' }}</h3>
            <p>{{ editingId ? '修改模板规则并同步列表。' : '录入新券模板规则并保存。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.trim="form.name" type="text" placeholder="券模板名称" />
          <select v-model.number="form.templateType">
            <option :value="1">新人券</option>
            <option :value="2">无门槛券</option>
            <option :value="3">指定商品券</option>
            <option :value="4">满减券</option>
          </select>
          <select v-model.number="form.validPeriodType">
            <option :value="1">固定日期范围</option>
            <option :value="2">领取后 N 天有效</option>
          </select>
          <input v-model.number="form.discountAmount" type="number" min="0" step="0.01" placeholder="优惠金额" />
          <input v-model.number="form.thresholdAmount" type="number" min="0" step="0.01" placeholder="门槛金额" />
          <input v-if="form.validPeriodType === 2" v-model.number="form.validDays" type="number" min="1" placeholder="有效天数" />
          <input v-if="form.validPeriodType === 1" v-model="form.validFrom" type="datetime-local" placeholder="开始时间" />
          <input v-if="form.validPeriodType === 1" v-model="form.validTo" type="datetime-local" placeholder="结束时间" />
          <label class="checkbox-field">
            <input v-model="form.isNewUserOnly" type="checkbox" />
            <span>仅新人可领</span>
          </label>
          <label class="checkbox-field">
            <input v-model="form.isAllStores" type="checkbox" />
            <span>全部门店可用</span>
          </label>
          <label class="checkbox-field">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用模板</span>
          </label>
          <input v-model.number="form.perUserLimit" type="number" min="1" placeholder="每人限领" />
          <input v-model.trim="form.remark" type="text" placeholder="备注" />
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeDialog">取消</button>
          <button v-if="editingId ? canEdit : canCreate" type="button" @click="submit">{{ editingId ? '保存修改' : '保存新增' }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { createCouponTemplate, deleteCouponTemplate, getCouponTemplateList, updateCouponTemplate } from '@/api/coupon-template'
import type { CouponTemplateListItemDto, SaveCouponTemplateRequest } from '@/types/coupon'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<CouponTemplateListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)

const query = reactive({
  keyword: '',
})

const typeMap: Record<number, string> = {
  1: '新人券',
  2: '无门槛券',
  3: '指定商品券',
  4: '满减券',
}

const createEmptyForm = (): SaveCouponTemplateRequest => ({
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
})

const form = reactive<SaveCouponTemplateRequest>(createEmptyForm())

const canCreate = authStorage.hasPermission('coupon-template.create')
const canEdit = authStorage.hasPermission('coupon-template.edit')
const canDelete = authStorage.hasPermission('coupon-template.delete')
const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const newUserTemplateCount = computed(() => items.value.filter((item) => item.isNewUserOnly).length)
const querySummary = computed(() => `关键词：${query.keyword || '全部'} / 每页：${pageSize.value}`)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
}

const normalizeDateTime = (value?: string) => (value ? value.replace('T', ' ') : undefined)
const toDateTimeLocal = (value?: string) => (value ? value.slice(0, 16).replace(' ', 'T') : undefined)

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
  editingId.value = null
  resetForm()
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
    validFrom: toDateTimeLocal(item.validFrom),
    validTo: toDateTimeLocal(item.validTo),
    isNewUserOnly: item.isNewUserOnly,
    isAllStores: true,
    perUserLimit: 1,
    isEnabled: item.isEnabled,
    remark: '',
  })
  dialogVisible.value = true
}

const closeDialog = () => {
  dialogVisible.value = false
  editingId.value = null
  resetForm()
}

const buildPayload = (): SaveCouponTemplateRequest => ({
  ...form,
  validFrom: form.validPeriodType === 1 ? normalizeDateTime(form.validFrom) : undefined,
  validTo: form.validPeriodType === 1 ? normalizeDateTime(form.validTo) : undefined,
  validDays: form.validPeriodType === 2 ? form.validDays : undefined,
})

const submit = async () => {
  try {
    const payload = buildPayload()

    if (editingId.value) {
      await updateCouponTemplate(editingId.value, payload)
      notify.success('券模板修改成功')
    } else {
      await createCouponTemplate(payload)
      pageIndex.value = 1
      notify.success('券模板创建成功')
    }

    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '券模板修改失败' : '券模板创建失败'))
  }
}

const removeItem = async (item: CouponTemplateListItemDto) => {
  if (!window.confirm(`确认删除券模板“${item.name}”吗？`)) {
    return
  }

  if (items.value.length === 1 && pageIndex.value > 1) {
    pageIndex.value -= 1
  }

  try {
    await deleteCouponTemplate(item.id)
    await loadData()
    notify.success('券模板删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '券模板删除失败'))
  }
}

const formatAmount = (value?: number) => (value ? value.toFixed(2) : '-')
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const formatValidity = (item: CouponTemplateListItemDto) => {
  if (item.validPeriodType === 1) {
    return `${formatDate(item.validFrom)} ~ ${formatDate(item.validTo)}`
  }
  return `领取后 ${item.validDays ?? 0} 天`
}

onMounted(loadData)
</script>

<style scoped>
.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
</style>
