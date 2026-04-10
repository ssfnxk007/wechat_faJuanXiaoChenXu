<template>
  <div class="business-page page-v2 coupon-template-page">
    <section class="hero-panel template-hero">
      <div class="hero-copy">
        <span class="page-kicker">发券运营</span>
        <h2>券模板管理</h2>
        <p>统一维护新人券、无门槛券、指定商品券与满减券规则，支持封面素材设置、商品范围配置与有效期管理。</p>
        <div class="hero-tags">
          <span class="badge info">模板规则归档</span>
          <span class="badge success">支持封面素材</span>
          <span class="badge warning">支持商品与门店范围</span>
        </div>
      </div>
      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight">
          <span class="quick-card-label">模板总数</span>
          <strong>{{ totalCount }}</strong>
          <p>当前查询范围内的券模板记录数量。</p>
        </article>
        <div class="hero-side-grid">
          <article class="quick-card compact">
            <span class="quick-card-label">启用模板</span>
            <strong>{{ enabledCount }}</strong>
            <p>当前页面可参与发放的模板数量。</p>
          </article>
          <article class="quick-card compact">
            <span class="quick-card-label">新人券</span>
            <strong>{{ newUserTemplateCount }}</strong>
            <p>仅允许单用户领取一次。</p>
          </article>
        </div>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue"><span class="label">模板总数</span><strong class="stat-value">{{ totalCount }}</strong><span class="stat-footnote">当前筛选范围内记录数</span></article>
      <article class="stat-card accent-indigo"><span class="label">当前页码</span><strong class="stat-value">{{ pageIndex }}</strong><span class="stat-footnote">共 {{ totalPages }} 页</span></article>
      <article class="stat-card accent-green"><span class="label">启用模板</span><strong class="stat-value">{{ enabledCount }}</strong><span class="stat-footnote">当前页启用状态统计</span></article>
      <article class="stat-card accent-amber"><span class="label">新人券模板</span><strong class="stat-value">{{ newUserTemplateCount }}</strong><span class="stat-footnote">仅允许单次领取</span></article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">业务操作台</span>
          <h3>模板筛选与管理动作</h3>
          <p class="section-tip">按模板名称快速定位配置档案，统一进行新增、编辑、删除和分页浏览。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetQuery">重置筛选</button>
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新增券模板</button>
        </div>
      </div>

      <div class="filter-panel-grid">
        <label class="field-card filter-field">
          <span class="field-label">模板名称</span>
          <input v-model.trim="query.keyword" type="text" placeholder="输入模板名称后回车搜索" @keyup.enter="handleSearch" />
        </label>
        <label class="field-card filter-field compact-field">
          <span class="field-label">分页条数</span>
          <select v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">每页 10 条</option>
            <option :value="20">每页 20 条</option>
            <option :value="50">每页 50 条</option>
          </select>
        </label>
        <div class="field-card summary-field">
          <span class="field-label">当前筛选</span>
          <strong>{{ querySummary }}</strong>
          <p>指定商品券将通过商品选择器配置，不再要求手工输入商品编号。</p>
        </div>
      </div>
    </section>

    <section class="card card-v2 data-card archive-card">
      <div class="section-head">
        <div class="section-head-main">
          <span class="section-kicker">模板档案</span>
          <h3>券模板列表</h3>
          <p class="section-tip">展示封面、模板类型、有效期、优惠力度、商品范围和启用状态。</p>
        </div>
      </div>

      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>封面</th>
              <th>模板名称</th>
              <th>有效期</th>
              <th>优惠规则</th>
              <th>商品范围</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="cover-thumb-cell">
                  <img v-if="item.imageUrl" :src="item.imageUrl" :alt="item.name" class="cover-thumb" />
                  <div v-else class="cover-thumb cover-thumb-empty">无图</div>
                </div>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ item.name }}</strong>
                  <div class="template-meta-row">
                    <span class="badge info">{{ typeMap[item.templateType] || '-' }}</span>
                    <span v-if="item.isNewUserOnly" class="badge warning">新人专享</span>
                    <span :class="['badge', item.isAllStores ? 'success' : 'warning']">{{ item.isAllStores ? '全部门店可用' : '指定门店可用' }}</span>
                  </div>
                </div>
              </td>
              <td><div class="table-primary-cell"><strong>{{ formatValidity(item) }}</strong><span>{{ validPeriodTypeMap[item.validPeriodType] || '-' }}</span></div></td>
              <td><div class="table-primary-cell"><strong>{{ formatDiscount(item) }}</strong><span>每用户限领 {{ item.perUserLimit }} 张</span></div></td>
              <td><div class="table-primary-cell"><strong>{{ formatProductIds(item.productIds) }}</strong><span>{{ item.remark || '未设置补充说明' }}</span></div></td>
              <td><span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span></td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td><div class="table-actions"><button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button><button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button></div></td>
            </tr>
            <tr v-if="items.length === 0"><td colspan="9" class="empty-text">当前没有符合条件的券模板</td></tr>
          </tbody>
        </table>
      </div>

      <div class="pager pager-v2"><div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条记录</div><div class="pager-actions"><button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button><button type="button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button></div></div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 template-dialog">
        <div class="dialog-head"><div class="dialog-head-main"><span class="section-kicker">模板表单</span><h3>{{ editingId ? '编辑券模板' : '新增券模板' }}</h3><p>{{ editingId ? '调整已有模板的投放规则与有效期设置。' : '建立新的发券模板，支持后续发放、领取和核销。' }}</p></div></div>

        <div class="grid-form dialog-form template-form-grid">
          <label><span>模板名称</span><input v-model.trim="form.name" type="text" placeholder="例如：新人欢迎礼券" /></label>
          <label><span>模板类型</span><select v-model.number="form.templateType"><option :value="1">新人券</option><option :value="2">无门槛券</option><option :value="3">指定商品券</option><option :value="4">满减券</option></select></label>
          <label><span>有效期类型</span><select v-model.number="form.validPeriodType"><option :value="1">固定日期范围</option><option :value="2">领取后 N 天有效</option></select></label>
          <label><span>优惠金额</span><input v-model.number="form.discountAmount" type="number" min="0" step="0.01" placeholder="例如：5" /></label>
          <label><span>门槛金额</span><input v-model.number="form.thresholdAmount" type="number" min="0" step="0.01" placeholder="满减券可填写门槛" /></label>
          <label><span>每用户限领</span><input v-model.number="form.perUserLimit" type="number" min="1" step="1" /></label>
          <label v-if="form.validPeriodType === 2"><span>领取后有效天数</span><input v-model.number="form.validDays" type="number" min="1" step="1" /></label>
          <label v-if="form.validPeriodType === 1"><span>开始时间</span><input v-model="validFromLocal" type="datetime-local" /></label>
          <label v-if="form.validPeriodType === 1"><span>结束时间</span><input v-model="validToLocal" type="datetime-local" /></label>

          <div class="field-span-3 selector-field-card media-selector-card">
            <div class="selector-field-head">
              <span>封面素材</span>
              <div class="toolbar-actions selector-actions">
                <button type="button" class="ghost-button" @click="openMediaDialog">选择素材</button>
                <label class="ghost-button upload-trigger">上传图片<input type="file" accept="image/*" class="file-input-hidden" @change="handleImageUpload" /></label>
                <button type="button" class="ghost-button" @click="clearImageAsset">清空</button>
              </div>
            </div>
            <div v-if="selectedImageAsset" class="selected-media-card">
              <img :src="selectedImageAsset.fileUrl" :alt="selectedImageAsset.name" class="selected-media-image" />
              <div class="table-primary-cell"><strong>{{ selectedImageAsset.name }}</strong><span>素材ID {{ selectedImageAsset.id }}</span></div>
            </div>
            <p v-else class="helper-text">未设置封面素材，可从素材库选择或直接上传图片。</p>
          </div>

          <div v-if="form.templateType === 3" class="field-span-3 selector-field-card">
            <div class="selector-field-head">
              <span>适用商品</span>
              <button type="button" class="ghost-button" @click="openProductDialog">选择商品</button>
            </div>
            <div v-if="selectedProducts.length > 0" class="selected-product-list">
              <span v-for="product in selectedProducts" :key="product.id" class="selected-product-chip">
                {{ product.name }}
                <button type="button" @click="removeSelectedProduct(product.id)">移除</button>
              </span>
            </div>
            <p v-else class="helper-text">当前还未选择商品。指定商品券请通过商品选择器完成配置。</p>
          </div>

          <label class="field-span-3"><span>备注说明</span><input v-model.trim="form.remark" type="text" placeholder="用于补充适用门店、活动场景等说明" /></label>
          <label class="checkbox-field checkbox-card"><input v-model="form.isNewUserOnly" type="checkbox" /><span>仅限新人领取一次</span></label>
          <label class="checkbox-field checkbox-card"><input v-model="form.isAllStores" type="checkbox" /><span>全部门店可用</span></label>
          <label class="checkbox-field checkbox-card"><input v-model="form.isEnabled" type="checkbox" /><span>启用模板</span></label>
        </div>

        <div class="dialog-actions"><button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">取消</button><button v-if="editingId ? canEdit : canCreate" type="button" class="primary-button" :disabled="submitting || deleting" @click="submit">{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button></div>
      </div>
    </div>

    <div v-if="productDialogVisible" class="dialog-mask" @click.self="closeProductDialog">
      <div class="dialog-card dialog-card-v2 product-selector-dialog">
        <div class="dialog-head"><div class="dialog-head-main"><span class="section-kicker">选择商品</span><h3>商品选择器</h3><p>支持按商品名称或 ERP 编码搜索并勾选适用商品。</p></div></div>
        <div class="filter-panel-grid product-search-grid">
          <label class="field-card filter-field"><span class="field-label">搜索商品</span><input v-model.trim="productQuery.keyword" type="text" placeholder="输入商品名称或 ERP 编码后回车搜索" @keyup.enter="loadProductOptions" /></label>
          <div class="field-card summary-field"><span class="field-label">当前结果</span><strong>{{ productOptions.length }} 项</strong><p>勾选结果会自动回填到模板商品范围。</p></div>
          <div class="toolbar-actions selector-actions"><button type="button" class="ghost-button" @click="loadProductOptions">搜索</button><button type="button" class="ghost-button" @click="resetProductQuery">重置</button></div>
        </div>
        <div class="table-wrap table-wrap-v2">
          <table class="table">
            <thead><tr><th>选择</th><th>ID</th><th>商品名称</th><th>ERP 编码</th><th>状态</th></tr></thead>
            <tbody>
              <tr v-for="product in productOptions" :key="product.id">
                <td><input :checked="isProductSelected(product.id)" type="checkbox" @change="toggleProductSelection(product)" /></td>
                <td class="cell-strong">{{ product.id }}</td>
                <td>{{ product.name }}</td>
                <td>{{ product.erpProductCode }}</td>
                <td><span :class="['status-badge', product.isEnabled ? 'success' : 'danger']">{{ product.isEnabled ? '启用' : '停用' }}</span></td>
              </tr>
              <tr v-if="productOptions.length === 0"><td colspan="5" class="empty-text">暂无可选商品</td></tr>
            </tbody>
          </table>
        </div>
        <div class="dialog-actions"><button type="button" class="primary-button" @click="closeProductDialog">完成选择</button></div>
      </div>
    </div>

    <div v-if="mediaDialogVisible" class="dialog-mask" @click.self="closeMediaDialog">
      <div class="dialog-card dialog-card-v2 product-selector-dialog">
        <div class="dialog-head"><div class="dialog-head-main"><span class="section-kicker">封面素材</span><h3>选择图片素材</h3><p>从素材库选择可用图片，作为券模板封面展示。</p></div></div>
        <div class="filter-panel-grid product-search-grid">
          <label class="field-card filter-field"><span class="field-label">搜索素材</span><input v-model.trim="mediaQuery.keyword" type="text" placeholder="输入素材名称后回车搜索" @keyup.enter="loadMediaOptions" /></label>
          <div class="field-card summary-field"><span class="field-label">当前结果</span><strong>{{ mediaOptions.length }} 项</strong><p>优先展示图片类素材，可直接点击选择。</p></div>
          <div class="toolbar-actions selector-actions"><button type="button" class="ghost-button" @click="loadMediaOptions">搜索</button><button type="button" class="ghost-button" @click="resetMediaQuery">重置</button></div>
        </div>
        <div class="media-grid">
          <button v-for="asset in mediaOptions" :key="asset.id" type="button" class="media-card" @click="selectMediaAsset(asset)"><img :src="asset.fileUrl" :alt="asset.name" class="media-card-image" /><strong>{{ asset.name }}</strong><span>{{ asset.bucketType }}</span></button>
          <div v-if="mediaOptions.length === 0" class="empty-text media-empty">当前没有可选素材</div>
        </div>
        <div class="dialog-actions"><button type="button" class="primary-button" @click="closeMediaDialog">关闭</button></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createCouponTemplate, deleteCouponTemplate, getCouponTemplateList, updateCouponTemplate } from '@/api/coupon-template'
