<template>
  <div class="business-page page-v2 product-page">
    <section class="hero-panel product-hero">
      <div class="hero-copy">
        <span class="page-kicker">商品中心</span>
        <h2>商品管理</h2>
        <p>维护 ERP 商品映射、销售价格和图片素材引用。主图作为商品识别主入口必填，详情图支持后置补充。</p>
        <div class="hero-tags">
          <span class="badge info">ERP 商品映射</span>
          <span class="badge success">主图必填</span>
          <span class="badge warning">详情图可后置</span>
        </div>
      </div>
      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight">
          <span class="quick-card-label">商品总数</span>
          <strong>{{ totalCount }}</strong>
          <p>当前查询范围内的商品档案数量。</p>
        </article>
        <div class="hero-side-grid">
          <article class="quick-card compact"><span class="quick-card-label">启用商品</span><strong>{{ enabledCount }}</strong><p>当前页启用中的商品</p></article>
          <article class="quick-card compact"><span class="quick-card-label">平均售价</span><strong>{{ averagePriceDisplay }}</strong><p>当前页价格均值</p></article>
        </div>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue"><span class="label">商品总数</span><strong class="stat-value">{{ totalCount }}</strong><span class="stat-footnote">当前筛选范围内记录数</span></article>
      <article class="stat-card accent-indigo"><span class="label">当前页码</span><strong class="stat-value">{{ pageIndex }}</strong><span class="stat-footnote">共 {{ totalPages }} 页</span></article>
      <article class="stat-card accent-green"><span class="label">启用商品</span><strong class="stat-value">{{ enabledCount }}</strong><span class="stat-footnote">当前页启用状态统计</span></article>
      <article class="stat-card accent-amber"><span class="label">均价</span><strong class="stat-value">{{ averagePriceDisplay }}</strong><span class="stat-footnote">当前页有售价商品的均价</span></article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">业务操作台</span>
          <h3>商品筛选与档案维护</h3>
          <p class="section-tip">按商品名称或 ERP 编码搜索，并统一管理商品基础信息和图片素材。</p>
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
          <input v-model.trim="query.keyword" type="text" placeholder="搜索商品名称或 ERP 编码" @keyup.enter="handleSearch" />
        </label>
        <label class="field-card filter-field compact-field">
          <span class="field-label">分页条数</span>
          <select v-model.number="pageSize" @change="handlePageSizeChange"><option :value="10">每页 10 条</option><option :value="20">每页 20 条</option><option :value="50">每页 50 条</option></select>
        </label>
        <div class="field-card summary-field"><span class="field-label">当前筛选</span><strong>{{ querySummary }}</strong><p>主图通过素材选择器配置，详情图支持后置补充。</p></div>
      </div>
    </section>

    <section class="product-guide-grid">
      <article class="card card-v2 action-entry-card">
        <div class="entry-head">
          <span class="section-kicker">建档原则</span>
          <h3>商品档案优先级</h3>
          <p class="section-tip">主图、ERP 编码和售价是第一优先级，详情图可在档案建立后继续补充。</p>
        </div>
        <div class="entry-metrics">
          <div>
            <strong>主图必填，详情图可后置</strong>
            <span>先满足识别和售卖最小闭环，再逐步丰富详情素材。</span>
          </div>
        </div>
      </article>

      <article class="card card-v2 action-entry-card">
        <div class="entry-head">
          <span class="section-kicker">素材规则</span>
          <h3>图片素材使用方式</h3>
          <p class="section-tip">统一从 `product / shared` 里选图，减少重复上传与素材分散。</p>
        </div>
        <div class="entry-metrics">
          <div>
            <strong>共享素材优先复用</strong>
            <span>主图负责列表识别，详情图负责页面补充说明。</span>
          </div>
        </div>
      </article>
    </section>

    <section class="card card-v2 data-card">
      <div class="section-head"><div class="section-head-main"><span class="section-kicker">商品档案</span><h3>商品列表</h3><p class="section-tip">展示主图、ERP 编码、售价与状态，便于券模板和核销模块复用。</p></div><div class="inline-metrics"><span class="badge info">总数 {{ totalCount }}</span><span class="badge warning">每页 {{ pageSize }}</span></div></div>
      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead><tr><th>ID</th><th>商品</th><th>ERP 编码</th><th>售价</th><th>图片</th><th>状态</th><th>创建时间</th><th>操作</th></tr></thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td><div class="table-primary-cell"><strong>{{ item.name }}</strong><span>{{ item.detailImageAssetIds.length > 0 ? `详情图 ${item.detailImageAssetIds.length} 张` : '未配置详情图' }}</span></div></td>
              <td class="cell-mono">{{ item.erpProductCode }}</td>
              <td>{{ formatPrice(item.salePrice) }}</td>
              <td><div class="product-thumb-cell"><img v-if="item.mainImageUrl" :src="item.mainImageUrl" alt="商品主图" class="product-thumb" /><span v-else class="muted-text">未配置主图</span></div></td>
              <td><span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span></td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td><div class="table-actions"><button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button><button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button></div></td>
            </tr>
            <tr v-if="items.length === 0"><td colspan="8" class="empty-text">当前没有商品数据</td></tr>
          </tbody>
        </table>
      </div>
      <div class="pager pager-v2"><div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条记录</div><div class="pager-actions"><button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button><button type="button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button></div></div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 product-dialog-card">
        <div class="dialog-head"><div class="dialog-head-main"><span class="section-kicker">商品表单</span><h3>{{ editingId ? '编辑商品' : '新增商品' }}</h3><p>{{ editingId ? '调整商品基础资料与图片素材。' : '建立新的商品档案，主图为必填项。' }}</p></div></div>
        <div class="grid-form dialog-form product-form-grid">
          <label><span>商品名称</span><input v-model.trim="form.name" type="text" placeholder="请输入商品名称" /></label>
          <label><span>ERP 商品编码</span><input v-model.trim="form.erpProductCode" type="text" placeholder="请输入 ERP 商品编码" /></label>
          <label><span>销售价格</span><input v-model.number="form.salePrice" type="number" min="0" step="0.01" placeholder="例如：19.90" /></label>
          <label class="checkbox-field checkbox-card"><input v-model="form.isEnabled" type="checkbox" /><span>启用商品</span></label>

          <div class="field-span-2 selector-field-card">
            <div class="selector-field-head">
              <span>商品主图</span>
              <div class="toolbar-actions"><button type="button" class="ghost-button" @click="openMediaDialog('main')">选择素材</button><label class="ghost-button upload-trigger">上传图片<input type="file" accept="image/*" @change="handleMainUpload" /></label></div>
            </div>
            <div v-if="selectedMainAsset" class="selected-main-preview"><img :src="selectedMainAsset.fileUrl" alt="商品主图" class="selected-main-image" /><div class="table-primary-cell"><strong>{{ selectedMainAsset.name }}</strong><span>{{ selectedMainAsset.bucketType }}</span></div><button type="button" class="ghost-button" @click="clearMainAsset">移除主图</button></div>
            <p v-else class="helper-text">主图必填。可从 `product / shared` 素材中选择，也可直接上传新图片。</p>
          </div>

          <div class="field-span-2 selector-field-card">
            <div class="selector-field-head">
              <span>详情图</span>
              <div class="toolbar-actions"><button type="button" class="ghost-button" @click="openMediaDialog('detail')">选择素材</button><label class="ghost-button upload-trigger">上传图片<input type="file" accept="image/*" multiple @change="handleDetailUpload" /></label></div>
            </div>
            <div v-if="selectedDetailAssets.length > 0" class="selected-detail-list"><div v-for="asset in selectedDetailAssets" :key="asset.id" class="selected-detail-card"><img :src="asset.fileUrl" :alt="asset.name" class="selected-detail-image" /><div class="table-primary-cell"><strong>{{ asset.name }}</strong><span>{{ asset.bucketType }}</span></div><button type="button" class="ghost-button" @click="removeDetailAsset(asset.id)">移除</button></div></div>
            <p v-else class="helper-text">详情图可选，支持后续补充，不影响商品先行建档。</p>
          </div>
        </div>
        <div class="dialog-actions"><button type="button" class="ghost-button" :disabled="submitting || deleting" @click="closeDialog">取消</button><button v-if="editingId ? canEdit : canCreate" type="button" class="primary-button" :disabled="submitting || deleting" @click="submit">{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button></div>
      </div>
    </div>

    <div v-if="mediaDialogVisible" class="dialog-mask" @click.self="closeMediaDialog">
      <div class="dialog-card dialog-card-v2 media-selector-dialog">
        <div class="dialog-head"><div class="dialog-head-main"><span class="section-kicker">素材选择</span><h3>{{ mediaDialogMode === 'main' ? '选择商品主图' : '选择商品详情图' }}</h3><p>支持从 `product / shared` 分区素材中检索和复用。</p></div></div>
        <div class="filter-panel-grid media-filter-grid">
          <label class="field-card filter-field"><span class="field-label">搜索素材</span><input v-model.trim="mediaQuery.keyword" type="text" placeholder="输入素材名称后回车检索" @keyup.enter="loadMediaOptions" /></label>
          <div class="field-card summary-field"><span class="field-label">素材范围</span><strong>product / shared</strong><p>商品主图与详情图都可复用 shared 素材。</p></div>
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
import { computed, onMounted, reactive, ref } from 'vue'
import { createProduct, deleteProduct, getProductList, updateProduct } from '@/api/product'
import { createMediaAsset, getMediaAssetList, uploadMediaAssetFile } from '@/api/media-asset'
import type { MediaAssetListItemDto } from '@/types/media-asset'
import type { ProductListItemDto, SaveProductRequest } from '@/types/product'
import { getErrorMessage } from '@/utils/http-error'
import { authStorage } from '@/utils.auth'
import { notify } from '@/utils/notify'

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

const createEmptyForm = (): SaveProductRequest => ({
  name: '',
  erpProductCode: '',
  mainImageAssetId: undefined,
  detailImageAssetIds: [],
  salePrice: undefined,
  isEnabled: true,
})

const form = reactive<SaveProductRequest>(createEmptyForm())
const canCreate = authStorage.hasPermission('product.create')
const canEdit = authStorage.hasPermission('product.edit')
const canDelete = authStorage.hasPermission('product.delete')

const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const averagePrice = computed(() => {
  const values = items.value.map((item) => Number(item.salePrice || 0)).filter((value) => value > 0)
  if (values.length === 0) return null
  return values.reduce((sum, value) => sum + value, 0) / values.length
})
const averagePriceDisplay = computed(() => (averagePrice.value === null ? '-' : `¥${averagePrice.value.toFixed(2)}`))
const querySummary = computed(() => `关键词：${query.keyword || '全部商品'} / 每页 ${pageSize.value} 条`)

const resetForm = () => {
  Object.assign(form, createEmptyForm())
  selectedMainAsset.value = null
  selectedDetailAssets.value = []
}

const loadData = async () => {
  try {
    const response = await getProductList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载商品列表失败'))
  }
}

const loadMediaOptions = async () => {
  try {
    const response = await getMediaAssetList({ keyword: mediaQuery.keyword || undefined, pageIndex: 1, pageSize: 40 })
    mediaOptions.value = response.data.items.filter((item) => item.bucketType === 'product' || item.bucketType === 'shared')
  } catch (error) {
    notify.error(getErrorMessage(error, '加载素材列表失败'))
  }
}

const handleSearch = async () => { pageIndex.value = 1; await loadData() }
const resetQuery = async () => { query.keyword = ''; pageSize.value = 10; pageIndex.value = 1; await loadData(); notify.info('已重置商品筛选条件') }
const handlePageSizeChange = async () => { pageIndex.value = 1; await loadData() }
const goPrevPage = async () => { if (pageIndex.value <= 1) return; pageIndex.value -= 1; await loadData() }
const goNextPage = async () => { if (pageIndex.value >= totalPages.value) return; pageIndex.value += 1; await loadData() }

const openCreateDialog = () => { editingId.value = null; resetForm(); dialogVisible.value = true }
const openEditDialog = (item: ProductListItemDto) => {
  editingId.value = item.id
  Object.assign(form, { name: item.name, erpProductCode: item.erpProductCode, mainImageAssetId: item.mainImageAssetId, detailImageAssetIds: item.detailImageAssetIds || [], salePrice: item.salePrice, isEnabled: item.isEnabled })
  selectedMainAsset.value = item.mainImageAssetId ? { id: item.mainImageAssetId, name: item.name, fileUrl: item.mainImageUrl || '', mediaType: 'image', bucketType: 'product', tags: [], sort: 0, isEnabled: true, createdAt: item.createdAt } : null
  selectedDetailAssets.value = (item.detailImageAssetIds || []).map((id, index) => ({ id, name: `详情图 ${index + 1}`, fileUrl: item.detailImageUrls[index] || '', mediaType: 'image', bucketType: 'product', tags: [], sort: index, isEnabled: true, createdAt: item.createdAt }))
  dialogVisible.value = true
}
const closeDialog = () => { dialogVisible.value = false; editingId.value = null; resetForm() }

const openMediaDialog = async (mode: 'main' | 'detail') => { mediaDialogMode.value = mode; mediaDialogVisible.value = true; await loadMediaOptions() }
const closeMediaDialog = () => { mediaDialogVisible.value = false }
const resetMediaQuery = async () => { mediaQuery.keyword = ''; await loadMediaOptions() }

const selectMediaAsset = (asset: MediaAssetListItemDto) => {
  if (mediaDialogMode.value === 'main') {
    selectedMainAsset.value = asset
    form.mainImageAssetId = asset.id
  } else if (!selectedDetailAssets.value.some((item) => item.id === asset.id)) {
    selectedDetailAssets.value = [...selectedDetailAssets.value, asset]
    form.detailImageAssetIds = selectedDetailAssets.value.map((item) => item.id)
  }
}

const clearMainAsset = () => { selectedMainAsset.value = null; form.mainImageAssetId = undefined }
const removeDetailAsset = (assetId: number) => { selectedDetailAssets.value = selectedDetailAssets.value.filter((item) => item.id !== assetId); form.detailImageAssetIds = selectedDetailAssets.value.map((item) => item.id) }

const saveUploadedAsset = async (file: File, bucketType = 'product') => {
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
    fileUrl: uploadResponse.data.fileUrl,
    mediaType: 'image',
    bucketType,
    tags: [],
    sort: 0,
    isEnabled: true,
    createdAt: new Date().toISOString(),
  } satisfies MediaAssetListItemDto
}

