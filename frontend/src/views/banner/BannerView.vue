<template>
  <div class="admin-page banner-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="banner-keyword">搜索标题</label>
          <input
            id="banner-keyword"
            v-model.trim="query.keyword"
            type="text"
            placeholder="请输入轮播图标题"
            @keyup.enter="handleSearch"
          />
        </div>
        <div class="field">
          <label for="banner-page-size">每页条数</label>
          <select id="banner-page-size" v-model.number="pageSize" @change="handlePageSizeChange">
            <option :value="10">10 条</option>
            <option :value="20">20 条</option>
            <option :value="50">50 条</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="primary-button compact" @click="handleSearch">查询</button>
        <button type="button" class="ghost-button compact" @click="resetQuery">重置</button>
        <button
          type="button"
          class="primary-button compact"
          :disabled="!canCreate"
          :title="canCreate ? '' : '当前账号缺少 banner.create 权限'"
          @click="openCreateDialog"
        >+ 新增轮播图</button>
        <button type="button" class="ghost-button compact" @click="loadData">刷新</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>轮播图列表</h3>
          <span class="count-pill">共 {{ totalCount }} 条</span>
        </div>
        <div class="data-card-meta">{{ querySummary }}</div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 120px;">图片</th>
              <th style="min-width: 200px;">标题</th>
              <th style="min-width: 110px;">跳转类型</th>
              <th style="min-width: 240px;">目标链接</th>
              <th style="min-width: 64px;">排序</th>
              <th style="min-width: 84px;">状态</th>
              <th style="min-width: 156px;">创建时间</th>
              <th style="min-width: 110px;">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td>{{ item.id }}</td>
              <td>
                <div class="banner-thumb" :style="item.imageUrl ? { backgroundImage: `url(${normalizeFileUrl(item.imageUrl)})` } : {}"></div>
              </td>
              <td>
                <div class="cell-stack">
                  <strong>{{ item.title }}</strong>
                  <span class="muted-line">素材 ID：{{ item.imageAssetId }}</span>
                </div>
              </td>
              <td>{{ describeLink(item.linkUrl).typeLabel }}</td>
              <td class="link-cell cell-mono">{{ item.linkUrl || '-' }}</td>
              <td>{{ item.sort }}</td>
              <td>
                <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <button
                  type="button"
                  class="cell-link"
                  :disabled="!canEdit"
                  :title="canEdit ? '' : '当前账号缺少 banner.edit 权限'"
                  @click="openEditDialog(item)"
                >编辑</button>
                <button
                  type="button"
                  class="cell-link danger"
                  :disabled="!canDelete"
                  :title="canDelete ? '' : '当前账号缺少 banner.delete 权限'"
                  @click="removeItem(item)"
                >删除</button>
              </td>
            </tr>
            <tr v-if="items.length === 0" class="empty-row">
              <td colspan="9">暂无轮播图数据</td>
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
      :title="editingId ? '编辑轮播图' : '新增轮播图'"
      sub="跳转目标通过类型和对象选择自动生成，不需要手动填写链接"
      size="xl"
      @close="closeDialog"
    >
      <div class="dialog-form-grid">
        <label class="dialog-field field-span-2">
          <span>轮播图标题</span>
          <input v-model.trim="form.title" type="text" placeholder="请输入轮播图标题" />
        </label>
        <label class="dialog-field">
          <span>排序值（越大越靠前）</span>
          <input v-model.number="form.sort" type="number" min="0" placeholder="0" />
        </label>
        <label class="dialog-field checkbox-row">
          <input v-model="form.isEnabled" type="checkbox" />
          <span>启用轮播图</span>
        </label>

        <label class="dialog-field field-span-2">
          <span>跳转类型</span>
          <select v-model="linkForm.type" @change="handleLinkTypeChange">
            <option value="coupon">券模板详情</option>
            <option value="pack">券包详情</option>
            <option value="product">商品详情</option>
            <option value="activity">活动页</option>
          </select>
        </label>

        <label v-if="linkForm.type === 'activity'" class="dialog-field field-span-2">
          <span>活动页</span>
          <select v-model="linkForm.activityKey">
            <option v-for="option in activityOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
          </select>
        </label>

        <label v-else class="dialog-field field-span-2">
          <span>跳转对象</span>
          <RemoteSelectField
            v-model="linkForm.targetId"
            v-model:keyword="targetQuery.keyword"
            :placeholder="targetPlaceholder"
            :empty-label="targetEmptyLabel"
            :options="targetOptions"
            @search="loadTargetOptions"
          />
        </label>

        <div class="detail-cell field-span-2">
          <span class="detail-label">生成后的跳转地址</span>
          <div class="cell-mono">{{ generatedLinkUrl || '请先选择跳转对象' }}</div>
          <div class="muted-line">{{ generatedLinkDescription }}</div>
        </div>
      </div>

      <section class="detail-section">
        <header class="detail-section-head">
          <h4>轮播图片</h4>
          <span class="detail-section-tip">推荐透明底 PNG（只保留主体元素），banner 背景色由小程序主题决定</span>
        </header>
        <div class="asset-picker-body">
          <div class="asset-picker-toolbar">
            <button type="button" class="primary-button compact" @click="triggerUpload">上传图片</button>
            <button type="button" class="ghost-button compact" @click="openMediaDialog">选择素材</button>
            <button v-if="selectedAsset" type="button" class="ghost-button compact" @click="clearSelectedAsset">清空</button>
          </div>
          <input ref="fileInputRef" type="file" accept="image/jpeg,image/png,image/gif,image/webp" class="hidden-file-input" @change="handleFileChange" />

          <div v-if="selectedAsset" class="selected-asset-card">
            <img :src="normalizeFileUrl(selectedAsset.fileUrl)" :alt="selectedAsset.name" class="selected-asset-image" />
            <div class="selected-asset-meta">
              <strong>{{ selectedAsset.name }}</strong>
              <span class="muted-line">素材 ID：{{ selectedAsset.id }}</span>
              <span class="muted-line">素材分区：{{ selectedAsset.bucketType }}</span>
            </div>
          </div>
          <div v-else class="detail-empty">请上传图片或选择已有素材，推荐透明底 PNG</div>
        </div>
      </section>

      <template #footer>
        <button type="button" class="ghost-button compact" @click="closeDialog">取消</button>
        <button type="button" class="primary-button compact" :disabled="submitting" @click="submit">
          {{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}
        </button>
      </template>
    </MainDetailDialog>

    <MainDetailDialog
      v-if="mediaDialogVisible"
      title="选择轮播素材"
      sub="仅可选择 banner / shared 分区素材"
      size="xl"
      @close="closeMediaDialog"
    >
      <div class="media-search-row">
        <input v-model.trim="mediaQuery.keyword" type="text" placeholder="请输入素材名称" @keyup.enter="loadMediaOptions" />
        <button type="button" class="primary-button compact" @click="loadMediaOptions">搜索</button>
      </div>

      <div class="media-grid">
        <button v-for="asset in mediaOptions" :key="asset.id" type="button" class="media-card" @click="selectMediaAsset(asset)">
          <img :src="normalizeFileUrl(asset.fileUrl)" :alt="asset.name" class="media-card-image" />
          <strong>{{ asset.name }}</strong>
          <span class="muted-line">{{ asset.bucketType }} / ID {{ asset.id }}</span>
        </button>
        <div v-if="mediaOptions.length === 0" class="detail-empty media-empty">暂无可选素材</div>
      </div>

      <template #footer>
        <button type="button" class="ghost-button compact" @click="closeMediaDialog">关闭</button>
      </template>
    </MainDetailDialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import RemoteSelectField, { type RemoteSelectOption } from '@/components/RemoteSelectField.vue'
import MainDetailDialog from '@/components/MainDetailDialog.vue'
import { getBannerList, createBanner, updateBanner, deleteBanner } from '@/api/banner'
import { createMediaAsset, getMediaAssetList, uploadMediaAssetFile } from '@/api/media-asset'
import { getCouponTemplateList } from '@/api/coupon-template'
import { getCouponPackList } from '@/api/coupon-pack'
import { getProductList } from '@/api/product'
import type { BannerListItemDto, SaveBannerRequest } from '@/types/banner'
import type { CouponTemplateListItemDto } from '@/types/coupon'
import type { CouponPackListItemDto } from '@/types/coupon-pack'
import type { MediaAssetListItemDto } from '@/types/media-asset'
import type { ProductListItemDto } from '@/types/product'
import { authStorage } from '@/utils/auth'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'
import { normalizeAssetUrl } from '@/utils/asset-url'

type LinkType = 'coupon' | 'pack' | 'product' | 'activity'

interface BannerFormState {
  title: string
  imageAssetId: number
  sort: number
  isEnabled: boolean
}

const activityOptions = [
  { value: '/pages/activity/detail?key=newcomer', label: '新人有礼活动页' },
  { value: '/pages/activity/detail?key=free', label: '免费领券活动页' },
  { value: '/pages/activity/detail?key=writeoff', label: '到店核销说明页' },
  { value: '/pages/mall/index', label: '商城首页' },
  { value: '/pages/index/index', label: '首页' },
]

const items = ref<BannerListItemDto[]>([])
const totalCount = ref(0)
const pageIndex = ref(1)
const totalPages = ref(1)
const pageSize = ref(10)
const dialogVisible = ref(false)
const mediaDialogVisible = ref(false)
const editingId = ref<number | null>(null)
const submitting = ref(false)
const selectedAsset = ref<MediaAssetListItemDto | null>(null)
const mediaOptions = ref<MediaAssetListItemDto[]>([])
const targetOptions = ref<RemoteSelectOption[]>([])
const fileInputRef = ref<HTMLInputElement | null>(null)

const query = reactive({ keyword: '' })
const mediaQuery = reactive({ keyword: '' })
const targetQuery = reactive({ keyword: '' })
const couponTemplateCache = ref<CouponTemplateListItemDto[]>([])
const couponPackCache = ref<CouponPackListItemDto[]>([])
const productCache = ref<ProductListItemDto[]>([])

const form = reactive<BannerFormState>({
  title: '',
  imageAssetId: 0,
  sort: 0,
  isEnabled: true,
})

const linkForm = reactive({
  type: 'coupon' as LinkType,
  targetId: 0,
  activityKey: '/pages/activity/detail?key=newcomer',
})

const canCreate = authStorage.hasPermission('banner.create')
const canEdit = authStorage.hasPermission('banner.edit')
const canDelete = authStorage.hasPermission('banner.delete')

const querySummary = computed(() => `${query.keyword || '全部标题'} · 每页 ${pageSize.value} 条`)
const targetPlaceholder = computed(() => {
  if (linkForm.type === 'coupon') return '搜索券模板名称'
  if (linkForm.type === 'pack') return '搜索券包名称'
  return '搜索商品名称'
})
const targetEmptyLabel = computed(() => {
  if (linkForm.type === 'coupon') return '请选择券模板'
  if (linkForm.type === 'pack') return '请选择券包'
  return '请选择商品'
})

const generatedLinkUrl = computed(() => buildLinkUrl())
const generatedLinkDescription = computed(() => {
  if (linkForm.type === 'coupon') return '将跳转到券详情页'
  if (linkForm.type === 'pack') return '将跳转到券包详情页'
  if (linkForm.type === 'product') return '将跳转到商品详情页'
  return '将跳转到预设活动页'
})

function buildLinkUrl() {
  if (linkForm.type === 'activity') {
    return linkForm.activityKey || ''
  }
  if (!linkForm.targetId) {
    return ''
  }
  if (linkForm.type === 'coupon') {
    return `/pages/coupon/detail?templateId=${linkForm.targetId}`
  }
  if (linkForm.type === 'pack') {
    return `/pages/coupon-pack/detail?id=${linkForm.targetId}`
  }
  return `/pages/product/detail?id=${linkForm.targetId}`
}

function describeLink(linkUrl?: string | null) {
  const value = String(linkUrl || '').trim()
  if (!value) return { type: 'unknown', typeLabel: '未配置' }
  if (value.startsWith('/pages/coupon/detail')) return { type: 'coupon', typeLabel: '券模板详情' }
  if (value.startsWith('/pages/coupon-pack/detail')) return { type: 'pack', typeLabel: '券包详情' }
  if (value.startsWith('/pages/product/detail?id=')) return { type: 'product', typeLabel: '商品详情' }
  if (value.startsWith('/pages/')) return { type: 'activity', typeLabel: '活动页' }
  return { type: 'unknown', typeLabel: '未识别' }
}

function parseLinkToForm(linkUrl?: string | null) {
  const value = String(linkUrl || '').trim()
  if (!value) {
    linkForm.type = 'coupon'
    linkForm.targetId = 0
    linkForm.activityKey = '/pages/activity/detail?key=newcomer'
    return
  }

  const couponMatch = value.match(/\/pages\/coupon\/detail\?templateId=(\d+)/i)
  if (couponMatch) {
    linkForm.type = 'coupon'
    linkForm.targetId = Number(couponMatch[1])
    return
  }

  const packMatch = value.match(/\/pages\/coupon-pack\/detail\?id=(\d+)/i)
  if (packMatch) {
    linkForm.type = 'pack'
    linkForm.targetId = Number(packMatch[1])
    return
  }

  const productMatch = value.match(/\/pages\/product\/detail\?id=(\d+)/i)
  if (productMatch) {
    linkForm.type = 'product'
    linkForm.targetId = Number(productMatch[1])
    return
  }

  linkForm.type = 'activity'
  linkForm.activityKey = value
  linkForm.targetId = 0
}

function resetForm() {
  form.title = ''
  form.imageAssetId = 0
  form.sort = 0
  form.isEnabled = true
  selectedAsset.value = null
  targetQuery.keyword = ''
  targetOptions.value = []
  parseLinkToForm('')
}

async function loadData() {
  try {
    const response = await getBannerList({ keyword: query.keyword || undefined, pageIndex: pageIndex.value, pageSize: pageSize.value })
    items.value = response.data.items
    totalCount.value = response.data.totalCount
    pageIndex.value = response.data.pageIndex
    totalPages.value = response.data.totalPages || 1
  } catch (error) {
    notify.error(getErrorMessage(error, '加载轮播图列表失败'))
  }
}

async function loadMediaOptions() {
  try {
    const [bannerRes, sharedRes] = await Promise.all([
      getMediaAssetList({ bucketType: 'banner', keyword: mediaQuery.keyword || undefined, pageIndex: 1, pageSize: 30 }),
      getMediaAssetList({ bucketType: 'shared', keyword: mediaQuery.keyword || undefined, pageIndex: 1, pageSize: 30 }),
    ])
    mediaOptions.value = [...bannerRes.data.items, ...sharedRes.data.items]
  } catch (error) {
    notify.error(getErrorMessage(error, '加载轮播素材失败'))
  }
}

async function loadTargetOptions() {
  try {
    if (linkForm.type === 'coupon') {
      const response = await getCouponTemplateList({ keyword: targetQuery.keyword || undefined, pageIndex: 1, pageSize: 50 })
      couponTemplateCache.value = response.data.items
      targetOptions.value = response.data.items.map((item) => ({ value: item.id, label: `${item.name} / ID ${item.id}` }))
      return
    }

    if (linkForm.type === 'pack') {
      const response = await getCouponPackList({ keyword: targetQuery.keyword || undefined, pageIndex: 1, pageSize: 50 })
      couponPackCache.value = response.data.items
      targetOptions.value = response.data.items.map((item) => ({ value: item.id, label: `${item.name} / ID ${item.id}` }))
      return
    }

    const response = await getProductList({ keyword: targetQuery.keyword || undefined, pageIndex: 1, pageSize: 50 })
    productCache.value = response.data.items
    targetOptions.value = response.data.items.map((item) => ({ value: item.id, label: `${item.name} / ${item.erpProductCode}` }))
  } catch (error) {
    notify.error(getErrorMessage(error, '加载跳转对象失败'))
  }
}

function handleLinkTypeChange() {
  linkForm.targetId = 0
  targetQuery.keyword = ''
  targetOptions.value = []
}

const normalizeFileUrl = normalizeAssetUrl

function triggerUpload() {
  fileInputRef.value?.click()
}

async function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  try {
    submitting.value = true
    const uploadResponse = await uploadMediaAssetFile(file)
    const createResponse = await createMediaAsset({
      name: file.name,
      fileUrl: uploadResponse.data.fileUrl,
      mediaType: 'image',
      bucketType: 'banner',
      tags: [],
      sort: 0,
      isEnabled: true,
    })

    selectedAsset.value = {
      id: createResponse.data,
      name: file.name,
      fileUrl: normalizeFileUrl(uploadResponse.data.fileUrl),
      mediaType: 'image',
      bucketType: 'banner',
      tags: [],
      sort: 0,
      isEnabled: true,
      createdAt: new Date().toISOString(),
    }
    form.imageAssetId = createResponse.data
    notify.success('轮播图图片上传成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '上传轮播图图片失败'))
  } finally {
    submitting.value = false
    target.value = ''
  }
}

function openMediaDialog() {
  mediaDialogVisible.value = true
  loadMediaOptions()
}

function closeMediaDialog() {
  mediaDialogVisible.value = false
}

function selectMediaAsset(asset: MediaAssetListItemDto) {
  selectedAsset.value = {
    ...asset,
    fileUrl: normalizeFileUrl(asset.fileUrl),
  }
  form.imageAssetId = asset.id
  mediaDialogVisible.value = false
}

function clearSelectedAsset() {
  selectedAsset.value = null
  form.imageAssetId = 0
}

function openCreateDialog() {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
  loadTargetOptions()
}

function openEditDialog(item: BannerListItemDto) {
  editingId.value = item.id
  form.title = item.title
  form.imageAssetId = item.imageAssetId
  form.sort = item.sort
  form.isEnabled = item.isEnabled
  selectedAsset.value = {
    id: item.imageAssetId,
    name: item.title,
    fileUrl: normalizeFileUrl(item.imageUrl),
    mediaType: 'image',
    bucketType: 'banner',
    tags: [],
    sort: 0,
    isEnabled: true,
    createdAt: item.createdAt,
  }
  parseLinkToForm(item.linkUrl)
  dialogVisible.value = true
  loadTargetOptions()
}

function closeDialog() {
  dialogVisible.value = false
  editingId.value = null
  resetForm()
}

async function submit() {
  const linkUrl = buildLinkUrl()
  if (!linkUrl) {
    notify.error('请选择轮播图跳转目标')
    return
  }

  try {
    submitting.value = true
    const payload: SaveBannerRequest = {
      title: form.title,
      imageAssetId: form.imageAssetId,
      linkUrl,
      sort: form.sort,
      isEnabled: form.isEnabled,
    }

    if (editingId.value) {
      const response = await updateBanner(editingId.value, payload)
      notify.success(response.message || '轮播图更新成功')
    } else {
      const response = await createBanner(payload)
      notify.success(response.message || '轮播图创建成功')
    }

    closeDialog()
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, editingId.value ? '更新轮播图失败' : '创建轮播图失败'))
  } finally {
    submitting.value = false
  }
}

