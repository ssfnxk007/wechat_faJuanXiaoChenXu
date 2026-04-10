<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>券包管理</h2>
        <p>维护可售卖券包资料，支持弹窗编辑、删除和服务端分页。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">新增券包</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">券包总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">服务端分页统计总记录数</span>
      </article>
      <article class="stat-card">
        <span class="label">当前页</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card">
        <span class="label">启用券包</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页启用中的券包</span>
      </article>
      <article class="stat-card">
        <span class="label">平均售价</span>
        <strong class="stat-value">{{ averagePrice }}</strong>
        <span class="stat-footnote">当前页券包平均售价</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>筛选与统计</h3>
          <p class="section-tip">使用服务端关键字查询与分页，统一第四轮弹窗编辑与删除操作体验。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">关键词查询</span>
          <span class="badge success">第 {{ pageIndex }} / {{ totalPages }} 页</span>
        </div>
      </div>
      <div class="filter-grid">
        <input v-model.trim="query.keyword" type="text" placeholder="搜索券包名称" @keyup.enter="handleSearch" />
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
          <h3>券包列表</h3>
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
              <th>售价</th>
              <th>状态</th>
              <th>限购</th>
              <th>备注</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>{{ item.name }}</td>
              <td>{{ formatAmount(item.salePrice) }}</td>
              <td>
                <span :class="['status-badge', item.status === 1 ? 'success' : 'danger']">
                  {{ item.status === 1 ? '启用' : '停用' }}
                </span>
              </td>
              <td>{{ item.perUserLimit }}</td>
              <td>{{ item.remark || '-' }}</td>
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
            <h3>{{ editingId ? '编辑券包' : '新增券包' }}</h3>
            <p>{{ editingId ? '修改券包资料并同步列表。' : '录入新券包资料并保存。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.trim="form.name" type="text" placeholder="券包名称" />
          <input v-model.number="form.salePrice" type="number" min="0.01" step="0.01" placeholder="售价" />
          <input v-model.number="form.perUserLimit" type="number" min="1" placeholder="每人限购" />
          <select v-model.number="form.status">
            <option :value="1">启用</option>
            <option :value="0">停用</option>
          </select>
          <input v-model="form.saleStartTime" type="datetime-local" placeholder="销售开始时间" />
          <input v-model="form.saleEndTime" type="datetime-local" placeholder="销售结束时间" />
          <input v-model.trim="form.remark" type="text" placeholder="备注" />
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">取消</button>
          <button v-if="editingId ? canEdit : canCreate" type="button" :disabled="submitting || deleting" @click="submit">{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { createCouponPack, deleteCouponPack, getCouponPackList, updateCouponPack } from '@/api/coupon-pack'
import type { CouponPackListItemDto, SaveCouponPackRequest } from '@/types/coupon-pack'
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

const query = reactive({
  keyword: '',
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

const canCreate = authStorage.hasPermission('coupon-pack.create')
const canEdit = authStorage.hasPermission('coupon-pack.edit')
const canDelete = authStorage.hasPermission('coupon-pack.delete')
const enabledCount = computed(() => items.value.filter((item) => item.status === 1).length)
const averagePrice = computed(() => {
  if (!items.value.length) return '0.00'
  const total = items.value.reduce((sum, item) => sum + Number(item.salePrice || 0), 0)
  return (total / items.value.length).toFixed(2)
})
const querySummary = computed(() => `关键词：${query.keyword || '全部'} / 每页：${pageSize.value}`)

const normalizeDateTime = (value?: string) => (value ? value.replace('T', ' ') : undefined)
const toDateTimeLocal = (value?: string) => (value ? value.slice(0, 16).replace(' ', 'T') : undefined)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
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
  dialogVisible.value = true
}

const openEditDialog = (item: CouponPackListItemDto) => {
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
  submitting.value = true
  try {
    const payload = buildPayload()

    if (editingId.value) {
      await updateCouponPack(editingId.value, payload)
      notify.success('券包修改成功')
    } else {
      await createCouponPack(payload)
      pageIndex.value = 1
      notify.success('券包创建成功')
    }

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

const formatAmount = (value?: number) => (value ? value.toFixed(2) : '-')
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>

<style scoped>
.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
</style>