const handleMainUpload = async (event: Event) => {
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

const handleDetailUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const files = Array.from(target.files || [])
  if (files.length === 0) return
  try {
    const assets: MediaAssetListItemDto[] = []
    for (const file of files) {
      assets.push(await saveUploadedAsset(file))
    }
    selectedDetailAssets.value = [...selectedDetailAssets.value, ...assets.filter((asset) => !selectedDetailAssets.value.some((item) => item.id === asset.id))]
    form.detailImageAssetIds = selectedDetailAssets.value.map((item) => item.id)
    notify.success('商品详情图上传成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '上传商品详情图失败'))
  } finally {
    target.value = ''
  }
}

const submit = async () => {
  if (!form.name.trim()) return notify.info('请输入商品名称')
  if (!form.erpProductCode.trim()) return notify.info('请输入 ERP 商品编码')
  if (!form.mainImageAssetId) return notify.info('商品主图为必选项')
  if (submitting.value) return
  submitting.value = true
  try {
    const payload: SaveProductRequest = { ...form, name: form.name.trim(), erpProductCode: form.erpProductCode.trim(), detailImageAssetIds: selectedDetailAssets.value.map((item) => item.id) }
    if (editingId.value) { await updateProduct(editingId.value, payload); notify.success('商品已更新') }
    else { await createProduct(payload); pageIndex.value = 1; notify.success('商品已创建') }
    closeDialog(); await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '保存商品失败' : '新增商品失败'))
  } finally {
    submitting.value = false
  }
}

