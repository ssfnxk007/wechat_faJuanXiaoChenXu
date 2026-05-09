<template>
  <div class="admin-page coupon-template-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="ct-keyword">模板名称</label>
          <input
            id="ct-keyword"
            v-model.trim="query.keyword"
            type="text"
            placeholder="输入模板名称后回车搜索"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="field">
          <label for="ct-page-size">每页条数</label>
          <select id="ct-page-size" v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">10 条</option>
            <option :value="20">20 条</option>
            <option :value="50">50 条</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="handleSearch">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button v-if="canCreate" type="button" class="primary-button compact" @click="openCreateDialog">+ 新增券模板</button>
        <button type="button" class="ghost-button compact" @click="loadData">刷新</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>券模板列表</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="data-card-meta">{{ querySummary }}</div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 80px;">封面</th>
              <th style="min-width: 220px;">模板名称</th>
              <th style="min-width: 200px;">有效期</th>
              <th style="min-width: 160px;">优惠规则</th>
              <th style="min-width: 130px;">分发模式</th>
              <th style="min-width: 180px;">商品范围</th>
              <th style="min-width: 84px;">状态</th>
              <th style="min-width: 156px;">创建时间</th>
              <th style="min-width: 110px;">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td>{{ item.id }}</td>
              <td>
                <img v-if="item.imageUrl" :src="normalizeAssetUrl(item.imageUrl)" :alt="item.name" class="cover-thumb" />
                <div v-else class="cover-thumb cover-thumb-empty">无图</div>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ item.name }}</strong>
                  <div class="meta-tag-row">
                    <span class="status-badge info">{{ typeMap[item.templateType] || '-' }}</span>
                    <span v-if="item.isNewUserOnly" class="status-badge warning">新人</span>
                    <span :class="['status-badge', item.isAllStores ? 'success' : 'warning']">{{ item.isAllStores ? '全店' : '指定店' }}</span>
                  </div>
                  <span class="muted-line">{{ formatStoreIds(item) }}</span>
                </div>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ formatValidity(item) }}</strong>
                  <span class="muted-line">{{ validPeriodTypeMap[item.validPeriodType] || '-' }}</span>
                </div>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ formatDiscount(item) }}</strong>
                  <span class="muted-line">每用户限领 {{ item.perUserLimit }} 张</span>
                </div>
              </td>
              <td>
                <div class="cell-stack">
                  <span :class="['status-badge', `mode-${item.distributionMode}`]">{{ distributionModeLabel(item.distributionMode) }}</span>
                  <span v-if="item.distributionMode === 1 && item.salePrice != null" class="muted-line">售价 ¥{{ item.salePrice.toFixed(2) }}</span>
                  <span v-else class="muted-line">按分发模式决定入口</span>
                </div>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ formatProductIds(item.productIds) }}</strong>
                  <span class="muted-line">{{ item.remark || '未设置补充说明' }}</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <button v-if="canEdit" type="button" class="cell-link" @click="openEditDialog(item)">编辑</button>
                <button v-if="canDelete" type="button" class="cell-link danger" @click="removeItem(item)">删除</button>
              </td>
            </tr>
            <tr v-if="items.length === 0" class="empty-row">
              <td colspan="10">当前没有符合条件的券模板</td>
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

    <MainDetailDialog
      v-if="dialogVisible"
      :title="editingId ? '编辑券模板' : '新增券模板'"
      :sub="editingId ? '调整已有模板的投放规则与有效期设置' : '建立新的发券模板，支持后续发放、领取和核销'"
      size="xl"
      @close="closeDialog"
    >
      <div class="dialog-form-grid">
        <label class="dialog-field">
          <span>模板名称</span>
          <input v-model.trim="form.name" type="text" placeholder="例如：新人欢迎礼券" />
        </label>
        <label class="dialog-field">
          <span>模板类型</span>
          <select v-model.number="form.templateType">
            <option :value="1">新人券</option>
            <option :value="2">无门槛券</option>
            <option :value="3">指定商品券</option>
            <option :value="4">满减券</option>
          </select>
        </label>
        <label class="dialog-field">
          <span>有效期类型</span>
          <select v-model.number="form.validPeriodType">
            <option :value="1">固定日期范围</option>
            <option :value="2">领取后 N 天有效</option>
          </select>
        </label>
        <label class="dialog-field">
          <span>分发模式</span>
          <select v-model.number="form.distributionMode" @change="onDistributionModeChange">
            <option v-for="option in DISTRIBUTION_MODE_OPTIONS" :key="option.value" :value="option.value">{{ option.label }}</option>
          </select>
        </label>
        <label v-if="form.distributionMode === 1" class="dialog-field">
          <span>售价（元）<em class="required-hint">*</em></span>
          <input v-model.number="form.salePrice" type="number" min="0.01" step="0.01" placeholder="仅单张售卖模式填写" />
        </label>
        <label class="dialog-field">
          <span>优惠金额</span>
          <input v-model.number="form.discountAmount" type="number" min="0" step="0.01" placeholder="例如：5" />
        </label>
        <label class="dialog-field">
          <span>门槛金额</span>
          <input v-model.number="form.thresholdAmount" type="number" min="0" step="0.01" placeholder="满减券可填写门槛" />
        </label>
        <label class="dialog-field">
          <span>每用户限领</span>
          <input v-model.number="form.perUserLimit" type="number" min="1" step="1" />
        </label>
        <label v-if="form.validPeriodType === 2" class="dialog-field">
          <span>领取后有效天数</span>
          <input v-model.number="form.validDays" type="number" min="1" step="1" />
        </label>
        <label v-if="form.validPeriodType === 1" class="dialog-field">
          <span>开始时间</span>
          <input v-model="validFromLocal" type="datetime-local" />
        </label>
        <label v-if="form.validPeriodType === 1" class="dialog-field">
          <span>结束时间</span>
          <input v-model="validToLocal" type="datetime-local" />
        </label>
        <label class="dialog-field field-span-2">
          <span>备注说明</span>
          <input v-model.trim="form.remark" type="text" placeholder="用于补充适用门店、活动场景等说明" />
        </label>
        <label class="dialog-field checkbox-row">
          <input v-model="form.isNewUserOnly" type="checkbox" />
          <span>仅限新人领取一次</span>
        </label>
        <label class="dialog-field checkbox-row">
          <input v-model="form.isAllStores" type="checkbox" />
          <span>全部门店可用</span>
        </label>
        <label class="dialog-field checkbox-row">
          <input v-model="form.isEnabled" type="checkbox" />
          <span>启用模板</span>
        </label>
      </div>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>封面素材</h4>
          <div class="head-actions">
            <button type="button" class="ghost-button compact" @click="openMediaDialog">选择素材</button>
            <label class="ghost-button compact upload-trigger">上传图片<input type="file" accept="image/*" class="file-input-hidden" @change="handleImageUpload" /></label>
            <button v-if="selectedImageAsset" type="button" class="ghost-button compact" @click="clearImageAsset">清空</button>
          </div>
        </header>
        <div v-if="selectedImageAsset" class="selected-media-card">
          <img :src="normalizeAssetUrl(selectedImageAsset.fileUrl)" :alt="selectedImageAsset.name" class="selected-media-image" />
          <div class="cell-stack">
            <strong>{{ selectedImageAsset.name }}</strong>
            <span class="muted-line">素材 ID {{ selectedImageAsset.id }}</span>
          </div>
        </div>
        <div v-else class="detail-empty">未设置封面素材，可从素材库选择或直接上传图片</div>
      </section>

      <section v-if="form.templateType === 3" class="detail-section">
        <header class="detail-section-head">
          <h4>适用商品</h4>
          <div class="head-actions">
            <button type="button" class="ghost-button compact" @click="openProductDialog">选择商品</button>
          </div>
        </header>
        <div v-if="selectedProducts.length > 0" class="chip-row">
          <span v-for="product in selectedProducts" :key="product.id" class="chip">
            {{ product.name }}
            <button type="button" @click="removeSelectedProduct(product.id)">×</button>
          </span>
        </div>
        <div v-else class="detail-empty">当前还未选择商品。指定商品券请通过商品选择器完成配置</div>
      </section>

      <section v-if="!form.isAllStores" class="detail-section">
        <header class="detail-section-head">
          <h4>适用门店</h4>
          <div class="head-actions">
            <button type="button" class="ghost-button compact" @click="openStoreDialog">选择门店</button>
          </div>
        </header>
        <div v-if="selectedStores.length > 0" class="chip-row">
          <span v-for="store in selectedStores" :key="store.id" class="chip">
            {{ store.name }} / {{ store.code || `ID ${store.id}` }}
            <button type="button" @click="removeSelectedStore(store.id)">×</button>
          </span>
        </div>
        <div v-else class="detail-empty">指定门店可用时，必须至少选择一个门店</div>
      </section>

      <template #footer>
        <button type="button" class="ghost-button compact" :disabled="submitting || deleting" @click="closeDialog">取消</button>
        <button
          v-if="editingId ? canEdit : canCreate"
          type="button"
          class="primary-button compact"
          :disabled="submitting || deleting"
          @click="submit"
        >{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="productDialogVisible"
      title="商品选择器"
      sub="支持按商品名称或 ERP 编码搜索并勾选适用商品"
      size="xl"
      @close="closeProductDialog"
    >
      <div class="picker-search-row">
        <input v-model.trim="productQuery.keyword" type="text" placeholder="输入商品名称或 ERP 编码后回车搜索" @keyup.enter="loadProductOptions" />
        <button type="button" class="primary-button compact" @click="loadProductOptions">搜索</button>
        <button type="button" class="ghost-button compact" @click="resetProductQuery">重置</button>
      </div>
      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="width: 60px;">选择</th>
              <th style="width: 56px;">ID</th>
              <th>商品名称</th>
              <th>ERP 编码</th>
              <th>状态</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="product in productOptions" :key="product.id">
              <td><input :checked="isProductSelected(product.id)" type="checkbox" @change="toggleProductSelection(product)" /></td>
              <td>{{ product.id }}</td>
              <td>{{ product.name }}</td>
              <td class="cell-mono">{{ product.erpProductCode }}</td>
              <td>
                <span :class="['status-badge', product.isEnabled ? 'success' : 'danger']">{{ product.isEnabled ? '启用' : '停用' }}</span>
              </td>
            </tr>
            <tr v-if="productOptions.length === 0" class="empty-row">
              <td colspan="5">暂无可选商品</td>
            </tr>
          </tbody>
        </table>
      </div>
      <template #footer>
        <button type="button" class="primary-button compact" @click="closeProductDialog">完成选择</button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="mediaDialogVisible"
      title="选择封面素材"
      sub="从素材库选择可用图片，作为券模板封面展示"
      size="xl"
      @close="closeMediaDialog"
    >
      <div class="picker-search-row">
        <input v-model.trim="mediaQuery.keyword" type="text" placeholder="输入素材名称后回车搜索" @keyup.enter="loadMediaOptions" />
        <button type="button" class="primary-button compact" @click="loadMediaOptions">搜索</button>
        <button type="button" class="ghost-button compact" @click="resetMediaQuery">重置</button>
      </div>
      <div class="media-grid">
        <button v-for="asset in mediaOptions" :key="asset.id" type="button" class="media-card" @click="selectMediaAsset(asset)">
          <img :src="normalizeAssetUrl(asset.fileUrl)" :alt="asset.name" class="media-card-image" />
          <strong>{{ asset.name }}</strong>
          <span class="muted-line">{{ asset.bucketType }}</span>
        </button>
        <div v-if="mediaOptions.length === 0" class="detail-empty media-empty">当前没有可选素材</div>
      </div>
      <template #footer>
        <button type="button" class="ghost-button compact" @click="closeMediaDialog">关闭</button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="storeDialogVisible"
      title="门店选择器"
      sub="支持按门店名称或 ERP 门店编号搜索并勾选适用门店"
      size="xl"
      @close="closeStoreDialog"
    >
      <div class="picker-search-row">
        <input v-model.trim="storeQuery.keyword" type="text" placeholder="输入门店名称或编号后回车搜索" @keyup.enter="loadStoreOptions" />
        <button type="button" class="primary-button compact" @click="loadStoreOptions">搜索</button>
        <button type="button" class="ghost-button compact" @click="resetStoreQuery">重置</button>
      </div>
      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="width: 60px;">选择</th>
              <th style="width: 56px;">ID</th>
              <th>门店名称</th>
              <th>ERP 门店编号</th>
              <th>状态</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="store in storeOptions" :key="store.id">
              <td><input :checked="isStoreSelected(store.id)" type="checkbox" @change="toggleStoreSelection(store)" /></td>
              <td>{{ store.id }}</td>
              <td>{{ store.name }}</td>
              <td class="cell-mono">{{ store.code }}</td>
              <td>
                <span :class="['status-badge', store.isEnabled ? 'success' : 'danger']">{{ store.isEnabled ? '启用' : '停用' }}</span>
              </td>
            </tr>
            <tr v-if="storeOptions.length === 0" class="empty-row">
              <td colspan="5">暂无可选门店</td>
            </tr>
          </tbody>
        </table>
      </div>
      <template #footer>
        <button type="button" class="primary-button compact" @click="closeStoreDialog">完成选择</button>
      </template>
    </MainDetailDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import MainDetailDialog from '@/components/MainDetailDialog.vue'
import { createCouponTemplate, deleteCouponTemplate, getCouponTemplateList, updateCouponTemplate } from '@/api/coupon-template'
import { createMediaAsset, getMediaAssetList, uploadMediaAssetFile } from '@/api/media-asset'
import { getProductList } from '@/api/product'
import { getStoreList } from '@/api/store'
import type { CouponTemplateListItemDto, SaveCouponTemplateRequest } from '@/types/coupon'
import type { MediaAssetListItemDto } from '@/types/media-asset'
import type { ProductListItemDto } from '@/types/product'
import type { StoreListItemDto } from '@/types/store'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils/auth'
import { notify } from '@/utils/notify'
import { normalizeAssetUrl } from '@/utils/asset-url'

type CouponTemplateForm = SaveCouponTemplateRequest & { imageUrl?: string }

const items = ref<CouponTemplateListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const validFromLocal = ref('')
const validToLocal = ref('')
const productDialogVisible = ref(false)
const mediaDialogVisible = ref(false)
const storeDialogVisible = ref(false)
const productOptions = ref<ProductListItemDto[]>([])
const mediaOptions = ref<MediaAssetListItemDto[]>([])
const storeOptions = ref<StoreListItemDto[]>([])
const selectedProducts = ref<ProductListItemDto[]>([])
const selectedStores = ref<StoreListItemDto[]>([])
const selectedImageAsset = ref<MediaAssetListItemDto | null>(null)
const submitting = ref(false)
const deleting = ref(false)

const query = reactive({ keyword: '' })
const productQuery = reactive({ keyword: '' })
const mediaQuery = reactive({ keyword: '' })
const storeQuery = reactive({ keyword: '' })

const typeMap: Record<number, string> = { 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }
const validPeriodTypeMap: Record<number, string> = { 1: '固定日期范围', 2: '领取后 N 天有效' }
const DISTRIBUTION_MODE_OPTIONS = [
  { value: 0, label: '免费领取' },
  { value: 1, label: '单张售卖' },
  { value: 2, label: '仅组包' },
] as const

const distributionModeLabel = (mode: number | null | undefined): string => {
  const found = DISTRIBUTION_MODE_OPTIONS.find((item) => item.value === mode)
  return found ? found.label : '未知'
}

const createEmptyForm = (): CouponTemplateForm => ({
  name: '',
  imageAssetId: undefined,
  imageUrl: undefined,
  templateType: 2,
  validPeriodType: 2,
  discountAmount: undefined,
  thresholdAmount: undefined,
  validDays: 7,
  validFrom: undefined,
  validTo: undefined,
  isNewUserOnly: false,
  isAllStores: true,
  perUserLimit: 1,
  isEnabled: true,
  distributionMode: 0,
  salePrice: undefined,
  remark: '',
  productIds: [],
  storeIds: [],
})

const form = reactive<CouponTemplateForm>(createEmptyForm())
const canCreate = authStorage.hasPermission('coupon-template.create')
const canEdit = authStorage.hasPermission('coupon-template.edit')
const canDelete = authStorage.hasPermission('coupon-template.delete')

const querySummary = computed(() => `关键词：${query.keyword || '全部模板'} · 每页 ${pageSize.value} 条`)

watch(validFromLocal, (value) => { form.validFrom = toServerDateTime(value) })
watch(validToLocal, (value) => { form.validTo = toServerDateTime(value) })
watch(selectedProducts, (value) => { form.productIds = value.map((item) => item.id) }, { deep: true })
watch(selectedStores, (value) => { form.storeIds = value.map((item) => item.id) }, { deep: true })

const onDistributionModeChange = () => {
  if (form.distributionMode !== 1) {
    form.salePrice = undefined
  }
}

const resetForm = () => {
  Object.assign(form, createEmptyForm())
  validFromLocal.value = ''
  validToLocal.value = ''
  selectedProducts.value = []
  selectedStores.value = []
  selectedImageAsset.value = null
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const toDateTimeLocal = (value?: string) => (value ? value.slice(0, 16).replace(' ', 'T') : '')
const toServerDateTime = (value?: string) => (value ? value.replace('T', ' ') : undefined)
const formatAmount = (value?: number) => (typeof value === 'number' ? `¥${value.toFixed(2)}` : '-')

const formatProductIds = (productIds?: number[]) => {
  if (!productIds || productIds.length === 0) return '全部商品'
  if (productIds.length <= 3) return productIds.join(', ')
  return `${productIds.slice(0, 3).join(', ')} 等 ${productIds.length} 项`
}

const formatStoreIds = (item: CouponTemplateListItemDto) => {
  if (item.isAllStores) return '全部门店'
  if (!item.storeIds || item.storeIds.length === 0) return '未配置门店'
  if (item.storeIds.length <= 3) return item.storeIds.join(', ')
  return `${item.storeIds.slice(0, 3).join(', ')} 等 ${item.storeIds.length} 家`
}

const formatDiscount = (item: CouponTemplateListItemDto) => {
  if (item.templateType === 4) return `${formatAmount(item.thresholdAmount)} 减 ${formatAmount(item.discountAmount)}`
  return formatAmount(item.discountAmount)
}

const formatValidity = (item: CouponTemplateListItemDto) => {
  if (item.validPeriodType === 1) return `${formatDate(item.validFrom)} ~ ${formatDate(item.validTo)}`
  return `领取后 ${item.validDays || 0} 天内有效`
}

const loadData = async () => {
  try {
    const response = await getCouponTemplateList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载券模板列表失败'))
  }
}

const loadProductOptions = async () => {
  try {
    const response = await getProductList({ keyword: productQuery.keyword || undefined, pageIndex: 1, pageSize: 20 })
    productOptions.value = response.data.items
  } catch (error) {
    notify.error(getErrorMessage(error, '加载商品列表失败'))
  }
}

const loadMediaOptions = async () => {
  try {
    const response = await getMediaAssetList({ bucketType: 'shared', keyword: mediaQuery.keyword || undefined, pageIndex: 1, pageSize: 24 })
    mediaOptions.value = response.data.items.filter((item) => item.mediaType === 'image' && item.bucketType === 'shared')
  } catch (error) {
    notify.error(getErrorMessage(error, '加载素材列表失败'))
  }
}

const loadStoreOptions = async () => {
  try {
    const response = await getStoreList({ keyword: storeQuery.keyword || undefined, pageIndex: 1, pageSize: 50 })
    storeOptions.value = response.data.items
  } catch (error) {
    notify.error(getErrorMessage(error, '加载门店列表失败'))
  }
}

const handleSearch = async () => { pageIndex.value = 1; await loadData() }
const resetQuery = async () => { query.keyword = ''; pageSize.value = 10; pageIndex.value = 1; await loadData(); notify.info('已重置券模板筛选条件') }
const handlePageSizeChange = async () => { pageIndex.value = 1; await loadData() }
const goPrevPage = async () => { if (pageIndex.value <= 1) return; pageIndex.value -= 1; await loadData() }
const goNextPage = async () => { if (pageIndex.value >= totalPages.value) return; pageIndex.value += 1; await loadData() }

const openCreateDialog = () => { resetForm(); editingId.value = null; dialogVisible.value = true }
const openEditDialog = (item: CouponTemplateListItemDto) => {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    imageAssetId: item.imageAssetId,
    imageUrl: item.imageUrl,
    templateType: item.templateType,
    validPeriodType: item.validPeriodType,
    discountAmount: item.discountAmount,
    thresholdAmount: item.thresholdAmount,
    validDays: item.validDays,
    validFrom: item.validFrom,
    validTo: item.validTo,
    isNewUserOnly: item.isNewUserOnly,
    isAllStores: item.isAllStores,
    perUserLimit: item.perUserLimit,
    isEnabled: item.isEnabled,
    distributionMode: item.distributionMode ?? 0,
    salePrice: item.salePrice,
    remark: item.remark,
    productIds: item.productIds || [],
    storeIds: item.storeIds || [],
  })
  validFromLocal.value = toDateTimeLocal(item.validFrom)
  validToLocal.value = toDateTimeLocal(item.validTo)
  selectedImageAsset.value = item.imageAssetId && item.imageUrl
    ? { id: item.imageAssetId, name: item.name, fileUrl: item.imageUrl, mediaType: 'image', bucketType: 'shared', tags: [], sort: 0, isEnabled: true, createdAt: item.createdAt }
    : null
  selectedProducts.value = (item.productIds || []).map((id) => ({
    id,
    name: `商品 #${id}`,
    erpProductCode: '',
    detailImageAssetIds: [],
    mainImageUrl: undefined,
    detailImageUrls: [],
    salePrice: undefined,
    isEnabled: true,
    createdAt: '',
  }))
  selectedStores.value = (item.storeIds || []).map((id) => ({
    id,
    code: '',
    name: `门店 #${id}`,
    contactName: '',
    contactPhone: '',
    isEnabled: true,
    createdAt: '',
  }))
  dialogVisible.value = true
}

