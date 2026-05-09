<template>
  <div class="admin-page product-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="prod-keyword">搜索商品</label>
          <input
            id="prod-keyword"
            v-model.trim="query.keyword"
            type="text"
            placeholder="商品名称、ERP 编码或 ERP ISBN 码"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="field">
          <label for="prod-page-size">每页条数</label>
          <select id="prod-page-size" v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">10 条</option>
            <option :value="20">20 条</option>
            <option :value="50">50 条</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="handleSearch">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button v-if="canCreate" type="button" class="primary-button compact" @click="openCreateDialog">+ 新增商品</button>
        <button type="button" class="ghost-button compact" @click="loadData">刷新</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>商品档案</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="data-card-meta">{{ querySummary }}</div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 80px;">主图</th>
              <th style="min-width: 240px;">商品信息</th>
              <th style="min-width: 140px;">价格</th>
              <th style="min-width: 64px;">库存</th>
              <th style="min-width: 180px;">提货券有效期</th>
              <th style="min-width: 72px;">状态</th>
              <th style="min-width: 84px;">前台展示</th>
              <th style="min-width: 156px;">创建时间</th>
              <th style="min-width: 110px;">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td>{{ item.id }}</td>
              <td>
                <img v-if="item.mainImageUrl" :src="normalizeFileUrl(item.mainImageUrl)" alt="商品主图" class="product-thumb" />
                <span v-else class="muted-line">未配置</span>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ item.name }}</strong>
                  <span class="muted-line">ERP 编码：<span class="cell-mono">{{ item.erpProductCode }}</span></span>
                  <span class="muted-line">ISBN：<span class="cell-mono">{{ item.erpIsbnCode || '-' }}</span> · 详情图 {{ item.detailImageAssetIds.length }} 张</span>
                </div>
              </td>
              <td>
                <div class="cell-stack">
                  <strong class="sale-price">{{ formatPrice(item.salePrice) }}</strong>
                  <span class="muted-line">ERP：{{ formatPrice(item.erpOriginalPrice) }}</span>
                  <span v-if="showPriceCompare(item)" class="discount-line">优惠 {{ formatDiscount(item.erpOriginalPrice, item.salePrice) }}</span>
                </div>
              </td>
              <td>{{ formatStock(item.stockQuantity) }}</td>
              <td>
                <span :class="['status-badge', item.directPurchaseValidPeriodType ? 'success' : 'danger']">
                  {{ formatValidity(item) }}
                </span>
              </td>
              <td>
                <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span>
              </td>
              <td>
                <span :class="['status-badge', item.showInMiniApp === true ? 'success' : 'warning']">
                  {{ item.showInMiniApp === true ? '展示' : (item.showInMiniApp === false ? '不展示' : '未设置') }}
                </span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <button v-if="canEdit" type="button" class="cell-link" @click="openEditDialog(item)">编辑</button>
                <button v-if="canDelete" type="button" class="cell-link danger" :disabled="deleting" @click="removeItem(item)">删除</button>
              </td>
            </tr>
            <tr v-if="items.length === 0" class="empty-row">
              <td colspan="10">暂无商品数据，可调整筛选条件或新增商品</td>
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
      :title="editingId ? '编辑商品' : '新增商品'"
      sub="商品可直接购买，保存时会同步对应的系统提货券模板"
      size="xl"
      @close="closeDialog"
    >
      <div class="dialog-form-grid">
        <label class="dialog-field">
          <span>商品名称</span>
          <input v-model.trim="form.name" type="text" maxlength="100" placeholder="请输入商品名称" />
        </label>
        <label class="dialog-field">
          <span>ERP 商品编码</span>
          <input v-model.trim="form.erpProductCode" type="text" maxlength="64" placeholder="请输入 ERP 商品编码" />
        </label>
        <label class="dialog-field">
          <span>ERP ISBN 码</span>
          <input v-model.trim="form.erpIsbnCode" type="text" maxlength="64" placeholder="用于前端条码展示" />
        </label>
        <label class="dialog-field">
          <span>ERP 售价</span>
          <input v-model.number="form.erpOriginalPrice" type="number" min="0" step="0.01" placeholder="请输入 ERP 售价" />
        </label>
        <label class="dialog-field">
          <span>销售价格</span>
          <input v-model.number="form.salePrice" type="number" min="0" step="0.01" placeholder="请输入销售价格" />
        </label>
        <label class="dialog-field">
          <span>库存数量</span>
          <input v-model.number="form.stockQuantity" type="number" min="0" step="1" placeholder="不填表示不限库存" />
        </label>
        <label class="dialog-field">
          <span>直购提货券有效期</span>
          <select v-model.number="form.directPurchaseValidPeriodType" @change="handleValidPeriodTypeChange">
            <option :value="1">固定日期范围</option>
            <option :value="2">购买后 N 天</option>
          </select>
        </label>
        <label v-if="form.directPurchaseValidPeriodType === 1" class="dialog-field">
          <span>开始日期</span>
          <input v-model="form.directPurchaseValidFrom" type="date" />
        </label>
        <label v-if="form.directPurchaseValidPeriodType === 1" class="dialog-field">
          <span>结束日期</span>
          <input v-model="form.directPurchaseValidTo" type="date" />
        </label>
        <label v-if="form.directPurchaseValidPeriodType === 2" class="dialog-field field-span-2">
          <span>购买后有效天数（自动按自然日截止到 23:59:59）</span>
          <input v-model.number="form.directPurchaseValidDays" type="number" min="1" step="1" placeholder="请输入有效天数，例如 7" />
        </label>
        <label class="dialog-field checkbox-row">
          <input v-model="form.isEnabled" type="checkbox" />
          <span>启用商品</span>
        </label>
        <label class="dialog-field checkbox-row">
          <input v-model="form.showInMiniApp" type="checkbox" />
          <span>前台展示（小程序商城）</span>
        </label>
      </div>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>商品主图（必填）</h4>
          <div class="head-actions">
            <button type="button" class="ghost-button compact" @click="openMediaDialog('main')">选择素材</button>
            <label class="ghost-button compact upload-trigger">上传主图<input type="file" accept="image/*" class="hidden-input" @change="handleMainUpload" /></label>
            <button v-if="selectedMainAsset" type="button" class="ghost-button compact" @click="clearMainAsset">移除</button>
          </div>
        </header>
        <div v-if="selectedMainAsset" class="selected-main-preview">
          <img :src="normalizeFileUrl(selectedMainAsset.fileUrl)" alt="商品主图" class="selected-main-image" />
          <div class="cell-stack">
            <strong>{{ selectedMainAsset.name }}</strong>
            <span class="muted-line">{{ selectedMainAsset.bucketType }}</span>
          </div>
        </div>
        <div v-else class="detail-empty">请先配置主图（可从 product / shared 素材中复用或直接上传）</div>
      </section>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>商品详情图（可选）</h4>
          <div class="head-actions">
            <button type="button" class="ghost-button compact" @click="openMediaDialog('detail')">选择素材</button>
            <label class="ghost-button compact upload-trigger">上传详情图<input type="file" accept="image/*" multiple class="hidden-input" @change="handleDetailUpload" /></label>
          </div>
        </header>
        <div v-if="selectedDetailAssets.length > 0" class="detail-asset-grid">
          <div v-for="asset in selectedDetailAssets" :key="asset.id" class="detail-asset-card">
            <img :src="normalizeFileUrl(asset.fileUrl)" :alt="asset.name" class="detail-asset-image" />
            <div class="cell-stack">
              <strong>{{ asset.name }}</strong>
              <span class="muted-line">{{ asset.bucketType }}</span>
            </div>
            <button type="button" class="cell-link danger" @click="removeDetailAsset(asset.id)">移除</button>
          </div>
        </div>
        <div v-else class="detail-empty">未配置详情图</div>
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
      v-if="mediaDialogVisible"
      :title="mediaDialogMode === 'main' ? '选择商品主图' : '选择商品详情图'"
      sub="支持从 product / shared 分区素材中检索复用"
      size="xl"
      @close="closeMediaDialog"
    >
      <div class="picker-search-row">
        <input v-model.trim="mediaQuery.keyword" type="text" placeholder="输入素材名称关键字" @keyup.enter="loadMediaOptions" />
        <button type="button" class="primary-button compact" @click="loadMediaOptions">搜索</button>
        <button type="button" class="ghost-button compact" @click="resetMediaQuery">重置</button>
      </div>
      <div class="media-grid">
        <button v-for="asset in mediaOptions" :key="asset.id" type="button" class="media-card" @click="selectMediaAsset(asset)">
          <img :src="normalizeFileUrl(asset.fileUrl)" :alt="asset.name" class="media-card-image" />
          <strong>{{ asset.name }}</strong>
          <span class="muted-line">{{ asset.bucketType }}</span>
        </button>
        <div v-if="mediaOptions.length === 0" class="detail-empty media-empty">暂无可选素材，可换关键字重试或先上传图片</div>
      </div>
      <template #footer>
        <button type="button" class="ghost-button compact" @click="closeMediaDialog">关闭</button>
      </template>
    </MainDetailDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import MainDetailDialog from '@/components/MainDetailDialog.vue'
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