const removeItem = async (item: ProductListItemDto) => {
  if (!window.confirm(`确认删除商品“${item.name}”吗？`)) return
  if (items.value.length === 1 && pageIndex.value > 1) pageIndex.value -= 1
  if (deleting.value) return
  deleting.value = true
  try { await deleteProduct(item.id); await loadData(); notify.success('商品已删除') } catch (error) { notify.error(getErrorMessage(error, '删除商品失败')) } finally { deleting.value = false }
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')
const formatPrice = (value?: number) => (value !== undefined ? `¥${value.toFixed(2)}` : '-')

onMounted(loadData)
</script>

<style scoped>
.product-hero { background: radial-gradient(circle at top right, rgba(59,130,246,.14), transparent 28%), linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%); }
.hero-side-stack { align-content: stretch; }
.quick-card-spotlight { min-height: 148px; background: linear-gradient(135deg, rgba(59,130,246,.08), rgba(16,185,129,.03)); border: 1px solid rgba(59,130,246,.14); }
.product-guide-grid { display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 16px; }
.product-filter-grid,.media-filter-grid { grid-template-columns: 1.4fr .8fr 1fr; }
.product-thumb-cell { display: flex; align-items: center; }
.product-thumb { width: 56px; height: 56px; object-fit: cover; border-radius: 12px; border: 1px solid var(--line); background: #fff; }
.product-dialog-card { width: min(980px, calc(100vw - 48px)); }
.product-form-grid { grid-template-columns: repeat(2, minmax(0,1fr)); }
.dialog-form input,.dialog-form select { width: 100%; min-height: 44px; padding: 10px 14px; border: 1px solid var(--line-strong); border-radius: 12px; background: #fff; }
.selected-main-preview { display: grid; grid-template-columns: 120px minmax(0,1fr) auto; gap: 14px; align-items: center; }
.selected-main-image,.selected-detail-image,.media-card-image { width: 120px; height: 120px; object-fit: cover; border-radius: 16px; border: 1px solid var(--line); background: #fff; }
.selected-detail-list { display: grid; grid-template-columns: repeat(2, minmax(0,1fr)); gap: 12px; }
.selected-detail-card { display: grid; grid-template-columns: 120px minmax(0,1fr) auto; gap: 12px; align-items: center; padding: 12px; border-radius: 16px; border: 1px solid var(--line); background: #fff; }
.media-selector-dialog { width: min(1080px, calc(100vw - 48px)); }
.selector-actions { justify-content: flex-end; }
@media (max-width:1100px){ .hero-side-grid,.product-guide-grid,.product-filter-grid,.media-filter-grid,.product-form-grid,.selected-detail-list,.media-grid{ grid-template-columns: repeat(2, minmax(0,1fr)); } .selected-main-preview,.selected-detail-card{ grid-template-columns: 1fr; } }
@media (max-width:820px){ .hero-side-grid,.product-guide-grid,.product-filter-grid,.media-filter-grid,.product-form-grid,.selected-detail-list,.media-grid{ grid-template-columns: 1fr; } }
</style>