const closeDialog = () => { dialogVisible.value = false; editingId.value = null; resetForm() }

const openProductDialog = async () => { productDialogVisible.value = true; await loadProductOptions() }
const closeProductDialog = () => { productDialogVisible.value = false }
const resetProductQuery = async () => { productQuery.keyword = ''; await loadProductOptions() }

const openStoreDialog = async () => { storeDialogVisible.value = true; await loadStoreOptions() }
const closeStoreDialog = () => { storeDialogVisible.value = false }
const resetStoreQuery = async () => { storeQuery.keyword = ''; await loadStoreOptions() }

const openMediaDialog = async () => { mediaDialogVisible.value = true; await loadMediaOptions() }
const closeMediaDialog = () => { mediaDialogVisible.value = false }
const resetMediaQuery = async () => { mediaQuery.keyword = ''; await loadMediaOptions() }

const isProductSelected = (productId: number) => selectedProducts.value.some((item) => item.id === productId)
const toggleProductSelection = (product: ProductListItemDto) => {
  if (isProductSelected(product.id)) selectedProducts.value = selectedProducts.value.filter((item) => item.id !== product.id)
  else selectedProducts.value = [...selectedProducts.value, product]
}
const removeSelectedProduct = (productId: number) => { selectedProducts.value = selectedProducts.value.filter((item) => item.id !== productId) }

