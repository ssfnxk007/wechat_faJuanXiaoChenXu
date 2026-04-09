<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>门店管理</h2>
        <p>维护可核销门店基础资料，支持弹窗编辑、删除和服务端分页。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">新增门店</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">门店总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">服务端分页统计总记录数</span>
      </article>
      <article class="stat-card">
        <span class="label">当前页</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card">
        <span class="label">启用门店</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页启用状态统计</span>
      </article>
      <article class="stat-card">
        <span class="label">查询条件</span>
        <strong class="stat-value">{{ query.keyword || '全部' }}</strong>
        <span class="stat-footnote">按编码或名称搜索</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>筛选与统计</h3>
          <p class="section-tip">使用服务端分页查询，统一第四轮弹窗编辑与删除操作体验。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">总数 {{ totalCount }}</span>
          <span class="badge success">第 {{ pageIndex }} / {{ totalPages }} 页</span>
        </div>
      </div>
      <div class="filter-grid">
        <input v-model.trim="query.keyword" type="text" placeholder="搜索门店编码或名称" @keyup.enter="handleSearch" />
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
          <h3>门店列表</h3>
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
              <th>门店编码</th>
              <th>门店名称</th>
              <th>联系人</th>
              <th>联系电话</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td class="cell-mono">{{ item.code }}</td>
              <td>{{ item.name }}</td>
              <td>{{ item.contactName || '-' }}</td>
              <td>{{ item.contactPhone || '-' }}</td>
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
            <h3>{{ editingId ? '编辑门店' : '新增门店' }}</h3>
            <p>{{ editingId ? '修改门店基础信息并即时刷新列表。' : '录入新门店资料并保存。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.trim="form.code" type="text" placeholder="门店编码" />
          <input v-model.trim="form.name" type="text" placeholder="门店名称" />
          <input v-model.trim="form.contactName" type="text" placeholder="联系人" />
          <input v-model.trim="form.contactPhone" type="text" placeholder="联系电话" />
          <label class="checkbox-field">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用门店</span>
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
import { createStore, deleteStore, getStoreList, updateStore } from '@/api/store'
import type { SaveStoreRequest, StoreListItemDto } from '@/types/store'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<StoreListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)

const query = reactive({
  keyword: '',
})

const createEmptyForm = (): SaveStoreRequest => ({
  code: '',
  name: '',
  contactName: '',
  contactPhone: '',
  isEnabled: true,
})

const form = reactive<SaveStoreRequest>(createEmptyForm())

const canCreate = authStorage.hasPermission('store.create')
const canEdit = authStorage.hasPermission('store.edit')
const canDelete = authStorage.hasPermission('store.delete')
const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const querySummary = computed(() => `关键词：${query.keyword || '全部'} / 每页：${pageSize.value}`)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
}

const loadData = async () => {
  try {
    const response = await getStoreList({
      keyword: query.keyword || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })

    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载门店列表失败'))
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
  notify.info('已重置门店筛选条件')
}

const handlePageSizeChange = async () => {
  pageIndex.value = 1
  await loadData()
}

const goPrevPage = async () => {
  if (pageIndex.value <= 1) {
    return
  }
  pageIndex.value -= 1
  await loadData()
}

const goNextPage = async () => {
  if (pageIndex.value >= totalPages.value) {
    return
  }
  pageIndex.value += 1
  await loadData()
}

const openCreateDialog = () => {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

const openEditDialog = (item: StoreListItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    code: item.code,
    name: item.name,
    contactName: item.contactName || '',
    contactPhone: item.contactPhone || '',
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
      await updateStore(editingId.value, { ...form })
      notify.success('门店修改成功')
    } else {
      await createStore({ ...form })
      pageIndex.value = 1
      notify.success('门店创建成功')
    }

    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '门店修改失败' : '门店创建失败'))
  }
}

const removeItem = async (item: StoreListItemDto) => {
  if (!window.confirm(`确认删除门店“${item.name}”吗？`)) {
    return
  }

  if (items.value.length === 1 && pageIndex.value > 1) {
    pageIndex.value -= 1
  }

  try {
    await deleteStore(item.id)
    await loadData()
    notify.success('门店删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '门店删除失败'))
  }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>

<style scoped>
.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
</style>
