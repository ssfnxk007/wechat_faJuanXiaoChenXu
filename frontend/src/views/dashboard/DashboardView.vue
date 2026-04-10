<template>
  <div class="dashboard-page page-v2">
    <section class="hero-panel dashboard-hero">
      <div class="hero-copy">
        <span class="page-kicker">运营总览</span>
        <h2>发券、售卖、核销统一运营台</h2>
        <p>面向发券小程序后台的业务总览页，集中展示核心能力、规则基线与系统健康情况，便于运营和管理人员快速把握全局。</p>
        <div class="hero-tags">
          <span class="badge info">微信建档</span>
          <span class="badge success">发券与售卖</span>
          <span class="badge warning">ERP 核销</span>
        </div>
      </div>

      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight">
          <span class="quick-card-label">当前阶段</span>
          <strong>后台 UI V2</strong>
          <p>核心业务页已经进入统一视觉基线阶段，后续继续做全站细节收口。</p>
        </article>
        <div class="hero-side-grid">
          <article class="quick-card compact">
            <span class="quick-card-label">接口状态</span>
            <strong>{{ healthText }}</strong>
            <p>基础服务健康检查结果</p>
          </article>
          <article class="quick-card compact">
            <span class="quick-card-label">下一步</span>
            <strong>整站统一</strong>
            <p>继续统一权限与基础数据页</p>
          </article>
        </div>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">核心券类型</span>
        <strong class="stat-value">4</strong>
        <span class="stat-footnote">新人券 / 无门槛券 / 指定商品券 / 满减券</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">运营渠道</span>
        <strong class="stat-value">3</strong>
        <span class="stat-footnote">公开领取 / 定向发放 / 券包售卖</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">核销链路</span>
        <strong class="stat-value">1</strong>
        <span class="stat-footnote">ERP 扫码后调用接口核销</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">平台状态</span>
        <strong class="stat-value">稳定</strong>
        <span class="stat-footnote">管理后台视觉与主链路持续收口中</span>
      </article>
    </section>

    <section class="summary-grid dashboard-summary-grid">
      <article class="quick-card dashboard-card">
        <span class="section-kicker">身份规则</span>
        <strong>用户识别</strong>
        <p>当前以小程序 OpenId 为主标识，手机号作为辅助匹配依据，用于建档、定向发券和订单归属。</p>
      </article>
      <article class="quick-card dashboard-card">
        <span class="section-kicker">新人机制</span>
        <strong>新人券限制</strong>
        <p>每个用户成为平台用户后仅可领取一次新人券，后续无论何时都不可重复领取。</p>
      </article>
      <article class="quick-card dashboard-card">
        <span class="section-kicker">有效期</span>
        <strong>双规则支持</strong>
        <p>支持固定日期范围和领取后 N 天有效两种模式，并支持领取后立即生效的业务场景。</p>
      </article>
      <article class="quick-card dashboard-card">
        <span class="section-kicker">门店范围</span>
        <strong>按门店控制</strong>
        <p>券可配置全部门店可用或指定门店可用，核销时按门店范围校验是否允许使用。</p>
      </article>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getHealth } from '@/api/health'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

const healthText = ref('检查中')

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
.dashboard-hero {
  background:
    radial-gradient(circle at top right, rgba(59, 130, 246, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%);
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
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.08), rgba(16, 185, 129, 0.03));
  border: 1px solid rgba(59, 130, 246, 0.14);
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

.dashboard-summary-grid {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.dashboard-card {
  display: grid;
  gap: 10px;
}

.dashboard-card p,
.dashboard-card strong {
  margin: 0;
}

@media (max-width: 1100px) {
  .dashboard-summary-grid,
  .hero-side-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 820px) {
  .dashboard-summary-grid,
  .hero-side-grid {
    grid-template-columns: 1fr;
  }
}
</style>