const isStoreSelected = (storeId: number) => selectedStores.value.some((item) => item.id === storeId)
const toggleStoreSelection = (store: StoreListItemDto) => {
  if (isStoreSelected(store.id)) selectedStores.value = selectedStores.value.filter((item) => item.id !== store.id)
  else selectedStores.value = [...selectedStores.value, store]
}
const removeSelectedStore = (storeId: number) => { selectedStores.value = selectedStores.value.filter((item) => item.id !== storeId) }

const selectMediaAsset = (asset: MediaAssetListItemDto) => {
  selectedImageAsset.value = asset
  form.imageAssetId = asset.id
  form.imageUrl = asset.fileUrl
  mediaDialogVisible.value = false
}

const clearImageAsset = () => {
  selectedImageAsset.value = null
  form.imageAssetId = undefined
  form.imageUrl = undefined
}

const saveUploadedAsset = async (file: File) => {
  const uploadResponse = await uploadMediaAssetFile(file)
  const createResponse = await createMediaAsset({ name: file.name, fileUrl: uploadResponse.data.fileUrl, mediaType: 'image', bucketType: 'shared', tags: [], sort: 0, isEnabled: true })
  return { id: createResponse.data, name: file.name, fileUrl: uploadResponse.data.fileUrl, mediaType: 'image', bucketType: 'shared', tags: [], sort: 0, isEnabled: true, createdAt: new Date().toISOString() } satisfies MediaAssetListItemDto
}

const handleImageUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  try {
    const asset = await saveUploadedAsset(file)
    selectedImageAsset.value = asset
    form.imageAssetId = asset.id
    form.imageUrl = asset.fileUrl
    notify.success('封面图片上传成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '上传封面图片失败'))
  } finally {
    target.value = ''
  }
}

const buildPayload = (): SaveCouponTemplateRequest => ({
  name: form.name.trim(),
  imageAssetId: selectedImageAsset.value?.id,
  templateType: form.templateType,
  validPeriodType: form.validPeriodType,
  discountAmount: form.discountAmount,
  thresholdAmount: form.thresholdAmount,
  validDays: form.validPeriodType === 2 ? form.validDays : undefined,
  validFrom: form.validPeriodType === 1 ? form.validFrom : undefined,
  validTo: form.validPeriodType === 1 ? form.validTo : undefined,
  isNewUserOnly: form.isNewUserOnly,
  isAllStores: form.isAllStores,
  perUserLimit: form.perUserLimit,
  isEnabled: form.isEnabled,
  distributionMode: form.distributionMode,
  salePrice: form.distributionMode === 1 ? form.salePrice : undefined,
  remark: form.remark?.trim() || undefined,
  productIds: form.templateType === 3 ? selectedProducts.value.map((item) => item.id) : [],
  storeIds: form.isAllStores ? [] : selectedStores.value.map((item) => item.id),
})

