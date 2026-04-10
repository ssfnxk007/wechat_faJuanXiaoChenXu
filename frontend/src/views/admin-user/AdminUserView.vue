<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>????</h2>
        <p>??????????????????????????????</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">????</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">?????</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card"><span class="label">?????</span><strong class="stat-value">{{ totalCount }}</strong><span class="stat-footnote">?????????</span></article>
      <article class="stat-card"><span class="label">????</span><strong class="stat-value">{{ enabledCount }}</strong><span class="stat-footnote">????????</span></article>
      <article class="stat-card"><span class="label">?????</span><strong class="stat-value">{{ assignedRoleCount }}</strong><span class="stat-footnote">??????????</span></article>
      <article class="stat-card"><span class="label">????</span><strong class="stat-value">{{ roleOptions.length }}</strong><span class="stat-footnote">???????????</span></article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title"><h3>?????</h3><p class="section-tip">????????????????????????</p></div>
        <div class="summary-inline"><span class="badge info">?? {{ totalCount }}</span><span class="badge success">? {{ pageIndex }} / {{ totalPages }} ?</span></div>
      </div>
      <div class="filter-grid">
        <input v-model.trim="query.keyword" type="text" placeholder="???????" @keyup.enter="handleSearch" />
        <select v-model.number="pageSize" @change="handlePageSizeChange"><option :value="10">?? 10 ?</option><option :value="20">?? 20 ?</option><option :value="50">?? 50 ?</option></select>
        <input :value="querySummary" type="text" readonly />
        <div class="toolbar-actions"><button type="button" @click="handleSearch">??</button><button type="button" class="ghost-button" @click="resetQuery">??</button></div>
      </div>
    </div>

    <div class="card">
      <div class="section-head"><div class="section-head-main"><h3>?????</h3><p class="section-tip">?????????????????????????</p></div></div>
      <div class="table-wrap">
        <table class="table">
          <thead><tr><th>ID</th><th>??</th><th>??</th><th>??</th><th>??</th><th>????</th><th>??</th></tr></thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td><td class="cell-mono">{{ item.username }}</td><td>{{ item.displayName }}</td><td>{{ item.roleNames || '-' }}</td>
              <td><span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '??' : '??' }}</span></td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td><div class="table-actions"><button v-if="canEdit" type="button" class="action-button" :disabled="submitting || deletingId === item.id" @click="openEditDialog(item)">??</button><button v-if="canResetPassword" type="button" class="action-button" :disabled="submitting || deletingId === item.id" @click="handleResetPassword(item)">????</button><button v-if="canDelete" type="button" class="action-button danger" :disabled="submitting || deletingId === item.id" @click="removeItem(item)">{{ deletingId === item.id ? '???...' : '??' }}</button></div></td>
            </tr>
            <tr v-if="items.length === 0"><td colspan="7" class="empty-text">???????</td></tr>
          </tbody>
        </table>
      </div>
      <div class="pager"><div class="pager-info">? {{ pageIndex }} ??? {{ totalPages }} ? / ? {{ totalCount }} ?</div><div class="pager-actions"><button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">???</button><button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">???</button></div></div>
    </div>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card wide">
        <div class="dialog-head"><div><h3>{{ editingId ? '?????' : '?????' }}</h3><p>{{ editingId ? '?????????????' : '?????????????????' }}</p></div><button type="button" class="ghost-button" @click="closeDialog">??</button></div>
        <div class="grid-form dialog-form">
          <input v-model.trim="form.username" type="text" placeholder="?????" />
          <input v-model.trim="form.displayName" type="text" placeholder="?????" />
          <input v-model.trim="form.password" type="password" :placeholder="editingId ? '?????????' : '????'" />
          <label class="field-span-2"><span class="field-label">????</span><select v-model="form.roleIds" multiple><option v-for="role in roleOptions" :key="role.id" :value="role.id">{{ role.name }}</option></select></label>
          <label class="checkbox-field field-span-2"><input v-model="form.isEnabled" type="checkbox" /><span>????</span></label>
        </div>
        <div class="dialog-actions"><button type="button" class="ghost-button" :disabled="submitting" @click="closeDialog">??</button><button v-if="editingId ? canEdit : canCreate" type="button" :disabled="submitting" @click="submit">{{ submitting ? '???...' : (editingId ? '????' : '????') }}</button></div>
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
const createEmptyForm = (): SaveAdminUserRequest => ({ username: '', displayName: '', password: '', isEnabled: true, roleIds: [] })
const form = reactive<SaveAdminUserRequest>(createEmptyForm())
const canCreate = authStorage.hasPermission('admin.user.create')
const canEdit = authStorage.hasPermission('admin.user.edit')
const canResetPassword = authStorage.hasPermission('admin.user.reset-password')
const canDelete = authStorage.hasPermission('admin.user.delete')
const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const assignedRoleCount = computed(() => items.value.filter((item) => item.roleIds.length > 0).length)
const querySummary = computed(() => `????${query.keyword || '????'} / ?? ${pageSize.value} ?`)
const resetForm = () => Object.assign(form, createEmptyForm())
const loadRoleOptions = async () => { const response = await getAdminRoleList({ pageIndex: 1, pageSize: 200 }); roleOptions.value = response.data.items }
const loadData = async () => {
  try {
    const response = await getAdminUserList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) { notify.error(getErrorMessage(error, '?????????')) }
}
const handleSearch = async () => { pageIndex.value = 1; await loadData() }
const resetQuery = async () => { query.keyword = ''; pageSize.value = 10; pageIndex.value = 1; await loadData(); notify.info('??????????') }
const handlePageSizeChange = async () => { pageIndex.value = 1; await loadData() }
const goPrevPage = async () => { if (pageIndex.value <= 1) return; pageIndex.value -= 1; await loadData() }
const goNextPage = async () => { if (pageIndex.value >= totalPages.value) return; pageIndex.value += 1; await loadData() }
const openCreateDialog = () => { editingId.value = null; resetForm(); dialogVisible.value = true }
const openEditDialog = (item: AdminUserListItemDto) => { editingId.value = item.id; Object.assign(form, { username: item.username, displayName: item.displayName, password: '', isEnabled: item.isEnabled, roleIds: [...item.roleIds] }); dialogVisible.value = true }
const closeDialog = () => { dialogVisible.value = false; editingId.value = null; resetForm() }
const submit = async () => {
  if (submitting.value) return
  submitting.value = true
  try {
    if (editingId.value) { await updateAdminUser(editingId.value, { ...form }); notify.success('???????') }
    else { await createAdminUser({ ...form }); pageIndex.value = 1; notify.success('???????') }
    closeDialog(); await loadData()
  } catch (error) { notify.error(getErrorMessage(error, editingId.value ? '???????' : '???????')) }
  finally { submitting.value = false }
}
const handleResetPassword = async (item: AdminUserListItemDto) => {
  const password = window.prompt(`?????? ${item.displayName} ????`, '')
  if (!password) return
  try { await resetAdminUserPassword(item.id, { password }); notify.success('?????????') } catch (error) { notify.error(getErrorMessage(error, '?????????')) }
}
const removeItem = async (item: AdminUserListItemDto) => {
  if (deletingId.value === item.id) return
  if (!window.confirm(`????????${item.displayName}???`)) return
  if (items.value.length === 1 && pageIndex.value > 1) pageIndex.value -= 1
  deletingId.value = item.id
  try { await deleteAdminUser(item.id); await loadData(); notify.success('???????') } catch (error) { notify.error(getErrorMessage(error, '???????')) }
  finally { deletingId.value = null }
}
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
onMounted(async () => { try { await loadRoleOptions() } catch (error) { notify.error(getErrorMessage(error, '????????')) } await loadData() })
</script>

<style scoped>
.dialog-form { grid-template-columns: repeat(2, minmax(0, 1fr)); }
</style>
