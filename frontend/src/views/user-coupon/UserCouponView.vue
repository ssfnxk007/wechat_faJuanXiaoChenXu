<template>
  <div class="admin-page user-coupon-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="uc-user">用户</label>
          <select id="uc-user" v-model.number="query.userId" @change="handleSearch">
            <option :value="0">全部用户</option>
            <option v-for="user in userOptions" :key="user.id" :value="user.id">{{ formatUserLabel(user) }}</option>
          </select>
        </div>
        <div class="field">
          <label for="uc-code">券码</label>
          <input id="uc-code" v-model.trim="query.couponCode" type="text" placeholder="输入券码后回车检索" @keyup.enter="handleSearch" />
        </div>
        <div class="field">
          <label for="uc-page-size">每页条数</label>
          <select id="uc-page-size" v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">10 条</option>
            <option :value="20">20 条</option>
            <option :value="50">50 条</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="handleSearch">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button v-if="canGrant" type="button" class="primary-button compact" @click="openGrantDialog">手动发券</button>
        <button v-if="canGrant" type="button" class="ghost-button compact" @click="openImportDialog">CSV 导入</button>
        <button type="button" class="ghost-button compact" @click="loadData">刷新</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>用户券档案</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="data-card-meta">{{ querySummary }}</div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 200px;">用户 / 模板</th>
              <th style="min-width: 200px;">券码</th>
              <th style="min-width: 84px;">状态</th>
              <th style="min-width: 200px;">有效期</th>
              <th style="min-width: 156px;">领取时间</th>
              <th style="min-width: 64px;">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td>{{ item.id }}</td>
              <td>
                <div class="cell-stack">
                  <strong>用户 #{{ item.appUserId }}</strong>
                  <span class="muted-line">模板 #{{ item.couponTemplateId }}</span>
                </div>
              </td>
              <td class="cell-mono">{{ item.couponCode }}</td>
              <td>
                <span :class="['status-badge', statusClassMap[item.status] ?? 'warning']">
                  {{ statusMap[item.status] ?? '未知' }}
                </span>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ formatDate(item.effectiveAt) }}</strong>
                  <span class="muted-line">至 {{ formatDate(item.expireAt) }}</span>
                </div>
              </td>
              <td>{{ formatDate(item.receivedAt) }}</td>
              <td>
                <button type="button" class="cell-link" @click="openDetailDialog(item)">编辑</button>
              </td>
            </tr>
            <tr v-if="items.length === 0" class="empty-row">
              <td colspan="7">当前没有符合条件的用户券记录</td>
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
      v-if="grantDialogVisible"
      title="手动发券"
      sub="选择券模板与目标用户，系统将按每用户数量进行批量发放。"
      size="lg"
      @close="closeGrantDialog"
    >
      <div class="dialog-form-grid">
        <label class="dialog-field">
          <span>券模板</span>
          <RemoteSelectField
            v-model="grantForm.couponTemplateId"
            v-model:keyword="selectorQuery.templateKeyword"
            placeholder="输入模板名称后搜索"
            empty-label="请选择券模板"
            :options="couponTemplateSelectOptions"
            @search="searchTemplates"
          />
        </label>
        <label class="dialog-field">
          <span>每用户发放张数</span>
          <input v-model.number="grantForm.quantityPerUser" type="number" min="1" step="1" />
        </label>
        <label class="dialog-field field-span-2">
          <span>用户检索</span>
          <div class="search-inline">
            <input v-model.trim="grantUserKeyword" type="text" placeholder="按手机号、昵称、OpenId 搜索用户" @keyup.enter="searchUsers" />
            <button type="button" class="ghost-button compact" @click="searchUsers">搜索</button>
          </div>
        </label>
        <div class="field-span-2 selector-block">
          <div class="selector-block-head">
            <span>选择发券用户</span>
            <strong>已选 {{ selectedGrantUserIds.length }} 人</strong>
          </div>
          <div v-if="filteredGrantUserOptions.length > 0" class="user-pick-grid">
            <button
              v-for="user in filteredGrantUserOptions"
              :key="user.id"
              type="button"
              :class="['user-pick-card', { active: selectedGrantUserIds.includes(user.id) }]"
              @click="toggleGrantUser(user.id)"
            >
              <strong>{{ formatUserLabel(user) }}</strong>
              <span class="muted-line">{{ user.miniOpenId || '未绑定小程序 OpenId' }}</span>
            </button>
          </div>
          <div v-else class="detail-empty">没有匹配的用户</div>
        </div>
      </div>

      <section v-if="grantResult" class="detail-section">
        <header class="detail-section-head">
          <h4>发券结果</h4>
          <span class="detail-section-tip">成功 {{ grantResult.successCount }} / 失败 {{ grantResult.failureCount }}</span>
        </header>
        <div class="result-list">
          <article v-for="item in grantResult.items" :key="`${item.appUserId}-${item.message}`" class="result-row">
            <div class="result-row-main">
              <strong>用户 #{{ item.appUserId }}</strong>
              <span :class="['status-badge', item.success ? 'success' : 'danger']">{{ item.success ? '成功' : '失败' }}</span>
            </div>
            <div class="muted-line">{{ item.message }} · 发放数量 {{ item.grantedCount }}</div>
          </article>
        </div>
      </section>

      <template #footer>
        <button type="button" class="ghost-button compact" :disabled="grantSubmitting" @click="closeGrantDialog">关闭</button>
        <button type="button" class="primary-button compact" :disabled="grantSubmitting" @click="submitGrant">
          {{ grantSubmitting ? '发券中...' : '确认发券' }}
        </button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="importDialogVisible"
      title="CSV 导入发券"
      sub="可导入用户 ID、手机号、小程序 OpenId、公众号 OpenId 等标识完成批量发券。"
      size="lg"
      @close="closeImportDialog"
    >
      <div class="dialog-form-grid">
        <label class="dialog-field">
          <span>券模板</span>
          <RemoteSelectField
            v-model="importForm.couponTemplateId"
            v-model:keyword="selectorQuery.templateKeyword"
            placeholder="输入模板名称后搜索"
            empty-label="请选择券模板"
            :options="couponTemplateSelectOptions"
            @search="searchTemplates"
          />
        </label>
        <label class="dialog-field">
          <span>每用户发放张数</span>
          <input v-model.number="importForm.quantityPerUser" type="number" min="1" step="1" />
        </label>
        <label class="dialog-field field-span-2">
          <span>CSV 文件</span>
          <input type="file" accept=".csv" @change="handleImportFileChange" />
        </label>
      </div>

      <div class="helper-card">
        <strong>导入模板字段建议</strong>
        <div>推荐首行包含以下任一标识列：<code>appUserId</code>、<code>mobile</code>、<code>miniOpenId</code>、<code>officialOpenId</code>。</div>
        <div>可选附带 <code>couponTemplateId</code> 与 <code>quantityPerUser</code>；若未填写则以本次表单参数为准。</div>
        <div><button type="button" class="cell-link" @click="downloadImportTemplate">下载示例模板</button></div>
      </div>

      <section v-if="importResult" class="detail-section">
        <header class="detail-section-head">
          <h4>导入结果</h4>
          <span class="detail-section-tip">成功 {{ importResult.successCount }} / 失败 {{ importResult.failureCount }}</span>
        </header>
        <div class="detail-grid">
          <div class="detail-cell"><span class="detail-label">总行数</span><div>{{ importResult.totalRows }}</div></div>
          <div class="detail-cell"><span class="detail-label">识别用户数</span><div>{{ importResult.parsedUserCount }}</div></div>
          <div class="detail-cell"><span class="detail-label">模板 ID</span><div>{{ importResult.couponTemplateId }}</div></div>
          <div class="detail-cell"><span class="detail-label">发放数量</span><div>{{ importResult.quantityPerUser }}</div></div>
        </div>
        <div v-if="importResult.invalidRows.length > 0" class="invalid-rows">
          <div class="invalid-title">无效数据行</div>
          <ul>
            <li v-for="row in importResult.invalidRows" :key="row">{{ row }}</li>
          </ul>
        </div>
      </section>

      <template #footer>
        <button type="button" class="ghost-button compact" :disabled="importSubmitting" @click="closeImportDialog">关闭</button>
        <button type="button" class="primary-button compact" :disabled="importSubmitting" @click="submitImport">
          {{ importSubmitting ? '导入中...' : '开始导入' }}
        </button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="qrDialogVisible"
      title="用户券二维码"
      sub="ERP 可扫描用户出示的二维码，并调用核销接口完成处理。"
      size="sm"
      @close="closeQrDialog"
    >
      <div class="qr-panel">
        <img v-if="qrCodeDataUrl" :src="qrCodeDataUrl" alt="用户券二维码" class="qr-image" />
        <div class="qr-code-text cell-mono">{{ selectedCoupon?.couponCode || '-' }}</div>
        <div class="qr-meta-grid">
          <div class="detail-cell"><span class="detail-label">用户</span><div>#{{ selectedCoupon?.appUserId ?? '-' }}</div></div>
          <div class="detail-cell"><span class="detail-label">模板</span><div>#{{ selectedCoupon?.couponTemplateId ?? '-' }}</div></div>
        </div>
      </div>

      <template #footer>
        <button type="button" class="ghost-button compact" @click="copyCouponCode(selectedCoupon?.couponCode || '')">复制券码</button>
        <button type="button" class="primary-button compact" @click="closeQrDialog">关闭</button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="detailDialogVisible"
      title="用户券明细"
      sub="模板信息、优惠规则、领取时间、有效期与核销轨迹"
      size="xl"
      @close="closeDetailDialog"
    >
      <div v-if="detailCoupon" class="detail-grid">
        <div class="detail-cell"><span class="detail-label">状态</span>
          <div><span :class="['status-badge', statusClassMap[detailCoupon.status] ?? 'warning']">{{ statusMap[detailCoupon.status] ?? '-' }}</span></div>
        </div>
        <div class="detail-cell"><span class="detail-label">模板名称</span><div>{{ detailCoupon.couponTemplateName || '-' }}</div></div>
        <div class="detail-cell"><span class="detail-label">券类型</span><div>{{ templateTypeMap[detailCoupon.templateType] || '-' }}</div></div>
        <div class="detail-cell"><span class="detail-label">模板状态</span>
          <div><span :class="['status-badge', detailCoupon.templateEnabled ? 'success' : 'danger']">{{ detailCoupon.templateEnabled ? '启用' : '停用' }}</span></div>
        </div>
        <div class="detail-cell"><span class="detail-label">有效期规则</span><div>{{ validPeriodTypeMap[detailCoupon.validPeriodType] || '-' }}</div></div>
        <div class="detail-cell"><span class="detail-label">券码</span><div class="cell-mono">{{ detailCoupon.couponCode || '-' }}</div></div>
        <div class="detail-cell"><span class="detail-label">优惠金额</span><div>{{ formatMoney(detailCoupon.discountAmount) }}</div></div>
        <div class="detail-cell"><span class="detail-label">门槛金额</span><div>{{ formatMoney(detailCoupon.thresholdAmount) }}</div></div>
        <div class="detail-cell"><span class="detail-label">每用户限领</span><div>{{ detailCoupon.perUserLimit ?? '-' }}</div></div>
        <div class="detail-cell"><span class="detail-label">新人券</span><div>{{ detailCoupon.isNewUserOnly ? '是' : '否' }}</div></div>
        <div class="detail-cell"><span class="detail-label">门店范围</span><div>{{ detailCoupon.isAllStores ? '全部门店可用' : '指定门店可用' }}</div></div>
        <div class="detail-cell"><span class="detail-label">领取时间</span><div>{{ formatDate(detailCoupon.receivedAt) }}</div></div>
        <div class="detail-cell"><span class="detail-label">生效时间</span><div>{{ formatDate(detailCoupon.effectiveAt) }}</div></div>
        <div class="detail-cell"><span class="detail-label">失效时间</span><div>{{ formatDate(detailCoupon.expireAt) }}</div></div>
        <div v-if="canEditExpireAt" class="detail-cell">
          <span class="detail-label">修改到期日期</span>
          <input v-model="detailForm.expireDate" type="date" class="inline-edit-input" />
        </div>
        <div class="detail-cell"><span class="detail-label">固定有效期</span><div>{{ formatDate(detailCoupon.validFrom) }} ~ {{ formatDate(detailCoupon.validTo) }}</div></div>
        <div class="detail-cell"><span class="detail-label">领后有效天数</span><div>{{ detailCoupon.validDays ?? '-' }}</div></div>
        <div class="detail-cell field-span-3"><span class="detail-label">模板备注</span><div>{{ detailCoupon.templateRemark || '-' }}</div></div>
      </div>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>核销轨迹</h4>
          <span class="detail-section-tip">该券是否被核销、在哪个门店核销、由谁处理及设备信息</span>
        </header>
        <div v-if="writeOffRecords.length === 0" class="detail-empty">暂无核销记录</div>
        <div v-else class="data-table-wrap">
          <table class="data-table">
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
      </section>

      <template #footer>
        <button type="button" class="ghost-button compact" @click="copyCouponCode(detailCoupon?.couponCode || '')">复制券码</button>
        <button type="button" class="ghost-button compact" @click="openQrFromDetail">查看二维码</button>
        <button
          v-if="canEditExpireAt"
          type="button"
          class="primary-button compact"
          :disabled="expireAtSubmitting"
          @click="submitExpireAt"
        >{{ expireAtSubmitting ? '保存中...' : '保存到期日期' }}</button>
        <button type="button" class="ghost-button compact" @click="closeDetailDialog">关闭</button>
      </template>
    </MainDetailDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import QRCode from 'qrcode'
