<template>
  <div class="business-page page-v2 banner-page">
    <section class="hero-panel banner-hero">
      <div class="hero-copy">
        <span class="page-kicker">首页运营</span>
        <h2>轮播图管理</h2>
        <p>轮播图跳转改为后台结构化配置，不再手填外链。可选择券、券包、商品详情或活动页，系统自动生成小程序内部链接。</p>
        <div class="hero-tags">
          <span class="badge info">图片上传</span>
          <span class="badge success">素材复用</span>
          <span class="badge warning">内部跳转</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">轮播总数</span>
          <strong>{{ totalCount }}</strong>
          <p>当前筛选范围内的轮播图记录数。</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">启用中</span>
          <strong>{{ enabledCount }}</strong>
          <p>会显示在小程序首页的轮播图。</p>
        </article>
      </div>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">业务操作台</span>
          <h3>筛选与维护</h3>
          <p class="section-tip">支持按标题搜索，并在弹窗中完成图片选择、上传和内部跳转目标配置。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetQuery">重置筛选</button>
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
          <button v-if="canCreate" type="button" class="primary-button" @click="openCreateDialog">新增轮播图</button>
        </div>
      </div>

      <div class="filter-panel-grid banner-filter-grid">
        <label class="field-card filter-field">
          <span class="field-label">搜索标题</span>
          <input v-model.trim="query.keyword" type="text" placeholder="请输入轮播图标题" @keyup.enter="handleSearch" />
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
          <p>轮播图只支持小程序内部业务跳转，不支持 H5 外链。</p>
        </div>
      </div>
    </section>

    <section class="card card-v2 data-card">
      <div class="section-head">
        <div class="section-head-main">
          <span class="section-kicker">列表概览</span>
          <h3>轮播图列表</h3>
          <p class="section-tip">系统会自动生成内部 linkUrl，便于首页轮播点击跳转。</p>
        </div>
      </div>

      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>图片</th>
              <th>标题</th>
              <th>跳转类型</th>
              <th>目标链接</th>
              <th>排序</th>
              <th>状态</th>
              <th>创建时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="banner-preview" :style="item.imageUrl ? { backgroundImage: `url(${item.imageUrl})` } : {}"></div>
              </td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ item.title }}</strong>
                  <span>素材 ID：{{ item.imageAssetId }}</span>
                </div>
              </td>
              <td>{{ describeLink(item.linkUrl).typeLabel }}</td>
              <td class="link-cell">{{ item.linkUrl || '-' }}</td>
              <td>{{ item.sort }}</td>
              <td>
                <span :class="['status-badge', item.isEnabled ? 'success' : 'danger']">{{ item.isEnabled ? '启用' : '停用' }}</span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
              <td>
                <div class="table-actions">
                  <button v-if="canEdit" type="button" class="action-button" @click="openEditDialog(item)">编辑</button>
                  <button v-if="canDelete" type="button" class="action-button danger" @click="removeItem(item)">删除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="9" class="empty-text">暂无轮播图数据</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pager pager-v2">
        <div class="pager-info">第 {{ pageIndex }} 页 / 共 {{ totalPages }} 页，共 {{ totalCount }} 条记录</div>
        <div class="pager-actions">
          <button type="button" class="ghost-button" :disabled="pageIndex <= 1" @click="goPrevPage">上一页</button>
          <button type="button" class="ghost-button" :disabled="pageIndex >= totalPages" @click="goNextPage">下一页</button>
        </div>
      </div>
    </section>

    <div v-if="dialogVisible" class="dialog-mask" @click.self="closeDialog">
      <div class="dialog-card dialog-card-v2 banner-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">轮播编辑</span>
            <h3>{{ editingId ? '编辑轮播图' : '新增轮播图' }}</h3>
            <p>跳转目标通过类型和对象选择自动生成，不需要手动填写链接。</p>
          </div>
          <button type="button" class="ghost-button" @click="closeDialog">关闭</button>
        </div>

        <div class="grid-form dialog-form">
          <label class="field-card field-span-2">
            <span class="field-label">轮播图标题</span>
            <input v-model.trim="form.title" type="text" placeholder="请输入轮播图标题" />
          </label>

          <label class="field-card">
            <span class="field-label">排序值</span>
            <input v-model.number="form.sort" type="number" min="0" placeholder="越大越靠前" />
          </label>

          <label class="field-card checkbox-field align-end">
            <input v-model="form.isEnabled" type="checkbox" />
            <span>启用轮播图</span>
          </label>

          <label class="field-card field-span-2">
            <span class="field-label">跳转类型</span>
            <select v-model="linkForm.type" @change="handleLinkTypeChange">
              <option value="coupon">券模板详情</option>
              <option value="pack">券包详情</option>
              <option value="product">商品详情</option>
              <option value="activity">活动页</option>
            </select>
          </label>

          <label v-if="linkForm.type === 'activity'" class="field-card field-span-2">
            <span class="field-label">活动页</span>
            <select v-model="linkForm.activityKey">
              <option v-for="option in activityOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
            </select>
          </label>

          <label v-else class="field-card field-span-2">
            <span class="field-label">跳转对象</span>
            <RemoteSelectField
              v-model="linkForm.targetId"
              v-model:keyword="targetQuery.keyword"
              :placeholder="targetPlaceholder"
              :empty-label="targetEmptyLabel"
              :options="targetOptions"
              @search="loadTargetOptions"
            />
          </label>

          <div class="field-card field-span-2 summary-field route-preview-card">
            <span class="field-label">生成后的跳转地址</span>
            <strong>{{ generatedLinkUrl || '请先选择跳转对象' }}</strong>
            <p>{{ generatedLinkDescription }}</p>
          </div>

          <div class="field-card field-span-2 asset-picker-card">
            <div class="asset-picker-head">
              <span class="field-label">轮播图片</span>
              <div class="toolbar-actions compact-actions">
                <button type="button" class="ghost-button" @click="triggerUpload">上传图片</button>
                <button type="button" class="ghost-button" @click="openMediaDialog">选择素材</button>
                <button v-if="selectedAsset" type="button" class="ghost-button" @click="clearSelectedAsset">清空</button>
              </div>
            </div>
            <p class="asset-picker-tip">推荐上传透明底 PNG（只保留主体元素，如钱包/礼盒/红包），banner 背景色由小程序主题决定。也支持 JPG / GIF / WebP，但带背景的图可能出现色差。</p>
            <input ref="fileInputRef" type="file" accept="image/jpeg,image/png,image/gif,image/webp" class="hidden-file-input" @change="handleFileChange" />

            <div v-if="selectedAsset" class="selected-asset-card">
              <img :src="selectedAsset.fileUrl" :alt="selectedAsset.name" class="selected-asset-image" />
              <div class="selected-asset-meta">
                <strong>{{ selectedAsset.name }}</strong>
                <span>素材 ID：{{ selectedAsset.id }}</span>
                <span>素材分区：{{ selectedAsset.bucketType }}</span>
              </div>
            </div>
            <div v-else class="empty-asset-state">请上传图片或选择已有素材，推荐透明底 PNG。</div>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="ghost-button" @click="closeDialog">取消</button>
          <button type="button" :disabled="submitting" @click="submit">{{ submitting ? '提交中...' : (editingId ? '保存修改' : '保存新增') }}</button>
        </div>
      </div>
    </div>

    <div v-if="mediaDialogVisible" class="dialog-mask" @click.self="closeMediaDialog">
      <div class="dialog-card dialog-card-v2 media-dialog">
        <div class="dialog-head">
          <div class="dialog-head-main">
            <span class="section-kicker">素材选择</span>
            <h3>选择轮播素材</h3>
            <p>仅可选择 `banner / shared` 分区素材。</p>
          </div>
          <button type="button" class="ghost-button" @click="closeMediaDialog">关闭</button>
        </div>

        <div class="filter-panel-grid banner-filter-grid compact-media-grid">
          <label class="field-card filter-field field-span-2">
            <span class="field-label">素材搜索</span>
            <input v-model.trim="mediaQuery.keyword" type="text" placeholder="请输入素材名称" @keyup.enter="loadMediaOptions" />
          </label>
          <div class="toolbar-actions selector-actions">
            <button type="button" class="ghost-button" @click="loadMediaOptions">搜索</button>
          </div>
        </div>

        <div class="media-grid">
          <button v-for="asset in mediaOptions" :key="asset.id" type="button" class="media-card" @click="selectMediaAsset(asset)">
            <img :src="asset.fileUrl" :alt="asset.name" class="media-card-image" />
            <strong>{{ asset.name }}</strong>
            <span>{{ asset.bucketType }} / ID {{ asset.id }}</span>
          </button>
          <div v-if="mediaOptions.length === 0" class="empty-text media-empty">暂无可选素材</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import RemoteSelectField, { type RemoteSelectOption } from '@/components/RemoteSelectField.vue'
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
import { authStorage } from '@/utils.auth'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

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

