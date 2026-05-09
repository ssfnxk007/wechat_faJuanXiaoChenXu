<template>
  <div class="admin-page writeoff-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="wo-code">券码</label>
          <input id="wo-code" v-model.trim="form.couponCode" type="text" placeholder="输入或扫描券码" />
        </div>
        <div class="field">
          <label for="wo-store">门店</label>
          <RemoteSelectField
            v-model="form.storeId"
            v-model:keyword="selectorQuery.storeKeyword"
            placeholder="搜索门店名称 / 编码"
            empty-label="请选择门店"
            :options="storeSelectOptions"
            @search="searchStores"
          />
        </div>
        <div class="field">
          <label for="wo-product">商品</label>
          <RemoteSelectField
            v-model="selectedProductId"
            v-model:keyword="selectorQuery.productKeyword"
            placeholder="搜索商品名称 / ERP 编码"
            empty-label="非指定商品券可留空"
            :options="productSelectOptions"
            @search="searchProducts"
          />
        </div>
        <div class="field">
          <label for="wo-operator">操作人</label>
          <input id="wo-operator" v-model.trim="form.operatorName" type="text" placeholder="输入操作人" />
        </div>
        <div class="field">
          <label for="wo-device">设备号</label>
          <input id="wo-device" v-model.trim="form.deviceCode" type="text" placeholder="输入设备号" />
        </div>
      </div>
      <div class="search-actions">
        <button v-if="canExecute" type="button" class="primary-button compact" :disabled="submitting" @click="submit">
          {{ submitting ? '核销中...' : '执行核销' }}
        </button>
        <button type="button" class="ghost-button compact" :disabled="submitting" @click="resetForm">重置</button>
        <button type="button" class="ghost-button compact" :disabled="submitting" @click="fillDemo">快速填充</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>核销结果</h3>
          <span :class="['count-pill', result ? '' : 'count-pill-pending']">{{ result ? '已返回' : '待执行' }}</span>
        </div>
        <div class="data-card-meta">门店：{{ currentStoreName }} · 商品：{{ currentProductName }}</div>
      </header>
      <div class="result-body">
        <div v-if="result" class="result-card">
          <strong class="result-message">{{ result.message }}</strong>
          <div class="result-meta-grid">
            <div class="result-meta-cell">
              <span class="result-meta-label">用户券 ID</span>
              <span class="cell-mono">{{ result.userCouponId }}</span>
            </div>
            <div class="result-meta-cell">
              <span class="result-meta-label">券码</span>
              <span class="cell-mono">{{ result.couponCode }}</span>
            </div>
            <div class="result-meta-cell">
              <span class="result-meta-label">用户 ID</span>
              <span class="cell-mono">{{ result.appUserId }}</span>
            </div>
            <div class="result-meta-cell">
              <span class="result-meta-label">模板 ID</span>
              <span class="cell-mono">{{ result.couponTemplateId }}</span>
            </div>
          </div>
        </div>
        <div v-else class="result-empty">尚未执行核销，请先填写上方表单并点击「执行核销」。</div>
      </div>
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
import { authStorage } from '@/utils/auth'
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
.count-pill-pending {
  background: #fef6e7;
  color: #b45309;
}

.result-body {
  padding: 16px;
}

.result-card {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 16px;
  border: 1px solid var(--line);
  border-radius: 8px;
  background: linear-gradient(180deg, #fcfdff 0%, #f7fbf9 100%);
}

.result-message {
  font-size: 15px;
  font-weight: 700;
  color: #16a34a;
}

.result-meta-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 8px;
}

.result-meta-cell {
  display: grid;
  gap: 4px;
  padding: 8px 12px;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: #fff;
}

.result-meta-label {
  font-size: 12px;
  color: #475467;
  font-weight: 600;
}

.result-empty {
  padding: 36px 16px;
  text-align: center;
  color: var(--muted);
  font-size: 13px;
  background: #fcfdff;
  border: 1px dashed var(--line);
  border-radius: 8px;
}

@media (max-width: 960px) {
  .result-meta-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
</style>
