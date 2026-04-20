<template>
  <div class="business-page page-v2 admin-role-page">
    <section class="hero-panel admin-role-hero">
      <div class="hero-copy">
        <span class="page-kicker">系统管理</span>
        <h2>角色管理</h2>
        <p>维护后台角色与菜单、权限授权关系，统一管理 RBAC 权限模型。页面优先服务角色筛选、授权核对和角色维护，不做展示型堆叠。</p>
        <div class="hero-tags">
          <span class="badge info">角色编码</span>
          <span class="badge success">菜单授权</span>
          <span class="badge warning">权限授权</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">角色总数</span>
          <strong>{{ totalCount }}</strong>
          <p>当前已创建的角色数量</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">启用角色</span>
          <strong>{{ enabledCount }}</strong>
          <p>当前可正常使用的角色</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">菜单数量</span>
          <strong>{{ menuOptions.length }}</strong>
          <p>可分配给角色的菜单项</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">已授权角色</span>
          <strong>{{ grantedCount }}</strong>
          <p>已配置菜单或权限的角色数量</p>
        </article>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">角色总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">当前已创建的角色数量</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">启用角色</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前可正常使用的角色</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">菜单数量</span>
        <strong class="stat-value">{{ menuOptions.length }}</strong>
        <span class="stat-footnote">可分配给角色的菜单项</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">已授权角色</span>
        <strong class="stat-value">{{ grantedCount }}</strong>
        <span class="stat-footnote">已配置菜单或权限的角色数量</span>
      </article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">检索与动作</span>
          <h3>角色工作台</h3>
          <p class="section-tip">支持按角色名称或编码检索，并在同一入口完成新增、编辑和删除。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetQuery">重置筛选</button>
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新增角色</button>
        </div>
      </div>

      <div class="filter-panel-grid admin-role-filter-grid">
        <label class="field-card filter-field">
          <span class="field-label">角色关键字</span>
          <input v-model.trim="query.keyword" type="text" placeholder="请输入角色名称或编码" @keyup.enter="handleSearch" />
        </label>
        <label class="field-card filter-field compact-field">
          <span class="field-label">分页条数</span>
          <select v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">每页 10 条</option>
            <option :value="20">每页 20 条</option>
            <option :value="50">每页 50 条</option>
          </select>
        </label>
        <div class="field-card summary-field">
          <span class="field-label">当前筛选</span>
          <strong>{{ querySummary }}</strong>
          <p>角色页负责汇总菜单和权限授权边界，账号页只绑定角色，不直接配置细粒度权限。</p>
        </div>
      </div>
    </section>

    <section class="admin-role-content-grid">
      <article class="card card-v2 data-card archive-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">角色档案</span>
            <h3>角色列表</h3>
            <p class="section-tip">查看角色编码、账号数量、菜单数量、权限数量与启用状态。</p>
          </div>
        </div>

        <div class="table-wrap table-wrap-v2">
          <table class="table">
            <thead>
              <tr>
                <th>ID</th>
                <th>角色名称</th>
                <th>角色编码</th>
                <th>账号数量</th>
                <th>菜单数</th>
                <th>权限数</th>
                <th>状态</th>
                <th>创建时间</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in items" :key="item.id">
                <td class="cell-strong">{{ item.id }}</td>
                <td>{{ item.name }}</td>
                <td class="cell-mono">{{ item.code }}</td>
                <td>{{ item.userCount }}</td>
                <td>{{ item.menuCount }}</td>
                <td>{{ item.permissionCount }}</td>
                <td>
                  <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">
                    {{ item.isEnabled ? '启用' : '停用' }}
                  </span>
                </td>
                <td>{{ formatDate(item.createdAt) }}</td>
                <td>
                  <div class="table-actions">
                    <button v-if="canEdit" type="button" class="action-button" :disabled="submitting || deleting" @click="openEditDialog(item)">编辑</button>
                    <button v-if="canDelete" type="button" class="action-button danger" :disabled="submitting || deleting" @click="removeItem(item)">{{ deleting ? '删除中...' : '删除' }}</button>
                  </div>
                </td>
              </tr>
              <tr v-if="items.length === 0">
                <td colspan="9" class="empty-text">暂无角色数据</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="pager pager-v2">
          <div class="pager-info">第 {{ pageIndex }} 页，共 {{ totalPages }} 页 / 共 {{ totalCount }} 条</div>
          <div class="pager-actions">
            <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
            <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
          </div>
        </div>
      </article>

      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">授权要点</span>
            <h3>角色侧规则</h3>
            <p class="section-tip">把 RBAC 里最容易混淆的边界固定下来，减少角色设计反复返工。</p>
          </div>
        </div>
        <div class="guide-list">
          <div class="guide-item">
            <strong>角色是授权边界，不是账号本身</strong>
            <p>角色页决定一个职责能访问哪些菜单和按钮权限，账号页只做角色绑定。</p>
          </div>
          <div class="guide-item">
            <strong>菜单与权限要同时看</strong>
            <p>只有菜单授权可能导致能见不能用，只有按钮授权可能导致路由 403；两者必须一起核对。</p>
          </div>
          <div class="guide-item">
            <strong>删除角色前先确认引用关系</strong>
            <p>角色被账号绑定时更建议先停用或调整授权，而不是直接删除。</p>
          </div>
        </div>
      </article>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 admin-role-dialog-card wide">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">角色表单</span>
            <h3>{{ editingId ? '编辑角色' : '新增角色' }}</h3>
            <p>{{ editingId ? '调整角色名称、编码与授权范围。' : '创建新的后台角色并配置菜单与权限。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form admin-role-form-grid">
          <label><span>角色名称</span><input v-model.trim="form.name" type="text" placeholder="请输入角色名称" /></label>
          <label><span>角色编码</span><input v-model.trim="form.code" type="text" placeholder="请输入角色编码，如 coupon-admin" /></label>
          <label class="checkbox-field checkbox-card field-span-2">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用角色</span>
          </label>

          <div class="field-span-2 option-panel">
            <strong>菜单授权</strong>
            <div class="checkbox-grid">
              <label v-for="menu in menuOptions" :key="menu.id" class="checkbox-card">
                <input v-model="form.menuIds" type="checkbox" :value="menu.id" />
                <span>{{ menu.label }}</span>
              </label>
            </div>
          </div>

          <div class="field-span-2 option-panel">
            <strong>权限授权</strong>
            <div class="checkbox-grid">
              <label v-for="permission in permissionOptions" :key="permission.id" class="checkbox-card">
                <input v-model="form.permissionIds" type="checkbox" :value="permission.id" />
                <span>{{ permission.name }}（{{ permission.code }}）</span>
              </label>
            </div>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">取消</button>
          <button v-if="editingId ? canEdit : canCreate" type="button" class="primary-button" :disabled="submitting || deleting" @click="submit">
            {{ submitting ? '提交中...' : (editingId ? '保存修改' : '创建角色') }}
          </button>
        </div>
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

interface MenuOption {
  id: number
  label: string
}

const items = ref<AdminRoleListItemDto[]>([])
const menuOptions = ref<MenuOption[]>([])
const permissionOptions = ref<AdminPermissionListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const submitting = ref(false)
const deleting = ref(false)
const query = reactive({ keyword: '' })

const createEmptyForm = (): SaveAdminRoleRequest => ({
  name: '',
  code: '',
  isEnabled: true,
  menuIds: [],
  permissionIds: [],
})

const form = reactive<SaveAdminRoleRequest>(createEmptyForm())
const canCreate = authStorage.hasPermission('admin.role.create')
const canEdit = authStorage.hasPermission('admin.role.edit')
const canDelete = authStorage.hasPermission('admin.role.delete')
const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const grantedCount = computed(() => items.value.filter((item) => item.menuCount > 0 || item.permissionCount > 0).length)
const querySummary = computed(() => `关键字：${query.keyword || '全部'} / 每页 ${pageSize.value} 条`)

const flattenMenus = (nodes: AdminMenuListItemDto[], level = 0): MenuOption[] =>
  nodes.flatMap((node) => [
    { id: node.id, label: `${'—'.repeat(level)}${level > 0 ? ' ' : ''}${node.name}` },
    ...flattenMenus(node.children || [], level + 1),
  ])

const resetForm = () => Object.assign(form, createEmptyForm())

const loadMenus = async () => {
  const response = await getAdminMenuList()
  menuOptions.value = flattenMenus(response.data)
}

const loadPermissions = async () => {
  const response = await getAdminPermissionList()
  permissionOptions.value = response.data
}

const loadData = async () => {
  try {
    const response = await getAdminRoleList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载角色列表失败'))
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

const openEditDialog = (item: AdminRoleListItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    code: item.code,
    isEnabled: item.isEnabled,
    menuIds: [...item.menuIds],
    permissionIds: [...item.permissionIds],
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
      await updateAdminRole(editingId.value, { ...form })
      notify.success('角色修改成功')
    } else {
      await createAdminRole({ ...form })
      pageIndex.value = 1
      notify.success('角色创建成功')
    }
    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '角色修改失败' : '角色创建失败'))
  } finally {
    submitting.value = false
  }
}

