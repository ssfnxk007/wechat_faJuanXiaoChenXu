<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>账号管理</h2>
        <p>维护后台登录账号、显示名称、角色绑定与账号启停状态。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">新增账号</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">账号总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">当前后台账号数量</span>
      </article>
      <article class="stat-card">
        <span class="label">启用账号</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前可登录的后台账号</span>
      </article>
      <article class="stat-card">
        <span class="label">已分配角色</span>
        <strong class="stat-value">{{ assignedRoleCount }}</strong>
        <span class="stat-footnote">至少绑定一个角色的账号</span>
      </article>
      <article class="stat-card">
        <span class="label">角色选项</span>
        <strong class="stat-value">{{ roleOptions.length }}</strong>
        <span class="stat-footnote">当前可绑定的角色数量</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>筛选条件</h3>
          <p class="section-tip">支持按账号名搜索，并切换每页显示条数。</p>
        </div>
      </div>

      <div class="filter-grid">
        <input v-model.trim="query.keyword" type="text" placeholder="请输入账号或显示名称" @keyup.enter="handleSearch" />
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
          <h3>账号列表</h3>
          <p class="section-tip">查看账号、显示名称、角色绑定、启用状态与创建时间。</p>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>账号</th>
              <th>显示名称</th>
              <th>角色</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td class="cell-mono">{{ item.username }}</td>
              <td>{{ item.displayName }}</td>
              <td>{{ item.roleNames || '-' }}</td>
              <td>
                <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">
                  {{ item.isEnabled ? '启用' : '停用' }}
                </span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <div class="table-actions">
                  <button v-if="canEdit" type="button" class="action-button" :disabled="submitting || deletingId === item.id" @click="openEditDialog(item)">编辑</button>
                  <button v-if="canResetPassword" type="button" class="action-button" :disabled="submitting || deletingId === item.id" @click="handleResetPassword(item)">重置密码</button>
                  <button v-if="canDelete" type="button" class="action-button danger" :disabled="submitting || deletingId === item.id" @click="removeItem(item)">{{ deletingId === item.id ? '删除中...' : '删除' }}</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="7" class="empty-text">暂无账号数据</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager">
        <div class="pager-info">第 {{ pageIndex }} 页，共 {{ totalPages }} 页 / 共 {{ totalCount }} 条</div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </div>
    </div>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card wide">
        <div class="dialog-head">
          <div>
            <h3>{{ editingId ? '编辑账号' : '新增账号' }}</h3>
            <p>{{ editingId ? '修改账号资料、启用状态与角色绑定。' : '创建新的后台账号并分配角色。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.trim="form.username" type="text" placeholder="请输入登录账号" />
          <input v-model.trim="form.displayName" type="text" placeholder="请输入显示名称" />
          <input v-model.trim="form.password" type="password" :placeholder="editingId ? '如不修改密码可留空' : '请输入登录密码'" />
          <label class="field-span-2">
            <span class="field-label">角色绑定</span>
            <select v-model="form.roleIds" multiple>
              <option v-for="role in roleOptions" :key="role.id" :value="role.id">{{ role.name }}</option>
            </select>
          </label>
          <label class="checkbox-field field-span-2">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用账号</span>
          </label>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" :disabled="submitting" @click="closeDialog">取消</button>
          <button v-if="editingId ? canEdit : canCreate" type="button" :disabled="submitting" @click="submit">
            {{ submitting ? '提交中...' : (editingId ? '保存修改' : '创建账号') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { createAdminUser, deleteAdminUser, getAdminRoleList, getAdminUserList, resetAdminUserPassword, updateAdminUser } from '@/api/admin-permission'
import type { AdminRoleListItemDto, AdminUserListItemDto, SaveAdminUserRequest } from '@/types/admin-permission'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<AdminUserListItemDto[]>([])
const roleOptions = ref<AdminRoleListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const submitting = ref(false)
const deletingId = ref<number | null>(null)
const query = reactive({ keyword: '' })

const createEmptyForm = (): SaveAdminUserRequest => ({
  username: '',
  displayName: '',
  password: '',
  isEnabled: true,
  roleIds: [],
})

const form = reactive<SaveAdminUserRequest>(createEmptyForm())
const canCreate = authStorage.hasPermission('admin.user.create')
const canEdit = authStorage.hasPermission('admin.user.edit')
const canResetPassword = authStorage.hasPermission('admin.user.reset-password')
const canDelete = authStorage.hasPermission('admin.user.delete')
const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const assignedRoleCount = computed(() => items.value.filter((item) => item.roleIds.length > 0).length)
const querySummary = computed(() => `关键字：${query.keyword || '全部'} / 每页 ${pageSize.value} 条`)

const resetForm = () => Object.assign(form, createEmptyForm())

const loadRoleOptions = async () => {
  const response = await getAdminRoleList({ pageIndex: 1, pageSize: 200 })
  roleOptions.value = response.data.items
}

const loadData = async () => {
  try {
    const response = await getAdminUserList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载账号列表失败'))
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
  notify.info('筛选条件已重置')
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

const openEditDialog = (item: AdminUserListItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    username: item.username,
    displayName: item.displayName,
    password: '',
    isEnabled: item.isEnabled,
    roleIds: [...item.roleIds],
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
      await updateAdminUser(editingId.value, { ...form })
      notify.success('账号修改成功')
    } else {
      await createAdminUser({ ...form })
      pageIndex.value = 1
      notify.success('账号创建成功')
    }
    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '账号修改失败' : '账号创建失败'))
  } finally {
    submitting.value = false
  }
}

const handleResetPassword = async (item: AdminUserListItemDto) => {
  const password = window.prompt(`请输入 ${item.displayName} 的新密码`, '')
  if (!password) return

  try {
    await resetAdminUserPassword(item.id, { password })
    notify.success('密码重置成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '密码重置失败'))
  }
}

const removeItem = async (item: AdminUserListItemDto) => {
  if (deletingId.value === item.id) return
  if (!window.confirm(`确认删除账号“${item.displayName}”吗？`)) return
  if (items.value.length === 1 && pageIndex.value > 1) pageIndex.value -= 1

  deletingId.value = item.id
  try {
    await deleteAdminUser(item.id)
    await loadData()
    notify.success('账号删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '账号删除失败'))
  } finally {
    deletingId.value = null
  }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(async () => {
  try {
    await loadRoleOptions()
  } catch (error) {
    notify.error(getErrorMessage(error, '加载角色选项失败'))
  }
  await loadData()
})
</script>

<style scoped>
.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
</style>
