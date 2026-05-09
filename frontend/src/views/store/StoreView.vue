<template>
  <div class="admin-page store-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="store-keyword">搜索门店</label>
          <input
            id="store-keyword"
            v-model.trim="query.keyword"
            type="text"
            placeholder="门店编码或门店名称"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="field">
          <label for="store-page-size">每页条数</label>
          <select id="store-page-size" v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">10 条</option>
            <option :value="20">20 条</option>
            <option :value="50">50 条</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="handleSearch">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button v-if="canCreate" type="button" class="primary-button compact" @click="openCreateDialog">+ 新增门店</button>
        <button type="button" class="ghost-button compact" @click="loadData">刷新</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>门店列表</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="data-card-meta">{{ querySummary }}</div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 200px;">门店信息</th>
              <th style="min-width: 120px;">联系人</th>
              <th style="min-width: 120px;">联系电话</th>
              <th style="min-width: 84px;">状态</th>
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
                  <span class="muted-line cell-mono">{{ item.code }}</span>
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
                <button v-if="canEdit" type="button" class="cell-link" @click="openEditDialog(item)">编辑</button>
                <button v-if="canDelete" type="button" class="cell-link danger" @click="removeItem(item)">删除</button>
              </td>
            </tr>
            <tr v-if="items.length === 0" class="empty-row">
              <td colspan="7">当前没有符合条件的门店记录</td>
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
      :title="editingId ? '编辑门店' : '新增门店'"
      :sub="editingId ? '维护门店基础资料并同步刷新列表' : '录入新的门店资料，纳入发券与核销适用范围'"
      size="md"
      @close="closeDialog"
    >
      <div class="dialog-form-grid">
        <label class="dialog-field">
          <span>门店编码</span>
          <input v-model.trim="form.code" type="text" placeholder="请输入门店编码" />
        </label>
        <label class="dialog-field">
          <span>门店名称</span>
          <input v-model.trim="form.name" type="text" placeholder="请输入门店名称" />
        </label>
        <label class="dialog-field">
          <span>联系人</span>
          <input v-model.trim="form.contactName" type="text" placeholder="请输入联系人" />
        </label>
        <label class="dialog-field">
          <span>联系电话</span>
          <input v-model.trim="form.contactPhone" type="text" placeholder="请输入联系电话" />
        </label>
        <label class="dialog-field field-span-2 checkbox-row">
          <input v-model="form.isEnabled" type="checkbox" />
          <span>启用门店（停用后不再参与发券适用范围与核销执行）</span>
        </label>
      </div>

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
import MainDetailDialog from '@/components/MainDetailDialog.vue'
import { createStore, deleteStore, getStoreList, updateStore } from '@/api/store'
import type { SaveStoreRequest, StoreListItemDto } from '@/types/store'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils/auth'
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

const query = reactive({ keyword: '' })

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
const querySummary = computed(() => `关键词：${query.keyword || '全部'} · 每页 ${pageSize.value} 条`)

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
  if (submitting.value) return

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
  if (!window.confirm(`确认删除门店"${item.name}"吗？`)) return

  if (items.value.length === 1 && pageIndex.value > 1) {
    pageIndex.value -= 1
  }

  if (deleting.value) return

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

const notifyColumnSettingsPlaceholder = () => {
  notify.info('列设置功能将在下一版本提供')
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>

<style scoped>
.dialog-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
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
.dialog-field input[type='number'] {
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
  width: 100%;
}

.checkbox-row {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  display: flex;
}

.checkbox-row input[type='checkbox'] {
  width: 16px;
  height: 16px;
  margin: 0;
}

.cell-link.danger {
  margin-left: 12px;
}

@media (max-width: 720px) {
  .dialog-form-grid { grid-template-columns: 1fr; }
}
</style>
