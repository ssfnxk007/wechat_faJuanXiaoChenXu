<template>
  <div class="business-page">
    <div class="page-header-row">
      <div><h2>菜单管理</h2><p>维护后台菜单树、页面路由和组件标识，作为角色授权的数据源。</p></div>
      <div class="inline-actions"><button type="button" class="ghost-button" @click="loadData">刷新菜单</button><button v-if="canCreate" type="button" @click="openCreateRootDialog">新增顶级菜单</button></div>
    </div>

    <div class="stats-grid">
      <article class="stat-card"><span class="label">菜单总数</span><strong class="stat-value">{{ flatItems.length }}</strong><span class="stat-footnote">当前系统菜单节点数量</span></article>
      <article class="stat-card"><span class="label">顶级菜单</span><strong class="stat-value">{{ rootCount }}</strong><span class="stat-footnote">ParentId 为空的菜单数</span></article>
      <article class="stat-card"><span class="label">启用菜单</span><strong class="stat-value">{{ enabledCount }}</strong><span class="stat-footnote">当前启用菜单数</span></article>
      <article class="stat-card"><span class="label">子级菜单</span><strong class="stat-value">{{ childCount }}</strong><span class="stat-footnote">作为下级存在的菜单数</span></article>
    </div>

    <div class="card">
      <div class="section-head"><div class="section-head-main"><h3>菜单树</h3><p class="section-tip">支持新增子菜单、编辑菜单和删除未引用节点。</p></div></div>
      <div class="table-wrap"><table class="table"><thead><tr><th>ID</th><th>菜单名称</th><th>路由路径</th><th>组件</th><th>排序</th><th>状态</th><th>创建时间</th><th>操作</th></tr></thead><tbody><tr v-for="item in flatItems" :key="item.id"><td class="cell-strong">{{ item.id }}</td><td><span :style="{ paddingLeft: `${item.level * 18}px` }">{{ item.level > 0 ? '└ ' : '' }}{{ item.name }}</span></td><td class="cell-mono">{{ item.path }}</td><td>{{ item.component }}</td><td>{{ item.sort }}</td><td><span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span></td><td>{{ formatDate(item.createdAt) }}</td><td><div class="table-actions"><button v-if="canCreate" type="button" class="action-button" @click="openCreateChildDialog(item)">新增下级</button><button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button><button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button></div></td></tr><tr v-if="flatItems.length === 0"><td colspan="8" class="empty-text">暂无菜单数据</td></tr></tbody></table></div>
    </div>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card wide">
        <div class="dialog-head"><div><h3>{{ editingId ? '编辑菜单' : '新增菜单' }}</h3><p>{{ editingId ? '修改菜单路由和显示状态。' : '新增菜单节点并配置层级关系。' }}</p></div><button type="button" class="ghost-button" @click="closeDialog">关闭</button></div>
        <div class="grid-form dialog-form"><select v-model="form.parentId"><option :value="undefined">顶级菜单</option><option v-for="item in parentOptions" :key="item.id" :value="item.id">{{ item.label }}</option></select><input v-model.trim="form.name" type="text" placeholder="菜单名称" /><input v-model.trim="form.path" type="text" placeholder="路由路径，如 /admin-users" /><input v-model.trim="form.component" type="text" placeholder="组件标识，如 AdminUserView" /><input v-model.number="form.sort" type="number" placeholder="排序号" /><label class="checkbox-field"><input v-model="form.isEnabled" type="checkbox" /><span>启用菜单</span></label></div>
        <div class="dialog-actions"><button type="button" class="ghost-button" @click="closeDialog">取消</button><button v-if="editingId ? canEdit : canCreate" type="button" @click="submit">{{ editingId ? '保存修改' : '保存新增' }}</button></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { createAdminMenu, deleteAdminMenu, getAdminMenuList, updateAdminMenu } from '@/api/admin-permission'
import type { AdminMenuListItemDto, SaveAdminMenuRequest } from '@/types/admin-permission'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'
interface FlatMenuItem extends AdminMenuListItemDto { level: number; label: string }
const items = ref<AdminMenuListItemDto[]>([])
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const createEmptyForm = (): SaveAdminMenuRequest => ({ parentId: undefined, name: '', path: '', component: '', sort: 10, isEnabled: true })
const form = reactive<SaveAdminMenuRequest>(createEmptyForm())
const flatItems = computed<FlatMenuItem[]>(() => flatten(items.value))
const parentOptions = computed(() => flatItems.value.filter((item) => item.id !== editingId.value))
const canCreate = authStorage.hasPermission('admin.menu.create')
const canEdit = authStorage.hasPermission('admin.menu.edit')
const canDelete = authStorage.hasPermission('admin.menu.delete')
const rootCount = computed(() => flatItems.value.filter((item) => !item.parentId).length)
const enabledCount = computed(() => flatItems.value.filter((item) => item.isEnabled).length)
const childCount = computed(() => flatItems.value.filter((item) => !!item.parentId).length)
const resetForm = () => Object.assign(form, createEmptyForm())
const flatten = (nodes: AdminMenuListItemDto[], level = 0): FlatMenuItem[] => nodes.flatMap((node) => [{ ...node, level, label: `${'—'.repeat(level)}${level > 0 ? ' ' : ''}${node.name}` }, ...flatten(node.children || [], level + 1)])
const loadData = async () => { try { const response = await getAdminMenuList(); items.value = response.data } catch (error) { notify.error(getErrorMessage(error, '加载菜单列表失败')) } }
const openCreateRootDialog = () => { editingId.value = null; resetForm(); dialogVisible.value = true }
const openCreateChildDialog = (item: FlatMenuItem) => { editingId.value = null; resetForm(); form.parentId = item.id; form.sort = item.sort + 1; dialogVisible.value = true }
const openEditDialog = (item: FlatMenuItem) => { editingId.value = item.id; Object.assign(form, { parentId: item.parentId, name: item.name, path: item.path, component: item.component, sort: item.sort, isEnabled: item.isEnabled }); dialogVisible.value = true }
const closeDialog = () => { dialogVisible.value = false; editingId.value = null; resetForm() }
const submit = async () => { try { if (editingId.value) { await updateAdminMenu(editingId.value, { ...form }); notify.success('菜单修改成功') } else { await createAdminMenu({ ...form }); notify.success('菜单创建成功') } closeDialog(); await loadData() } catch (error) { notify.error(getErrorMessage(error, editingId.value ? '菜单修改失败' : '菜单创建失败')) } }
const removeItem = async (item: FlatMenuItem) => { if (!window.confirm(`确认删除菜单“${item.name}”吗？`)) return; try { await deleteAdminMenu(item.id); await loadData(); notify.success('菜单删除成功') } catch (error) { notify.error(getErrorMessage(error, '菜单删除失败')) } }
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
onMounted(loadData)
</script>

<style scoped>
.dialog-form { grid-template-columns: repeat(2, minmax(0, 1fr)); }
</style>