import { createMediaAsset, getMediaAssetList, uploadMediaAssetFile } from '@/api/media-asset'
import { getProductList } from '@/api/product'
import type { CouponTemplateListItemDto, SaveCouponTemplateRequest } from '@/types/coupon'
import type { MediaAssetListItemDto } from '@/types/media-asset'
import type { ProductListItemDto } from '@/types/product'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

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
const productOptions = ref<ProductListItemDto[]>([])
const mediaOptions = ref<MediaAssetListItemDto[]>([])
const selectedProducts = ref<ProductListItemDto[]>([])
const selectedImageAsset = ref<MediaAssetListItemDto | null>(null)
const submitting = ref(false)
const deleting = ref(false)

const query = reactive({ keyword: '' })
const productQuery = reactive({ keyword: '' })
const mediaQuery = reactive({ keyword: '' })

const typeMap: Record<number, string> = { 1: '新人券', 2: '无门槛券', 3: '指定商品券', 4: '满减券' }
const validPeriodTypeMap: Record<number, string> = { 1: '固定日期范围', 2: '领取后 N 天有效' }

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
  remark: '',
  productIds: [],
})

const form = reactive<CouponTemplateForm>(createEmptyForm())
const canCreate = authStorage.hasPermission('coupon-template.create')
const canEdit = authStorage.hasPermission('coupon-template.edit')
const canDelete = authStorage.hasPermission('coupon-template.delete')

