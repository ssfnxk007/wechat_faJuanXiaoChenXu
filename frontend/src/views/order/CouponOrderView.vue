<template>
  <div class="business-page page-v2 coupon-order-page">
    <section class="hero-panel order-hero">
      <div class="hero-copy">
        <span class="page-kicker">交易中心</span>
        <h2>订单管理</h2>
        <p>集中查看券包订单、支付状态与发券结果，支持创建订单、处理支付并回看订单下发放的全部用户券。</p>
        <div class="hero-tags">
          <span class="badge info">订单与支付统一归档</span>
          <span class="badge success">支持发券结果回看</span>
          <span class="badge warning">适合运营核对订单闭环</span>
        </div>
      </div>
      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight"><span class="quick-card-label">订单总数</span><strong>{{ totalCount }}</strong><p>当前查询范围内的券包订单记录。</p></article>
        <div class="hero-side-grid">
          <article class="quick-card compact"><span class="quick-card-label">待支付</span><strong>{{ pendingCount }}</strong><p>当前页待处理订单</p></article>
          <article class="quick-card compact"><span class="quick-card-label">已支付</span><strong>{{ paidCount }}</strong><p>当前页已完成支付</p></article>
        </div>
      </div>
    </section>

    <div class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue"><span class="label">订单总数</span><strong class="stat-value">{{ totalCount }}</strong><span class="stat-footnote">当前查询范围内订单数</span></article>
      <article class="stat-card accent-indigo"><span class="label">当前页码</span><strong class="stat-value">{{ pageIndex }}</strong><span class="stat-footnote">共 {{ totalPages }} 页</span></article>
      <article class="stat-card accent-amber"><span class="label">待支付</span><strong class="stat-value">{{ pendingCount }}</strong><span class="stat-footnote">当前页待支付订单</span></article>
      <article class="stat-card accent-green"><span class="label">已支付</span><strong class="stat-value">{{ paidCount }}</strong><span class="stat-footnote">当前页已支付订单</span></article>
    </div>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">检索与动作</span>
          <h3>订单筛选与处理入口</h3>
          <p class="section-tip">按订单号、状态与用户筛选订单，并在统一入口完成新建订单和支付处理。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetQuery">重置筛选</button>
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新建订单</button>
        </div>
      </div>

      <div class="filter-panel-grid order-filter-grid">
        <label class="field-card filter-field"><span class="field-label">订单号</span><input v-model.trim="query.keyword" type="text" placeholder="输入订单号后回车检索" @keyup.enter="handleSearch" /></label>
        <label class="field-card filter-field compact-field"><span class="field-label">状态</span><select v-model="filters.status" @change="handleSearch"><option value="all">全部</option><option value="1">待支付</option><option value="2">已支付</option><option value="3">已关闭</option></select></label>
        <label class="field-card filter-field compact-field"><span class="field-label">用户</span><select v-model.number="filters.userId" @change="handleSearch"><option :value="0">全部用户</option><option v-for="user in userOptions" :key="user.id" :value="user.id">{{ formatUserLabel(user) }}</option></select></label>
        <label class="field-card filter-field compact-field"><span class="field-label">分页条数</span><select v-model.number="pageSize" @change="handlePageSizeChange"><option :value="10">每页 10 条</option><option :value="20">每页 20 条</option><option :value="50">每页 50 条</option></select></label>
        <div class="field-card summary-field order-summary-card"><span class="field-label">当前筛选</span><strong>{{ querySummary }}</strong><p>当前列表基于服务端分页加载，并叠加前端状态 / 用户过滤。</p></div>
      </div>
    </section>

    <section class="card card-v2 data-card archive-card">
      <div class="section-head"><div class="section-head-main"><span class="section-kicker">订单档案</span><h3>订单列表</h3><p class="section-tip">展示订单号、用户、券包、金额、支付状态与创建时间，可直接查看详情或处理支付。</p></div><div class="inline-metrics"><span class="badge info">当前页 {{ filteredItems.length }} 条</span><span class="badge warning">每页 {{ pageSize }} 条</span></div></div>
      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead><tr><th>ID</th><th>订单信息</th><th>用户与券包</th><th>金额</th><th>状态</th><th>支付时间</th><th>创建时间</th><th>操作</th></tr></thead>
          <tbody>
            <tr v-for="item in filteredItems" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td><div class="table-primary-cell"><strong>{{ item.orderNo }}</strong><span>订单编号</span></div></td>
              <td><div class="table-primary-cell"><strong>用户 #{{ item.appUserId }}</strong><span>券包 #{{ item.couponPackId }}</span></div></td>
              <td class="cell-strong">{{ formatAmount(item.orderAmount) }}</td>
              <td><span :class="['status-badge', statusClassMap[item.status] ?? 'warning']">{{ statusMap[item.status] || '未知状态' }}</span></td>
              <td>{{ formatDate(item.paidAt) }}</td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td><div class="table-actions"><button type="button" class="action-button" @click="openDetailDialog(item.id)">详情</button><button v-if="canPay && item.status === 1" type="button" class="action-button" :disabled="payingOrderId === item.id" @click="payOrder(item.id)">{{ payingOrderId === item.id ? '处理中...' : '处理支付' }}</button></div></td>
            </tr>
            <tr v-if="filteredItems.length === 0"><td colspan="8" class="empty-text">当前没有符合条件的订单记录</td></tr>
          </tbody>
        </table>
      </div>
      <div class="pager pager-v2"><div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条记录</div><div class="pager-actions"><button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button><button type="button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button></div></div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 order-dialog">
        <div class="dialog-head"><div class="dialog-head-main"><span class="section-kicker">订单表单</span><h3>新建订单</h3><p>从现有用户与券包档案中选择，生成待支付订单。</p></div></div>
        <div class="grid-form dialog-form order-form-grid">
          <label>
            <span>搜索用户</span>
            <RemoteSelectField v-model="form.userId" v-model:keyword="selectorQuery.userKeyword" placeholder="手机号 / 昵称 / OpenId" empty-label="请选择用户" :options="userSelectOptions" @search="searchUsers" />
          </label>
          <label>
            <span>搜索券包</span>
            <RemoteSelectField v-model="form.couponPackId" v-model:keyword="selectorQuery.couponPackKeyword" placeholder="券包名称" empty-label="请选择券包" :options="couponPackSelectOptions" @search="searchCouponPacks" />
          </label>
        </div>
        <div class="dialog-actions"><button type="button" class="ghost-button" :disabled="submitting" @click="closeDialog">取消</button><button type="button" class="primary-button" :disabled="submitting" @click="submit">{{ submitting ? '提交中...' : '创建订单' }}</button></div>
      </div>
    </div>

    <div v-if="detailDialogVisible" class="dialog-mask" @click.self="closeDetailDialog">
      <div class="dialog-card dialog-card-v2 order-detail-dialog">
        <div class="dialog-head"><div class="dialog-head-main"><span class="section-kicker">订单详情</span><h3>订单明细</h3><p>查看支付流水与本订单发放的全部用户券。</p></div></div>
        <div v-if="detail" class="detail-grid">
          <div class="result-panel"><strong>订单号</strong><div class="cell-mono">{{ detail.orderNo }}</div></div>
          <div class="result-panel"><strong>订单状态</strong><div><span :class="['status-badge', statusClassMap[detail.status] ?? 'warning']">{{ statusMap[detail.status] || '-' }}</span></div></div>
          <div class="result-panel"><strong>订单金额</strong><div>{{ formatAmount(detail.orderAmount) }}</div></div>
          <div class="result-panel"><strong>用户 ID</strong><div>{{ detail.appUserId }}</div></div>
          <div class="result-panel"><strong>券包</strong><div>{{ detail.couponPackName }} (#{{ detail.couponPackId }})</div></div>
          <div class="result-panel"><strong>支付单号</strong><div class="cell-mono">{{ detail.paymentNo || '-' }}</div></div>
          <div class="result-panel"><strong>支付时间</strong><div>{{ formatDate(detail.paidAt) }}</div></div>
          <div class="result-panel"><strong>创建时间</strong><div>{{ formatDate(detail.createdAt) }}</div></div>
        </div>

        <div class="card toolbar-card detail-history-card"><div class="toolbar-title"><span class="section-kicker">支付流水</span><h3>支付记录</h3><p class="section-tip">展示当前订单关联的支付记录与处理结果。</p></div><div v-if="!detail || detail.payments.length === 0" class="empty-text">暂无支付记录</div><div v-else class="table-wrap"><table class="table"><thead><tr><th>支付单号</th><th>金额</th><th>状态</th><th>渠道流水号</th><th>支付时间</th></tr></thead><tbody><tr v-for="payment in detail.payments" :key="payment.id"><td class="cell-mono">{{ payment.paymentNo }}</td><td>{{ formatAmount(payment.amount) }}</td><td><span :class="['status-badge', payment.status === 2 ? 'success' : 'warning']">{{ payment.status === 2 ? '成功' : '待处理' }}</span></td><td class="cell-mono">{{ payment.channelTradeNo || '-' }}</td><td>{{ formatDate(payment.paidAt) }}</td></tr></tbody></table></div></div>

        <div class="card toolbar-card detail-history-card"><div class="toolbar-title"><span class="section-kicker">发券结果</span><h3>订单发放券</h3><p class="section-tip">查看本订单完成支付后实际发放到用户卡包的券。</p></div><div v-if="!detail || detail.grantedCoupons.length === 0" class="empty-text">当前订单暂无发券记录</div><div v-else class="table-wrap"><table class="table"><thead><tr><th>模板</th><th>优惠</th><th>券码</th><th>状态</th><th>有效期</th></tr></thead><tbody><tr v-for="coupon in detail.grantedCoupons" :key="coupon.id"><td><div class="table-primary-cell"><strong>{{ coupon.couponTemplateName }}</strong><span>{{ templateTypeMap[coupon.templateType] || '-' }}</span></div></td><td>{{ formatCouponBenefit(coupon) }}</td><td class="cell-mono">{{ coupon.couponCode }}</td><td><span :class="['status-badge', coupon.status === 1 ? 'success' : 'warning']">{{ couponStatusMap[coupon.status] || '未知' }}</span></td><td><div class="table-primary-cell"><strong>{{ formatDate(coupon.effectiveAt) }}</strong><span>至 {{ formatDate(coupon.expireAt) }}</span></div></td></tr></tbody></table></div></div>

        <div class="dialog-actions"><button v-if="canPay && detail?.status === 1" type="button" class="ghost-button" :disabled="payingOrderId === detail.id" @click="payOrder(detail.id)">{{ payingOrderId === detail.id ? '处理中...' : '处理支付' }}</button><button type="button" class="primary-button" :disabled="submitting || payingOrderId === detail?.id" @click="closeDetailDialog">关闭</button></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import RemoteSelectField from '@/components/RemoteSelectField.vue'
import { createCouponOrder, getCouponOrderDetail, getCouponOrderList } from '@/api/coupon-pack'
import { getCouponPackList } from '@/api/coupon-pack'
import { createPayment, paymentCallback } from '@/api/payment'
import type { CouponOrderDetailDto, CouponOrderListItemDto } from '@/types/coupon-pack'
import type { CouponPackListItemDto } from '@/types/coupon-pack'
import { getUserList } from '@/api/user'
import type { UserListItemDto } from '@/types/user'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const statusMap: Record<number, string> = { 1: '待支付', 2: '已支付', 3: '已关闭' }
const statusClassMap: Record<number, 'success' | 'warning' | 'danger'> = { 1: 'warning', 2: 'success', 3: 'danger' }
const couponStatusMap: Record<number, string> = { 1: '待使用', 2: '已核销', 3: '已过期', 4: '已失效' }
const templateTypeMap: Record<number, string> = { 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }

const items = ref<CouponOrderListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const detailDialogVisible = ref(false)
const detail = ref<CouponOrderDetailDto | null>(null)
const userOptions = ref<UserListItemDto[]>([])
const couponPackOptions = ref<CouponPackListItemDto[]>([])
const selectorQuery = reactive({ userKeyword: '', couponPackKeyword: '' })
const submitting = ref(false)
const payingOrderId = ref<number | null>(null)
const canCreate = authStorage.hasPermission('coupon-order.create')
const canPay = authStorage.hasPermission('coupon-order.pay')

const query = reactive({ keyword: '' })
const filters = reactive({ status: 'all', userId: 0 })
const form = reactive({ userId: 0, couponPackId: 0 })

const pendingCount = computed(() => items.value.filter((item) => item.status === 1).length)
const paidCount = computed(() => items.value.filter((item) => item.status === 2).length)
const userSelectOptions = computed(() => userOptions.value.map((user) => ({ value: user.id, label: formatUserLabel(user) })))
const couponPackSelectOptions = computed(() => couponPackOptions.value.map((pack) => ({ value: pack.id, label: formatCouponPackLabel(pack) })))
const querySummary = computed(() => `订单号：${query.keyword || '全部'} / 状态：${filters.status === 'all' ? '全部' : statusMap[Number(filters.status)]} / 用户：${filters.userId || '全部'} / 每页 ${pageSize.value} 条`)
const filteredItems = computed(() => items.value.filter((item) => (filters.status === 'all' || item.status === Number(filters.status)) && (!filters.userId || item.appUserId === filters.userId)))

const formatAmount = (value: number) => `¥${Number(value || 0).toFixed(2)}`
const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const formatUserLabel = (user: UserListItemDto) => user.mobile ? `${user.mobile} / 用户 #${user.id}` : (user.nickname?.trim() || user.miniOpenId || `用户 #${user.id}`)
const formatCouponPackLabel = (pack: CouponPackListItemDto) => `${pack.name} / ¥${Number(pack.salePrice || 0).toFixed(2)}`
const formatCouponBenefit = (coupon: { templateType: number; discountAmount?: number; thresholdAmount?: number; isNewUserOnly: boolean }) => {
  if (coupon.templateType === 4) return `满 ${Number(coupon.thresholdAmount || 0).toFixed(2)} 减 ${Number(coupon.discountAmount || 0).toFixed(2)}`
  if (coupon.discountAmount && coupon.discountAmount > 0) return coupon.isNewUserOnly ? `新人立减 ${Number(coupon.discountAmount).toFixed(2)}` : `立减 ${Number(coupon.discountAmount).toFixed(2)}`
  return coupon.isNewUserOnly ? '新人专享' : '-'
}

const loadOptions = async () => {
  try {
    const [userResponse, couponPackResponse] = await Promise.all([
      getUserList({ keyword: selectorQuery.userKeyword || undefined, pageIndex: 1, pageSize: 50 }),
      getCouponPackList({ keyword: selectorQuery.couponPackKeyword || undefined, pageIndex: 1, pageSize: 50 }),
    ])
    userOptions.value = userResponse.data.items
    couponPackOptions.value = couponPackResponse.data.items
  } catch (error) {
    notify.error(getErrorMessage(error, '加载订单选项失败'))
  }
}

const searchUsers = async () => { await loadOptions() }
const searchCouponPacks = async () => { await loadOptions() }

const loadData = async () => {
  try {
    const response = await getCouponOrderList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) { notify.error(getErrorMessage(error, '加载订单列表失败')) }
}

const handleSearch = async () => { pageIndex.value = 1; await loadData() }
const resetQuery = async () => { query.keyword = ''; filters.status = 'all'; filters.userId = 0; pageSize.value = 10; pageIndex.value = 1; await loadData(); notify.info('已重置订单筛选条件') }
const handlePageSizeChange = async () => { pageIndex.value = 1; await loadData() }
const goPrevPage = async () => { if (pageIndex.value <= 1) return; pageIndex.value -= 1; await loadData() }
const goNextPage = async () => { if (pageIndex.value >= totalPages.value) return; pageIndex.value += 1; await loadData() }

const resetForm = () => { form.userId = 0; form.couponPackId = 0 }
const openCreateDialog = async () => { resetForm(); dialogVisible.value = true; await loadOptions() }
const closeDialog = () => { dialogVisible.value = false; resetForm(); selectorQuery.userKeyword = ''; selectorQuery.couponPackKeyword = '' }

const openDetailDialog = async (orderId: number) => {
  try { const response = await getCouponOrderDetail(orderId); detail.value = response.data; detailDialogVisible.value = true } catch (error) { notify.error(getErrorMessage(error, '加载订单详情失败')) }
}
const closeDetailDialog = () => { detailDialogVisible.value = false; detail.value = null }

const submit = async () => {
  if (form.userId <= 0) return notify.info('请选择用户')
  if (form.couponPackId <= 0) return notify.info('请选择券包')
  if (submitting.value) return
  submitting.value = true
  try { await createCouponOrder({ ...form }); closeDialog(); pageIndex.value = 1; await loadData(); notify.success('订单创建成功') } catch (error) { notify.error(getErrorMessage(error, '创建订单失败')) } finally { submitting.value = false }
}

const payOrder = async (orderId: number) => {
  if (payingOrderId.value) return
  payingOrderId.value = orderId
  try {
    const payment = await createPayment({ orderId })
    await paymentCallback({ paymentNo: payment.data.paymentNo, success: true, channelTradeNo: `PAY-${payment.data.paymentNo}`, rawCallback: 'payment success' })
    await loadData()
    if (detail.value?.id === orderId) await openDetailDialog(orderId)
    notify.success('支付处理成功，已刷新订单状态')
  } catch (error) { notify.error(getErrorMessage(error, '支付处理失败')) } finally { payingOrderId.value = null }
}

onMounted(async () => {
  await Promise.all([loadData(), loadOptions()])
})
</script>

<style scoped>
.order-hero { background: radial-gradient(circle at top right, rgba(234,179,8,.12), transparent 28%), linear-gradient(135deg, #ffffff 0%, #fffdf7 52%, #f5f8fb 100%); }
.hero-side-stack { align-content: stretch; }
.hero-side-grid { display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 12px; }
.quick-card-spotlight { min-height: 148px; background: linear-gradient(135deg, rgba(234,179,8,.08), rgba(59,130,246,.03)); border: 1px solid rgba(234,179,8,.14); }
.quick-card.compact { min-height: 112px; }
.quick-card-label { display: inline-flex; width: fit-content; padding: 4px 10px; border-radius: 999px; background: rgba(37,99,235,.08); color: var(--primary); font-size: 12px; font-weight: 700; }
.order-filter-grid { grid-template-columns: 1.2fr .8fr .8fr .8fr 1.4fr; }
.field-card { display: grid; gap: 10px; padding: 16px; border-radius: 18px; border: 1px solid rgba(226,232,240,.96); background: linear-gradient(180deg,#fff 0%,#fbfdff 100%); }
.field-label { font-size: 12px; font-weight: 700; color: #475467; letter-spacing: .04em; }
.summary-field strong { font-size: 16px; }
.summary-field p { margin: 0; color: var(--muted); font-size: 13px; line-height: 1.6; }
.order-summary-card { grid-column: span 1; }
.order-dialog { width: min(720px, calc(100vw - 48px)); }
.order-detail-dialog { width: min(1080px, calc(100vw - 48px)); }
.order-form-grid { grid-template-columns: repeat(2, minmax(0,1fr)); }
.dialog-form>label { display: grid; gap: 8px; }
.dialog-form>label>span { font-size: 13px; font-weight: 700; color: #344054; }
.dialog-form input,.dialog-form select,.filter-field select { width: 100%; height: 44px; padding: 0 14px; border: 1px solid var(--line-strong); border-radius: 12px; background: #fff; }
.detail-grid { display: grid; grid-template-columns: repeat(4, minmax(0,1fr)); gap: 12px; }
.result-panel { display: grid; gap: 6px; padding: 14px 16px; border-radius: 16px; border: 1px solid rgba(226,232,240,.96); background: #fff; }
.result-panel strong { color: #344054; font-size: 13px; }
.detail-history-card { margin-top: 4px; }
@media (max-width:1100px){ .order-filter-grid,.order-form-grid,.detail-grid,.hero-side-grid{ grid-template-columns: repeat(2, minmax(0,1fr)); } }
@media (max-width:820px){ .order-filter-grid,.order-form-grid,.detail-grid,.hero-side-grid{ grid-template-columns: 1fr; } }
</style>
