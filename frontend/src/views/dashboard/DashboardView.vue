<template>
  <div class="dashboard-page page-v2 dashboard-mixed-page">
    <section class="dashboard-lead-grid">
      <article class="card card-v2 dashboard-command-card">
        <div class="page-title-block">
          <span class="page-kicker">运营首页</span>
          <h2>发券后台混合工作台</h2>
          <p>首屏直接承接高频工作：配置、履约、核销、追踪四条主线优先，说明性内容下沉到卡片与规则区。</p>
        </div>
        <div class="hero-tags dashboard-status-tags">
          <span class="badge info">首页形态：混合</span>
          <span class="badge success">高频入口优先</span>
          <span class="badge warning">只改 UI，不改逻辑</span>
        </div>
        <div class="dashboard-entry-grid">
          <RouterLink v-for="entry in primaryEntries" :key="entry.to" :to="entry.to" class="dashboard-entry-card">
            <span class="dashboard-entry-kicker">{{ entry.kicker }}</span>
            <strong>{{ entry.label }}</strong>
            <p>{{ entry.description }}</p>
          </RouterLink>
        </div>
      </article>

      <div class="dashboard-side-stack">
        <article class="stat-card accent-blue">
          <span class="label">系统状态</span>
          <strong class="stat-value dashboard-side-value">{{ healthText }}</strong>
          <span class="stat-footnote">基础健康检查与接口可用性</span>
        </article>
        <article class="stat-card accent-green">
          <span class="label">首批改造</span>
          <strong class="stat-value dashboard-side-value">骨架 + 关键页</strong>
          <span class="stat-footnote">先统一工作台与关键业务页，再覆盖剩余页面</span>
        </article>
        <article class="stat-card accent-amber">
          <span class="label">当前原则</span>
          <strong class="stat-value dashboard-side-value">入口优先</strong>
          <span class="stat-footnote">减少 hero / stats 展示感，强化筛选、表格、动作链路</span>
        </article>
      </div>
    </section>

    <section class="dashboard-mixed-grid">
      <div class="dashboard-main-stack">
        <article class="card card-v2 dashboard-section-card">
          <div class="section-head">
            <div class="section-head-main">
              <span class="section-kicker">今日主线</span>
              <h3>核心工作入口</h3>
              <p class="section-tip">把高频配置、履约与核销入口放在一屏内，减少说明文字阻塞。</p>
            </div>
          </div>
          <div class="workbench-grid">
            <article v-for="item in workbenchItems" :key="item.title" class="workbench-card">
              <div class="workbench-card-head">
                <strong>{{ item.title }}</strong>
                <span :class="['badge', item.badgeType]">{{ item.badge }}</span>
              </div>
              <p>{{ item.description }}</p>
              <RouterLink :to="item.to" class="workbench-link">进入{{ item.title }}</RouterLink>
            </article>
          </div>
        </article>

        <article class="card card-v2 dashboard-section-card">
          <div class="section-head">
            <div class="section-head-main">
              <span class="section-kicker">业务链路</span>
              <h3>页面族与落点</h3>
              <p class="section-tip">按实际业务链路组织页面，而不是按展示风格组织页面。</p>
            </div>
          </div>
          <div class="flow-list">
            <div v-for="group in flowGroups" :key="group.title" class="flow-row">
              <div class="flow-row-main">
                <strong>{{ group.title }}</strong>
                <p>{{ group.description }}</p>
              </div>
              <div class="flow-row-tags">
                <span v-for="tag in group.tags" :key="tag" class="tag">{{ tag }}</span>
              </div>
            </div>
          </div>
        </article>
      </div>

      <div class="dashboard-side-stack">
        <article class="card card-v2 dashboard-section-card">
          <div class="section-head">
            <div class="section-head-main">
              <span class="section-kicker">风险提醒</span>
              <h3>本轮不能碰的边界</h3>
            </div>
          </div>
          <div class="risk-list">
            <div v-for="risk in risks" :key="risk.title" class="risk-item">
              <strong>{{ risk.title }}</strong>
              <p>{{ risk.description }}</p>
            </div>
          </div>
        </article>

        <article class="card card-v2 dashboard-section-card">
          <div class="section-head">
            <div class="section-head-main">
              <span class="section-kicker">规则速记</span>
              <h3>运营侧需要快速确认的规则</h3>
            </div>
          </div>
          <div class="rules-grid">
            <div v-for="rule in rules" :key="rule.title" class="rule-card">
              <strong>{{ rule.title }}</strong>
              <p>{{ rule.description }}</p>
            </div>
          </div>
        </article>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getHealth } from '@/api/health'
import { authStorage } from '@/utils.auth'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

const healthText = ref('检查中')
const menuPaths = computed(() => authStorage.getMenuPaths())
const routeAllowed = (path: string) => menuPaths.value.length === 0 || menuPaths.value.includes(path) || path === '/'

const entryCatalog = [
  { to: '/coupon-templates', label: '券模板管理', kicker: '配置优先', description: '维护模板规则、素材与适用范围。' },
  { to: '/coupon-packs', label: '券包管理', kicker: '售卖组合', description: '配置可售卖券包与组合内容。' },
  { to: '/coupon-orders', label: '订单管理', kicker: '履约闭环', description: '回看订单、支付与发券结果。' },
  { to: '/writeoff', label: '核销中心', kicker: '门店执行', description: '处理核销输入、结果确认与补录。' },
  { to: '/users', label: '用户管理', kicker: '用户基础', description: '确认建档、绑定与识别信息。' },
  { to: '/share-tracking', label: '分享追踪', kicker: '增长分析', description: '查看分享意图和打开数据。' },
]