import RemoteSelectField from '@/components/RemoteSelectField.vue'
import MainDetailDialog from '@/components/MainDetailDialog.vue'
import {
  getUserCouponDetail,
  getUserCouponList,
  getUserCouponWriteOffRecords,
  importGrantUserCoupons,
  manualGrantUserCoupons,
  updateUserCouponExpireAt,
} from '@/api/user-coupon'
import { getCouponTemplateList } from '@/api/coupon-template'
import { getUserList } from '@/api/user'
import type { CouponTemplateListItemDto } from '@/types/coupon'
import type { UserListItemDto } from '@/types/user'
import type {
  CouponWriteOffRecordDto,
  ImportGrantUserCouponsResultDto,
  ManualGrantUserCouponsResultDto,
  UserCouponDetailDto,
  UserCouponListItemDto,
} from '@/types/user-coupon'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils/auth'
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
const couponTemplateOptions = ref<CouponTemplateListItemDto[]>([])
const userOptions = ref<UserListItemDto[]>([])
const grantUserKeyword = ref('')
const selectedGrantUserIds = ref<number[]>([])
const selectorQuery = reactive({ templateKeyword: '' })
const grantSubmitting = ref(false)
const importSubmitting = ref(false)
const expireAtSubmitting = ref(false)

const query = reactive({
  userId: 0,
  couponCode: '',
})

