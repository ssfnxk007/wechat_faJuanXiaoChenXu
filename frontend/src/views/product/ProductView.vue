
<template>
  <div class="business-page page-v2 product-page">
    <section class="hero-panel product-hero">
      <div class="hero-copy">
        <span class="page-kicker">商品中心</span>
        <h2>商品管理</h2>
        <p>维护 ERP 商品编码、销售价格、主图详情图，以及商品直购提货券的有效期配置。</p>
        <div class="hero-tags">
          <span class="badge info">ERP 商品映射</span>
          <span class="badge success">直购提货券</span>
          <span class="badge warning">有效期必填</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">商品总数</span>
          <strong>{{ totalCount }}</strong>
          <p>当前查询范围内的商品记录数</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">启用商品</span>
          <strong>{{ enabledCount }}</strong>
          <p>当前页处于启用状态的商品</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前页码</span>
          <strong>{{ pageIndex }} / {{ totalPages }}</strong>
          <p>支持关键字搜索与分页浏览</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">平均售价</span>
          <strong>{{ averagePriceDisplay }}</strong>
          <p>当前页有售价商品的均价</p>
        </article>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">商品总数</span>
        <strong class="stat-value">{{ totalCount }}</strong>
        <span class="stat-footnote">当前筛选结果总记录数</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">当前页码</span>
        <strong class="stat-value">{{ pageIndex }}</strong>
        <span class="stat-footnote">共 {{ totalPages }} 页</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">启用商品</span>
        <strong class="stat-value">{{ enabledCount }}</strong>
        <span class="stat-footnote">当前页启用状态统计</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">当前筛选</span>
        <strong class="stat-value stat-value-text">{{ query.keyword || '全部商品' }}</strong>
        <span class="stat-footnote">按商品名称、ERP 编码或 ERP ISBN 码检索</span>
      </article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">筛选与操作</span>
          <h3>商品档案工作台</h3>
          <p class="section-tip">商品可直接购买，后台必须配置直购提货券有效期，主图、ERP 商品编码、ERP ISBN 码、ERP 售价、销售价格均为关键字段。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetQuery">重置筛选</button>
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新增商品</button>
        </div>
      </div>

      <div class="filter-panel-grid product-filter-grid">
        <label class="field-card filter-field">
          <span class="field-label">搜索商品</span>
          <input
            v-model.trim="query.keyword"
            type="text"
            placeholder="搜索商品名称、ERP 编码或 ERP ISBN 码"
            @keyup.enter="handleSearch"
          />
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
          <span class="field-label">当前说明</span>
          <strong>{{ querySummary }}</strong>
          <p>直购提货券有效期支持固定日期范围，或购买后 N 天自然日截止。</p>
        </div>
      </div>
    </section>

    <section class="card card-v2">
      <div class="table-card-head">
        <div>
          <span class="section-kicker">商品列表</span>
          <h3>商品档案</h3>
        </div>
      </div>

      <div class="table-shell">
        <table class="data-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>主图</th>
              <th>商品信息</th>
              <th>价格</th>
              <th>库存</th>
              <th>提货券有效期</th>
              <th>状态</th>
              <th>前台展示</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td>#{{ item.id }}</td>
              <td>
                <div class="product-thumb-cell">
                  <img v-if="item.mainImageUrl" :src="normalizeFileUrl(item.mainImageUrl)" alt="商品主图" class="product-thumb" />
                  <span v-else class="muted-text">未配置主图</span>
                </div>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ item.name }}</strong>
                  <span>ERP 编码：{{ item.erpProductCode }}</span>
                  <span>ERP ISBN 码：{{ item.erpIsbnCode || '-' }}</span>
                  <span>详情图：{{ item.detailImageAssetIds.length }} 张</span>
                </div>
              </td>
              <td>
                <div class="price-compare-cell">
                  <strong class="sale-price-value">{{ formatPrice(item.salePrice) }}</strong>
                  <span>ERP 售价：{{ formatPrice(item.erpOriginalPrice) }}</span>
                  <span v-if="showPriceCompare(item)" class="discount-value">优惠 {{ formatDiscount(item.erpOriginalPrice, item.salePrice) }}</span>
                </div>
              </td>
              <td>{{ formatStock(item.stockQuantity) }}</td>
              <td>
                <span :class="item.directPurchaseValidPeriodType ? 'status-badge status-enabled' : 'status-badge status-disabled'">
                  {{ formatValidity(item) }}
                </span>
              </td>
              <td>
                <span :class="item.isEnabled ? 'status-badge status-enabled' : 'status-badge status-disabled'">
                  {{ item.isEnabled ? '启用' : '停用' }}
                </span>
              </td>
              <td>
                <span :class="item.showInMiniApp === true ? 'status-badge status-enabled' : 'status-badge status-disabled'">
                  {{ item.showInMiniApp === true ? '展示' : (item.showInMiniApp === false ? '不展示' : '未设置') }}
                </span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <div class="table-actions">
                  <button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button>
                  <button v-if="canDelete" type="button" class="action-button danger" :disabled="deleting" @click="removeItem(item)">删除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="10">
                <div class="empty-state compact-empty">
                  <strong>暂无商品数据</strong>
                  <p>可尝试调整筛选条件，或新增商品。</p>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination-bar">
        <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
        <span>第 {{ pageIndex }} / {{ totalPages }} 页</span>
        <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
      </div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 product-dialog-card">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">商品档案</span>
            <h3>{{ editingId ? '编辑商品' : '新增商品' }}</h3>
            <p>商品可直接购买，保存时会同步对应的系统提货券模板。</p>
          </div>
        </div>

        <div class="grid-form dialog-form product-form-grid">
          <label>
            <span>商品名称</span>
            <input v-model.trim="form.name" type="text" maxlength="100" placeholder="请输入商品名称" />
          </label>
          <label>
            <span>ERP 商品编码</span>
            <input v-model.trim="form.erpProductCode" type="text" maxlength="64" placeholder="请输入 ERP 商品编码" />
          </label>
          <label>
            <span>ERP ISBN 码</span>
            <input v-model.trim="form.erpIsbnCode" type="text" maxlength="64" placeholder="请输入 ERP ISBN 码（用于前端条码展示）" />
          </label>
          <label>
            <span>ERP 售价</span>
            <input v-model.number="form.erpOriginalPrice" type="number" min="0" step="0.01" placeholder="请输入 ERP 售价" />
          </label>
          <label>
            <span>销售价格</span>
            <input v-model.number="form.salePrice" type="number" min="0" step="0.01" placeholder="请输入销售价格" />
          </label>
          <label>
            <span>库存数量</span>
            <input v-model.number="form.stockQuantity" type="number" min="0" step="1" placeholder="不填表示不限库存" />
          </label>
          <label>
            <span>直购提货券有效期</span>
            <select v-model.number="form.directPurchaseValidPeriodType" @change="handleValidPeriodTypeChange">
              <option :value="1">固定日期范围</option>
              <option :value="2">购买后 N 天</option>
            </select>
          </label>

          <label v-if="form.directPurchaseValidPeriodType === 1">
            <span>开始日期</span>
            <input v-model="form.directPurchaseValidFrom" type="date" />
          </label>
          <label v-if="form.directPurchaseValidPeriodType === 1">
            <span>结束日期</span>
            <input v-model="form.directPurchaseValidTo" type="date" />
          </label>
          <label v-if="form.directPurchaseValidPeriodType === 2" class="field-span-2">
            <span>购买后有效天数</span>
            <input v-model.number="form.directPurchaseValidDays" type="number" min="1" step="1" placeholder="请输入有效天数，例如 7" />
            <small class="helper-text">到期时间自动按自然日截止到 23:59:59。</small>
          </label>

          <label class="checkbox-field checkbox-card field-span-2">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用商品</span>
          </label>

          <label class="checkbox-field checkbox-card field-span-2">
            <input v-model="form.showInMiniApp" type="checkbox" />
            <span>前台展示</span>
            <small class="helper-text">勾选后才会出现在小程序首页推荐、商城商品列表和商品详情入口；不影响组券包使用。</small>
          </label>

          <div class="field-span-2 media-section">
            <div class="section-inline-head">
              <div>
                <span class="field-label">商品主图</span>
                <p class="helper-text">主图必填，可从 `product/shared` 素材中复用或直接上传。</p>
              </div>
              <div class="inline-actions">
                <button type="button" class="ghost-button" @click="openMediaDialog('main')">选择素材</button>
                <label class="ghost-button upload-button">
                  上传主图
                  <input type="file" accept="image/*" class="hidden-input" @change="handleMainUpload" />
                </label>
              </div>
            </div>
            <div v-if="selectedMainAsset" class="selected-main-preview">
              <img :src="normalizeFileUrl(selectedMainAsset.fileUrl)" alt="商品主图" class="selected-main-image" />
              <div class="table-primary-cell">
                <strong>{{ selectedMainAsset.name }}</strong>
                <span>{{ selectedMainAsset.bucketType }}</span>
              </div>
              <button type="button" class="ghost-button" @click="clearMainAsset">移除主图</button>
            </div>
            <p v-else class="helper-text">请先配置主图。</p>
          </div>

          <div class="field-span-2 media-section">
            <div class="section-inline-head">
              <div>
                <span class="field-label">详情图</span>
                <p class="helper-text">详情图可选，用于商品详情展示。</p>
              </div>
              <div class="inline-actions">
                <button type="button" class="ghost-button" @click="openMediaDialog('detail')">选择素材</button>
                <label class="ghost-button upload-button">
                  上传详情图
                  <input type="file" accept="image/*" multiple class="hidden-input" @change="handleDetailUpload" />
                </label>
              </div>
            </div>
            <div v-if="selectedDetailAssets.length > 0" class="selected-detail-list">
              <div v-for="asset in selectedDetailAssets" :key="asset.id" class="selected-detail-card">
                <img :src="normalizeFileUrl(asset.fileUrl)" :alt="asset.name" class="selected-detail-image" />
                <div class="table-primary-cell">
                  <strong>{{ asset.name }}</strong>
                  <span>{{ asset.bucketType }}</span>
                </div>
                <button type="button" class="ghost-button" @click="removeDetailAsset(asset.id)">移除</button>
              </div>
            </div>
            <p v-else class="helper-text">未配置详情图。</p>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">取消</button>
          <button
            v-if="editingId ? canEdit : canCreate"
            type="button"
            class="primary-button"
            :disabled="submitting || deleting"
            @click="submit"
          >
            {{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="mediaDialogVisible" class="dialog-mask" @click.self="closeMediaDialog">
      <div class="dialog-card dialog-card-v2 media-selector-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">素材选择</span>
            <h3>{{ mediaDialogMode === 'main' ? '选择商品主图' : '选择商品详情图' }}</h3>
            <p>支持从 `product/shared` 分区素材中检索复用。</p>
          </div>
        </div>
        <div class="filter-panel-grid media-filter-grid">
          <label class="field-card filter-field">
            <span class="field-label">搜索素材</span>
            <input v-model.trim="mediaQuery.keyword" type="text" placeholder="输入素材名称关键字" @keyup.enter="loadMediaOptions" />
          </label>
          <div class="field-card summary-field">
            <span class="field-label">素材范围</span>
            <strong>product / shared</strong>
            <p>商品主图与详情图都可复用 shared 素材。</p>
          </div>
          <div class="table-actions selector-actions">
            <button type="button" class="ghost-button" @click="resetMediaQuery">重置</button>
            <button type="button" class="primary-button" @click="loadMediaOptions">搜索</button>
          </div>
        </div>

        <div class="media-grid">
          <button v-for="asset in mediaOptions" :key="asset.id" type="button" class="media-card" @click="selectMediaAsset(asset)">
            <img :src="normalizeFileUrl(asset.fileUrl)" :alt="asset.name" class="media-card-image" />
            <strong>{{ asset.name }}</strong>
            <span>{{ asset.bucketType }}</span>
          </button>
          <div v-if="mediaOptions.length === 0" class="empty-state compact-empty media-empty">
            <strong>暂无可选素材</strong>
            <p>可换个关键字重试，或先上传图片。</p>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeMediaDialog">关闭</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { createProduct, deleteProduct, getProductList, updateProduct } from '@/api/product'
import { createMediaAsset, getMediaAssetList, uploadMediaAssetFile } from '@/api/media-asset'
import type { MediaAssetListItemDto } from '@/types/media-asset'
import type { ProductDirectPurchaseValidPeriodType, ProductListItemDto, SaveProductRequest } from '@/types/product'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils/auth'
import { notify } from '@/utils/notify'
import { normalizeAssetUrl } from '@/utils/asset-url'

const FIXED_DATE_RANGE = 1 as ProductDirectPurchaseValidPeriodType
const AFTER_RECEIVE_DAYS = 2 as ProductDirectPurchaseValidPeriodType

const items = ref<ProductListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const mediaDialogVisible = ref(false)
const mediaDialogMode = ref<'main' | 'detail'>('main')
const mediaOptions = ref<MediaAssetListItemDto[]>([])
const selectedMainAsset = ref<MediaAssetListItemDto | null>(null)
const selectedDetailAssets = ref<MediaAssetListItemDto[]>([])
const submitting = ref(false)
const deleting = ref(false)

const query = reactive({ keyword: '' })
const mediaQuery = reactive({ keyword: '' })

const canCreate = authStorage.hasPermission('product.create')
const canEdit = authStorage.hasPermission('product.edit')
const canDelete = authStorage.hasPermission('product.delete')

const createEmptyForm = (): SaveProductRequest => ({
  name: '',
  erpProductCode: '',
  erpIsbnCode: '',
  mainImageAssetId: undefined,
  detailImageAssetIds: [],
  erpOriginalPrice: undefined,
  salePrice: undefined,
  stockQuantity: undefined,
  isEnabled: true,
  showInMiniApp: false,
  directPurchaseValidPeriodType: AFTER_RECEIVE_DAYS,
  directPurchaseValidDays: 7,
  directPurchaseValidFrom: undefined,
  directPurchaseValidTo: undefined,
})

const form = reactive<SaveProductRequest>(createEmptyForm())

const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const averagePrice = computed(() => {
  const values = items.value
    .map((item) => toNullableNumber(item.salePrice))
    .filter((value): value is number => typeof value === 'number' && value > 0)

  if (values.length === 0) return null
  return values.reduce((sum, value) => sum + value, 0) / values.length
})
const averagePriceDisplay = computed(() => (averagePrice.value === null ? '-' : `¥${averagePrice.value.toFixed(2)}`))
const querySummary = computed(() => `关键字：${query.keyword || '全部商品'} / 每页 ${pageSize.value} 条`)

const normalizeFileUrl = normalizeAssetUrl

function toNullableNumber(value: unknown) {
  if (value === '' || value === null || value === undefined) return undefined
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : undefined
}

function normalizeDateInputValue(value?: string | null) {
  if (!value) return undefined
  return value.slice(0, 10)
}

function resetForm() {
  Object.assign(form, createEmptyForm())
  selectedMainAsset.value = null
  selectedDetailAssets.value = []
}

async function loadData() {
  try {
    const response = await getProductList({
      keyword: query.keyword || undefined,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
    })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载商品列表失败'))
  }
}

async function loadMediaOptions() {
  try {
    const response = await getMediaAssetList({
      keyword: mediaQuery.keyword || undefined,
      pageIndex: 1,
      pageSize: 40,
    })
    mediaOptions.value = response.data.items.filter((item) => item.bucketType === 'product' || item.bucketType === 'shared')
  } catch (error) {
    notify.error(getErrorMessage(error, '加载素材列表失败'))
  }
}
async function handleSearch() {
  pageIndex.value = 1
  await loadData()
}

async function resetQuery() {
  query.keyword = ''
  pageSize.value = 10
  pageIndex.value = 1
  await loadData()
  notify.info('已重置商品筛选条件')
}

async function handlePageSizeChange() {
  pageIndex.value = 1
  await loadData()
}

async function goPrevPage() {
  if (pageIndex.value <= 1) return
  pageIndex.value -= 1
  await loadData()
}

async function goNextPage() {
  if (pageIndex.value >= totalPages.value) return
  pageIndex.value += 1
  await loadData()
}

function openCreateDialog() {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

function openEditDialog(item: ProductListItemDto) {
  editingId.value = item.id
  Object.assign(form, {
    name: item.name,
    erpProductCode: item.erpProductCode,
    erpIsbnCode: item.erpIsbnCode ?? '',
    mainImageAssetId: item.mainImageAssetId ?? undefined,
    detailImageAssetIds: [...(item.detailImageAssetIds || [])],
    erpOriginalPrice: item.erpOriginalPrice ?? undefined,
    salePrice: item.salePrice ?? undefined,
    stockQuantity: item.stockQuantity ?? undefined,
    isEnabled: item.isEnabled,
    showInMiniApp: item.showInMiniApp === true,
    directPurchaseValidPeriodType: item.directPurchaseValidPeriodType ?? AFTER_RECEIVE_DAYS,
    directPurchaseValidDays: item.directPurchaseValidDays ?? 7,
    directPurchaseValidFrom: normalizeDateInputValue(item.directPurchaseValidFrom),
    directPurchaseValidTo: normalizeDateInputValue(item.directPurchaseValidTo),
  })

  selectedMainAsset.value = item.mainImageAssetId
    ? {
        id: item.mainImageAssetId,
        name: item.name,
        fileUrl: normalizeFileUrl(item.mainImageUrl || ''),
        mediaType: 'image',
        bucketType: 'product',
        tags: [],
        sort: 0,
        isEnabled: true,
        createdAt: item.createdAt,
      }
    : null

  selectedDetailAssets.value = (item.detailImageAssetIds || []).map((id, index) => ({
    id,
    name: `详情图${index + 1}`,
    fileUrl: normalizeFileUrl(item.detailImageUrls[index] || ''),
    mediaType: 'image',
    bucketType: 'product',
    tags: [],
    sort: index,
    isEnabled: true,
    createdAt: item.createdAt,
  }))

  dialogVisible.value = true
}

function closeDialog() {
  dialogVisible.value = false
  editingId.value = null
  resetForm()
}

async function openMediaDialog(mode: 'main' | 'detail') {
  mediaDialogMode.value = mode
  mediaDialogVisible.value = true
  await loadMediaOptions()
}

function closeMediaDialog() {
  mediaDialogVisible.value = false
}

async function resetMediaQuery() {
  mediaQuery.keyword = ''
  await loadMediaOptions()
}

function selectMediaAsset(asset: MediaAssetListItemDto) {
  const normalizedAsset = {
    ...asset,
    fileUrl: normalizeFileUrl(asset.fileUrl),
  }

  if (mediaDialogMode.value === 'main') {
    selectedMainAsset.value = normalizedAsset
    form.mainImageAssetId = asset.id
    closeMediaDialog()
    return
  }

  if (selectedDetailAssets.value.some((item) => item.id === asset.id)) {
    return
  }

  selectedDetailAssets.value = [...selectedDetailAssets.value, normalizedAsset]
  form.detailImageAssetIds = selectedDetailAssets.value.map((item) => item.id)
}

function clearMainAsset() {
  selectedMainAsset.value = null
  form.mainImageAssetId = undefined
}

function removeDetailAsset(assetId: number) {
  selectedDetailAssets.value = selectedDetailAssets.value.filter((item) => item.id !== assetId)
  form.detailImageAssetIds = selectedDetailAssets.value.map((item) => item.id)
}

function handleValidPeriodTypeChange() {
  if (form.directPurchaseValidPeriodType === FIXED_DATE_RANGE) {
    form.directPurchaseValidDays = undefined
    return
  }

  form.directPurchaseValidFrom = undefined
  form.directPurchaseValidTo = undefined
  if (!toNullableNumber(form.directPurchaseValidDays) || toNullableNumber(form.directPurchaseValidDays)! <= 0) {
    form.directPurchaseValidDays = 7
  }
}

async function saveUploadedAsset(file: File, bucketType = 'product') {
  const uploadResponse = await uploadMediaAssetFile(file)
  const createResponse = await createMediaAsset({
    name: file.name,
    fileUrl: uploadResponse.data.fileUrl,
    mediaType: 'image',
    bucketType,
    tags: [],
    sort: 0,
    isEnabled: true,
  })

  return {
    id: createResponse.data,
    name: file.name,
    fileUrl: normalizeFileUrl(uploadResponse.data.fileUrl),
    mediaType: 'image',
    bucketType,
    tags: [],
    sort: 0,
    isEnabled: true,
    createdAt: new Date().toISOString(),
  } satisfies MediaAssetListItemDto
}

async function handleMainUpload(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  try {
    const asset = await saveUploadedAsset(file)
    selectedMainAsset.value = asset
    form.mainImageAssetId = asset.id
    notify.success('商品主图上传成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '上传商品主图失败'))
  } finally {
    target.value = ''
  }
}

async function handleDetailUpload(event: Event) {
  const target = event.target as HTMLInputElement
  const files = Array.from(target.files || [])
  if (files.length === 0) return

  try {
    const assets: MediaAssetListItemDto[] = []
    for (const file of files) {
      assets.push(await saveUploadedAsset(file))
    }
    selectedDetailAssets.value = [
      ...selectedDetailAssets.value,
      ...assets.filter((asset) => !selectedDetailAssets.value.some((item) => item.id === asset.id)),
    ]
    form.detailImageAssetIds = selectedDetailAssets.value.map((item) => item.id)
    notify.success('商品详情图上传成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '上传商品详情图失败'))
  } finally {
    target.value = ''
  }
}
function buildPayload(): SaveProductRequest {
  const payload: SaveProductRequest = {
    name: form.name.trim(),
    erpProductCode: form.erpProductCode.trim(),
    erpIsbnCode: form.erpIsbnCode?.trim() || undefined,
    mainImageAssetId: form.mainImageAssetId,
    detailImageAssetIds: selectedDetailAssets.value.map((item) => item.id),
    erpOriginalPrice: toNullableNumber(form.erpOriginalPrice),
    salePrice: toNullableNumber(form.salePrice),
    stockQuantity: toNullableNumber(form.stockQuantity),
    isEnabled: form.isEnabled,
    showInMiniApp: form.showInMiniApp,
    directPurchaseValidPeriodType: form.directPurchaseValidPeriodType,
  }

  if (form.directPurchaseValidPeriodType === FIXED_DATE_RANGE) {
    payload.directPurchaseValidFrom = form.directPurchaseValidFrom || undefined
    payload.directPurchaseValidTo = form.directPurchaseValidTo || undefined
    payload.directPurchaseValidDays = undefined
  } else {
    payload.directPurchaseValidDays = toNullableNumber(form.directPurchaseValidDays)
    payload.directPurchaseValidFrom = undefined
    payload.directPurchaseValidTo = undefined
  }

  return payload
}

async function submit() {
  if (!form.name.trim()) return notify.info('请输入商品名称')
  if (!form.erpProductCode.trim()) return notify.info('请输入 ERP 商品编码')
  if (!form.mainImageAssetId) return notify.info('商品主图为必填项')
  if (toNullableNumber(form.erpOriginalPrice) === undefined) return notify.info('请输入 ERP 售价')
  if (toNullableNumber(form.salePrice) === undefined) return notify.info('请输入销售价格')

  if (form.directPurchaseValidPeriodType === FIXED_DATE_RANGE) {
    if (!form.directPurchaseValidFrom || !form.directPurchaseValidTo) return notify.info('请选择提货券有效期的开始和结束日期')
    if (form.directPurchaseValidFrom > form.directPurchaseValidTo) return notify.info('提货券结束日期不能早于开始日期')
  } else if (!toNullableNumber(form.directPurchaseValidDays) || toNullableNumber(form.directPurchaseValidDays)! <= 0) {
    return notify.info('购买后有效天数必须大于 0')
  }

  if (submitting.value) return
  submitting.value = true

  try {
    const payload = buildPayload()
    if (editingId.value) {
      await updateProduct(editingId.value, payload)
      notify.success('商品已更新')
    } else {
      await createProduct(payload)
      pageIndex.value = 1
      notify.success('商品已创建')
    }

    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '保存商品失败' : '新增商品失败'))
  } finally {
    submitting.value = false
  }
}

async function removeItem(item: ProductListItemDto) {
  if (!window.confirm(`确认删除商品“${item.name}”吗？`)) return
  if (items.value.length === 1 && pageIndex.value > 1) pageIndex.value -= 1
  if (deleting.value) return

  deleting.value = true
  try {
    await deleteProduct(item.id)
    await loadData()
    notify.success('商品已删除')
  } catch (error) {
    notify.error(getErrorMessage(error, '删除商品失败'))
  } finally {
    deleting.value = false
  }
}

function formatDate(value?: string | null) {
  return value ? value.replace('T', ' ').slice(0, 19) : '-'
}

function formatPrice(value?: number | null) {
  return typeof value === 'number' && Number.isFinite(value) ? `¥${value.toFixed(2)}` : '-'
}

function formatStock(value?: number | null) {
  return typeof value === 'number' && Number.isFinite(value) ? String(value) : '-'
}

function formatDiscount(erpOriginalPrice?: number | null, salePrice?: number | null) {
  if (typeof erpOriginalPrice !== 'number' || typeof salePrice !== 'number') return '-'
  const discount = erpOriginalPrice - salePrice
  return discount > 0 ? `¥${discount.toFixed(2)}` : '-'
}

function showPriceCompare(item: ProductListItemDto) {
  return typeof item.erpOriginalPrice === 'number'
    && typeof item.salePrice === 'number'
    && item.erpOriginalPrice > item.salePrice
}

function formatValidity(item: ProductListItemDto) {
  if (item.directPurchaseValidPeriodType === FIXED_DATE_RANGE) {
    if (!item.directPurchaseValidFrom || !item.directPurchaseValidTo) return '未配置'
    return `${item.directPurchaseValidFrom.slice(0, 10)} 至 ${item.directPurchaseValidTo.slice(0, 10)}`
  }

  if (item.directPurchaseValidPeriodType === AFTER_RECEIVE_DAYS) {
    return item.directPurchaseValidDays ? `购买后 ${item.directPurchaseValidDays} 天内有效` : '未配置'
  }

  return '未配置'
}

onMounted(loadData)
</script>

<style scoped>
.product-hero { background: radial-gradient(circle at top right, rgba(59,130,246,.14), transparent 28%), linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%); }
.product-filter-grid,.media-filter-grid { grid-template-columns: 1.4fr .8fr 1fr; }
.product-thumb-cell { display: flex; align-items: center; }
.product-thumb { width: 56px; height: 56px; object-fit: cover; border-radius: 12px; border: 1px solid var(--line); background: #fff; }
.product-dialog-card { width: min(980px, calc(100vw - 48px)); }
.product-form-grid { grid-template-columns: repeat(2, minmax(0,1fr)); }
.price-compare-cell { display: grid; gap: 4px; }
.sale-price-value { color: var(--primary); font-weight: 700; }
.discount-value { color: #f59e0b; font-size: 12px; font-weight: 600; }
.dialog-form input,.dialog-form select { width: 100%; min-height: 44px; padding: 10px 14px; border: 1px solid var(--line-strong); border-radius: 12px; background: #fff; }
.field-span-2 { grid-column: span 2; }
.media-section { display: grid; gap: 12px; }
.section-inline-head { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; }
.inline-actions { display: flex; gap: 10px; flex-wrap: wrap; }
.helper-text { color: var(--muted); font-size: 12px; }
.upload-button { position: relative; overflow: hidden; cursor: pointer; }
.hidden-input { position: absolute; inset: 0; opacity: 0; cursor: pointer; }
.selected-main-preview { display: grid; grid-template-columns: 120px minmax(0,1fr) auto; gap: 14px; align-items: center; }
.selected-main-image,.selected-detail-image,.media-card-image { width: 120px; height: 120px; object-fit: cover; border-radius: 16px; border: 1px solid var(--line); background: #fff; }
.selected-detail-list { display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 12px; }
.selected-detail-card { display: grid; grid-template-columns: 120px minmax(0,1fr) auto; gap: 12px; align-items: center; padding: 12px; border-radius: 16px; border: 1px solid var(--line); background: #fff; }
.media-selector-dialog { width: min(1080px, calc(100vw - 48px)); }
.selector-actions { justify-content: flex-end; align-items: stretch; }
.media-grid { display: grid; grid-template-columns: repeat(4, minmax(0,1fr)); gap: 14px; }
.media-card { display: grid; gap: 8px; padding: 12px; border: 1px solid var(--line); border-radius: 16px; background: #fff; text-align: left; }
.media-card strong { font-size: 14px; }
.media-card span { color: var(--muted); font-size: 12px; }
.media-empty { grid-column: 1 / -1; }

@media (max-width: 1100px) {
  .hero-side-grid,.product-filter-grid,.media-filter-grid,.product-form-grid,.selected-detail-list,.media-grid { grid-template-columns: repeat(2, minmax(0,1fr)); }
  .selected-main-preview,.selected-detail-card { grid-template-columns: 1fr; }
}

@media (max-width: 820px) {
  .hero-side-grid,.product-filter-grid,.media-filter-grid,.product-form-grid,.selected-detail-list,.media-grid { grid-template-columns: 1fr; }
  .field-span-2 { grid-column: span 1; }
  .section-inline-head { flex-direction: column; }
}
</style>
