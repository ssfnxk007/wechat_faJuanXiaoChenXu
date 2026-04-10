<template>
  <div class="business-page page-v2 user-coupon-page">
    <section class="hero-panel user-coupon-hero">
      <div class="hero-copy">
        <span class="page-kicker">发券运营</span>
        <h2>用户券中心</h2>
        <p>统一查看发券结果、券码状态、有效期与核销轨迹，并支持手动发券、批量导入发券和二维码展示。</p>
        <div class="hero-tags">
          <span class="badge info">公开领取与定向发放统一归档</span>
          <span class="badge success">支持二维码核销展示</span>
          <span class="badge warning">支持 CSV 批量导入</span>
        </div>
      </div>
      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight">
          <span class="quick-card-label">当前记录</span>
          <strong>{{ totalCount }}</strong>
          <p>当前筛选条件下的用户券归档数，便于运营快速定位发券结果。</p>
        </article>
        <div class="hero-side-grid">
          <article class="quick-card compact">
            <span class="quick-card-label">待使用</span>
            <strong>{{ unusedCount }}</strong>
            <p>已发放且未核销</p>
          </article>
          <article class="quick-card compact">
            <span class="quick-card-label">已核销</span>
            <strong>{{ usedCount }}</strong>
            <p>已完成核销处理</p>
          </article>
        </div>
      </div>
    </section>

    <div class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">用户券总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">当前查询范围内的发券结果</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">当前页码</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">待使用</span>
        <strong class="stat-value">{{ unusedCount }}</strong>
        <span class="stat-footnote">当前页可用于核销的券</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">已过期 / 已失效</span>
        <strong class="stat-value">{{ expiredCount }}</strong>
        <span class="stat-footnote">需重点关注的失效记录</span>
      </article>
    </div>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">检索与动作</span>
          <h3>用户券查询台</h3>
          <p class="section-tip">按用户 ID 或券码快速定位记录，并在同一入口完成发券动作与结果核对。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canGrant" type="button" class="primary-button" @click="openGrantDialog">手动发券</button>
          <button v-if="canGrant" type="button" class="ghost-button" @click="openImportDialog">导入发券</button>
        </div>
      </div>

      <div class="filter-panel-grid user-coupon-filter-grid">
        <label class="field-card filter-field compact-field">
          <span class="field-label">用户 ID</span>
          <input v-model.number="query.userId" type="number" min="1" placeholder="输入用户 ID" @keyup.enter="handleSearch" />
        </label>
        <label class="field-card filter-field">
          <span class="field-label">券码</span>
          <input v-model.trim="query.couponCode" type="text" placeholder="输入券码后回车检索" @keyup.enter="handleSearch" />
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
          <p>支持直接打开二维码与券详情，便于与门店核销岗位联动核对。</p>
        </div>
      </div>
    </section>

    <section class="operation-grid">
      <article class="card card-v2 action-entry-card">
        <div class="entry-head">
          <span class="section-kicker">人工处理</span>
          <h3>手动发券</h3>
          <p class="section-tip">按用户 ID 批量发放指定模板，适用于活动补发、运营定向投放与现场处理。</p>
        </div>
        <div class="entry-metrics">
          <div>
            <strong>{{ lastGrantSummary }}</strong>
            <span>最近一次发券结果</span>
          </div>
          <button v-if="canGrant" type="button" class="primary-button" @click="openGrantDialog">立即发券</button>
        </div>
      </article>

      <article class="card card-v2 action-entry-card">
        <div class="entry-head">
          <span class="section-kicker">批量处理</span>
          <h3>CSV 导入发券</h3>
          <p class="section-tip">支持按用户 ID、手机号、小程序 OpenId、公众号 OpenId 批量导入匹配发券。</p>
        </div>
        <div class="entry-metrics">
          <div>
            <strong>{{ lastImportSummary }}</strong>
            <span>最近一次导入结果</span>
          </div>
          <button v-if="canGrant" type="button" class="ghost-button" @click="openImportDialog">打开导入面板</button>
        </div>
      </article>
    </section>

    <section class="card card-v2 data-card archive-card">
      <div class="section-head">
        <div class="section-head-main">
          <span class="section-kicker">发券档案</span>
          <h3>用户券列表</h3>
          <p class="section-tip">集中展示用户券状态、有效期与券码，可直接查看二维码、详情与核销记录。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">当前页 {{ items.length }} 条</span>
          <span class="badge warning">每页 {{ pageSize }} 条</span>
        </div>
      </div>

      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>用户与模板</th>
              <th>券码</th>
              <th>状态</th>
              <th>有效期</th>
              <th>领取时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="table-primary-cell">
                  <strong>用户 #{{ item.appUserId }}</strong>
                  <span>模板 #{{ item.couponTemplateId }}</span>
                </div>
              </td>
              <td class="cell-mono">{{ item.couponCode }}</td>
              <td>
                <span :class="['status-badge', statusClassMap[item.status] ?? 'warning']">
                  {{ statusMap[item.status] ?? '未知状态' }}
                </span>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ formatDate(item.effectiveAt) }}</strong>
                  <span>至 {{ formatDate(item.expireAt) }}</span>
                </div>
              </td>
              <td>{{ formatDate(item.receivedAt) }}</td>
              <td>
                <div class="table-actions">
                  <button type="button" class="action-button" @click="openDetailDialog(item)">详情</button>
                  <button type="button" class="action-button" @click="openQrDialog(item)">二维码</button>
                  <button type="button" class="action-button" @click="copyCouponCode(item.couponCode)">复制券码</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="7" class="empty-text">当前没有符合条件的用户券记录</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager pager-v2">
        <div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条记录</div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </div>
    </section>

    <div v-if="grantDialogVisible" class="dialog-mask" @click.self="closeGrantDialog">
      <div class="dialog-card dialog-card-v2 user-coupon-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">人工发券</span>
            <h3>手动发券</h3>
            <p>输入券模板 ID 与用户 ID 列表，系统将按每用户数量进行批量发放。</p>
          </div>
        </div>

        <div class="grid-form dialog-form user-coupon-form-grid">
          <label>
            <span>券模板 ID</span>
            <input v-model.number="grantForm.couponTemplateId" type="number" min="1" placeholder="例如：3" />
          </label>
          <label>
            <span>每用户发放张数</span>
            <input v-model.number="grantForm.quantityPerUser" type="number" min="1" step="1" />
          </label>
          <label class="field-span-2">
            <span>用户 ID 列表</span>
            <textarea v-model.trim="grantForm.appUserIdsText" rows="6" placeholder="多个用户 ID 可用逗号、空格或换行分隔"></textarea>
          </label>
        </div>

        <div v-if="grantResult" class="result-board">
          <div class="result-board-summary">
            <strong>发券结果</strong>
            <span>成功 {{ grantResult.successCount }} / 失败 {{ grantResult.failureCount }}</span>
          </div>
          <div class="result-board-list">
            <article v-for="item in grantResult.items" :key="`${item.appUserId}-${item.message}`" class="result-item-card">
              <strong>用户 #{{ item.appUserId }}</strong>
              <span :class="['status-badge', item.success ? 'success' : 'danger']">{{ item.success ? '成功' : '失败' }}</span>
              <p>{{ item.message }}</p>
              <small>发放数量：{{ item.grantedCount }}</small>
            </article>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeGrantDialog">关闭</button>
          <button type="button" class="primary-button" @click="submitGrant">确认发券</button>
        </div>
      </div>
    </div>

    <div v-if="importDialogVisible" class="dialog-mask" @click.self="closeImportDialog">
      <div class="dialog-card dialog-card-v2 user-coupon-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">批量导入</span>
            <h3>CSV 导入发券</h3>
            <p>可导入用户 ID、手机号、小程序 OpenId、公众号 OpenId 等标识完成批量发券。</p>
          </div>
          <button type="button" class="ghost-button" @click="downloadImportTemplate">下载模板</button>
        </div>

        <div class="grid-form dialog-form user-coupon-form-grid">
          <label>
            <span>券模板 ID</span>
            <input v-model.number="importForm.couponTemplateId" type="number" min="1" placeholder="例如：3" />
          </label>
          <label>
            <span>每用户发放张数</span>
            <input v-model.number="importForm.quantityPerUser" type="number" min="1" step="1" />
          </label>
          <label class="field-span-2 file-field">
            <span>CSV 文件</span>
            <input type="file" accept=".csv" @change="handleImportFileChange" />
          </label>
        </div>

        <div class="helper-card">
          <strong>导入模板字段建议</strong>
          <p>推荐首行包含以下任一标识列：<code>appUserId</code>、<code>mobile</code>、<code>miniOpenId</code>、<code>officialOpenId</code>。</p>
          <p>可选附带 <code>couponTemplateId</code> 与 <code>quantityPerUser</code>；若未填写则以本次表单参数为准。</p>
        </div>

        <div v-if="importResult" class="result-board">
          <div class="result-board-summary">
            <strong>导入结果</strong>
            <span>成功 {{ importResult.successCount }} / 失败 {{ importResult.failureCount }}</span>
          </div>
          <div class="import-result-grid">
            <div class="result-panel">
              <strong>总行数</strong>
              <div>{{ importResult.totalRows }}</div>
            </div>
            <div class="result-panel">
              <strong>识别用户数</strong>
              <div>{{ importResult.parsedUserCount }}</div>
            </div>
            <div class="result-panel">
              <strong>模板 ID</strong>
              <div>{{ importResult.couponTemplateId }}</div>
            </div>
            <div class="result-panel">
              <strong>发放数量</strong>
              <div>{{ importResult.quantityPerUser }}</div>
            </div>
          </div>
          <div v-if="importResult.invalidRows.length > 0" class="invalid-rows">
            <div class="invalid-title">无效数据行</div>
            <ul>
              <li v-for="row in importResult.invalidRows" :key="row">{{ row }}</li>
            </ul>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeImportDialog">关闭</button>
          <button type="button" class="primary-button" @click="submitImport">开始导入</button>
        </div>
      </div>
    </div>

    <div v-if="qrDialogVisible" class="dialog-mask" @click.self="closeQrDialog">
      <div class="dialog-card dialog-card-v2 qr-dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">二维码</span>
            <h3>用户券二维码</h3>
            <p>ERP 可扫描用户出示的二维码，并调用核销接口完成处理。</p>
          </div>
        </div>

        <div class="qr-panel">
          <img v-if="qrCodeDataUrl" :src="qrCodeDataUrl" alt="用户券二维码" class="qr-image" />
          <div class="qr-code-text cell-mono">{{ selectedCoupon?.couponCode || '-' }}</div>
          <div class="qr-meta-grid">
            <div class="result-panel">
              <strong>用户</strong>
              <div>#{{ selectedCoupon?.appUserId ?? '-' }}</div>
            </div>
            <div class="result-panel">
              <strong>模板</strong>
              <div>#{{ selectedCoupon?.couponTemplateId ?? '-' }}</div>
            </div>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="copyCouponCode(selectedCoupon?.couponCode || '')">复制券码</button>
          <button type="button" class="primary-button" @click="closeQrDialog">关闭</button>
        </div>
      </div>
    </div>

    <div v-if="detailDialogVisible" class="dialog-mask" @click.self="closeDetailDialog">
      <div class="dialog-card dialog-card-v2 user-coupon-detail-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">券详情</span>
            <h3>用户券明细</h3>
            <p>查看模板信息、优惠规则、领取时间、有效期与核销轨迹。</p>
          </div>
        </div>

        <div class="detail-grid">
          <div class="result-panel">
            <strong>状态</strong>
            <div>
              <span :class="['status-badge', statusClassMap[detailCoupon?.status || 0] ?? 'warning']">
                {{ statusMap[detailCoupon?.status || 0] ?? '-' }}
              </span>
            </div>
          </div>
          <div class="result-panel">
            <strong>模板名称</strong>
            <div>{{ detailCoupon?.couponTemplateName || '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>券类型</strong>
            <div>{{ templateTypeMap[detailCoupon?.templateType || 0] || '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>模板状态</strong>
            <div>
              <span :class="['status-badge', detailCoupon?.templateEnabled ? 'success' : 'danger']">
                {{ detailCoupon?.templateEnabled ? '启用' : '停用' }}
              </span>
            </div>
          </div>
          <div class="result-panel">
            <strong>有效期规则</strong>
            <div>{{ validPeriodTypeMap[detailCoupon?.validPeriodType || 0] || '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>券码</strong>
            <div class="cell-mono">{{ detailCoupon?.couponCode || '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>优惠金额</strong>
            <div>{{ formatMoney(detailCoupon?.discountAmount) }}</div>
          </div>
          <div class="result-panel">
            <strong>门槛金额</strong>
            <div>{{ formatMoney(detailCoupon?.thresholdAmount) }}</div>
          </div>
          <div class="result-panel">
            <strong>每用户限领</strong>
            <div>{{ detailCoupon?.perUserLimit ?? '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>新人券</strong>
            <div>{{ detailCoupon?.isNewUserOnly ? '是' : '否' }}</div>
          </div>
          <div class="result-panel">
            <strong>门店范围</strong>
            <div>{{ detailCoupon?.isAllStores ? '全部门店可用' : '指定门店可用' }}</div>
          </div>
          <div class="result-panel">
            <strong>领取时间</strong>
            <div>{{ formatDate(detailCoupon?.receivedAt) }}</div>
          </div>
          <div class="result-panel">
            <strong>生效时间</strong>
            <div>{{ formatDate(detailCoupon?.effectiveAt) }}</div>
          </div>
          <div class="result-panel">
            <strong>失效时间</strong>
            <div>{{ formatDate(detailCoupon?.expireAt) }}</div>
          </div>
          <div class="result-panel">
            <strong>固定有效期</strong>
            <div>{{ formatDate(detailCoupon?.validFrom) }} ~ {{ formatDate(detailCoupon?.validTo) }}</div>
          </div>
          <div class="result-panel">
            <strong>领后有效天数</strong>
            <div>{{ detailCoupon?.validDays ?? '-' }}</div>
          </div>
          <div class="result-panel field-span-3">
            <strong>模板备注</strong>
            <div>{{ detailCoupon?.templateRemark || '-' }}</div>
          </div>
        </div>

        <div class="card toolbar-card detail-history-card">
          <div class="toolbar-title">
            <span class="section-kicker">核销轨迹</span>
            <h3>核销记录</h3>
            <p class="section-tip">查看该券是否被核销、在哪个门店核销、由谁处理及设备信息。</p>
          </div>

          <div v-if="writeOffRecords.length === 0" class="empty-text">暂无核销记录</div>
          <div v-else class="table-wrap">
            <table class="table">
              <thead>
                <tr>
                  <th>核销时间</th>
                  <th>门店</th>
                  <th>操作人</th>
                  <th>设备号</th>
                  <th>券码</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="record in writeOffRecords" :key="record.id">
                  <td>{{ formatDate(record.writeOffAt) }}</td>
                  <td>{{ record.storeName || `门店#${record.storeId}` }}</td>
                  <td>{{ record.operatorName || '-' }}</td>
                  <td>{{ record.deviceCode || '-' }}</td>
                  <td class="cell-mono">{{ record.couponCode }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="openQrDialog(selectedCoupon!)">查看二维码</button>
          <button type="button" class="primary-button" @click="closeDetailDialog">关闭</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import QRCode from 'qrcode'
import {
  getUserCouponDetail,
  getUserCouponList,
  getUserCouponWriteOffRecords,
  importGrantUserCoupons,
  manualGrantUserCoupons,
} from '@/api/user-coupon'
import type {
  CouponWriteOffRecordDto,
  ImportGrantUserCouponsResultDto,
  ManualGrantUserCouponsResultDto,
  UserCouponDetailDto,
  UserCouponListItemDto,
} from '@/types/user-coupon'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<UserCouponListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)

const grantDialogVisible = ref(false)
const importDialogVisible = ref(false)
const qrDialogVisible = ref(false)
const detailDialogVisible = ref(false)

const selectedCoupon = ref<UserCouponListItemDto | null>(null)
const detailCoupon = ref<UserCouponDetailDto | null>(null)
const writeOffRecords = ref<CouponWriteOffRecordDto[]>([])
const grantResult = ref<ManualGrantUserCouponsResultDto | null>(null)
const importResult = ref<ImportGrantUserCouponsResultDto | null>(null)
const importFile = ref<File | null>(null)
const qrCodeDataUrl = ref('')

const query = reactive({
  userId: undefined as number | undefined,
  couponCode: '',
})

const grantForm = reactive({
  couponTemplateId: 0,
  quantityPerUser: 1,
  appUserIdsText: '',
})

const importForm = reactive({
  couponTemplateId: 0,
  quantityPerUser: 1,
})

const statusMap: Record<number, string> = {
  1: '待使用',
  2: '已核销',
  3: '已过期',
  4: '已失效',
}

const statusClassMap: Record<number, 'success' | 'warning' | 'danger'> = {
  1: 'success',
  2: 'warning',
  3: 'danger',
  4: 'danger',
}

const templateTypeMap: Record<number, string> = {
  1: '新人券',
  2: '无门槛券',
  3: '指定商品券',
  4: '满减券',
}

const validPeriodTypeMap: Record<number, string> = {
  1: '固定日期范围',
  2: '领取后 N 天有效',
}

const canGrant = authStorage.hasPermission('user-coupon.grant')

const unusedCount = computed(() => items.value.filter((item) => item.status === 1).length)
const usedCount = computed(() => items.value.filter((item) => item.status === 2).length)
const expiredCount = computed(() => items.value.filter((item) => item.status === 3 || item.status === 4).length)

const querySummary = computed(() => {
  const user = query.userId ? `用户 #${query.userId}` : '全部用户'
  const code = query.couponCode ? `券码 ${query.couponCode}` : '全部券码'
  return `${user} / ${code} / 每页 ${pageSize.value} 条`
})

const lastGrantSummary = computed(() => {
  if (!grantResult.value) return '尚未执行手动发券'
  return `成功 ${grantResult.value.successCount}，失败 ${grantResult.value.failureCount}`
})

const lastImportSummary = computed(() => {
  if (!importResult.value) return '尚未执行导入发券'
  return `成功 ${importResult.value.successCount}，失败 ${importResult.value.failureCount}`
})

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const formatMoney = (value?: number) => (typeof value === 'number' ? `¥${value.toFixed(2)}` : '-')

const parseAppUserIds = (text: string) =>
  Array.from(
    new Set(
      text
        .split(/[\s,]+/)
        .map((item) => Number(item.trim()))
        .filter((item) => Number.isInteger(item) && item > 0),
    ),
  )

const loadData = async () => {
  try {
    const response = await getUserCouponList({
      userId: query.userId,
      couponCode: query.couponCode || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })

    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载用户券列表失败'))
  }
}

const handleSearch = async () => {
  pageIndex.value = 1
  await loadData()
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

const openGrantDialog = () => {
  grantDialogVisible.value = true
  grantResult.value = null
}

const closeGrantDialog = () => {
  grantDialogVisible.value = false
  grantResult.value = null
  grantForm.couponTemplateId = 0
  grantForm.quantityPerUser = 1
  grantForm.appUserIdsText = ''
}

const openImportDialog = () => {
  importDialogVisible.value = true
  importResult.value = null
}

const closeImportDialog = () => {
  importDialogVisible.value = false
  importResult.value = null
  importFile.value = null
  importForm.couponTemplateId = 0
  importForm.quantityPerUser = 1
}

const handleImportFileChange = (event: Event) => {
  const target = event.target as HTMLInputElement
  importFile.value = target.files?.[0] || null
}

const downloadImportTemplate = () => {
  const content = 'mobile,couponTemplateId,quantityPerUser\n13800138000,3,1\n13900139000,3,2\n'
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = 'user-coupon-import-template.csv'
  link.click()
  URL.revokeObjectURL(url)
}

const submitGrant = async () => {
  const appUserIds = parseAppUserIds(grantForm.appUserIdsText)

  if (grantForm.couponTemplateId <= 0) {
    notify.info('请输入券模板 ID')
    return
  }

  if (grantForm.quantityPerUser <= 0) {
    notify.info('每用户发放张数必须大于 0')
    return
  }

  if (appUserIds.length === 0) {
    notify.info('请至少输入一个用户 ID')
    return
  }

  try {
    const response = await manualGrantUserCoupons({
      couponTemplateId: grantForm.couponTemplateId,
      quantityPerUser: grantForm.quantityPerUser,
      appUserIds,
    })
    grantResult.value = response.data
    notify.success(`手动发券完成，成功 ${response.data.successCount} 人`)
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '手动发券失败'))
  }
}

const submitImport = async () => {
  if (importForm.couponTemplateId <= 0) {
    notify.info('请输入券模板 ID')
    return
  }

  if (importForm.quantityPerUser <= 0) {
    notify.info('每用户发放张数必须大于 0')
    return
  }

  if (!importFile.value) {
    notify.info('请先选择 CSV 文件')
    return
  }

  try {
    const response = await importGrantUserCoupons(importFile.value, importForm.couponTemplateId, importForm.quantityPerUser)
    importResult.value = response.data
    notify.success(`导入发券完成，成功 ${response.data.successCount} 人`)
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '导入发券失败'))
  }
}

const openQrDialog = async (item: UserCouponListItemDto) => {
  try {
    selectedCoupon.value = item
    qrCodeDataUrl.value = await QRCode.toDataURL(item.couponCode, {
      width: 240,
      margin: 1,
    })
    qrDialogVisible.value = true
  } catch (error) {
    notify.error(getErrorMessage(error, '生成二维码失败'))
  }
}

const closeQrDialog = () => {
  qrDialogVisible.value = false
  qrCodeDataUrl.value = ''
}

const openDetailDialog = async (item: UserCouponListItemDto) => {
  try {
    selectedCoupon.value = item
    const [detailResponse, recordResponse] = await Promise.all([
      getUserCouponDetail(item.id),
      getUserCouponWriteOffRecords(item.id),
    ])
    detailCoupon.value = detailResponse.data
    writeOffRecords.value = recordResponse.data
    detailDialogVisible.value = true
  } catch (error) {
    notify.error(getErrorMessage(error, '加载用户券详情失败'))
  }
}

const closeDetailDialog = () => {
  detailDialogVisible.value = false
  detailCoupon.value = null
  writeOffRecords.value = []
}

const copyCouponCode = async (couponCode: string) => {
  if (!couponCode) return

  try {
    await navigator.clipboard.writeText(couponCode)
    notify.success('券码已复制')
  } catch (error) {
    notify.error(getErrorMessage(error, '复制券码失败'))
  }
}

onMounted(loadData)
</script>

<style scoped>
.user-coupon-page {
  gap: 22px;
}

.user-coupon-hero {
  background:
    radial-gradient(circle at top right, rgba(22, 163, 74, 0.12), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fcff 52%, #f3f7fb 100%);
}

.hero-side-stack {
  align-content: stretch;
}

.hero-side-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.quick-card-spotlight {
  min-height: 148px;
  background: linear-gradient(135deg, rgba(22, 163, 74, 0.08), rgba(59, 130, 246, 0.03));
  border: 1px solid rgba(22, 163, 74, 0.14);
}

.quick-card.compact {
  min-height: 112px;
}

.quick-card-label {
  display: inline-flex;
  width: fit-content;
  padding: 4px 10px;
  border-radius: 999px;
  background: rgba(37, 99, 235, 0.08);
  color: var(--primary);
  font-size: 12px;
  font-weight: 700;
}

.operations-card {
  gap: 18px;
}

.operation-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.action-entry-card {
  display: grid;
  gap: 16px;
}

.entry-head {
  display: grid;
  gap: 8px;
}

.entry-head h3,
.entry-head p {
  margin: 0;
}

.entry-metrics {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.entry-metrics div {
  display: grid;
  gap: 4px;
}

.entry-metrics strong {
  font-size: 18px;
}

.entry-metrics span {
  color: var(--muted);
  font-size: 13px;
}

.filter-panel-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.user-coupon-filter-grid {
  grid-template-columns: 0.8fr 1.2fr 0.8fr 1.2fr;
}

.field-card {
  display: grid;
  gap: 10px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(226, 232, 240, 0.96);
  background: linear-gradient(180deg, #fff 0%, #fbfdff 100%);
}

.field-label {
  font-size: 12px;
  font-weight: 700;
  color: #475467;
  letter-spacing: 0.04em;
}

.summary-field strong {
  font-size: 16px;
}

.summary-field p {
  margin: 0;
  color: var(--muted);
  font-size: 13px;
  line-height: 1.6;
}

.archive-card {
  gap: 18px;
}

.user-coupon-dialog {
  width: min(920px, calc(100vw - 48px));
}

.user-coupon-detail-dialog {
  width: min(1080px, calc(100vw - 48px));
}

.user-coupon-form-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.dialog-form > label {
  display: grid;
  gap: 8px;
}

.dialog-form > label > span {
  font-size: 13px;
  font-weight: 700;
  color: #344054;
}

.dialog-form input,
.dialog-form select,
.dialog-form textarea {
  width: 100%;
  padding: 12px 14px;
  border: 1px solid var(--line-strong);
  border-radius: 12px;
  background: #fff;
  resize: vertical;
}

.dialog-form input,
.dialog-form select {
  height: 44px;
  padding: 0 14px;
}

.file-field input {
  height: auto;
  padding: 10px 12px;
}

.result-board {
  display: grid;
  gap: 14px;
  padding: 18px;
  border-radius: 18px;
  border: 1px solid rgba(226, 232, 240, 0.96);
  background: linear-gradient(180deg, #fcfdff 0%, #f8fbff 100%);
}

.result-board-summary {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.result-board-list {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.result-item-card {
  display: grid;
  gap: 8px;
  padding: 14px;
  border-radius: 16px;
  border: 1px solid rgba(226, 232, 240, 0.96);
  background: #fff;
}

.result-item-card p,
.result-item-card small {
  margin: 0;
}

.helper-card {
  display: grid;
  gap: 8px;
  padding: 16px 18px;
  border-radius: 18px;
  background: linear-gradient(180deg, #fff 0%, #f8fbff 100%);
  border: 1px solid rgba(191, 219, 254, 0.72);
}

.helper-card p,
.helper-card strong {
  margin: 0;
}

.import-result-grid,
.qr-meta-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.result-panel {
  display: grid;
  gap: 6px;
  padding: 14px 16px;
  border-radius: 16px;
  border: 1px solid rgba(226, 232, 240, 0.96);
  background: #fff;
}

.result-panel strong {
  color: #344054;
  font-size: 13px;
}

.invalid-rows ul {
  margin: 8px 0 0;
  padding-left: 18px;
}

.invalid-title {
  font-weight: 700;
  color: #344054;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.detail-history-card {
  margin-top: 4px;
}

.qr-dialog-card {
  width: min(460px, calc(100vw - 48px));
}

.qr-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
  padding: 8px 0;
}

.qr-image {
  width: 240px;
  height: 240px;
  object-fit: contain;
}

.qr-code-text {
  word-break: break-all;
  text-align: center;
}

@media (max-width: 1100px) {
  .operation-grid,
  .detail-grid,
  .result-board-list,
  .user-coupon-filter-grid,
  .hero-side-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 820px) {
  .operation-grid,
  .detail-grid,
  .result-board-list,
  .user-coupon-filter-grid,
  .user-coupon-form-grid,
  .import-result-grid,
  .qr-meta-grid,
  .hero-side-grid {
    grid-template-columns: 1fr;
  }

  .entry-metrics,
  .result-board-summary {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
