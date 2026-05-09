<template>
  <div class="admin-page share-tracking-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="st-date-from">开始日期</label>
          <input id="st-date-from" v-model="query.dateFrom" type="date" />
        </div>
        <div class="field">
          <label for="st-date-to">结束日期</label>
          <input id="st-date-to" v-model="query.dateTo" type="date" />
        </div>
        <div class="field">
          <label for="st-target-type">目标类型</label>
          <select id="st-target-type" v-model="query.targetType">
            <option value="">全部</option>
            <option value="activity">活动页</option>
            <option value="coupon">券模板</option>
          </select>
        </div>
        <div class="field">
          <label for="st-target-key">targetKey</label>
          <input id="st-target-key" v-model.trim="query.targetKey" type="text" placeholder="如 newcomer / template:12" />
        </div>
        <div class="field">
          <label for="st-coupon-tpl">券模板 ID</label>
          <input id="st-coupon-tpl" v-model.number="query.couponTemplateId" type="number" min="0" placeholder="可选" />
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="loadAll">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button type="button" class="ghost-button compact" @click="copySummary">复制汇总</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>按日汇总</h3>
          <span class="count-pill">共 {{ summaryItems.length }} 条</span>
        </div>
        <div class="data-card-meta">分享意图 {{ totalShareIntent }} · 打开 {{ totalOpen }}</div>
      </header>
      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>日期</th>
              <th>目标类型</th>
              <th>targetKey</th>
              <th class="num-cell">分享意图</th>
              <th class="num-cell">打开数</th>
              <th class="num-cell">打开率</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in summaryItems" :key="`${item.date}-${item.targetType}-${item.targetKey}`">
              <td>{{ formatDate(item.date) }}</td>
              <td>{{ item.targetType }}</td>
              <td class="cell-mono">{{ item.targetKey }}</td>
              <td class="num-cell">{{ item.shareIntentCount }}</td>
              <td class="num-cell">{{ item.openCount }}</td>
              <td class="num-cell">{{ formatRate(item.openRate) }}</td>
            </tr>
            <tr v-if="summaryItems.length === 0" class="empty-row">
              <td colspan="6">暂无汇总数据</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>事件明细</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="head-inline-filter">
          <label for="st-event-type">事件类型</label>
          <select id="st-event-type" v-model="query.eventType" @change="loadDetails">
            <option value="">全部</option>
            <option value="shareIntent">shareIntent</option>
            <option value="open">open</option>
          </select>
        </div>
      </header>
      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>时间</th>
              <th>事件</th>
              <th>shareId</th>
              <th>target</th>
              <th>用户</th>
              <th>路径</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in detailItems" :key="item.id">
              <td>{{ formatDateTime(item.createdAt) }}</td>
              <td>{{ item.eventType }}</td>
              <td class="cell-mono">{{ item.shareId }}</td>
              <td class="cell-mono">{{ item.targetType }} / {{ item.targetKey }}</td>
              <td class="cell-mono">{{ formatUserCell(item) }}</td>
              <td class="cell-mono">{{ item.pagePath }}</td>
            </tr>
            <tr v-if="detailItems.length === 0" class="empty-row">
              <td colspan="6">暂无明细数据</td>
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
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { getShareTrackingDetails, getShareTrackingSummary } from '@/api/share-tracking'
import type { ShareTrackingDetailItemDto, ShareTrackingSummaryItemDto } from '@/types/share-tracking'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

const today = new Date()
const defaultDateTo = toDateInput(today)
const defaultDateFrom = toDateInput(new Date(today.getTime() - 6 * 24 * 60 * 60 * 1000))

const summaryItems = ref<ShareTrackingSummaryItemDto[]>([])
const detailItems = ref<ShareTrackingDetailItemDto[]>([])
const query = reactive({
  dateFrom: defaultDateFrom,
  dateTo: defaultDateTo,
  targetType: '',
  targetKey: '',
  couponTemplateId: 0,
  eventType: '',
})
const pageIndex = ref(1)
const pageSize = ref(20)
const totalPages = ref(1)
const totalCount = ref(0)

const totalShareIntent = computed(() => summaryItems.value.reduce((acc, item) => acc + item.shareIntentCount, 0))
const totalOpen = computed(() => summaryItems.value.reduce((acc, item) => acc + item.openCount, 0))

function buildCommonParams() {
  return {
    dateFrom: query.dateFrom || undefined,
    dateTo: query.dateTo || undefined,
    targetType: query.targetType || undefined,
    targetKey: query.targetKey || undefined,
    couponTemplateId: query.couponTemplateId > 0 ? query.couponTemplateId : undefined,
  }
}

async function loadSummary() {
  try {
    const response = await getShareTrackingSummary(buildCommonParams())
    summaryItems.value = response.data
  } catch (error) {
    notify.error(getErrorMessage(error, '加载分享汇总失败'))
  }
}

async function loadDetails() {
  try {
    const response = await getShareTrackingDetails({
      ...buildCommonParams(),
      eventType: query.eventType || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })
    detailItems.value = response.data.items
    totalPages.value = response.data.totalPages || 1
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.error(getErrorMessage(error, '加载分享明细失败'))
  }
}

async function loadAll() {
  pageIndex.value = 1
  await Promise.all([loadSummary(), loadDetails()])
}

async function resetQuery() {
  query.dateFrom = defaultDateFrom
  query.dateTo = defaultDateTo
  query.targetType = ''
  query.targetKey = ''
  query.couponTemplateId = 0
  query.eventType = ''
  await loadAll()
}

async function goPrevPage() {
  if (pageIndex.value <= 1) return
  pageIndex.value -= 1
  await loadDetails()
}

async function goNextPage() {
  if (pageIndex.value >= totalPages.value) return
  pageIndex.value += 1
  await loadDetails()
}

async function copySummary() {
  const lines = [
    'date,targetType,targetKey,shareIntentCount,openCount,openRate',
    ...summaryItems.value.map((item) =>
      `${formatDate(item.date)},${item.targetType},${item.targetKey},${item.shareIntentCount},${item.openCount},${item.openRate}`),
  ]
  try {
    await navigator.clipboard.writeText(lines.join('\n'))
    notify.success('汇总结果已复制')
  } catch {
    notify.error('复制失败，请检查浏览器权限')
  }
}

function formatRate(value: number) {
  return `${(Number(value || 0) * 100).toFixed(2)}%`
}

function formatDate(value: string) {
  return String(value || '').slice(0, 10)
}

function formatDateTime(value: string) {
  return String(value || '').replace('T', ' ').slice(0, 19)
}

function formatUserCell(item: ShareTrackingDetailItemDto) {
  if (item.eventType === 'shareIntent') {
    return item.fromUserId ? `from:${item.fromUserId}` : '-'
  }
  if (item.openUserId) {
    return `open:${item.openUserId}`
  }
  return item.visitorKey || '-'
}

function toDateInput(value: Date) {
  const year = value.getFullYear()
  const month = String(value.getMonth() + 1).padStart(2, '0')
  const day = String(value.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

onMounted(loadAll)
</script>

<style scoped>
.head-inline-filter {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #475467;
}

.head-inline-filter > label {
  font-weight: 600;
}

.head-inline-filter > select {
  height: 28px;
  padding: 0 8px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 12px;
}
</style>
