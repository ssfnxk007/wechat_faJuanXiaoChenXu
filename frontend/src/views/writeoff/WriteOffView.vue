<template>
  <div class="business-page page-v2 writeoff-page-v2">
    <section class="hero-panel writeoff-hero">
      <div class="hero-copy">
        <span class="page-kicker">执行工作台</span>
        <h2>核销中心</h2>
        <p>把 ERP 核销入口、输入表单、结果反馈和操作说明放进同一工作台，减少大段说明对执行动作的干扰。</p>
        <div class="hero-tags">
          <span class="badge info">ERP 核销</span>
          <span class="badge success">门店 / 商品联动</span>
          <span class="badge warning">支持补录</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">执行状态</span>
          <strong>{{ result ? '已返回' : '待处理' }}</strong>
          <p>执行后立即展示结果与命中信息</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前门店</span>
          <strong>{{ currentStoreName }}</strong>
          <p>用于确认本次核销所属门店</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前商品</span>
          <strong>{{ currentProductName }}</strong>
          <p>指定商品券核销时带入商品编号</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">核销模式</span>
          <strong>ERP</strong>
          <p>扫码后调用接口并回写业务系统</p>
        </article>
      </div>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">核销输入</span>
          <h3>执行参数</h3>
          <p class="section-tip">优先展示门店、商品、券码与操作人输入，不让说明性内容盖过执行入口。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" :disabled="submitting" @click="resetForm">重置表单</button>
          <button type="button" class="ghost-button" :disabled="submitting" @click="fillDemo">快速填充</button>
          <button v-if="canExecute" type="button" class="primary-button" :disabled="submitting" @click="submit">{{ submitting ? '核销中...' : '执行核销' }}</button>
        </div>
      </div>

      <div class="filter-panel-grid writeoff-filter-grid">
        <label class="field-card filter-field">
          <span class="field-label">券码</span>
          <input v-model.trim="form.couponCode" type="text" placeholder="输入券码" />
        </label>
        <label class="field-card filter-field field-with-selector">
          <span class="field-label">门店</span>
          <RemoteSelectField v-model="form.storeId" v-model:keyword="selectorQuery.storeKeyword" placeholder="搜索门店名称 / 编码" empty-label="请选择门店" :options="storeSelectOptions" @search="searchStores" />
        </label>
        <label class="field-card filter-field field-with-selector">
          <span class="field-label">商品</span>
          <RemoteSelectField v-model="selectedProductId" v-model:keyword="selectorQuery.productKeyword" placeholder="搜索商品名称 / ERP 编码" empty-label="全部商品或非指定商品券" :options="productSelectOptions" @search="searchProducts" />
        </label>
        <label class="field-card filter-field">
          <span class="field-label">操作人</span>
          <input v-model.trim="form.operatorName" type="text" placeholder="输入操作人" />
        </label>
        <label class="field-card filter-field">
          <span class="field-label">设备号</span>
          <input v-model.trim="form.deviceCode" type="text" placeholder="输入设备号" />
        </label>
        <div class="field-card summary-field">
          <span class="field-label">标准流程</span>
          <strong>扫码 → 带参调用 → 返回结果</strong>
          <p>门店、商品、操作人、设备号建议全部留痕，便于 ERP 对账与追踪。</p>
        </div>
      </div>
    </section>

    <section class="writeoff-content-grid">
      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">结果回看</span>
            <h3>核销结果</h3>
            <p class="section-tip">成功后直接展示结果消息与关键 ID，方便复制给 ERP 或运营侧追查。</p>
          </div>
        </div>
        <div v-if="result" class="result-panel writeoff-result-panel">
          <strong>{{ result.message }}</strong>
          <div class="inline-metrics">
            <span class="badge success">用户券ID：{{ result.userCouponId }}</span>
            <span class="badge info">券码：{{ result.couponCode }}</span>
            <span class="badge warning">用户ID：{{ result.appUserId }}</span>
            <span class="badge info">模板ID：{{ result.couponTemplateId }}</span>
          </div>
        </div>
        <div v-else class="empty-text">尚未执行核销，请先填写上方表单并执行。</div>
      </article>

      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">执行说明</span>
            <h3>接入与人工补录规则</h3>
            <p class="section-tip">说明区下沉到侧栏卡片，只保留一线执行真正需要的信息。</p>
          </div>
        </div>
        <div class="writeoff-guide-list">
          <div class="guide-item">
            <strong>标准接入</strong>
            <p>ERP 扫描券码后，携带门店、商品、操作人和设备号调用核销接口。</p>
          </div>
          <div class="guide-item">
            <strong>人工补录</strong>
            <p>后台保留手动录入入口，便于核查异常券码或补记现场执行结果。</p>
          </div>
          <div class="guide-item">
            <strong>结果落库</strong>
            <p>建议 ERP 保存返回的券码、用户券 ID、模板 ID 和结果消息，便于后续追踪。</p>
          </div>
        </div>
      </article>
    </section>
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
.writeoff-page-v2 {
  gap: 22px;
}

.writeoff-hero {
  background:
    radial-gradient(circle at top right, rgba(59, 130, 246, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%);
}

.writeoff-filter-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.filter-field input {
  width: 100%;
  height: 44px;
  padding: 0 14px;
  border: 1px solid var(--line-strong);
  border-radius: 12px;
  background: #fff;
}

.field-with-selector :deep(.remote-select-field),
.field-with-selector :deep(.remote-select-control) {
  width: 100%;
}

.writeoff-content-grid {
  display: grid;
  gap: 18px;
  grid-template-columns: minmax(0, 1.5fr) minmax(320px, 1fr);
}

.writeoff-result-panel,
.writeoff-guide-list {
  display: grid;
  gap: 12px;
}

@media (max-width: 1100px) {
  .hero-side-grid,
  .writeoff-filter-grid,
  .writeoff-content-grid {
    grid-template-columns: 1fr;
  }
}
</style>
