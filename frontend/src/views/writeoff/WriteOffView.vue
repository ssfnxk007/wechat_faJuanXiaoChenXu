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
        <strong class="stat-value">{{ currentStoreName }}</strong>
        <span class="stat-footnote">用于确认本次核销所属门店</span>
      </article>
      <article class="stat-card">
        <span class="label">当前商品</span>
        <strong class="stat-value">{{ currentProductName }}</strong>
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
        <RemoteSelectField v-model="form.storeId" v-model:keyword="selectorQuery.storeKeyword" placeholder="搜索门店名称 / 编码" empty-label="请选择门店" :options="storeSelectOptions" @search="searchStores" />
        <RemoteSelectField v-model="selectedProductId" v-model:keyword="selectorQuery.productKeyword" placeholder="搜索商品名称 / ERP 编码" empty-label="全部商品或非指定商品券" :options="productSelectOptions" @search="searchProducts" />
        <input v-model.trim="form.operatorName" type="text" placeholder="操作人" />
        <input v-model.trim="form.deviceCode" type="text" placeholder="设备号" />
      </div>

      <div class="toolbar-actions">
        <button v-if="canExecute" type="button" :disabled="submitting" @click="submit">{{ submitting ? '核销中...' : '执行核销' }}</button>
        <button type="button" class="ghost-button" :disabled="submitting" @click="fillDemo">快速填充</button>
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
        建议 ERP 实际接入时通过门店、商品选择器或系统映射带入业务数据，并将返回的核销结果写回 ERP 日志，便于后续追踪。
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import RemoteSelectField from '@/components/RemoteSelectField.vue'
import { getProductList } from '@/api/product'
import { getStoreList } from '@/api/store'
import { writeOffCoupon } from '@/api/user-coupon'
import type { ProductListItemDto } from '@/types/product'
import type { StoreListItemDto } from '@/types/store'
import type { CouponWriteOffRequest, CouponWriteOffResultDto } from '@/types/user-coupon'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

const result = ref<CouponWriteOffResultDto | null>(null)
const storeOptions = ref<StoreListItemDto[]>([])
const productOptions = ref<ProductListItemDto[]>([])
const selectedProductId = ref(0)
const selectorQuery = reactive({ storeKeyword: '', productKeyword: '' })
const submitting = ref(false)
const canExecute = authStorage.hasPermission('writeoff.execute')
const form = reactive<CouponWriteOffRequest>({
  couponCode: '',
  storeId: 0,
  productId: undefined,
  operatorName: '',
  deviceCode: '',
})

const currentStoreName = computed(() => storeOptions.value.find((item) => item.id === form.storeId)?.name || '-')
const currentProductName = computed(() => productOptions.value.find((item) => item.id === selectedProductId.value)?.name || '-')
const storeSelectOptions = computed(() => storeOptions.value.map((store) => ({ value: store.id, label: `${store.name} / ${store.code}` })))
const productSelectOptions = computed(() => productOptions.value.map((product) => ({ value: product.id, label: `${product.name} / ${product.erpProductCode}` })))

watch(selectedProductId, (value) => {
  form.productId = value > 0 ? value : undefined
})

const loadOptions = async () => {
  try {
    const [storeResponse, productResponse] = await Promise.all([
      getStoreList({ keyword: selectorQuery.storeKeyword || undefined, pageIndex: 1, pageSize: 50 }),
      getProductList({ keyword: selectorQuery.productKeyword || undefined, pageIndex: 1, pageSize: 50 }),
    ])
    storeOptions.value = storeResponse.data.items
    productOptions.value = productResponse.data.items
  } catch (error) {
    notify.error(getErrorMessage(error, '加载门店或商品选项失败'))
  }
}

const searchStores = async () => { await loadOptions() }
const searchProducts = async () => { await loadOptions() }

const resetForm = () => {
  form.couponCode = ''
  form.storeId = 0
  form.productId = undefined
  selectedProductId.value = 0
  form.operatorName = ''
  form.deviceCode = ''
  result.value = null
  notify.info('已重置核销表单')
}

const fillDemo = () => {
  form.couponCode = form.couponCode || 'TEST-COUPON-CODE'
  form.storeId = form.storeId || 1
  selectedProductId.value = selectedProductId.value || 0
  form.operatorName = form.operatorName || 'ERP操作员'
  form.deviceCode = form.deviceCode || 'POS-01'
  notify.info('已填充常用参数')
}

const submit = async () => {
  if (submitting.value) return
  submitting.value = true
  try {
    const response = await writeOffCoupon({ ...form })
    result.value = response.data
    notify.success('核销执行成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '核销失败'))
  } finally {
    submitting.value = false
  }
}

onMounted(loadOptions)
</script>

<style scoped>
.writeoff-form-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}
.writeoff-form-grid select,
.writeoff-form-grid input {
  width: 100%;
  min-height: 44px;
  padding: 10px 14px;
  border: 1px solid var(--line-strong);
  border-radius: 12px;
  background: #fff;
}
</style>
