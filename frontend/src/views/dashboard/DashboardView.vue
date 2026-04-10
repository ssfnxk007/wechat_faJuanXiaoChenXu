<template>
  <div class="dashboard-page">
    <section class="hero-panel">
      <div class="hero-copy">
        <div class="badge info">后台 V1 视觉方案</div>
        <h2>发券、售券、核销统一运营台</h2>
        <p>
          这一版先把后台管理系统的视觉风格统一下来，方便后续继续叠加权限、支付、微信登录和 ERP 核销能力。
        </p>
        <div class="hero-tags">
          <span class="tag">微信小程序登录</span>
          <span class="tag">券模板 / 券包</span>
          <span class="tag">支付后自动发券</span>
          <span class="tag">ERP 扫码核销</span>
        </div>
      </div>

      <div class="hero-side">
        <div class="quick-card">
          <strong>当前阶段</strong>
          <p>后台管理 UI 统一改造，继续承接现有业务能力与接口流程。</p>
        </div>
        <div class="quick-card">
          <strong>接口健康</strong>
          <p>{{ healthText }}</p>
        </div>
        <div class="quick-card">
          <strong>下一步重点</strong>
          <p>把这版样式扩展到全部业务页，并继续补细节交互和权限联动。</p>
        </div>
      </div>
    </section>

    <section class="stats-grid">
      <article class="stat-card">
        <span class="label">核心业务</span>
        <strong class="stat-value">4</strong>
        <span class="stat-footnote">新人券 / 无门槛券 / 商品券 / 满减券</span>
      </article>
      <article class="stat-card">
        <span class="label">渠道能力</span>
        <strong class="stat-value">3</strong>
        <span class="stat-footnote">公开领取 / 指定发放 / 券包售卖</span>
      </article>
      <article class="stat-card">
        <span class="label">核销链路</span>
        <strong class="stat-value">1</strong>
        <span class="stat-footnote">ERP 扫用户二维码后调接口核销</span>
      </article>
      <article class="stat-card">
        <span class="label">系统状态</span>
        <strong class="stat-value">V1</strong>
        <span class="stat-footnote">后台 UI 已进入统一视觉阶段</span>
      </article>
    </section>

    <section class="summary-grid">
      <article class="quick-card">
        <strong>会员身份</strong>
        <p>当前以 `MiniOpenId` 为主标识，手机号作为辅助匹配。</p>
      </article>
      <article class="quick-card">
        <strong>新人券规则</strong>
        <p>每个 `openid` 终身仅可领取一次。</p>
      </article>
      <article class="quick-card">
        <strong>有效期策略</strong>
        <p>支持固定日期范围、领取后 N 天、立即生效。</p>
      </article>
      <article class="quick-card">
        <strong>门店范围</strong>
        <p>支持全部门店可用或指定门店可用。</p>
      </article>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getHealth } from '@/api/health'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

const healthText = ref('???...')

onMounted(async () => {
  try {
    const response = await getHealth()
    healthText.value = `${response.data.service} ? ${response.message}`
  } catch (error) {
    console.warn('[dashboard:getHealth]', error)
    healthText.value = 'API ???'
    notify.error(getErrorMessage(error, '??????????'))
  }
})
</script>
