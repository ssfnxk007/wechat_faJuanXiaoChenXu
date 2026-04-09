<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>券包明细管理</h2>
        <p>为指定券包配置明细，支持弹窗新增/编辑、删除和快速重载。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" :disabled="query.couponPackId <= 0" @click="openCreateDialog">新增明细</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">当前券包</span>
        <strong class="stat-value">{{ query.couponPackId || '-' }}</strong>
        <span class="stat-footnote">先指定券包 ID 再查看明细</span>
      </article>
      <article class="stat-card">
        <span class="label">明细数量</span>
        <strong class="stat-value">{{ items.length }}</strong>
        <span class="stat-footnote">当前券包下已配置明细条数</span>
      </article>
      <article class="stat-card">
        <span class="label">总发券张数</span>
        <strong class="stat-value">{{ totalQuantity }}</strong>
        <span class="stat-footnote">支付成功后预计发券总张数</span>
      </article>
      <article class="stat-card">
        <span class="label">当前模式</span>
        <strong class="stat-value">CRUD</strong>
        <span class="stat-footnote">支持新增、编辑和删除</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>查询工具</h3>
          <p class="section-tip">按券包 ID 加载该券包下的全部模板明细。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">券包明细</span>
          <span class="badge success">当前 {{ items.length }} 条</span>
        </div>
      </div>
      <div class="filter-grid">
        <input v-model.number="query.couponPackId" type="number" min="0" placeholder="请输入券包ID" @keyup.enter="loadData" />
        <input :value="querySummary" type="text" readonly />
        <div class="toolbar-actions">
          <button type="button" @click="loadData">查询</button>
          <button type="button" class="ghost-button" @click="resetQuery">重置</button>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>明细列表</h3>
          <p class="section-tip">同一券包下同一券模板只保留一条记录，数量用于控制发放张数。</p>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>券包ID</th>
              <th>券模板ID</th>
              <th>券模板名称</th>
              <th>数量</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>{{ item.couponPackId }}</td>
              <td>{{ item.couponTemplateId }}</td>
              <td>{{ item.couponTemplateName }}</td>
              <td><span class="badge info">{{ item.quantity }} 张</span></td>
              <td>
                <div class="table-actions">
                  <button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button>
                  <button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="6" class="empty-text">暂无明细，请先输入券包 ID 并查询。</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>{{ editingId ? '编辑券包明细' : '新增券包明细' }}</h3>
            <p>{{ editingId ? '修改券包明细并同步列表。' : '为当前券包新增模板明细。' }}</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.number="form.couponPackId" type="number" min="1" placeholder="券包ID" />
          <input v-model.number="form.couponTemplateId" type="number" min="1" placeholder="券模板ID" />
          <input v-model.number="form.quantity" type="number" min="1" placeholder="数量" />
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
import { computed, reactive, ref } from 'vue'
import { deleteCouponPackItem, getCouponPackItemList, saveCouponPackItem, updateCouponPackItem } from '@/api/coupon-pack-item'
import type { CouponPackItemDto, SaveCouponPackItemRequest } from '@/types/coupon-pack-item'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<CouponPackItemDto[]>([])
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)

const query = reactive({
  couponPackId: 0,
})

const createEmptyForm = (): SaveCouponPackItemRequest => ({
  couponPackId: query.couponPackId || 0,
  couponTemplateId: 0,
  quantity: 1,
})

const form = reactive<SaveCouponPackItemRequest>(createEmptyForm())
const canCreate = authStorage.hasPermission('coupon-pack-item.create')
const canEdit = authStorage.hasPermission('coupon-pack-item.edit')
const canDelete = authStorage.hasPermission('coupon-pack-item.delete')

const totalQuantity = computed(() => items.value.reduce((sum, item) => sum + item.quantity, 0))
const querySummary = computed(() => `券包ID：${query.couponPackId || '未指定'} / 当前明细：${items.value.length} 条`)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
}

const loadData = async () => {
  if (query.couponPackId <= 0) {
    items.value = []
    return
  }

  try {
    const response = await getCouponPackItemList(query.couponPackId)
    items.value = response.data
  } catch (error) {
    notify.error(getErrorMessage(error, '加载券包明细失败'))
  }
}

const resetQuery = () => {
  query.couponPackId = 0
  items.value = []
  notify.info('已重置券包明细查询条件')
}

const openCreateDialog = () => {
  editingId.value = null
  resetForm()
  form.couponPackId = query.couponPackId
  dialogVisible.value = true
}

const openEditDialog = (item: CouponPackItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    couponPackId: item.couponPackId,
    couponTemplateId: item.couponTemplateId,
    quantity: item.quantity,
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
      await updateCouponPackItem(editingId.value, { ...form })
      notify.success('券包明细修改成功')
    } else {
      await saveCouponPackItem({ ...form })
      notify.success('券包明细保存成功')
    }

    query.couponPackId = form.couponPackId
    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '券包明细修改失败' : '券包明细保存失败'))
  }
}

const removeItem = async (item: CouponPackItemDto) => {
  if (!window.confirm(`确认删除模板“${item.couponTemplateName}”的券包明细吗？`)) {
    return
  }

  try {
    await deleteCouponPackItem(item.id)
    await loadData()
    notify.success('券包明细删除成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '券包明细删除失败'))
  }
}
</script>

<style scoped>
.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
</style>