const removeItem = async (item: AdminRoleListItemDto) => {
  if (deleting.value) return
  if (!window.confirm(`确认删除角色“${item.name}”吗？`)) return
  if (items.value.length === 1 && pageIndex.value > 1) pageIndex.value -= 1

  deleting.value = true
  try {
    await deleteAdminRole(item.id)
    await loadData()
    notify.success('角色删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '角色删除失败'))
  } finally {
    deleting.value = false
  }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(async () => {
  try {
    await loadMenus()
    await loadPermissions()
  } catch (error) {
    notify.error(getErrorMessage(error, '加载角色基础数据失败'))
  }
  await loadData()
})
</script>

<style scoped>
.admin-role-hero {
  background:
    radial-gradient(circle at top right, rgba(59, 130, 246, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%);
}

.admin-role-filter-grid {
  grid-template-columns: 1.4fr 0.8fr 1fr;
}

.admin-role-content-grid {
  display: grid;
  gap: 18px;
  grid-template-columns: minmax(0, 1.6fr) minmax(320px, 1fr);
}

.guide-list {
  display: grid;
  gap: 12px;
}

.admin-role-form-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.admin-role-form-grid label {
  display: grid;
  gap: 8px;
}

.admin-role-form-grid label span {
  font-size: 13px;
  font-weight: 700;
  color: #344054;
}

@media (max-width: 1100px) {
  .hero-side-grid,
  .admin-role-filter-grid,
  .admin-role-content-grid,
  .admin-role-form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
