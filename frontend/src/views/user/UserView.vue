<template>
  <div class="business-page page-v2 user-page-v2">
    <section class="hero-panel user-hero">
      <div class="hero-copy">
        <span class="page-kicker">用户中心</span>
        <h2>用户管理</h2>
        <p>维护小程序用户建档结果与手机号辅助匹配信息，为发券、导入识别、订单归属和 ERP 核销提供基础数据。</p>
        <div class="hero-tags">
          <span class="badge info">OpenId 主标识</span>
          <span class="badge success">手机号辅助匹配</span>
          <span class="badge warning">支持运营检索</span>
        </div>
      </div>
      <div class="hero-side hero-side-stack">
        <article class="quick-card quick-card-spotlight">
          <span class="quick-card-label">用户总数</span>
          <strong>{{ items.length }}</strong>
          <p>当前已建档的小程序用户数量。</p>
        </article>
        <div class="hero-side-grid">
          <article class="quick-card compact">
            <span class="quick-card-label">已绑手机号</span>
            <strong>{{ boundMobileCount }}</strong>
            <p>可用于导入辅助匹配</p>
          </article>
          <article class="quick-card compact">
            <span class="quick-card-label">当前命中</span>
            <strong>{{ filteredItems.length }}</strong>
            <p>符合筛选条件的用户数</p>
          </article>
        </div>
      </div>
    </section>

    <section class="stats-grid stats-grid-v2">
      <article class="stat-card accent-blue">
        <span class="label">用户总数</span>
        <strong class="stat-value">{{ items.length }}</strong>
        <span class="stat-footnote">当前已建档的小程序用户数量</span>
      </article>
      <article class="stat-card accent-indigo">
        <span class="label">已绑手机号</span>
        <strong class="stat-value">{{ boundMobileCount }}</strong>
        <span class="stat-footnote">可用于运营导入辅助匹配</span>
      </article>
      <article class="stat-card accent-green">
        <span class="label">当前命中</span>
        <strong class="stat-value">{{ filteredItems.length }}</strong>
        <span class="stat-footnote">当前筛选条件命中数</span>
      </article>
      <article class="stat-card accent-amber">
        <span class="label">未绑手机号</span>
        <strong class="stat-value">{{ items.length - boundMobileCount }}</strong>
        <span class="stat-footnote">建议后续补齐辅助识别信息</span>
      </article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">业务筛选</span>
          <h3>用户检索与状态筛选</h3>
          <p class="section-tip">按 OpenId、昵称、手机号和绑定状态快速定位用户，不在主页面暴露开发式建档与绑定入口。</p>
        </div>
        <div class="toolbar-actions">
          <button type="button" class="ghost-button" @click="resetFilters">重置筛选</button>
          <button type="button" class="ghost-button" @click="loadData">刷新列表</button>
        </div>
      </div>

      <div class="filter-panel-grid user-filter-grid">
        <label class="field-card filter-field">
          <span class="field-label">综合关键词</span>
          <input v-model.trim="filters.keyword" type="text" placeholder="搜索 OpenId / 昵称 / 手机号" />
        </label>
        <label class="field-card filter-field">
          <span class="field-label">昵称</span>
          <input v-model.trim="filters.nickname" type="text" placeholder="按昵称进一步过滤" />
        </label>
        <label class="field-card filter-field compact-field">
          <span class="field-label">手机号状态</span>
          <select v-model="filters.mobileStatus">
            <option value="all">全部状态</option>
            <option value="bound">仅已绑定手机号</option>
            <option value="unbound">仅未绑定手机号</option>
          </select>
        </label>
      </div>

      <div class="surface-note">
        <strong>说明</strong>
        <p>用户建档、手机号绑定属于系统接入链路，不作为运营主页面的常用表单入口展示；正式后台以查询和查看结果为主。</p>
      </div>
    </section>

    <section class="card card-v2 data-card">
      <div class="section-head">
        <div class="section-head-main">
          <span class="section-kicker">用户档案</span>
          <h3>用户列表</h3>
          <p class="section-tip">展示 OpenId、昵称、手机号和建档时间，用于运营识别和发券匹配。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">当前展示 {{ filteredItems.length }} 条</span>
          <span class="badge warning">未绑手机号 {{ items.length - boundMobileCount }}</span>
        </div>
      </div>

      <div class="table-wrap table-wrap-v2">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>用户信息</th>
              <th>手机号</th>
              <th>建档时间</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredItems" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td>
                <div class="table-primary-cell">
                  <strong>{{ item.nickname || '未设置昵称' }}</strong>
                  <span class="cell-mono">{{ item.miniOpenId }}</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', item.mobile ? 'success' : 'warning']">
                  {{ item.mobile || '未绑定手机号' }}
                </span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
            </tr>
            <tr v-if="filteredItems.length === 0">
              <td colspan="4" class="empty-text">当前没有符合条件的用户记录</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { getUserList } from '@/api/user'
