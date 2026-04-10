<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>订单管理</h2>
        <p>查看券包订单，支持创建订单、分页浏览、支付处理和订单详情查看。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        <button v-if="canCreate" type="button" @click="openCreateDialog">创建订单</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">订单总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">服务端分页统计总记录数</span>
      </article>
      <article class="stat-card">
        <span class="label">当前页</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card">
        <span class="label">已支付</span>
        <strong class="stat-value">{{ paidCount }}</strong>
        <span class="stat-footnote">当前页已支付订单数</span>
      </article>
      <article class="stat-card">
        <span class="label">待支付</span>
        <strong class="stat-value">{{ pendingCount }}</strong>
        <span class="stat-footnote">当前页待支付订单数</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>筛选与统计</h3>
          <p class="section-tip">列表数据使用服务端分页，状态和用户条件保留为当前页本地过滤。</p>
        </div>
        <div class="summary-inline">
          <span class="badge info">关键词查询</span>
          <span class="badge success">第 {{ pageIndex }} / {{ totalPages }} 页</span>
          <span class="badge warning">总数 {{ totalCount }}</span>
        </div>
      </div>
      <div class="filter-grid">
        <input v-model.trim="query.keyword" type="text" placeholder="按订单号搜索" @keyup.enter="handleSearch" />
        <select v-model.number="pageSize" @change="handlePageSizeChange">
          <option :value="10">每页 10 条</option>
          <option :value="20">每页 20 条</option>
          <option :value="50">每页 50 条</option>
        </select>
        <select v-model="filters.status">
          <option value="all">全部状态</option>
          <option value="1">待支付</option>
          <option value="2">已支付</option>
          <option value="3">已退款</option>
          <option value="4">已关闭</option>
        </select>
        <input v-model.number="filters.userId" type="number" min="0" placeholder="按用户ID筛选" />
      </div>
      <div class="toolbar-row">
        <div class="muted-text">{{ querySummary }}</div>
        <div class="toolbar-actions">
          <button type="button" @click="handleSearch">查询</button>
          <button type="button" class="ghost-button" @click="resetQuery">重置</button>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>订单列表</h3>
          <p class="section-tip">支付成功后，将按券包明细自动发放用户券实例。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">当前页 {{ items.length }}</span>
          <span class="badge success">筛选结果 {{ filteredItems.length }}</span>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>订单号</th>
              <th>用户ID</th>
              <th>券包ID</th>
              <th>金额</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredItems" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td class="cell-mono">{{ item.orderNo }}</td>
              <td>{{ item.appUserId }}</td>
              <td>{{ item.couponPackId }}</td>
              <td>{{ formatAmount(item.orderAmount) }}</td>
              <td>
                <span :class="['status-badge', item.status === 2 ? 'success' : item.status === 1 ? 'warning' : 'danger']">
                  {{ statusMap[item.status] }}
                </span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <div class="table-actions">
                  <button type="button" class="action-button" @click="openDetailDialog(item.id)">详情</button>
                  <button
                    v-if="canPay"
                    type="button"
                    class="action-button"
                    :disabled="item.status !== 1"
                    @click="mockPay(item.id)"
                  >
                    执行支付
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="filteredItems.length === 0">
              <td colspan="8" class="empty-text">暂无符合筛选条件的订单数据</td>
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
            <h3>创建订单</h3>
            <p>录入用户ID和券包ID，生成待支付订单后可在列表中执行支付处理。</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.number="form.userId" type="number" min="1" placeholder="用户ID" />
          <input v-model.number="form.couponPackId" type="number" min="1" placeholder="券包ID" />
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeDialog">取消</button>
          <button v-if="canCreate" type="button" @click="submit">创建订单</button>
        </div>
      </div>
    </div>

    <div v-if="detailDialogVisible" class="dialog-mask" @click.self="closeDetailDialog">
      <div class="dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>订单详情</h3>
            <p>查看订单基础信息、支付流水和发券结果。</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDetailDialog">关闭</button>
        </div>

        <div class="grid-form detail-grid">
          <div class="result-panel"><strong>订单号</strong><div class="cell-mono">{{ detail?.orderNo }}</div></div>
          <div class="result-panel"><strong>订单状态</strong><div><span :class="['status-badge', detail?.status === 2 ? 'success' : detail?.status === 1 ? 'warning' : 'danger']">{{ statusMap[detail?.status || 0] ?? '-' }}</span></div></div>
          <div class="result-panel"><strong>用户ID</strong><div>{{ detail?.appUserId }}</div></div>
          <div class="result-panel"><strong>券包</strong><div>{{ detail?.couponPackName }} (#{{ detail?.couponPackId }})</div></div>
          <div class="result-panel"><strong>订单金额</strong><div>{{ detail ? formatAmount(detail.orderAmount) : '-' }}</div></div>
          <div class="result-panel"><strong>主支付单号</strong><div class="cell-mono">{{ detail?.paymentNo || '-' }}</div></div>
          <div class="result-panel"><strong>下单时间</strong><div>{{ formatDate(detail?.createdAt) }}</div></div>
          <div class="result-panel"><strong>支付时间</strong><div>{{ formatDate(detail?.paidAt) }}</div></div>
        </div>

        <div class="card toolbar-card detail-history-card">
          <div class="toolbar-title">
            <h3>支付流水</h3>
            <p class="section-tip">记录该订单产生的支付流水及回调信息。</p>
          </div>
          <div v-if="!detail?.payments?.length" class="empty-text">暂无支付流水</div>
          <div v-else class="table-wrap">
            <table class="table">
              <thead>
                <tr>
                  <th>???ID</th>
                  <th>???ID</th>
                  <th>?????</th>
                  <th>???</th>
                  <th>????</th>
                  <th>????</th>
                  <th>??</th>
                  <th>??</th>
                  <th>????</th>
                  <th>???</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="payment in detail.payments" :key="payment.id">
                  <td class="cell-mono">{{ payment.paymentNo }}</td>
                  <td>{{ formatAmount(payment.amount) }}</td>
                  <td><span :class="['status-badge', payment.status === 2 ? 'success' : payment.status === 1 ? 'warning' : 'danger']">{{ paymentStatusMap[payment.status] || `状态${payment.status}` }}</span></td>
                  <td class="cell-mono">{{ payment.channelTradeNo || '-' }}</td>
                  <td>{{ formatDate(payment.paidAt || payment.createdAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="card toolbar-card detail-history-card">
          <div class="toolbar-title">
            <h3>发券结果</h3>
            <p class="section-tip">订单支付成功后，根据券包明细生成的用户券实例。</p>
          </div>
          <div v-if="!detail?.grantedCoupons?.length" class="empty-text">暂无发券结果</div>
          <div v-else class="table-wrap">
            <table class="table">
              <thead>
                <tr>
                  <th>???ID</th>
                  <th>???ID</th>
                  <th>?????</th>
                  <th>???</th>
                  <th>????</th>
                  <th>????</th>
                  <th>??</th>
                  <th>??</th>
                  <th>????</th>
                  <th>???</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="coupon in detail.grantedCoupons" :key="coupon.id">
                  <td class="cell-strong">{{ coupon.id }}</td>
                  <td>{{ coupon.couponTemplateId }}</td>
                  <td>{{ coupon.couponTemplateName || '-' }}</td>
                  <td>{{ templateTypeMap[coupon.templateType] || '-' }}</td>
                  <td>{{ formatCouponBenefit(coupon) }}</td>
                  <td>{{ coupon.isAllStores ? '????' : '????' }}</td>
                  <td class="cell-mono">{{ coupon.couponCode }}</td>
                  <td><span :class="['status-badge', coupon.status === 1 ? 'success' : coupon.status === 2 ? 'warning' : 'danger']">{{ userCouponStatusMap[coupon.status] || `??${coupon.status}` }}</span></td>
                  <td>{{ formatDate(coupon.receivedAt) }}</td>
                  <td>{{ formatDate(coupon.effectiveAt) }} ~ {{ formatDate(coupon.expireAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { createCouponOrder, getCouponOrderDetail, getCouponOrderList } from '@/api/coupon-pack'
import { createPayment, paymentCallback } from '@/api/payment'
import type { CouponOrderDetailDto, CouponOrderListItemDto } from '@/types/coupon-pack'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const statusMap: Record<number, string> = {
  1: '待支付',
  2: '已支付',
  3: '已退款',
  4: '已关闭',
}

const paymentStatusMap: Record<number, string> = {
  1: '待支付',
  2: '已支付',
  3: '支付失败',
}

const userCouponStatusMap: Record<number, string> = {
  1: '未使用',
  2: '已使用',
  3: '已过期',
  4: '已作废',
}

const templateTypeMap: Record<number, string> = {
  1: '???',
  2: '????',
  3: '?????',
  4: '???',
}

const items = ref<CouponOrderListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const detail = ref<CouponOrderDetailDto | null>(null)
const canCreate = authStorage.hasPermission('coupon-order.create')
const canPay = authStorage.hasPermission('coupon-order.pay')

const query = reactive({
  keyword: '',
})

const form = reactive({
  userId: 0,
  couponPackId: 0,
})

const filters = reactive({
  status: 'all',
  userId: 0,
})

const paidCount = computed(() => items.value.filter((item) => item.status === 2).length)
const pendingCount = computed(() => items.value.filter((item) => item.status === 1).length)
const querySummary = computed(() => `关键词：${query.keyword || '全部'} / 每页：${pageSize.value} / 状态：${statusMap[Number(filters.status)] || '全部'} / 用户：${filters.userId || '全部'}`)

const filteredItems = computed(() => {
  return items.value.filter((item) => {
    const matchStatus = filters.status === 'all' || item.status === Number(filters.status)
    const matchUser = !filters.userId || item.appUserId === filters.userId
    return matchStatus && matchUser
  })
})

const loadData = async () => {
  try {
    const response = await getCouponOrderList({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })
    items.value = response.data.items.filter((item) => {
      const keyword = query.keyword.trim().toLowerCase()
      return !keyword || item.orderNo.toLowerCase().includes(keyword)
    })
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载订单列表失败'))
  }
}

const handleSearch = async () => {
  pageIndex.value = 1
  await loadData()
}

const resetQuery = async () => {
  query.keyword = ''
  filters.status = 'all'
  filters.userId = 0
  pageSize.value = 10
  pageIndex.value = 1
  await loadData()
  notify.info('已重置订单筛选条件')
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

const resetForm = () => {
  form.userId = 0
  form.couponPackId = 0
}

const openCreateDialog = () => {
  resetForm()
  dialogVisible.value = true
}

const closeDialog = () => {
  dialogVisible.value = false
  resetForm()
}

const openDetailDialog = async (orderId: number) => {
  try {
    const response = await getCouponOrderDetail(orderId)
    detail.value = response.data
    detailDialogVisible.value = true
  } catch (error) {
    notify.error(getErrorMessage(error, '加载订单详情失败'))
  }
}

const closeDetailDialog = () => {
  detailDialogVisible.value = false
  detail.value = null
}

const submit = async () => {
  try {
    await createCouponOrder({ ...form })
    closeDialog()
    pageIndex.value = 1
    await loadData()
    notify.success('订单创建成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '创建订单失败'))
  }
}

const mockPay = async (orderId: number) => {
  try {
    const payment = await createPayment({ orderId })
    await paymentCallback({
      paymentNo: payment.data.paymentNo,
      success: true,
      channelTradeNo: `MOCK-${payment.data.paymentNo}`,
      rawCallback: 'mock payment success',
    })
    await loadData()
    if (detail.value?.id === orderId) {
      await openDetailDialog(orderId)
    }
    notify.success('支付处理成功，已刷新订单状态')
  } catch (error) {
    notify.error(getErrorMessage(error, '支付处理失败'))
  }
}

const formatAmount = (value: number) => Number(value || 0).toFixed(2)
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const formatCouponBenefit = (coupon: {
  templateType: number
  discountAmount?: number
  thresholdAmount?: number
  isNewUserOnly: boolean
}) => {
  if (coupon.templateType === 4) {
    const threshold = Number(coupon.thresholdAmount || 0).toFixed(2)
    const discount = Number(coupon.discountAmount || 0).toFixed(2)
    return `? ${threshold} ? ${discount}`
  }

  if (coupon.discountAmount && coupon.discountAmount > 0) {
    const discount = Number(coupon.discountAmount).toFixed(2)
    return coupon.isNewUserOnly ? `????? ${discount}` : `? ${discount}`
  }

  return coupon.isNewUserOnly ? '????' : '-'
}

onMounted(loadData)
</script>

<style scoped>
.dialog-form {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.detail-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.detail-history-card {
  margin-top: 8px;
}
</style>