const submit = async () => {
  if (!form.name.trim()) return notify.info('请输入模板名称')
  if ((form.discountAmount ?? 0) <= 0) return notify.info('请输入有效的优惠金额')
  if (form.templateType === 4 && (form.thresholdAmount ?? 0) <= 0) return notify.info('满减券必须填写门槛金额')
  if (form.validPeriodType === 1 && (!form.validFrom || !form.validTo)) return notify.info('固定日期范围必须填写开始和结束时间')
  if (form.validPeriodType === 2 && (form.validDays ?? 0) <= 0) return notify.info('领取后有效天数必须大于 0')
  if (form.templateType === 3 && selectedProducts.value.length === 0) return notify.info('指定商品券必须至少选择一个商品')
  if (!form.isAllStores && selectedStores.value.length === 0) return notify.info('指定门店可用时必须至少选择一个门店')
  if (form.distributionMode === 1 && (!form.salePrice || form.salePrice <= 0)) return notify.error('单张售卖券必须填写大于 0 的售价')

  if (submitting.value) return
  submitting.value = true
  try {
    const payload = buildPayload()
    if (editingId.value) {
      await updateCouponTemplate(editingId.value, payload)
      notify.success('券模板已更新')
    } else {
      await createCouponTemplate(payload)
      notify.success('券模板已创建')
    }
    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '保存券模板失败' : '新增券模板失败'))
  } finally {
    submitting.value = false
  }
}

