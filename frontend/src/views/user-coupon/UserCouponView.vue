<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>鐢ㄦ埛鍒稿垪琛?/h2>
        <p>鏌ョ湅鏀粯鍚庣敓鎴愮殑鐢ㄦ埛鍒稿疄渚嬶紝鏀寔鏈嶅姟绔垎椤点€佹寜鐢ㄦ埛鍜屽埜鐮佹煡璇€佸埜鐮佷簩缁寸爜棰勮銆?/p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="loadData">鍒锋柊鍒楄〃</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">鐢ㄦ埛鍒告€绘暟</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">鏈嶅姟绔垎椤电粺璁℃€昏褰曟暟</span>
      </article>
      <article class="stat-card">
        <span class="label">褰撳墠椤?/span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">鍏?{{ totalPages }} 椤?/span>
      </article>
      <article class="stat-card">
        <span class="label">鏈娇鐢?/span>
        <strong class="stat-value">{{ unusedCount }}</strong>
        <span class="stat-footnote">褰撳墠椤靛彲鏍搁攢鍒告暟</span>
      </article>
      <article class="stat-card">
        <span class="label">宸蹭娇鐢?/span>
        <strong class="stat-value">{{ usedCount }}</strong>
        <span class="stat-footnote">褰撳墠椤靛凡鏍搁攢鍒告暟</span>
      </article>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <h3>绛涢€変笌缁熻</h3>
          <p class="section-tip">浣跨敤鏈嶅姟绔弬鏁版煡璇㈢敤鎴峰埜锛屽噺灏戝墠绔叏閲忕瓫閫夊帇鍔涖€?/p>
        </div>
        <div class="summary-inline">
          <span class="badge info">鏀寔鍒哥爜鏌ヨ</span>
          <span class="badge success">绗?{{ pageIndex }} / {{ totalPages }} 椤?/span>
          <span class="badge warning">姣忛〉 {{ pageSize }}</span>
        </div>
      </div>

      <div class="filter-grid">
        <input v-model.number="query.userId" type="number" min="0" placeholder="鎸夌敤鎴稩D鏌ヨ" />
        <input v-model.trim="query.couponCode" type="text" placeholder="鎸夊埜鐮佹煡璇? @keyup.enter="handleSearch" />
        <select v-model.number="pageSize" @change="handlePageSizeChange">
          <option :value="10">姣忛〉 10 鏉?/option>
          <option :value="20">姣忛〉 20 鏉?/option>
          <option :value="50">姣忛〉 50 鏉?/option>
        </select>
        <div class="toolbar-actions">
          <button type="button" @click="handleSearch">鏌ヨ</button>
          <button type="button" class="ghost-button" @click="resetQuery">閲嶇疆</button>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>鐢ㄦ埛鍒歌褰?/h3>
          <p class="section-tip">鍙瑙堝埜鐮佷簩缁寸爜锛屼緵鐢ㄦ埛鍑虹ず缁?ERP 鎴栭棬搴楄澶囨壂鎻忋€?/p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">褰撳墠椤?{{ items.length }}</span>
          <span class="badge success">鏀寔 ERP 鏍搁攢</span>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>鐢ㄦ埛ID</th>
              <th>鍒告ā鏉縄D</th>
              <th>鍒哥爜</th>
              <th>鐘舵€?/th>
              <th>棰嗗彇鏃堕棿</th>
              <th>鐢熸晥鏃堕棿</th>
              <th>澶辨晥鏃堕棿</th>
              <th>鎿嶄綔</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>{{ item.appUserId }}</td>
              <td>{{ item.couponTemplateId }}</td>
              <td class="cell-mono">{{ item.couponCode }}</td>
              <td>
                <span :class="['status-badge', statusClassMap[item.status] ?? 'warning']">
                  {{ statusMap[item.status] ?? `鐘舵€?{item.status}` }}
                </span>
              </td>
              <td>{{ formatDate(item.receivedAt) }}</td>
              <td>{{ formatDate(item.effectiveAt) }}</td>
              <td>{{ formatDate(item.expireAt) }}</td>
              <td>
                <div class="table-actions">
                  <button type="button" class="action-button" @click="openDetailDialog(item)">璇︽儏</button>
                  <button type="button" class="action-button" @click="openQrDialog(item)">浜岀淮鐮?/button>
                  <button type="button" class="action-button" @click="copyCouponCode(item.couponCode)">澶嶅埗鍒哥爜</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="9" class="empty-text">鏆傛棤鐢ㄦ埛鍒告暟鎹?/td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager">
        <div class="pager-info">绗?{{ pageIndex }} 椤?/ 鍏?{{ totalPages }} 椤碉紝鍏?{{ totalCount }} 鏉?/div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">涓婁竴椤?/button>
          <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">涓嬩竴椤?/button>
        </div>
      </div>
    </div>

        <div v-if="grantDialogVisible" class="dialog-mask" @click.self="closeGrantDialog">
      <div class="dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>手动发券</h3>
            <p>按用户 ID 批量发放指定券模板，支持一人多张。</p>
          </div>
          <button type="button" class="ghost-button" @click="closeGrantDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <input v-model.number="grantForm.couponTemplateId" type="number" min="1" placeholder="券模板ID" />
          <input v-model.number="grantForm.quantityPerUser" type="number" min="1" placeholder="每人发放张数" />
          <textarea
            v-model.trim="grantForm.appUserIdsText"
            class="field-span-2"
            rows="6"
            placeholder="请输入用户ID，支持逗号、空格、换行分隔，例如：1,2,3"
          />
        </div>

        <div v-if="grantResult" class="result-panel">
          <strong>成功 {{ grantResult.successCount }} 人，失败 {{ grantResult.failureCount }} 人</strong>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeGrantDialog">取消</button>
          <button v-if="canGrant" type="button" @click="submitGrant">确认发券</button>
        </div>
      </div>
    </div>