async function removeItem(item: BannerListItemDto) {
  if (!window.confirm(`确认删除轮播图"${item.title}"吗？`)) {
    return
  }
  try {
    const response = await deleteBanner(item.id)
    notify.success(response.message || '轮播图删除成功')
    await loadData()
  } catch (error) {
    notify.error(getErrorMessage(error, '删除轮播图失败'))
  }
}

function formatDate(value?: string | null) {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')} ${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
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
  notify.info('已重置筛选条件')
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

const notifyColumnSettingsPlaceholder = () => {
  notify.info('列设置功能将在下一版本提供')
}

watch(() => linkForm.type, async () => {
  if (linkForm.type !== 'activity') {
    await loadTargetOptions()
  }
})

onMounted(loadData)
</script>

<style scoped>
.dialog-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
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

.banner-thumb {
  width: 96px;
  height: 56px;
  border-radius: 6px;
  background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
  background-size: cover;
  background-position: center;
}

.link-cell {
  max-width: 280px;
  word-break: break-all;
  font-size: 12px;
  color: #475467;
}

.asset-picker-body {
  padding: 12px 14px;
  display: grid;
  gap: 12px;
}

.asset-picker-toolbar {
  display: flex;
  gap: 8px;
}

.hidden-file-input {
  display: none;
}

.selected-asset-card {
  display: grid;
  grid-template-columns: 200px minmax(0, 1fr);
  gap: 14px;
  padding: 12px;
  background: #fafbfc;
  border: 1px solid var(--line);
  border-radius: 6px;
}

.selected-asset-image {
  width: 100%;
  height: 116px;
  object-fit: cover;
  border-radius: 6px;
  background: #e2e8f0;
}

.selected-asset-meta {
  display: grid;
  gap: 4px;
  font-size: 13px;
  color: var(--text);
  align-content: start;
}

.media-search-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px;
}

.media-search-row input {
  height: 32px;
  padding: 0 10px;
  border: 1px solid var(--line-strong);
  border-radius: 6px;
  background: #fff;
  font-size: 13px;
}

.media-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
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
  height: 116px;
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

@media (max-width: 960px) {
  .dialog-form-grid { grid-template-columns: 1fr; }
  .selected-asset-card { grid-template-columns: 1fr; }
}
</style>