import type { UserListItemDto } from '@/types/user'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

const items = ref<UserListItemDto[]>([])
const filters = reactive({
  keyword: '',
  nickname: '',
  mobileStatus: 'all',
})

const filteredItems = computed(() => {
  return items.value.filter((item) => {
    const keyword = filters.keyword.trim().toLowerCase()
    const nickname = filters.nickname.trim().toLowerCase()
    const mobile = item.mobile ?? ''
    const itemNickname = item.nickname ?? ''

    const matchesKeyword =
      keyword.length === 0 ||
      item.miniOpenId.toLowerCase().includes(keyword) ||
      mobile.toLowerCase().includes(keyword) ||
      itemNickname.toLowerCase().includes(keyword)

    const matchesNickname = nickname.length === 0 || itemNickname.toLowerCase().includes(nickname)

    const matchesMobileStatus =
      filters.mobileStatus === 'all' ||
      (filters.mobileStatus === 'bound' && mobile.length > 0) ||
      (filters.mobileStatus === 'unbound' && mobile.length === 0)

    return matchesKeyword && matchesNickname && matchesMobileStatus
  })
})

const boundMobileCount = computed(() => items.value.filter((item) => !!item.mobile).length)

const loadData = async () => {
  try {
    const response = await getUserList()
    items.value = response.data.items
  } catch (error) {
    notify.error(getErrorMessage(error, '加载用户列表失败'))
  }
}

const resetFilters = () => {
  filters.keyword = ''
  filters.nickname = ''
  filters.mobileStatus = 'all'
  notify.info('已重置用户筛选条件')
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>

<style scoped>
.user-hero {
  background:
    radial-gradient(circle at top right, rgba(59, 130, 246, 0.14), transparent 28%),
    linear-gradient(135deg, #ffffff 0%, #f8fbff 52%, #f4f7fb 100%);
}

.hero-side-stack {
  align-content: stretch;
}

.hero-side-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.quick-card-spotlight {
  min-height: 148px;
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.08), rgba(16, 185, 129, 0.03));
  border: 1px solid rgba(59, 130, 246, 0.14);
}

.quick-card.compact {
  min-height: 112px;
}

.quick-card-label {
  display: inline-flex;
  width: fit-content;
  padding: 4px 10px;
  border-radius: 999px;
  background: rgba(37, 99, 235, 0.08);
  color: var(--primary);
  font-size: 12px;
  font-weight: 700;
}

.user-filter-grid {
  grid-template-columns: 1.4fr 1fr 0.8fr;
}

.field-card {
  display: grid;
  gap: 10px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(226, 232, 240, 0.96);
  background: linear-gradient(180deg, #fff 0%, #fbfdff 100%);
}

.field-label {
  font-size: 12px;
  font-weight: 700;
  color: #475467;
  letter-spacing: 0.04em;
}

.surface-note {
  display: grid;
  gap: 6px;
  padding: 14px 16px;
  border-radius: 16px;
  border: 1px solid rgba(226, 232, 240, 0.96);
  background: linear-gradient(180deg, #fff 0%, #f8fbff 100%);
}

.surface-note p,
.surface-note strong {
  margin: 0;
}

@media (max-width: 1100px) {
  .hero-side-grid,
  .user-filter-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 820px) {
  .hero-side-grid,
  .user-filter-grid {
    grid-template-columns: 1fr;
  }
}
</style>
