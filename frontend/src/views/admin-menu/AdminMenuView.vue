<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>菜单管理</h2>
        <p>维护后台菜单树、路由路径与组件标识，统一作为导航展示和角色授权的数据基础。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新菜单</button>
        <button v-if="canCreate" type="button" @click="openCreateRootDialog">新增顶级菜单</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">菜单节点</span>
        <strong class="stat-value">{{ flatItems.length }}</strong>
        <span class="stat-footnote">当前系统菜单节点总数</span>
      </article>
      <article class="stat-card">
        <span class="label">顶级菜单</span>
        <strong class="stat-value">{{ rootCount }}</strong>
        <span class="stat-footnote">根节点菜单数量</span>
      </article>
      <article class="stat-card">
        <span class="label">启用菜单</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前可见菜单数量</span>
      </article>
      <article class="stat-card">
        <span class="label">层级深度</span>
        <strong class="stat-value">{{ maxLevel + 1 }}</strong>
        <span class="stat-footnote">当前菜单树最大层级</span>
      </article>
    </div>

    <div class="hero-panel">
      <div class="hero-copy">
        <div class="badge info">导航与授权基础数据</div>
        <h2>让菜单树、路由配置和权限结构保持一致</h2>
        <p>本页聚焦后台菜单层级维护。你可以按树形结构查看菜单归属关系，并继续完成新增下级、编辑、删除等操作，保证导航和权限配置始终同步。</p>
        <div class="hero-tags">
          <span class="tag">树形层级可读</span>
          <span class="tag">支持新增下级</span>
          <span class="tag">支持状态管理</span>
          <span class="tag">统一授权来源</span>
        </div>
      </div>

      <div class="hero-side">
        <div class="quick-card">
          <strong>菜单树</strong>
          <p>按层级展示父子关系，适合快速审查导航结构和归属范围。</p>
        </div>
        <div class="quick-card">
          <strong>路由映射</strong>
          <p>每个节点同步展示路由路径和组件标识，便于页面映射核查。</p>
        </div>
        <div class="quick-card">
          <strong>授权基础</strong>
          <p>角色菜单授权依赖此处数据，菜单结构调整后可直接进入角色配置。</p>
        </div>
      </div>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>结构概览</h3>
          <p class="section-tip">通过层级、状态和路径信息统一查看菜单配置，便于后台导航维护和授权核查。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">根节点 {{ rootCount }}</span>
          <span class="badge success">子级 {{ childCount }}</span>
          <span class="badge warning">最大深度 {{ maxLevel + 1 }}</span>
        </div>
      </div>

      <div class="menu-overview-grid">
        <div class="overview-card">
          <span class="overview-label">排序区间</span>
          <strong>{{ sortRangeText }}</strong>
          <small>当前菜单使用的排序范围</small>
        </div>
        <div class="overview-card">
          <span class="overview-label">结构状态</span>
          <strong>{{ childCount > 0 ? '存在多级结构' : '仅根节点结构' }}</strong>
          <small>按树形方式展示节点层级</small>
        </div>
        <div class="overview-card">
          <span class="overview-label">启用占比</span>
          <strong>{{ enabledRate }}</strong>
          <small>当前可见菜单在总节点中的占比</small>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>菜单树</h3>
          <p class="section-tip">支持新增下级、编辑节点和删除未引用菜单；树形列保留缩进与层级线索，方便快速识别结构。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">当前 {{ flatItems.length }} 个节点</span>
          <span class="badge success">已启用 {{ enabledCount }} 个</span>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>菜单名称</th>
              <th>路由路径</th>
              <th>组件</th>
              <th>排序</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in flatItems" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="menu-tree-cell" :style="{ '--menu-level': item.level }">
                  <span class="tree-indent" aria-hidden="true"></span>
                  <span class="tree-node-dot" :class="item.level === 0 ? 'root' : 'child'"></span>
                  <div class="menu-tree-main">
                    <span class="menu-tree-name">{{ item.name }}</span>
                    <span class="menu-tree-meta">{{ item.parentId ? `上级ID：${item.parentId}` : '顶级菜单' }}</span>
                  </div>
                </div>
              </td>
              <td class="cell-mono">{{ item.path }}</td>
              <td>{{ item.component }}</td>
              <td>{{ item.sort }}</td>
              <td>
                <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <div class="table-actions">
                  <button v-if="canCreate" type="button" class="action-button" @click="openCreateChildDialog(item)">新增下级</button>
                  <button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button>
                  <button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button>
                </div>
              </td>
            </tr>
            <tr v-if="flatItems.length === 0">
              <td colspan="8" class="empty-text">暂无菜单数据</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card wide">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>{{ editingId ? '编辑菜单' : '新增菜单' }}</h3>
            <p>{{ editingId ? '修改菜单的路径、组件、排序和启用状态。' : '新增菜单节点并配置所属层级与展示信息。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <select v-model="form.parentId">
            <option :value="undefined">顶级菜单</option>
            <option v-for="item in parentOptions" :key="item.id" :value="item.id">{{ item.label }}</option>
          </select>
          <input v-model.trim="form.name" type="text" placeholder="菜单名称" />
          <input v-model.trim="form.path" type="text" placeholder="路由路径，如 /admin-users" />
          <input v-model.trim="form.component" type="text" placeholder="组件标识，如 AdminUserView" />
          <input v-model.number="form.sort" type="number" placeholder="排序号" />
          <label class="checkbox-field">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用菜单</span>
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
import { createAdminMenu, deleteAdminMenu, getAdminMenuList, updateAdminMenu } from '@/api/admin-permission'
import type { AdminMenuListItemDto, SaveAdminMenuRequest } from '@/types/admin-permission'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

interface FlatMenuItem extends AdminMenuListItemDto {
  level: number
  label: string
}

const items = ref<AdminMenuListItemDto[]>([])
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const submitting = ref(false)
const deleting = ref(false)

const createEmptyForm = (): SaveAdminMenuRequest => ({
  parentId: undefined,
  name: '',
  path: '',
  component: '',
  sort: 10,
  isEnabled: true,
})

const form = reactive<SaveAdminMenuRequest>(createEmptyForm())

const flatten = (nodes: AdminMenuListItemDto[], level = 0): FlatMenuItem[] =>
  nodes.flatMap((node) => [
    { ...node, level, label: `${'—'.repeat(level)}${level > 0 ? ' ' : ''}${node.name}` },
    ...flatten(node.children || [], level + 1),
  ])

const flatItems = computed<FlatMenuItem[]>(() => flatten(items.value))
const parentOptions = computed(() => flatItems.value.filter((item) => item.id !== editingId.value))
const canCreate = authStorage.hasPermission('admin.menu.create')
const canEdit = authStorage.hasPermission('admin.menu.edit')
const canDelete = authStorage.hasPermission('admin.menu.delete')
const rootCount = computed(() => flatItems.value.filter((item) => !item.parentId).length)
const enabledCount = computed(() => flatItems.value.filter((item) => item.isEnabled).length)
const childCount = computed(() => flatItems.value.filter((item) => !!item.parentId).length)
const maxLevel = computed(() => flatItems.value.reduce((max, item) => Math.max(max, item.level), 0))
const sortRangeText = computed(() => {
  if (flatItems.value.length === 0) {
    return '-'
  }

  const values = flatItems.value.map((item) => item.sort)
  return `${Math.min(...values)} - ${Math.max(...values)}`
})
const enabledRate = computed(() => {
  if (flatItems.value.length === 0) {
    return '0%'
  }

  return `${Math.round((enabledCount.value / flatItems.value.length) * 100)}%`
})

const resetForm = () => Object.assign(form, createEmptyForm())

const loadData = async () => {
  try {
    const response = await getAdminMenuList()
    items.value = response.data
  } catch (error) {
    notify.error(getErrorMessage(error, '加载菜单列表失败'))
  }
}

const openCreateRootDialog = () => {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

const openCreateChildDialog = (item: FlatMenuItem) => {
  editingId.value = null
  resetForm()
  form.parentId = item.id
  form.sort = item.sort + 1
  dialogVisible.value = true
}

const openEditDialog = (item: FlatMenuItem) => {
  editingId.value = item.id
  Object.assign(form, {
    parentId: item.parentId,
    name: item.name,
    path: item.path,
    component: item.component,
    sort: item.sort,
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
      await updateAdminMenu(editingId.value, { ...form })
      notify.success('菜单修改成功')
    } else {
      await createAdminMenu({ ...form })
      notify.success('菜单创建成功')
    }

    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '菜单修改失败' : '菜单创建失败'))
  } finally {
    submitting.value = false
  }
}

const removeItem = async (item: FlatMenuItem) => {
  if (!window.confirm(`确认删除菜单“${item.name}”吗？`)) {
    return
  }

  if (deleting.value) {
    return
  }

  deleting.value = true
  try {
    await deleteAdminMenu(item.id)
    await loadData()
    notify.success('菜单删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '菜单删除失败'))
  } finally {
    deleting.value = false
  }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>

<style scoped>
.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.menu-overview-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}

.overview-card {
  display: grid;
  gap: 6px;
  padding: 16px 18px;
  border-radius: 18px;
  border: 1px solid var(--line);
  background: linear-gradient(180deg, #ffffff 0%, #f8fafc 100%);
}

.overview-label {
  color: var(--muted);
  font-size: 12px;
  font-weight: 600;
}

.overview-card strong {
  font-size: 18px;
  line-height: 1.2;
}

.overview-card small {
  color: var(--muted);
  font-size: 12px;
}

.menu-tree-cell {
  --menu-level: 0;
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.tree-indent {
  width: calc(var(--menu-level) * 18px);
  flex: 0 0 calc(var(--menu-level) * 18px);
}

.tree-node-dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  flex: 0 0 auto;
  background: rgba(148, 163, 184, 0.7);
  box-shadow: 0 0 0 4px rgba(148, 163, 184, 0.12);
}

.tree-node-dot.root {
  background: var(--primary);
  box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.14);
}

.menu-tree-main {
  min-width: 0;
  display: grid;
  gap: 4px;
}

.menu-tree-name {
  font-weight: 700;
  color: var(--text);
}

.menu-tree-meta {
  color: var(--muted);
  font-size: 12px;
}

@media (max-width: 960px) {
  .menu-overview-grid {
    grid-template-columns: 1fr;
  }

  .dialog-form {
    grid-template-columns: 1fr;
  }
}
</style>