const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const newUserTemplateCount = computed(() => items.value.filter((item) => item.isNewUserOnly).length)
const querySummary = computed(() => `关键词：${query.keyword || '全部模板'} / 每页 ${pageSize.value} 条`)

watch(validFromLocal, (value) => { form.validFrom = toServerDateTime(value) })
watch(validToLocal, (value) => { form.validTo = toServerDateTime(value) })
watch(selectedProducts, (value) => { form.productIds = value.map((item) => item.id) }, { deep: true })

const resetForm = () => {
  Object.assign(form, createEmptyForm())
  validFromLocal.value = ''
  validToLocal.value = ''
  selectedProducts.value = []
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
    remark: item.remark,
    productIds: item.productIds || [],
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
  dialogVisible.value = true
}

const closeDialog = () => { dialogVisible.value = false; editingId.value = null; resetForm() }

const openProductDialog = async () => { productDialogVisible.value = true; await loadProductOptions() }
const closeProductDialog = () => { productDialogVisible.value = false }
const resetProductQuery = async () => { productQuery.keyword = ''; await loadProductOptions() }

const openMediaDialog = async () => { mediaDialogVisible.value = true; await loadMediaOptions() }
const closeMediaDialog = () => { mediaDialogVisible.value = false }
const resetMediaQuery = async () => { mediaQuery.keyword = ''; await loadMediaOptions() }