const grantForm = reactive({
  couponTemplateId: 0,
  quantityPerUser: 1,
})

const importForm = reactive({
  couponTemplateId: 0,
  quantityPerUser: 1,
})

const detailForm = reactive({
  expireDate: '',
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
const canEditExpireAt = canGrant

const couponTemplateSelectOptions = computed(() => couponTemplateOptions.value.map((template) => ({ value: template.id, label: formatTemplateLabel(template) })))

const querySummary = computed(() => {
  const user = query.userId ? formatUserLabel(userOptions.value.find((item) => item.id === query.userId) || { id: query.userId, miniOpenId: '', createdAt: '' }) : '全部用户'
  const code = query.couponCode ? `券码 ${query.couponCode}` : '全部券码'
  return `${user} · ${code} · 每页 ${pageSize.value} 条`
})

const filteredGrantUserOptions = computed(() => {
  const keyword = grantUserKeyword.value.trim().toLowerCase()
  if (!keyword) return userOptions.value.slice(0, 24)

  return userOptions.value.filter((item) => {
    const text = [item.mobile, item.nickname, item.miniOpenId, String(item.id)].filter(Boolean).join(' ').toLowerCase()
    return text.includes(keyword)
  }).slice(0, 48)
})

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const formatDateInput = (value?: string) => (value ? value.slice(0, 10) : '')
const formatMoney = (value?: number) => (typeof value === 'number' ? `¥${value.toFixed(2)}` : '-')
const formatUserLabel = (user: Pick<UserListItemDto, 'id' | 'miniOpenId' | 'mobile' | 'nickname'>) => user.mobile ? `${user.mobile} / 用户 #${user.id}` : (user.nickname?.trim() || user.miniOpenId || `用户 #${user.id}`)
const formatTemplateLabel = (template: CouponTemplateListItemDto) => `${template.name} / ${templateTypeMap[template.templateType] || '券模板'}`

const toggleGrantUser = (userId: number) => {
  selectedGrantUserIds.value = selectedGrantUserIds.value.includes(userId)
    ? selectedGrantUserIds.value.filter((item) => item !== userId)
    : [...selectedGrantUserIds.value, userId]
}

const loadCouponTemplateOptions = async () => {
  const response = await getCouponTemplateList({ keyword: selectorQuery.templateKeyword || undefined, pageIndex: 1, pageSize: 50 })
  couponTemplateOptions.value = response.data.items
}

const loadUserOptions = async () => {
  const response = await getUserList({ keyword: grantUserKeyword.value || undefined, pageIndex: 1, pageSize: 50 })
  userOptions.value = response.data.items
}

const searchTemplates = async () => { await loadCouponTemplateOptions() }
const searchUsers = async () => { await loadUserOptions() }

const loadData = async () => {
  try {
    const response = await getUserCouponList({
      userId: query.userId || undefined,
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

const resetQuery = async () => {
  query.userId = 0
  query.couponCode = ''
  pageSize.value = 10
  pageIndex.value = 1
  await loadData()
  notify.info('已重置筛选条件')
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
  grantUserKeyword.value = ''
  selectedGrantUserIds.value = []
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
  const appUserIds = selectedGrantUserIds.value

  if (grantForm.couponTemplateId <= 0) {
    notify.info('请选择券模板')
    return
  }

  if (grantForm.quantityPerUser <= 0) {
    notify.info('每用户发放张数必须大于 0')
    return
  }

  if (appUserIds.length === 0) {
    notify.info('请至少选择一个用户')
    return
  }

  if (grantSubmitting.value) return
  grantSubmitting.value = true

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
  } finally {
    grantSubmitting.value = false
  }
}

const submitImport = async () => {
  if (importForm.couponTemplateId <= 0) {
    notify.info('请选择券模板')
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

  if (importSubmitting.value) return
  importSubmitting.value = true

  try {
    const response = await importGrantUserCoupons(importFile.value, importForm.couponTemplateId, importForm.quantityPerUser)
    importResult.value = response.data
    notify.success(`导入发券完成，成功 ${response.data.successCount} 人`)
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '导入发券失败'))
  } finally {
    importSubmitting.value = false
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

const openQrFromDetail = async () => {
  if (!selectedCoupon.value) return
  await openQrDialog(selectedCoupon.value)
}

const closeQrDialog = () => {
  qrDialogVisible.value = false
  qrCodeDataUrl.value = ''
}

const loadUserCouponDetailBundle = async (id: number) => {
  const [detailResponse, recordResponse] = await Promise.all([
    getUserCouponDetail(id),
    getUserCouponWriteOffRecords(id),
  ])
  detailCoupon.value = detailResponse.data
  writeOffRecords.value = recordResponse.data
  detailForm.expireDate = formatDateInput(detailResponse.data.expireAt)
}

const openDetailDialog = async (item: UserCouponListItemDto) => {
  try {
    selectedCoupon.value = item
    await loadUserCouponDetailBundle(item.id)
    detailDialogVisible.value = true
  } catch (error) {
    notify.error(getErrorMessage(error, '加载用户券详情失败'))
  }
}

const closeDetailDialog = () => {
  detailDialogVisible.value = false
  detailCoupon.value = null
  writeOffRecords.value = []
  detailForm.expireDate = ''
}

const submitExpireAt = async () => {
  if (!detailCoupon.value) return
  if (!detailForm.expireDate) return notify.info('请选择新的到期日期')
  if (expireAtSubmitting.value) return

  expireAtSubmitting.value = true
  try {
    await updateUserCouponExpireAt(detailCoupon.value.id, { expireDate: detailForm.expireDate })
    notify.success('到期日期已更新')
    await loadUserCouponDetailBundle(detailCoupon.value.id)
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '更新到期日期失败'))
  } finally {
    expireAtSubmitting.value = false
  }
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

const notifyColumnSettingsPlaceholder = () => {
  notify.info('列设置功能将在下一版本提供')
}

onMounted(async () => {
  try {
    await Promise.all([loadCouponTemplateOptions(), loadUserOptions()])
  } catch (error) {
    notify.error(getErrorMessage(error, '加载发券选项失败'))
  }

  await loadData()
})
</script>

<style scoped>
.user-coupon-page :deep(.dialog-body) {
  gap: 14px;
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

.dialog-field input[type='number'],
.dialog-field input[type='text'],
.dialog-field input[type='date'],
.dialog-field input[type='file'],
.dialog-field select {
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
  width: 100%;
}

.dialog-field input[type='file'] {
  height: auto;
  padding: 6px 8px;
}

.search-inline {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px;
  align-items: center;
}

.search-inline input {
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
  width: 100%;
}

.selector-block {
  display: grid;
  gap: 8px;
}

.selector-block-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 13px;
  font-weight: 600;
  color: #344054;
}

.selector-block-head strong {
  font-size: 12px;
  color: var(--primary);
  font-weight: 700;
}

.user-pick-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  max-height: 220px;
  overflow: auto;
  padding-right: 4px;
}

.user-pick-card {
  display: grid;
  gap: 2px;
  padding: 8px 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  text-align: left;
  cursor: pointer;
  font-size: 13px;
  transition: border-color 0.15s ease, background 0.15s ease;
}

.user-pick-card:hover {
  border-color: var(--primary);
  background: #f8fbff;
}

.user-pick-card.active {
  border-color: var(--primary);
  background: rgba(37, 99, 235, 0.06);
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.12);
}

.result-list {
  display: grid;
  gap: 8px;
  padding: 10px 14px;
  max-height: 240px;
  overflow: auto;
}

.result-row {
  display: grid;
  gap: 4px;
  padding: 8px 10px;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: #fcfdff;
}

.result-row-main {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.invalid-rows {
  padding: 10px 14px;
  border-top: 1px solid var(--line);
  background: #fef6e7;
  font-size: 12px;
  color: #b45309;
}

.invalid-rows ul {
  margin: 6px 0 0;
  padding-left: 18px;
}

.invalid-title {
  font-weight: 700;
  color: #b45309;
}

.qr-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 4px 0;
}

.qr-image {
  width: 240px;
  height: 240px;
  object-fit: contain;
}

.qr-code-text {
  word-break: break-all;
  text-align: center;
  font-size: 13px;
  color: var(--text);
}

.qr-meta-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  width: 100%;
}

.inline-edit-input {
  width: 100%;
  height: 28px;
  padding: 0 8px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
}

@media (max-width: 1100px) {
  .user-pick-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .dialog-form-grid { grid-template-columns: 1fr; }
}

@media (max-width: 720px) {
  .user-pick-grid { grid-template-columns: 1fr; }
  .qr-meta-grid { grid-template-columns: 1fr; }
}
</style>
