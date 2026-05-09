<template>
  <div class="admin-page coupon-order-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="order-keyword">订单号</label>
          <input
            id="order-keyword"
            v-model.trim="query.keyword"
            type="text"
            placeholder="输入订单号回车检索"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="field">
          <label for="order-status">状态</label>
          <select id="order-status" v-model="filters.status" @change="handleSearch">
            <option value="all">全部</option>
            <option value="1">待支付</option>
            <option value="2">已支付</option>
            <option value="3">已退款</option>
            <option value="4">已关闭</option>
          </select>
        </div>
        <div class="field">
          <label for="order-user">用户</label>
          <select id="order-user" v-model.number="filters.userId" @change="handleSearch">
            <option :value="0">全部用户</option>
            <option v-for="user in userOptions" :key="user.id" :value="user.id">
              {{ formatUserLabel(user) }}
            </option>
          </select>
        </div>
        <div class="field">
          <label for="order-page-size">每页条数</label>
          <select id="order-page-size" v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">10 条</option>
            <option :value="20">20 条</option>
            <option :value="50">50 条</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="handleSearch">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button v-if="canCreate" type="button" class="primary-button compact" @click="openCreateDialog">+ 新建订单</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>订单查询</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="data-card-meta">{{ querySummary }}</div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 200px;">订单号</th>
              <th style="min-width: 120px;">用户</th>
              <th style="min-width: 200px;">券包</th>
              <th class="num-cell" style="min-width: 96px;">金额</th>
              <th style="min-width: 84px;">状态</th>
              <th style="min-width: 156px;">支付时间</th>
              <th style="min-width: 156px;">创建时间</th>
              <th style="min-width: 64px;">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredItems" :key="item.id">
              <td>{{ item.id }}</td>
              <td class="cell-mono">{{ item.orderNo }}</td>
              <td>用户 #{{ item.appUserId }}</td>
              <td>券包 #{{ item.couponPackId }}</td>
              <td class="num-cell">{{ formatAmount(item.orderAmount) }}</td>
              <td>
                <span :class="['status-badge', statusClassMap[item.status] ?? 'warning']">
                  {{ statusMap[item.status] || '未知' }}
                </span>
              </td>
              <td>{{ formatDate(item.paidAt) }}</td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <button type="button" class="cell-link" @click="openDetailDialog(item.id)">编辑</button>
              </td>
            </tr>
            <tr v-if="filteredItems.length === 0" class="empty-row">
              <td colspan="9">当前没有符合条件的订单记录</td>
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
      title="新建订单"
      sub="从现有用户与券包档案中选择，生成待支付订单。"
      size="md"
      @close="closeDialog"
    >
      <div class="dialog-form-grid">
        <label class="dialog-field">
          <span>搜索用户</span>
          <RemoteSelectField
            v-model="form.userId"
            v-model:keyword="selectorQuery.userKeyword"
            placeholder="手机号 / 昵称 / OpenId"
            empty-label="请选择用户"
            :options="userSelectOptions"
            @search="searchUsers"
          />
        </label>
        <label class="dialog-field">
          <span>搜索券包</span>
          <RemoteSelectField
            v-model="form.couponPackId"
            v-model:keyword="selectorQuery.couponPackKeyword"
            placeholder="券包名称"
            empty-label="请选择券包"
            :options="couponPackSelectOptions"
            @search="searchCouponPacks"
          />
        </label>
      </div>
      <template #footer>
        <button type="button" class="ghost-button compact" :disabled="submitting" @click="closeDialog">取消</button>
        <button type="button" class="primary-button compact" :disabled="submitting" @click="submit">
          {{ submitting ? '提交中...' : '创建订单' }}
        </button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="detailDialogVisible"
      title="订单明细"
      sub="支付流水与发券结果"
      size="xl"
      @close="closeDetailDialog"
    >
      <div v-if="detail" class="detail-grid">
        <div class="detail-cell"><span class="detail-label">订单号</span><div class="cell-mono">{{ detail.orderNo }}</div></div>
        <div class="detail-cell"><span class="detail-label">订单状态</span>
          <span :class="['status-badge', statusClassMap[detail.status] ?? 'warning']">{{ statusMap[detail.status] || '-' }}</span>
        </div>
        <div class="detail-cell"><span class="detail-label">订单金额</span><div>{{ formatAmount(detail.orderAmount) }}</div></div>
        <div class="detail-cell"><span class="detail-label">用户 ID</span><div>{{ detail.appUserId }}</div></div>
        <div class="detail-cell"><span class="detail-label">券包</span><div>{{ detail.couponPackName }} (#{{ detail.couponPackId }})</div></div>
        <div class="detail-cell"><span class="detail-label">支付单号</span><div class="cell-mono">{{ detail.paymentNo || '-' }}</div></div>
        <div class="detail-cell"><span class="detail-label">支付时间</span><div>{{ formatDate(detail.paidAt) }}</div></div>
        <div class="detail-cell"><span class="detail-label">创建时间</span><div>{{ formatDate(detail.createdAt) }}</div></div>
      </div>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>支付流水</h4>
          <span class="detail-section-tip">展示当前订单关联的支付记录与处理结果</span>
        </header>
        <div v-if="!detail || detail.payments.length === 0" class="detail-empty">暂无支付记录</div>
        <div v-else class="data-table-wrap">
          <table class="data-table">
            <thead>
              <tr>
                <th>支付单号</th>
                <th class="num-cell">金额</th>
                <th>状态</th>
                <th>渠道流水号</th>
                <th>支付时间</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="payment in detail.payments" :key="payment.id">
                <td class="cell-mono">{{ payment.paymentNo }}</td>
                <td class="num-cell">{{ formatAmount(payment.amount) }}</td>
                <td>
                  <span :class="['status-badge', payment.status === 2 ? 'success' : 'warning']">
                    {{ payment.status === 2 ? '成功' : '待处理' }}
                  </span>
                </td>
                <td class="cell-mono">{{ payment.channelTradeNo || '-' }}</td>
                <td>{{ formatDate(payment.paidAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>发券结果</h4>
          <span class="detail-section-tip">查看本订单完成支付后实际发放到用户卡包的券</span>
        </header>
        <div v-if="!detail || detail.grantedCoupons.length === 0" class="detail-empty">当前订单暂无发券记录</div>
        <div v-else class="data-table-wrap">
          <table class="data-table">
            <thead>
              <tr>
                <th>模板</th>
                <th>优惠</th>
                <th>券码</th>
                <th>状态</th>
                <th>有效期</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="coupon in detail.grantedCoupons" :key="coupon.id">
                <td>
                  <strong>{{ coupon.couponTemplateName }}</strong>
                  <span class="detail-subtle"> · {{ templateTypeMap[coupon.templateType] || '-' }}</span>
                </td>
                <td>{{ formatCouponBenefit(coupon) }}</td>
                <td class="cell-mono">{{ coupon.couponCode }}</td>
                <td>
                  <span :class="['status-badge', coupon.status === 1 ? 'success' : 'warning']">
                    {{ couponStatusMap[coupon.status] || '未知' }}
                  </span>
                </td>
                <td>
                  <div>{{ formatDate(coupon.effectiveAt) }}</div>
                  <div class="detail-subtle">至 {{ formatDate(coupon.expireAt) }}</div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <template #footer>
        <button
          v-if="canPay && detail?.status === 1"
          type="button"
          class="primary-button compact"
          :disabled="payingOrderId === detail?.id || refundingOrderId === detail?.id"
          @click="payOrder(detail!.id)"
        >
          {{ payingOrderId === detail?.id ? '处理中...' : '处理支付' }}
        </button>
        <button
          v-if="canRefund && detail?.status === 2"
          type="button"
          class="danger-button compact"
          :disabled="refundingOrderId === detail?.id || payingOrderId === detail?.id"
          @click="refundOrderAction(detail!.id)"
        >
          {{ refundingOrderId === detail?.id ? '退款中...' : '退款' }}
        </button>
        <button type="button" class="ghost-button compact" @click="closeDetailDialog">关闭</button>
      </template>
    </MainDetailDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import RemoteSelectField from '@/components/RemoteSelectField.vue'
import MainDetailDialog from '@/components/MainDetailDialog.vue'
import { createCouponOrder, getCouponOrderDetail, getCouponOrderList } from '@/api/coupon-pack'
import { getCouponPackList } from '@/api/coupon-pack'
import { refundOrder, syncPaidOrder } from '@/api/payment'
import type { CouponOrderDetailDto, CouponOrderListItemDto } from '@/types/coupon-pack'
import type { CouponPackListItemDto } from '@/types/coupon-pack'
import { getUserList } from '@/api/user'
import type { UserListItemDto } from '@/types/user'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils/auth'
import { notify } from '@/utils/notify'

const statusMap: Record<number, string> = { 1: '待支付', 2: '已支付', 3: '已退款', 4: '已关闭' }
const statusClassMap: Record<number, 'success' | 'warning' | 'danger'> = { 1: 'warning', 2: 'success', 3: 'danger', 4: 'danger' }
const couponStatusMap: Record<number, string> = { 1: '待使用', 2: '已核销', 3: '已过期', 4: '已失效', 5: '已回收' }
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
const refundingOrderId = ref<number | null>(null)
const canCreate = authStorage.hasPermission('coupon-order.create')
const canPay = authStorage.hasPermission('coupon-order.pay')
const canRefund = authStorage.hasPermission('coupon-order.refund')

const query = reactive({ keyword: '' })
const filters = reactive({ status: 'all', userId: 0 })
const form = reactive({ userId: 0, couponPackId: 0 })

const userSelectOptions = computed(() => userOptions.value.map((user) => ({ value: user.id, label: formatUserLabel(user) })))
const couponPackSelectOptions = computed(() => couponPackOptions.value.map((pack) => ({ value: pack.id, label: formatCouponPackLabel(pack) })))
const querySummary = computed(() => `订单号：${query.keyword || '全部'} · 状态：${filters.status === 'all' ? '全部' : statusMap[Number(filters.status)]} · 用户：${filters.userId || '全部'} · 每页 ${pageSize.value} 条`)
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
    await syncPaidOrder(orderId)
    await loadData()
    if (detail.value?.id === orderId) await openDetailDialog(orderId)
    notify.success('支付处理成功，已刷新订单状态')
  } catch (error) { notify.error(getErrorMessage(error, '支付处理失败')) } finally { payingOrderId.value = null }
}

const refundOrderAction = async (orderId: number) => {
  if (!confirm('确定要对该订单执行退款吗？退款后关联的用户券将被回收。')) return
  if (refundingOrderId.value) return
  refundingOrderId.value = orderId
  try {
    await refundOrder({ orderId })
    await loadData()
    if (detail.value?.id === orderId) await openDetailDialog(orderId)
    notify.success('退款成功，已刷新订单状态')
  } catch (error) { notify.error(getErrorMessage(error, '退款失败')) } finally { refundingOrderId.value = null }
}

const notifyColumnSettingsPlaceholder = () => {
  notify.info('列设置功能将在下一版本提供')
}

onMounted(async () => {
  await Promise.all([loadData(), loadOptions()])
})
</script>

<style scoped>
.coupon-order-page :deep(.dialog-body) {
  gap: 16px;
}

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

.detail-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.detail-cell {
  display: grid;
  gap: 4px;
  padding: 10px 12px;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: #fcfdff;
  font-size: 13px;
}

.detail-label {
  font-size: 12px;
  color: #475467;
  font-weight: 600;
}

.detail-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
  border: 1px solid var(--line);
  border-radius: 8px;
  overflow: hidden;
  background: #fff;
}

.detail-section-head {
  display: flex;
  align-items: baseline;
  gap: 12px;
  padding: 10px 14px;
  border-bottom: 1px solid var(--line);
  background: #fafbfc;
}

.detail-section-head h4 {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
  color: var(--text);
}

.detail-section-tip {
  font-size: 12px;
  color: var(--muted);
}

.detail-empty {
  padding: 20px;
  text-align: center;
  color: var(--muted);
  font-size: 13px;
}

.detail-subtle {
  color: var(--muted);
  font-size: 12px;
}

@media (max-width: 1100px) {
  .detail-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .dialog-form-grid { grid-template-columns: 1fr; }
}

@media (max-width: 720px) {
  .detail-grid { grid-template-columns: 1fr; }
}
</style>