<div v-if="qrDialogVisible" class="dialog-mask" @click.self="closeQrDialog">
      <div class="dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>鍒哥爜浜岀淮鐮?/h3>
            <p>缁欑敤鎴峰睍绀烘浜岀淮鐮侊紝渚?ERP 鎴栭棬搴楁壂鐮佹牳閿€銆?/p>
          </div>
          <button type="button" class="ghost-button" @click="closeQrDialog">鍏抽棴</button>
        </div>

        <div class="qr-preview">
          <img v-if="qrCodeDataUrl" :src="qrCodeDataUrl" alt="鍒哥爜浜岀淮鐮? />
          <div class="cell-mono">{{ selectedCoupon?.couponCode }}</div>
          <div class="toolbar-actions">
            <button type="button" @click="copyCouponCode(selectedCoupon?.couponCode || '')">澶嶅埗鍒哥爜</button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="detailDialogVisible" class="dialog-mask" @click.self="closeDetailDialog">
      <div class="dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <h3>鍒歌鎯?/h3>
            <p>鏌ョ湅褰撳墠鐢ㄦ埛鍒哥殑鐘舵€併€佹椂闂磋寖鍥村拰鍒哥爜淇℃伅銆?/p>
          </div>
          <button type="button" class="ghost-button" @click="closeDetailDialog">鍏抽棴</button>
        </div>

        <div class="grid-form detail-grid">
          <div class="result-panel">
            <strong>鐢ㄦ埛鍒窱D</strong>
            <div class="cell-strong">{{ detailCoupon?.id }}</div>
          </div>
          <div class="result-panel">
            <strong>鐢ㄦ埛ID</strong>
            <div>{{ detailCoupon?.appUserId }}</div>
          </div>
          <div class="result-panel">
            <strong>鍒告ā鏉縄D</strong>
            <div>{{ detailCoupon?.couponTemplateId }}</div>
          </div>
          <div class="result-panel">
            <strong>褰撳墠鐘舵€?/strong>
            <div>
              <span :class="['status-badge', statusClassMap[detailCoupon?.status || 0] ?? 'warning']">
                {{ statusMap[detailCoupon?.status || 0] ?? '-' }}
              </span>
            </div>
          </div>
          <div class="result-panel">
            <strong>妯℃澘鍚嶇О</strong>
            <div>{{ detailCoupon?.couponTemplateName || '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>鍒哥被鍨?/strong>
            <div>{{ templateTypeMap[detailCoupon?.templateType || 0] || '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>鏈夋晥鏈熻鍒?/strong>
            <div>{{ validPeriodTypeMap[detailCoupon?.validPeriodType || 0] || '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>妯℃澘鐘舵€?/strong>
            <div>
              <span :class="['status-badge', detailCoupon?.templateEnabled ? 'success' : 'danger']">
                {{ detailCoupon?.templateEnabled ? '鍚敤' : '鍋滅敤' }}
              </span>
            </div>
          </div>
          <div class="result-panel">
            <strong>鍒哥爜</strong>
            <div class="cell-mono">{{ detailCoupon?.couponCode }}</div>
          </div>
          <div class="result-panel">
            <strong>浼樻儬閲戦</strong>
            <div>{{ detailCoupon?.discountAmount ?? '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>闂ㄦ閲戦</strong>
            <div>{{ detailCoupon?.thresholdAmount ?? '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>姣忎汉闄愰</strong>
            <div>{{ detailCoupon?.perUserLimit ?? '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>鏄惁鏂颁汉鍒?/strong>
            <div>{{ detailCoupon?.isNewUserOnly ? '鏄? : '鍚? }}</div>
          </div>
          <div class="result-panel">
            <strong>閫傜敤闂ㄥ簵</strong>
            <div>{{ detailCoupon?.isAllStores ? '鍏ㄩ儴闂ㄥ簵' : '鎸囧畾闂ㄥ簵' }}</div>
          </div>
          <div class="result-panel">
            <strong>棰嗗彇鏃堕棿</strong>
            <div>{{ formatDate(detailCoupon?.receivedAt) }}</div>
          </div>
          <div class="result-panel">
            <strong>鐢熸晥鏃堕棿</strong>
            <div>{{ formatDate(detailCoupon?.effectiveAt) }}</div>
          </div>
          <div class="result-panel">
            <strong>澶辨晥鏃堕棿</strong>
            <div>{{ formatDate(detailCoupon?.expireAt) }}</div>
          </div>
          <div class="result-panel">
            <strong>鍥哄畾鏈夋晥鏈?/strong>
            <div>{{ formatDate(detailCoupon?.validFrom) }} ~ {{ formatDate(detailCoupon?.validTo) }}</div>
          </div>
          <div class="result-panel">
            <strong>棰嗗彇鍚庢湁鏁堝ぉ鏁?/strong>
            <div>{{ detailCoupon?.validDays ?? '-' }}</div>
          </div>
          <div class="result-panel">
            <strong>妯℃澘澶囨敞</strong>
            <div>{{ detailCoupon?.templateRemark || '-' }}</div>
          </div>
        </div>

        <div class="card toolbar-card detail-history-card">
          <div class="toolbar-title">
            <h3>鏍搁攢杞ㄨ抗</h3>
            <p class="section-tip">鏌ョ湅杩欏紶鍒告槸鍚︽牳閿€杩囥€佸湪鍝釜闂ㄥ簵鏍搁攢銆佺敱璋佹搷浣溿€佷娇鐢ㄤ粈涔堣澶囥€?/p>
          </div>

          <div v-if="writeOffRecords.length === 0" class="empty-text">鏆傛棤鏍搁攢璁板綍</div>
          <div v-else class="table-wrap">
            <table class="table">
              <thead>
                <tr>
                  <th>鏍搁攢鏃堕棿</th>
                  <th>闂ㄥ簵</th>
                  <th>鎿嶄綔浜?/th>
                  <th>璁惧鍙?/th>
                  <th>鍒哥爜</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="record in writeOffRecords" :key="record.id">
                  <td>{{ formatDate(record.writeOffAt) }}</td>
                  <td>{{ record.storeName || `闂ㄥ簵#${record.storeId}` }}</td>
                  <td>{{ record.operatorName || '-' }}</td>
                  <td>{{ record.deviceCode || '-' }}</td>
                  <td class="cell-mono">{{ record.couponCode }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeDetailDialog">鍏抽棴</button>
          <button type="button" @click="openQrDialog(selectedCoupon!)">鏌ョ湅浜岀淮鐮?/button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import QRCode from 'qrcode'
import { getUserCouponDetail, getUserCouponList, getUserCouponWriteOffRecords, manualGrantUserCoupons } from '@/api/user-coupon'
import type { CouponWriteOffRecordDto, ManualGrantUserCouponsResultDto, UserCouponDetailDto, UserCouponListItemDto } from '@/types/user-coupon'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const items = ref<UserCouponListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const qrDialogVisible = ref(false)
const detailDialogVisible = ref(false)
const qrCodeDataUrl = ref('')
const selectedCoupon = ref<UserCouponListItemDto | null>(null)
const detailCoupon = ref<UserCouponDetailDto | null>(null)
const writeOffRecords = ref<CouponWriteOffRecordDto[]>([])
const grantDialogVisible = ref(false)
const grantResult = ref<ManualGrantUserCouponsResultDto | null>(null)
const canGrant = authStorage.hasPermission('user-coupon.grant')

const grantForm = reactive({
  couponTemplateId: 0,
  quantityPerUser: 1,
  appUserIdsText: '',
})

const query = reactive({
  userId: 0,
  couponCode: '',
})

const statusMap: Record<number, string> = {
  1: '鏈娇鐢?,
  2: '宸蹭娇鐢?,
  3: '宸茶繃鏈?,
  4: '宸蹭綔搴?,
}

const statusClassMap: Record<number, string> = {
  1: 'success',
  2: 'warning',
  3: 'danger',
  4: 'danger',
}

const templateTypeMap: Record<number, string> = {
  1: '鏂颁汉鍒?,
  2: '鏃犻棬妲涘埜',
  3: '鎸囧畾鍟嗗搧鍒?,
  4: '婊″噺鍒?,
}

const validPeriodTypeMap: Record<number, string> = {
  1: '鍥哄畾鏃ユ湡鑼冨洿',
  2: '棰嗗彇鍚?N 澶╂湁鏁?,
}

const unusedCount = computed(() => items.value.filter((item) => item.status === 1).length)
const usedCount = computed(() => items.value.filter((item) => item.status === 2).length)

const loadData = async () => {
  try {
    const response = await getUserCouponList({
      userId: query.userId > 0 ? query.userId : undefined,
      couponCode: query.couponCode || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })

    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载用户券失败'))
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
  notify.info('已重置用户券筛选条件')
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

const submitGrant = async () => {
  const appUserIds = grantForm.appUserIdsText
    .split(/[\s,，]+/)
    .map((item) => Number(item.trim()))
    .filter((item) => Number.isFinite(item) && item > 0)

  if (grantForm.couponTemplateId <= 0) {
    notify.warning('请输入券模板ID')
    return
  }

  if (appUserIds.length === 0) {
    notify.warning('请至少输入一个用户ID')
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
const openQrDialog = async (item: UserCouponListItemDto) => {
  try {
    selectedCoupon.value = item
    qrCodeDataUrl.value = await QRCode.toDataURL(item.couponCode, {
      width: 220,
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
    notify.error(getErrorMessage(error, '加载券详情失败'))
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
    notify.error(getErrorMessage(error, '澶嶅埗鍒哥爜澶辫触'))
  }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>

<style scoped>
.detail-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.detail-history-card {
  margin-top: 8px;
}
</style>