const querySummary = computed(() => `关键字：${query.keyword || '全部商品'} · 每页 ${pageSize.value} 条`)

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
  if (!window.confirm(`确认删除商品"${item.name}"吗？`)) return
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

function notifyColumnSettingsPlaceholder() {
  notify.info('列设置功能将在下一版本提供')
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
.dialog-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  padding: 12px 14px;
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
.dialog-field input[type='date'],
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

.cell-link.danger {
  margin-left: 12px;
}

.product-thumb {
  width: 56px;
  height: 56px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid var(--line);
  background: #fff;
}

.sale-price {
  color: var(--primary);
  font-weight: 700;
}

.discount-line {
  color: #b45309;
  font-size: 12px;
  font-weight: 600;
}

.head-actions {
  margin-left: auto;
  display: flex;
  gap: 8px;
}

.upload-trigger {
  position: relative;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.hidden-input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}

.selected-main-preview {
  display: grid;
  grid-template-columns: 96px minmax(0, 1fr);
  gap: 12px;
  padding: 12px 14px;
  align-items: center;
  background: #fafbfc;
  border-top: 1px solid var(--line);
}

.selected-main-image {
  width: 96px;
  height: 96px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid var(--line);
  background: #fff;
}

.detail-asset-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
  padding: 12px 14px;
}

.detail-asset-card {
  display: grid;
  grid-template-columns: 80px minmax(0, 1fr) auto;
  gap: 10px;
  align-items: center;
  padding: 8px;
  border: 1px solid var(--line);
  border-radius: 6px;
  background: #fcfdff;
}

.detail-asset-image {
  width: 80px;
  height: 80px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid var(--line);
  background: #fff;
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
  .dialog-form-grid { grid-template-columns: 1fr; }
  .detail-asset-grid { grid-template-columns: 1fr; }
}

@media (max-width: 720px) {
  .selected-main-preview { grid-template-columns: 1fr; }
  .detail-asset-card { grid-template-columns: 1fr; }
}
</style>