const primaryEntries = computed(() => entryCatalog.filter((entry) => routeAllowed(entry.to)).slice(0, 6))

const workbenchItems = computed(() => [
  { title: '券模板管理', badge: '高频', badgeType: 'info', description: '先确定模板规则，再承接券包、下单与发放。', to: '/coupon-templates' },
  { title: '券包管理', badge: '主链路', badgeType: 'success', description: '券包负责售卖组合与定价，是交易链路的上游。', to: '/coupon-packs' },
  { title: '订单管理', badge: '履约', badgeType: 'warning', description: '订单页承担支付处理、退款与发券结果核对。', to: '/coupon-orders' },
  { title: '核销中心', badge: '执行', badgeType: 'warning', description: '核销操作需要快速、稳定、低解释成本的工作台结构。', to: '/writeoff' },
].filter((item) => routeAllowed(item.to)))

const flowGroups = [
  { title: '配置链路', description: '模板 → 券包 → 券包明细 → 首页投放，先定规则，再定入口。', tags: ['券模板', '券包', '券包明细', '轮播图'] },
  { title: '履约链路', description: '订单 → 支付 → 发券结果 → 用户券，围绕成交后的实际履约状态组织页面。', tags: ['订单管理', '用户券', '退款/支付'] },
  { title: '执行链路', description: '门店 / 商品 / 核销中心配套出现，便于一线执行与回查。', tags: ['门店管理', '商品管理', '核销中心'] },
  { title: '增长链路', description: '用户档案和分享追踪作为识别与传播分析支撑，不抢占主链路入口。', tags: ['用户管理', '分享追踪'] },
]

const risks = [
  { title: '不改路由和权限落点', description: '保留现有 path、menuPaths 与页面职责，避免误伤权限入口。' },
  { title: '不改接口和入参', description: '本轮只调整界面表达、层级和布局，不触碰业务逻辑。' },
  { title: '不让说明压过操作', description: '功能入口、筛选、表格和动作优先于展示型 hero / stats。' },
]

const rules = [
  { title: '新人券限制', description: '同一用户仅允许领取一次新人券。' },
  { title: '有效期支持双模式', description: '同时支持固定日期范围和领取后 N 天有效。' },
  { title: '门店范围校验', description: '核销时按门店范围确认是否允许使用。' },
  { title: '分享追踪独立看板', description: '传播数据单独沉淀，不与交易页混排。' },
]

onMounted(async () => {
  try {
    const response = await getHealth()
    healthText.value = `${response.data.service} · ${response.message}`
  } catch (error) {
    console.warn('[dashboard:getHealth]', error)
    healthText.value = '接口异常'
    notify.error(getErrorMessage(error, '加载系统健康状态失败'))
  }
})
</script>

<style scoped>
.dashboard-mixed-page {
  gap: 24px;
}

.dashboard-lead-grid,
.dashboard-mixed-grid {
  display: grid;
  gap: 18px;
  grid-template-columns: minmax(0, 2fr) minmax(320px, 0.9fr);
}

.dashboard-command-card,
.dashboard-section-card {
  display: grid;
  gap: 18px;
}

.dashboard-status-tags {
  margin-top: 0;
}

.dashboard-entry-grid,
.workbench-grid,
.rules-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.dashboard-entry-card,
.workbench-card,
.rule-card,
.risk-item,
.flow-row {
  border: 1px solid rgba(228, 231, 236, 0.96);
  border-radius: 18px;
  background: linear-gradient(180deg, #fff 0%, #f8fafc 100%);
}

.dashboard-entry-card {
  display: grid;
  gap: 8px;
  padding: 18px;
  transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
}

.dashboard-entry-card:hover,
.workbench-link:hover {
  transform: translateY(-1px);
}

.dashboard-entry-card:hover {
  box-shadow: var(--shadow-md);
  border-color: rgba(37, 99, 235, 0.18);
}

.dashboard-entry-kicker {
  color: var(--primary);
  font-size: 12px;
  font-weight: 700;
}

.dashboard-entry-card strong,
.workbench-card strong,
.rule-card strong,
.risk-item strong,
.flow-row strong {
  font-size: 15px;
}

.dashboard-entry-card p,
.workbench-card p,
.rule-card p,
.risk-item p,
.flow-row p {
  margin: 0;
  color: var(--muted);
}

.dashboard-side-stack,
.dashboard-main-stack,
.risk-list,
.flow-list {
  display: grid;
  gap: 18px;
}

.dashboard-side-value {
  font-size: 22px;
  line-height: 1.2;
}

.workbench-card,
.rule-card,
.risk-item {
  display: grid;
  gap: 10px;
  padding: 18px;
}

.workbench-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.workbench-link {
  display: inline-flex;
  align-items: center;
  width: fit-content;
  min-height: 34px;
  padding: 0 12px;
  border-radius: 10px;
  background: var(--primary-soft);
  color: var(--primary);
  font-size: 13px;
  font-weight: 700;
}

.flow-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 18px;
}

.flow-row-main {
  display: grid;
  gap: 8px;
}

.flow-row-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  justify-content: flex-end;
}

@media (max-width: 1180px) {
  .dashboard-lead-grid,
  .dashboard-mixed-grid,
  .dashboard-entry-grid,
  .workbench-grid,
  .rules-grid {
    grid-template-columns: 1fr;
  }

  .flow-row {
    flex-direction: column;
  }

  .flow-row-tags {
    justify-content: flex-start;
  }
}
</style>