const isProductSelected = (productId: number) => selectedProducts.value.some((item) => item.id === productId)
const toggleProductSelection = (product: ProductListItemDto) => {
  if (isProductSelected(product.id)) selectedProducts.value = selectedProducts.value.filter((item) => item.id !== product.id)
  else selectedProducts.value = [...selectedProducts.value, product]
}
const removeSelectedProduct = (productId: number) => { selectedProducts.value = selectedProducts.value.filter((item) => item.id !== productId) }

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
  remark: form.remark?.trim() || undefined,
  productIds: form.templateType === 3 ? selectedProducts.value.map((item) => item.id) : [],
})

const submit = async () => {
  if (!form.name.trim()) return notify.info('请输入模板名称')
  if ((form.discountAmount ?? 0) <= 0) return notify.info('请输入有效的优惠金额')
  if (form.templateType === 4 && (form.thresholdAmount ?? 0) <= 0) return notify.info('满减券必须填写门槛金额')
  if (form.validPeriodType === 1 && (!form.validFrom || !form.validTo)) return notify.info('固定日期范围必须填写开始和结束时间')
  if (form.validPeriodType === 2 && (form.validDays ?? 0) <= 0) return notify.info('领取后有效天数必须大于 0')
  if (form.templateType === 3 && selectedProducts.value.length === 0) return notify.info('指定商品券必须至少选择一个商品')

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
  if (!window.confirm(`确认删除券模板“${item.name}”吗？`)) return
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

onMounted(loadData)
</script>

<style scoped>
.template-hero { background: radial-gradient(circle at top right, rgba(37, 99, 235, 0.14), transparent 28%), linear-gradient(135deg, #ffffff 0%, #f7faff 54%, #f3f6fb 100%); }
.hero-side-stack { align-content: stretch; }
.hero-side-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.quick-card-spotlight { min-height: 148px; background: linear-gradient(135deg, rgba(37, 99, 235, 0.08), rgba(59, 130, 246, 0.02)); border: 1px solid rgba(59, 130, 246, 0.16); }
.quick-card.compact { min-height: 112px; }
.quick-card-label { display: inline-flex; width: fit-content; padding: 4px 10px; border-radius: 999px; background: rgba(37, 99, 235, 0.08); color: var(--primary); font-size: 12px; font-weight: 700; }
.filter-panel-grid { display: grid; grid-template-columns: 1.4fr 0.8fr 1fr; gap: 14px; }
.field-card,.selector-field-card { display: grid; gap: 10px; padding: 16px; border-radius: 18px; border: 1px solid rgba(226, 232, 240, 0.96); background: linear-gradient(180deg, #fff 0%, #fbfdff 100%); }
.field-label { font-size: 12px; font-weight: 700; color: #475467; letter-spacing: 0.04em; }
.summary-field strong { font-size: 16px; }
.summary-field p { margin: 0; color: var(--muted); font-size: 13px; line-height: 1.6; }
.cover-thumb-cell { display: flex; align-items: center; }
.cover-thumb { width: 56px; height: 56px; object-fit: cover; border-radius: 12px; border: 1px solid var(--line); background: #fff; }
.cover-thumb-empty { display: grid; place-items: center; color: var(--muted); font-size: 12px; }
.template-meta-row { display: flex; flex-wrap: wrap; gap: 8px; }
.template-dialog { width: min(920px, calc(100vw - 48px)); }
.template-form-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.dialog-form > label { display: grid; gap: 8px; }
.dialog-form > label > span,.selector-field-head span { font-size: 13px; font-weight: 700; color: #344054; }
.dialog-form input,.dialog-form select { width: 100%; height: 44px; padding: 0 14px; border: 1px solid var(--line-strong); border-radius: 12px; background: #fff; }
.selector-field-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.selected-product-list { display: flex; flex-wrap: wrap; gap: 10px; }
.selected-product-chip { display: inline-flex; align-items: center; gap: 8px; min-height: 34px; padding: 0 12px; border-radius: 999px; background: rgba(37,99,235,.08); color: var(--primary); }
.selected-product-chip button { height: 24px; min-width: auto; padding: 0 10px; border-radius: 999px; border: 1px solid rgba(37,99,235,.16); background: #fff; color: var(--primary); }
.media-selector-card { gap: 12px; }
.selected-media-card { display: flex; align-items: center; gap: 14px; padding: 14px; border-radius: 16px; border: 1px solid rgba(226,232,240,.96); background: linear-gradient(180deg, #fff 0%, #fbfdff 100%); }
.selected-media-image,.media-card-image { width: 120px; height: 120px; object-fit: cover; border-radius: 16px; border: 1px solid var(--line); background: #fff; }
.upload-trigger { position: relative; overflow: hidden; }
.file-input-hidden { position: absolute; inset: 0; opacity: 0; cursor: pointer; }
.media-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.media-card { display: grid; gap: 10px; padding: 14px; border-radius: 18px; border: 1px solid rgba(226,232,240,.96); background: linear-gradient(180deg,#fff 0%,#fbfdff 100%); text-align: left; }
.media-card strong,.media-card span { margin: 0; }
.media-card span { color: var(--muted); font-size: 12px; }
.media-empty { grid-column: 1 / -1; }
.product-selector-dialog { width: min(980px, calc(100vw - 48px)); }
.product-search-grid { grid-template-columns: 1.4fr 1fr auto; align-items: end; }
.selector-actions { justify-content: flex-end; }
@media (max-width:1100px){ .filter-panel-grid,.template-form-grid,.hero-side-grid,.product-search-grid,.media-grid{ grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width:820px){ .filter-panel-grid,.template-form-grid,.hero-side-grid,.product-search-grid,.media-grid{ grid-template-columns: 1fr; } .selected-media-card { flex-direction: column; align-items: flex-start; } }
</style>
