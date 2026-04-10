<template>
  <div class="business-page store-page-v2">
    <div class="page-header-row">
      <div>
        <h2>门店管理</h2>
        <p>维护可参与发券、领券与核销链路的门店基础资料，支持分页查询、弹窗编辑与状态管理。</p>
      </div>
      <div class="toolbar-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">新增门店</button>
      </div>
    </div>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">门店总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">服务端分页返回的门店总记录数</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">当前页码</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">启用门店</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页中处于启用状态的门店数量</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">查询条件</span>
        <strong class="stat-value">{{ query.keyword || '全部' }}</strong>
        <span class="stat-footnote">按门店编码或门店名称搜索</span>
      </article>
    </section>

    <section class="card toolbar-card card-v2">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>筛选与分页</h3>
          <p class="section-tip">按关键词进行服务端查询，并结合页大小控制列表密度。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">总数 {{ totalCount }}</span>
          <span class="badge success">第 {{ pageIndex }} / {{ totalPages }} 页</span>
        </div>
      </div>

      <div class="filter-grid store-filter-grid">
        <input v-model.trim="query.keyword" type="text" placeholder="搜索门店编码或门店名称" />
        <select v-model.number="pageSize" @change="handlePageSizeChange">
          <option :value="10">每页 10 条</option>
          <option :value="20">每页 20 条</option>
          <option :value="50">每页 50 条</option>
        </select>
      </div>

      <div class="toolbar-actions">
        <button type="button" @click="handleSearch">执行查询</button>
        <button type="button" class="ghost-button" @click="resetQuery">重置条件</button>
      </div>
    </section>

    <section class="card data-card card-v2">
      <div class="section-head">
        <div class="section-head-main">
          <h3>门店列表</h3>
          <p class="section-tip">统一查看门店编码、联系人、启用状态与创建时间，支撑券适用门店范围配置。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">{{ querySummary }}</span>
        </div>
      </div>

      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>门店信息</th>
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
              <td>
                <div class="table-primary-cell">
                  <strong>{{ item.name }}</strong>
                  <span>{{ item.code }}</span>
                </div>
              </td>
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
              <td colspan="7" class="empty-text">暂无门店数据</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager pager-v2">
        <div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条记录</div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>{{ editingId ? '编辑门店' : '新增门店' }}</h3>
            <p>{{ editingId ? '维护门店基础资料并同步刷新列表。' : '录入新的门店资料，纳入发券与核销适用范围。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.trim="form.code" type="text" placeholder="请输入门店编码" />
          <input v-model.trim="form.name" type="text" placeholder="请输入门店名称" />
          <input v-model.trim="form.contactName" type="text" placeholder="请输入联系人" />
          <input v-model.trim="form.contactPhone" type="text" placeholder="请输入联系电话" />
          <label class="checkbox-field field-span-2">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用门店</span>
          </label>
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
const submitting = ref(false)
const deleting = ref(false)

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
  if (submitting.value) {
    return
  }

  submitting.value = true
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
  } finally {
    submitting.value = false
  }
}

const removeItem = async (item: StoreListItemDto) => {
  if (!window.confirm(`确认删除门店“${item.name}”吗？`)) {
    return
  }

  if (items.value.length === 1 && pageIndex.value > 1) {
    pageIndex.value -= 1
  }

  if (deleting.value) {
    return
  }

  deleting.value = true
  try {
    await deleteStore(item.id)
    await loadData()
    notify.success('门店删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '门店删除失败'))
  } finally {
    deleting.value = false
  }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>

<style scoped>
.store-filter-grid {
  grid-template-columns: 2fr 1fr;
}

.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

@media (max-width: 1280px) {
  .store-filter-grid,
  .dialog-form {
    grid-template-columns: 1fr;
  }
}
</style>
