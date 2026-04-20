<template>
  <div class="business-page page-v2 share-tracking-page-v2">
    <section class="hero-panel share-tracking-hero">
      <div class="hero-copy">
        <span class="page-kicker">传播分析</span>
        <h2>分享追踪</h2>
        <p>围绕分享意图与打开结果形成分析工作台：先筛选时间和目标，再看汇总与明细，不让分析页退化成一屏纯表格。</p>
        <div class="hero-tags">
          <span class="badge info">shareIntent / open</span>
          <span class="badge success">活动页 / 券模板</span>
          <span class="badge warning">支持复制汇总</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">分享意图总数</span>
          <strong>{{ totalShareIntent }}</strong>
          <p>当前筛选范围内的 shareIntent 总计</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">打开总数</span>
          <strong>{{ totalOpen }}</strong>
          <p>当前筛选范围内的 open 总计</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前页码</span>
          <strong>{{ pageIndex }} / {{ totalPages }}</strong>
          <p>明细支持分页浏览</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前事件</span>
          <strong>{{ query.eventType || '全部' }}</strong>
          <p>可切换 shareIntent / open</p>
        </article>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">分享意图总数</span>
        <strong class="stat-value">{{ totalShareIntent }}</strong>
        <span class="stat-footnote">当前筛选范围内 shareIntent 总计</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">打开总数</span>
        <strong class="stat-value">{{ totalOpen }}</strong>
        <span class="stat-footnote">当前筛选范围内 open 总计</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">明细总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">分页明细统计结果</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">当前事件类型</span>
        <strong class="stat-value stat-value-text">{{ query.eventType || '全部' }}</strong>
        <span class="stat-footnote">用于快速聚焦传播链路阶段</span>
      </article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">筛选与动作</span>
          <h3>传播分析入口</h3>
          <p class="section-tip">按日期、目标类型和目标标识过滤，再决定看按日汇总还是事件明细。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetQuery">重置</button>
          <button type="button" class="primary-button" @click="loadAll">查询</button>
          <button type="button" class="ghost-button" @click="copySummary">复制汇总</button>
        </div>
      </div>

      <div class="filter-panel-grid tracking-filter-grid">
        <label class="field-card filter-field">
          <span class="field-label">开始日期</span>
          <input v-model="query.dateFrom" type="date" />
        </label>
        <label class="field-card filter-field">
          <span class="field-label">结束日期</span>
          <input v-model="query.dateTo" type="date" />
        </label>
        <label class="field-card filter-field">
          <span class="field-label">目标类型</span>
          <select v-model="query.targetType">
            <option value="">全部</option>
            <option value="activity">活动页</option>
            <option value="coupon">券模板</option>
          </select>
        </label>
        <label class="field-card filter-field">
          <span class="field-label">targetKey</span>
          <input v-model.trim="query.targetKey" type="text" placeholder="如 newcomer / template:12" />
        </label>
        <label class="field-card filter-field">
          <span class="field-label">券模板ID</span>
          <input v-model.number="query.couponTemplateId" type="number" min="0" placeholder="可选" />
        </label>
        <div class="field-card summary-field">
          <span class="field-label">分析说明</span>
          <strong>先看按日汇总，再钻取事件明细</strong>
          <p>汇总适合看趋势与转化，事件明细适合回查 shareId、targetKey 和页面路径。</p>
        </div>
      </div>
    </section>

    <section class="share-content-grid">
      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">Summary</span>
            <h3>按日汇总</h3>
            <p class="section-tip">优先看日期、目标与打开率，快速判断哪条传播入口更有效。</p>
          </div>
        </div>
        <div class="table-wrap table-wrap-v2">
          <table class="table">
            <thead>
              <tr>
                <th>日期</th>
                <th>目标类型</th>
                <th>targetKey</th>
                <th>分享意图</th>
                <th>打开数</th>
                <th>打开率</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in summaryItems" :key="`${item.date}-${item.targetType}-${item.targetKey}`">
                <td>{{ formatDate(item.date) }}</td>
                <td>{{ item.targetType }}</td>
                <td class="mono">{{ item.targetKey }}</td>
                <td>{{ item.shareIntentCount }}</td>
                <td>{{ item.openCount }}</td>
                <td>{{ formatRate(item.openRate) }}</td>
              </tr>
              <tr v-if="summaryItems.length === 0">
                <td colspan="6" class="empty-text">暂无汇总数据</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">Details</span>
            <h3>事件明细</h3>
            <p class="section-tip">切换事件类型后回看 shareId、target 和用户信息，适合问题追踪。</p>
          </div>
          <label class="field-card compact-field event-field-card">
            <span class="field-label">事件类型</span>
            <select v-model="query.eventType" @change="loadDetails">
              <option value="">全部</option>
              <option value="shareIntent">shareIntent</option>
              <option value="open">open</option>
            </select>
          </label>
        </div>
        <div class="table-wrap table-wrap-v2">
          <table class="table">
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
                <td class="mono">{{ item.shareId }}</td>
                <td class="mono">{{ item.targetType }} / {{ item.targetKey }}</td>
                <td class="mono">{{ formatUserCell(item) }}</td>
                <td class="mono">{{ item.pagePath }}</td>
              </tr>
              <tr v-if="detailItems.length === 0">
                <td colspan="6" class="empty-text">暂无明细数据</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="pager pager-v2">
          <div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条</div>
          <div class="pager-actions">
            <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
            <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
          </div>
        </div>
      </article>
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
.share-tracking-hero {
  background:
    radial-gradient(circle at top right, rgba(59, 130, 246, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%);
}





.tracking-filter-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}





.share-content-grid {
  display: grid;
  gap: 18px;
}

.event-field-card {
  min-width: 180px;
}

.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace;
  font-size: 12px;
}

@media (max-width: 1200px) {
  .hero-side-grid,
  .tracking-filter-grid {
    grid-template-columns: 1fr;
  }
}
</style>