const enabledCount = computed(() => items.value.filter((item) => item.isEnabled).length)
const querySummary = computed(() => `${query.keyword || '全部标题'} / 每页 ${pageSize.value} 条`)
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

function normalizeFileUrl(value?: string | null) {
  if (!value) return ''
  if (/^https?:\/\//i.test(value)) return value
  const base = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5265/api').replace(/\/api\/?$/, '')
  return `${base}${value.startsWith('/') ? value : `/${value}`}`
}

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
  if (!window.confirm(`确认删除轮播图“${item.title}”吗？`)) {
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

watch(() => linkForm.type, async () => {
  if (linkForm.type !== 'activity') {
    await loadTargetOptions()
  }
})

onMounted(loadData)
</script>

<style scoped>
.banner-filter-grid {
  grid-template-columns: minmax(0, 1fr) 180px minmax(260px, 320px);
}

.banner-preview {
  width: 132px;
  height: 76px;
  border-radius: 16px;
  background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
  background-size: cover;
  background-position: center;
}

.link-cell {
  max-width: 280px;
  color: #64748b;
  word-break: break-all;
}

.banner-dialog,
.media-dialog {
  width: min(980px, calc(100vw - 36px));
}

.route-preview-card strong {
  color: #0f172a;
  word-break: break-all;
}

.asset-picker-card {
  display: grid;
  gap: 16px;
}

.asset-picker-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
}

.asset-picker-tip {
  margin: 0;
  padding: 10px 14px;
  font-size: 12px;
  line-height: 1.6;
  color: #b45309;
  background: #fffbeb;
  border: 1px solid #fde68a;
  border-radius: 12px;
}

.compact-actions,
.selector-actions {
  display: flex;
  gap: 10px;
}

.hidden-file-input {
  display: none;
}

.selected-asset-card {
  display: grid;
  grid-template-columns: 220px minmax(0, 1fr);
  gap: 16px;
  padding: 16px;
  background: #f8fafc;
  border: 1px solid rgba(148, 163, 184, 0.2);
  border-radius: 18px;
}

.selected-asset-image,
.media-card-image {
  width: 100%;
  height: 132px;
  object-fit: cover;
  border-radius: 14px;
  background: #e2e8f0;
}

.selected-asset-meta {
  display: grid;
  gap: 8px;
  color: #475569;
}

.empty-asset-state {
  padding: 24px;
  text-align: center;
  color: #64748b;
  border: 1px dashed rgba(148, 163, 184, 0.6);
  border-radius: 18px;
}

.compact-media-grid {
  grid-template-columns: minmax(0, 1fr) auto;
  margin-bottom: 18px;
}

.media-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 16px;
}

.media-card {
  display: grid;
  gap: 10px;
  padding: 12px;
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 18px;
  background: #fff;
  text-align: left;
}

.media-card strong {
  color: #0f172a;
}

.media-card span,
.media-empty {
  color: #64748b;
}

.align-end {
  justify-content: flex-end;
}

@media (max-width: 960px) {
  .banner-filter-grid,
  .compact-media-grid,
  .selected-asset-card {
    grid-template-columns: 1fr;
  }

  .asset-picker-head {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
