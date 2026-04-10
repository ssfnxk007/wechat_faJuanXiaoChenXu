<template>
  <div class="business-page writeoff-page">
    <div class="page-header-row">
      <div>
        <h2>核销中心</h2>
        <p>处理券码核销、门店确认、商品校验与结果回写场景。</p>
      </div>
      <div class="inline-actions">
        <button type="button" class="ghost-button" @click="resetForm">重置表单</button>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="label">核销模式</span>
        <strong class="stat-value">ERP</strong>
        <span class="stat-footnote">用户出示券码或二维码，ERP 调用接口完成核销</span>
      </article>
      <article class="stat-card">
        <span class="label">执行状态</span>
        <strong class="stat-value">{{ result ? '已返回' : '待处理' }}</strong>
        <span class="stat-footnote">执行后展示核销结果和关联券信息</span>
      </article>
      <article class="stat-card">
        <span class="label">当前门店</span>
        <strong class="stat-value">{{ form.storeId || '-' }}</strong>
        <span class="stat-footnote">用于确认本次核销所属门店</span>
      </article>
      <article class="stat-card">
        <span class="label">当前商品</span>
        <strong class="stat-value">{{ form.productId || '-' }}</strong>
        <span class="stat-footnote">指定商品券核销时携带商品编号</span>
      </article>
    </div>

    <div class="hero-panel">
      <div class="hero-copy">
        <div class="badge warning">核销操作台</div>
        <h2>ERP 核销接口操作入口</h2>
        <p>按照业务流程，ERP 扫描用户出示的二维码或券码后，携带门店、商品、操作人和设备信息调用核销接口完成处理。页面保留人工录入入口，便于后台核查与补录。</p>
        <div class="hero-tags">
          <span class="tag">扫码后调用接口</span>
          <span class="tag">门店维度核销</span>
          <span class="tag">操作人留痕</span>
          <span class="tag">设备号留痕</span>
        </div>
      </div>

      <div class="hero-side">
        <div class="quick-card">
          <strong>标准流程</strong>
          <p>1. ERP 获取券码 2. 携带门店、商品和操作人调用核销接口 3. 成功后回写业务系统。</p>
        </div>
        <div class="quick-card">
          <strong>适用场景</strong>
          <p>门店收银、会员服务台、活动现场与人工补录核销场景。</p>
        </div>
        <div class="quick-card">
          <strong>结果输出</strong>
          <p>返回用户券、券模板与业务提示，便于 ERP 保存核销日志。</p>
        </div>
      </div>
    </div>

    <div class="card form-card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>核销输入</h3>
          <p class="section-tip">输入券码、门店、商品、操作人和设备号后执行核销。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge warning">接口调用</span>
          <span class="badge info">核销处理</span>
        </div>
      </div>

      <div class="grid-form writeoff-form-grid">
        <input v-model.trim="form.couponCode" type="text" placeholder="券码" />
        <input v-model.number="form.storeId" type="number" min="0" placeholder="门店ID" />
        <input v-model.number="form.productId" type="number" min="0" placeholder="商品ID（选填）" />
        <input v-model.trim="form.operatorName" type="text" placeholder="操作人" />
        <input v-model.trim="form.deviceCode" type="text" placeholder="设备号" />
      </div>

      <div class="toolbar-actions">
        <button v-if="canExecute" type="button" @click="submit">执行核销</button>
        <button type="button" class="ghost-button" @click="fillDemo">快速填充</button>
      </div>
    </div>

    <div class="card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>核销结果</h3>
          <p class="section-tip">成功后可看到命中的用户券、模板与业务提示。</p>
        </div>
      </div>

      <div v-if="result" class="result-panel">
        <strong>{{ result.message }}</strong>
        <div class="inline-metrics">
          <span class="badge success">用户券ID：{{ result.userCouponId }}</span>
          <span class="badge info">券码：{{ result.couponCode }}</span>
          <span class="badge warning">用户ID：{{ result.appUserId }}</span>
          <span class="badge info">模板ID：{{ result.couponTemplateId }}</span>
        </div>
      </div>
      <div v-else class="empty-text">尚未执行核销，请先填写上方表单。</div>
    </div>

    <div class="card toolbar-card">
      <div class="toolbar-title">
        <h3>使用说明</h3>
        <p class="section-tip">供后台运营、实施与 ERP 对接同事查看标准调用方式。</p>
      </div>
      <div class="summary-inline">
        <span class="badge info">先扫码</span>
        <span class="badge success">再调接口</span>
        <span class="badge warning">记录门店与设备</span>
      </div>
      <div class="muted-text">
        建议 ERP 实际接入时携带门店编号、商品ID、操作人、设备号，并将返回的核销结果写回 ERP 日志，便于后续追踪。
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { writeOffCoupon } from '@/api/user-coupon'
import type { CouponWriteOffRequest, CouponWriteOffResultDto } from '@/types/user-coupon'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const result = ref<CouponWriteOffResultDto | null>(null)
const canExecute = authStorage.hasPermission('writeoff.execute')
const form = reactive<CouponWriteOffRequest>({
  couponCode: '',
  storeId: 0,
  productId: undefined,
  operatorName: '',
  deviceCode: '',
})

const resetForm = () => {
  form.couponCode = ''
  form.storeId = 0
  form.productId = undefined
  form.operatorName = ''
  form.deviceCode = ''
  result.value = null
  notify.info('已重置核销表单')
}

const fillDemo = () => {
  form.couponCode = form.couponCode || 'TEST-COUPON-CODE'
  form.storeId = form.storeId || 1
  form.productId = form.productId || 1001
  form.operatorName = form.operatorName || 'ERP操作员'
  form.deviceCode = form.deviceCode || 'POS-01'
  notify.info('已填充常用参数')
}

const submit = async () => {
  try {
    const response = await writeOffCoupon({ ...form })
    result.value = response.data
    notify.success('核销执行成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '核销失败'))
  }
}
</script>

<style scoped>
.writeoff-form-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}
</style>