const removeItem = async (item: CouponTemplateListItemDto) => {
  if (!window.confirm(`确认删除券模板"${item.name}"吗？`)) return
  if (deleting.value) return
  deleting.value = true
  try {
    await deleteCouponTemplate(item.id)
    notify.success('券模板已删除')
    if (items.value.length === 1 && pageIndex.value > 1) pageIndex.value -= 1
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '删除券模板失败'))
  } finally {
    deleting.value = false
  }
}

const notifyColumnSettingsPlaceholder = () => {
  notify.info('列设置功能将在下一版本提供')
}

onMounted(loadData)
</script>

<style scoped>
.dialog-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.dialog-field {
  display: grid;
  gap: 6px;
}

.dialog-field > span {
  font-size: 13px;
  font-weight: 600;
  color: #344054;
}

.dialog-field input[type='text'],
.dialog-field input[type='number'],
.dialog-field input[type='datetime-local'],
.dialog-field select {
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
  width: 100%;
}

.checkbox-row {
  flex-direction: row;
  align-items: center;
  gap: 8px;
  display: flex;
}

.checkbox-row input[type='checkbox'] {
  width: 16px;
  height: 16px;
  margin: 0;
}

.required-hint {
  color: #dc2626;
  margin-left: 4px;
  font-style: normal;
}

