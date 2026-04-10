<template>
  <div class="business-page">
    <div class="page-header-row">
      <div><h2>????</h2><p>????????????????????? RBAC ?????</p></div>
      <div class="inline-actions"><button type="button" class="ghost-button" @click="loadData">????</button><button v-if="canCreate" type="button" @click="openCreateDialog">????</button></div>
    </div>

    <div class="stats-grid">
      <article class="stat-card"><span class="label">????</span><strong class="stat-value">{{ totalCount }}</strong><span class="stat-footnote">??????</span></article>
      <article class="stat-card"><span class="label">????</span><strong class="stat-value">{{ enabledCount }}</strong><span class="stat-footnote">????????</span></article>
      <article class="stat-card"><span class="label">????</span><strong class="stat-value">{{ menuOptions.length }}</strong><span class="stat-footnote">???????</span></article>
      <article class="stat-card"><span class="label">?????</span><strong class="stat-value">{{ grantedCount }}</strong><span class="stat-footnote">???????????</span></article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row"><div class="toolbar-title"><h3>?????</h3><p class="section-tip">???????????????????????</p></div><div class="summary-inline"><span class="badge info">?? {{ totalCount }}</span><span class="badge success">? {{ pageIndex }} / {{ totalPages }} ?</span></div></div>
      <div class="filter-grid"><input v-model.trim="query.keyword" type="text" placeholder="?????????" @keyup.enter="handleSearch" /><select v-model.number="pageSize" @change="handlePageSizeChange"><option :value="10">?? 10 ?</option><option :value="20">?? 20 ?</option><option :value="50">?? 50 ?</option></select><input :value="querySummary" type="text" readonly /><div class="toolbar-actions"><button type="button" @click="handleSearch">??</button><button type="button" class="ghost-button" @click="resetQuery">??</button></div></div>
    </div>

    <div class="card">
      <div class="section-head"><div class="section-head-main"><h3>????</h3><p class="section-tip">???????????????????</p></div></div>
      <div class="table-wrap"><table class="table"><thead><tr><th>ID</th><th>????</th><th>????</th><th>????</th><th>???</th><th>???</th><th>??</th><th>????</th><th>??</th></tr></thead><tbody><tr v-for="item in items" :key="item.id"><td class="cell-strong">{{ item.id }}</td><td>{{ item.name }}</td><td class="cell-mono">{{ item.code }}</td><td>{{ item.userCount }}</td><td>{{ item.menuCount }}</td><td>{{ item.permissionCount }}</td><td><span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '??' : '??' }}</span></td><td>{{ formatDate(item.createdAt) }}</td><td><div class="table-actions"><button v-if="canEdit" type="button" class="action-button" :disabled="submitting || deleting" @click="openEditDialog(item)">??</button><button v-if="canDelete" type="button" class="action-button danger" :disabled="submitting || deleting" @click="removeItem(item)">{{ deleting ? '???...' : '??' }}</button></div></td></tr><tr v-if="items.length === 0"><td colspan="9" class="empty-text">??????</td></tr></tbody></table></div>
      <div class="pager"><div class="pager-info">? {{ pageIndex }} ??? {{ totalPages }} ? / ? {{ totalCount }} ?</div><div class="pager-actions"><button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">???</button><button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">???</button></div></div>
    </div>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card wide">
        <div class="dialog-head"><div><h3>{{ editingId ? '????' : '????' }}</h3><p>{{ editingId ? '????????????' : '????????????' }}</p></div><button type="button" class="ghost-button" @click="closeDialog">??</button></div>
        <div class="grid-form dialog-form"><input v-model.trim="form.name" type="text" placeholder="????" /><input v-model.trim="form.code" type="text" placeholder="?????? coupon-admin" /><label class="checkbox-field field-span-2"><input v-model="form.isEnabled" type="checkbox" /><span>????</span></label><div class="field-span-2 option-panel"><strong>????</strong><div class="checkbox-grid"><label v-for="menu in menuOptions" :key="menu.id" class="checkbox-card"><input v-model="form.menuIds" type="checkbox" :value="menu.id" /><span>{{ menu.label }}</span></label></div></div><div class="field-span-2 option-panel"><strong>????</strong><div class="checkbox-grid"><label v-for="permission in permissionOptions" :key="permission.id" class="checkbox-card"><input v-model="form.permissionIds" type="checkbox" :value="permission.id" /><span>{{ permission.name }}?{{ permission.code }}?</span></label></div></div></div>
        <div class="dialog-actions"><button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">??</button><button v-if="editingId ? canEdit : canCreate" type="button" :disabled="submitting || deleting" @click="submit">{{ submitting ? '???...' : (editingId ? '????' : '????') }}</button></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { createAdminRole, deleteAdminRole, getAdminMenuList, getAdminPermissionList, getAdminRoleList, updateAdminRole } from '@/api/admin-permission'
