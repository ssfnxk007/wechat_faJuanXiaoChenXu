<template>
  <div class="business-page page-v2 user-page-v2">
    <section class="hero-panel user-hero">
      <div class="hero-copy">
        <span class="page-kicker">用户识别</span>
        <h2>用户管理</h2>
        <p>把用户档案页收成查询与识别工作台：高频目标是定位用户、确认手机号覆盖率和支撑发券匹配，而不是暴露接入型表单入口。</p>
        <div class="hero-tags">
          <span class="badge info">OpenId 主标识</span>
          <span class="badge success">手机号辅助匹配</span>
          <span class="badge warning">结果查看优先</span>
        </div>
      </div>
      <div class="hero-side hero-side-grid">
        <article class="quick-card compact">
          <span class="quick-card-label">用户总数</span>
          <strong>{{ items.length }}</strong>
          <p>当前已建档的小程序用户数量</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">已绑手机号</span>
          <strong>{{ boundMobileCount }}</strong>
          <p>可用于运营导入与辅助匹配</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">当前命中</span>
          <strong>{{ filteredItems.length }}</strong>
          <p>符合筛选条件的用户数</p>
        </article>
        <article class="quick-card compact">
          <span class="quick-card-label">未绑手机号</span>
          <strong>{{ items.length - boundMobileCount }}</strong>
          <p>建议补齐辅助识别信息</p>
        </article>
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
        <span class="stat-footnote">仍需补齐辅助识别信息</span>
      </article>
    </section>

    <section class="card toolbar-card card-v2 operations-card">
      <div class="toolbar-row">
        <div class="toolbar-title">
          <span class="section-kicker">筛选与说明</span>
          <h3>用户检索工作台</h3>
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
        <div class="field-card summary-field">
          <span class="field-label">页面定位</span>
          <strong>查询结果页，不是建档入口</strong>
          <p>用户建档、手机号绑定属于系统接入链路；正式后台以查询、识别和发券匹配结果查看为主。</p>
        </div>
      </div>
    </section>

    <section class="user-content-grid">
      <article class="card card-v2 data-card">
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
      </article>

      <article class="card card-v2 data-card">
        <div class="section-head">
          <div class="section-head-main">
            <span class="section-kicker">识别要点</span>
            <h3>运营侧查看重点</h3>
            <p class="section-tip">把运营真正关心的判断标准固定下来，减少额外说明文字。</p>
          </div>
        </div>
        <div class="user-guides">
          <div class="guide-item">
            <strong>优先确认手机号覆盖率</strong>
            <p>手机号是导入识别和订单归属的辅助匹配依据，应优先关注未绑定用户占比。</p>
          </div>
          <div class="guide-item">
            <strong>昵称仅用于辅助识别</strong>
            <p>昵称可能变化，稳定识别仍以 OpenId 为主、手机号为辅。</p>
          </div>
          <div class="guide-item">
            <strong>接入动作不在本页处理</strong>
            <p>建档和绑定属于系统接入链路，本页聚焦查找结果和业务匹配，不增加运营表单负担。</p>
          </div>
        </div>
      </article>
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

.user-filter-grid {
  grid-template-columns: 1.25fr 1fr 0.8fr 1.1fr;
}

.user-content-grid {
  display: grid;
  gap: 18px;
  grid-template-columns: minmax(0, 1.55fr) minmax(320px, 1fr);
}

.user-guides {
  display: grid;
  gap: 12px;
}

@media (max-width: 1100px) {
  .hero-side-grid,
  .user-filter-grid,
  .user-content-grid {
    grid-template-columns: 1fr;
  }
}
</style>