.cell-link.danger {
  margin-left: 12px;
}

.cover-thumb {
  width: 56px;
  height: 56px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid var(--line);
  background: #fff;
}

.cover-thumb-empty {
  display: grid;
  place-items: center;
  color: var(--muted);
  font-size: 12px;
}

.meta-tag-row {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin: 2px 0;
}

.status-badge.mode-0 { background: #e8f5ee; color: #166534; }
.status-badge.mode-1 { background: #e8f1ff; color: #1d4ed8; }
.status-badge.mode-2 { background: #fef6e7; color: #b45309; }

.head-actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.upload-trigger {
  position: relative;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.file-input-hidden {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}

.selected-media-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px;
  border-top: 1px solid var(--line);
  background: #fafbfc;
}

.selected-media-image {
  width: 96px;
  height: 96px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid var(--line);
  background: #fff;
}

.chip-row {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 12px 14px;
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 8px;
  border-radius: 4px;
  background: rgba(37, 99, 235, 0.08);
  color: var(--primary);
  font-size: 12px;
  font-weight: 600;
}

.chip button {
  padding: 0 4px;
  border: 0;
  background: transparent;
  color: var(--primary);
  font-size: 14px;
  line-height: 1;
  cursor: pointer;
}

.picker-search-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  gap: 8px;
  align-items: center;
}

.picker-search-row input {
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
  width: 100%;
}

.media-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 12px;
}

.media-card {
  display: grid;
  gap: 6px;
  padding: 8px;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: #fff;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
}

.media-card:hover {
  border-color: var(--primary);
  background: #f8fbff;
}

.media-card-image {
  width: 100%;
  height: 110px;
  object-fit: cover;
  border-radius: 4px;
  background: #e2e8f0;
}

.media-card strong {
  font-size: 13px;
  color: var(--text);
}

.media-empty {
  grid-column: 1 / -1;
}

@media (max-width: 1100px) {
  .dialog-form-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}

@media (max-width: 720px) {
  .dialog-form-grid { grid-template-columns: 1fr; }
  .selected-media-card { flex-direction: column; align-items: flex-start; }
}
</style>