import type { AdminMenuListItemDto, AdminPermissionListItemDto, AdminRoleListItemDto, SaveAdminRoleRequest } from '@/types/admin-permission'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'
interface MenuOption { id: number; label: string }
const items = ref<AdminRoleListItemDto[]>([])
const menuTree = ref<AdminMenuListItemDto[]>([])
const permissionOptions = ref<AdminPermissionListItemDto[]>([])
const menuOptions = computed<MenuOption[]>(() => flattenMenuTree(menuTree.value))
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const submitting = ref(false)
const deleting = ref(false)
const query = reactive({ keyword: '' })
const createEmptyForm = (): SaveAdminRoleRequest => ({ name: '', code: '', isEnabled: true, menuIds: [], permissionIds: [] })
const form = reactive<SaveAdminRoleRequest>(createEmptyForm())
const canCreate = authStorage.hasPermission('admin.role.create')
const canEdit = authStorage.hasPermission('admin.role.edit')
const canDelete = authStorage.hasPermission('admin.role.delete')
const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const grantedCount = computed(() => items.value.filter((item) => item.menuCount > 0).length)
const querySummary = computed(() => `????${query.keyword || '??'} / ???${pageSize.value}`)
const resetForm = () => Object.assign(form, createEmptyForm())
const loadMenus = async () => { const response = await getAdminMenuList(); menuTree.value = response.data }
const loadPermissions = async () => { const response = await getAdminPermissionList(); permissionOptions.value = response.data }
const loadData = async () => { try { const response = await getAdminRoleList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value }); items.value = response.data.items; totalCount.value = response.data.totalCount; pageIndex.value = response.data.pageIndex; totalPages.value = response.data.totalPages || 1 } catch (error) { notify.error(getErrorMessage(error, '????????')) } }
const handleSearch = async () => { pageIndex.value = 1; await loadData() }
const resetQuery = async () => { query.keyword = ''; pageSize.value = 10; pageIndex.value = 1; await loadData(); notify.info('?????????') }
const handlePageSizeChange = async () => { pageIndex.value = 1; await loadData() }
const goPrevPage = async () => { if (pageIndex.value <= 1) return; pageIndex.value -= 1; await loadData() }
const goNextPage = async () => { if (pageIndex.value >= totalPages.value) return; pageIndex.value += 1; await loadData() }
const openCreateDialog = () => { editingId.value = null; resetForm(); dialogVisible.value = true }
const openEditDialog = (item: AdminRoleListItemDto) => { editingId.value = item.id; Object.assign(form, { name: item.name, code: item.code, isEnabled: item.isEnabled, menuIds: [...item.menuIds], permissionIds: [...item.permissionIds] }); dialogVisible.value = true }
const closeDialog = () => { dialogVisible.value = false; editingId.value = null; resetForm() }
const submit = async () => { if (submitting.value) return; submitting.value = true; try { if (editingId.value) { await updateAdminRole(editingId.value, { ...form }); notify.success('??????') } else { await createAdminRole({ ...form }); pageIndex.value = 1; notify.success('??????') } closeDialog(); await loadData() } catch (error) { notify.error(getErrorMessage(error, editingId.value ? '??????' : '??????')) } finally { submitting.value = false } }
const removeItem = async (item: AdminRoleListItemDto) => { if (deleting.value) return; if (!window.confirm(`???????${item.name}???`)) return; if (items.value.length === 1 && pageIndex.value > 1) pageIndex.value -= 1; deleting.value = true; try { await deleteAdminRole(item.id); await loadData(); notify.success('??????') } catch (error) { notify.error(getErrorMessage(error, '??????')) } finally { deleting.value = false } }
const flattenMenuTree = (nodes: AdminMenuListItemDto[], level = 0): MenuOption[] => nodes.flatMap((node) => [{ id: node.id, label: `${'?'.repeat(level)}${level > 0 ? ' ' : ''}${node.name}?${node.path}?` }, ...flattenMenuTree(node.children || [], level + 1)])
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
onMounted(async () => { try { await loadMenus(); await loadPermissions() } catch (error) { notify.error(getErrorMessage(error, '????????')) } await loadData() })
</script>

<style scoped>
.dialog-form { grid-template-columns: repeat(2, minmax(0, 1fr)); }
</style>
