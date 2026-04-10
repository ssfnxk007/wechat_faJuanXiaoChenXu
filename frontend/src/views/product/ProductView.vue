<template>
  <div class="business-page product-page">
    <div class="page-header-row">
      <div>
        <h2>商品管理</h2>
        <p>维护 ERP 商品映射、售价信息与指定商品券的基础商品数据。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">新增商品</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">商品总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">服务端分页统计总记录数</span>
      </article>
      <article class="stat-card">
        <span class="label">当前页</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card">
        <span class="label">启用商品</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页启用状态统计</span>
      </article>
      <article class="stat-card">
        <span class="label">均价</span>
        <strong class="stat-value">{{ averagePriceDisplay }}</strong>
        <span class="stat-footnote">当前页有售价商品的平均值</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>筛选与统计</h3>
          <p class="section-tip">支持按商品名称或 ERP 编码查询，并统一分页浏览。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">关键词查询</span>
          <span class="badge success">第 {{ pageIndex }} / {{ totalPages }} 页</span>
        </div>
      </div>

      <div class="filter-grid filter-grid-product">
        <input v-model.trim="query.keyword" type="text" placeholder="搜索商品名称或 ERP 编码" @keyup.enter="handleSearch" />
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

    <div class="card table-card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>商品列表</h3>
          <p class="section-tip">统一维护商品资料，供券模板配置和业务核销使用。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">总数 {{ totalCount }}</span>
          <span class="badge warning">每页 {{ pageSize }}</span>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>商品名称</th>
              <th>ERP 商品编码</th>
              <th>售价</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>{{ item.name }}</td>
              <td class="cell-mono">{{ item.erpProductCode }}</td>
              <td>{{ formatPrice(item.salePrice) }}</td>
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
              <td colspan="7" class="empty-text">暂无商品数据</td>
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
      <div class="dialog-card product-dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>{{ editingId ? '编辑商品' : '新增商品' }}</h3>
            <p>{{ editingId ? '更新商品基础资料并同步列表展示。' : '录入商品基础资料，供运营配置与核销链路使用。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.trim="form.name" type="text" placeholder="商品名称" />
          <input v-model.trim="form.erpProductCode" type="text" placeholder="ERP 商品编码" />
          <input v-model.number="form.salePrice" type="number" min="0" step="0.01" placeholder="售价" />
          <label class="checkbox-field">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用商品</span>
          </label>
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
import { createProduct, deleteProduct, getProductList, updateProduct } from '@/api/product'
import type { ProductListItemDto, SaveProductRequest } from '@/types/product'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<ProductListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)

const query = reactive({
  keyword: '',
})

const createEmptyForm = (): SaveProductRequest => ({
  name: '',
  erpProductCode: '',
  salePrice: undefined,
  isEnabled: true,
})

const form = reactive<SaveProductRequest>(createEmptyForm())

const canCreate = authStorage.hasPermission('product.create')
const canEdit = authStorage.hasPermission('product.edit')
const canDelete = authStorage.hasPermission('product.delete')

const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const averagePrice = computed(() => {
  const values = items.value
    .map((item) => Number(item.salePrice || 0))
    .filter((value) => value > 0)

  if (values.length === 0) {
    return null
  }

  return values.reduce((sum, value) => sum + value, 0) / values.length
})
const averagePriceDisplay = computed(() => (averagePrice.value === null ? '-' : `¥${averagePrice.value.toFixed(2)}`))
const querySummary = computed(() => `关键词：${query.keyword || '全部'} / 每页：${pageSize.value}`)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
}

const loadData = async () => {
  try {
    const response = await getProductList({
      keyword: query.keyword || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })

    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载商品列表失败'))
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
  notify.info('已重置商品筛选条件')
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

const openEditDialog = (item: ProductListItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    erpProductCode: item.erpProductCode,
    salePrice: item.salePrice,
    isEnabled: item.isEnabled,
  })
  dialogVisible.value = true
}

const closeDialog = () => {
  dialogVisible.value = false
  editingId.value = null
  resetForm()
}

const submit = async () => {
  try {
    if (editingId.value) {
      await updateProduct(editingId.value, { ...form })
      notify.success('商品修改成功')
    } else {
      await createProduct({ ...form })
      pageIndex.value = 1
      notify.success('商品创建成功')
    }

    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '商品修改失败' : '商品创建失败'))
  }
}

const removeItem = async (item: ProductListItemDto) => {
  if (!window.confirm(`确认删除商品“${item.name}”吗？`)) {
    return
  }

  if (items.value.length === 1 && pageIndex.value > 1) {
    pageIndex.value -= 1
  }

  try {
    await deleteProduct(item.id)
    await loadData()
    notify.success('商品删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '商品删除失败'))
  }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const formatPrice = (value?: number) => (value !== undefined ? `¥${value.toFixed(2)}` : '-')

onMounted(loadData)
</script>

<style scoped>
.filter-grid-product {
  grid-template-columns: minmax(260px, 1.4fr) 220px minmax(280px, 1fr) auto;
}

.product-dialog-card {
  max-width: 760px;
}

.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
</style>
