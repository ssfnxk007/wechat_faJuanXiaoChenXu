<template>
  <div class="admin-page user-page">
    <section class="search-card">
      <div class="search-grid">
        <div class="field">
          <label for="user-keyword">综合关键词</label>
          <input id="user-keyword" v-model.trim="filters.keyword" type="text" placeholder="OpenId / 昵称 / 手机号" />
        </div>
        <div class="field">
          <label for="user-nickname">昵称</label>
          <input id="user-nickname" v-model.trim="filters.nickname" type="text" placeholder="按昵称进一步过滤" />
        </div>
        <div class="field">
          <label for="user-mobile-status">手机号状态</label>
          <select id="user-mobile-status" v-model="filters.mobileStatus">
            <option value="all">全部状态</option>
            <option value="bound">仅已绑定</option>
            <option value="unbound">仅未绑定</option>
          </select>
        </div>
      </div>
      <div class="search-actions">
        <button type="button" class="ghost-button compact" @click="resetFilters">重置</button>
        <button type="button" class="ghost-button compact" @click="loadData">刷新</button>
        <button type="button" class="ghost-button compact" @click="notifyColumnSettingsPlaceholder">列设置</button>
      </div>
    </section>

    <section class="data-card">
      <header class="data-card-head">
        <div class="data-card-title">
          <h3>用户档案</h3>
          <span class="count-pill">命中 {{ filteredItems.length }} / 共 {{ items.length }} 条</span>
        </div>
        <div class="data-card-meta">
          已绑手机号 {{ boundMobileCount }} · 未绑 {{ items.length - boundMobileCount }} · 接入流程不在本页处理
        </div>
      </header>

      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th style="min-width: 56px;">ID</th>
              <th style="min-width: 280px;">用户信息</th>
              <th style="min-width: 160px;">手机号</th>
              <th style="min-width: 156px;">建档时间</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredItems" :key="item.id">
              <td>{{ item.id }}</td>
              <td>
                <div class="cell-stack">
                  <strong>{{ item.nickname || '未设置昵称' }}</strong>
                  <span class="muted-line cell-mono">{{ item.miniOpenId }}</span>
                </div>
              </td>
              <td>
                <span :class="['status-badge', item.mobile ? 'success' : 'warning']">
                  {{ item.mobile || '未绑定' }}
                </span>
              </td>
              <td>{{ formatDate(item.createdAt) }}</td>
            </tr>
            <tr v-if="filteredItems.length === 0" class="empty-row">
              <td colspan="4">当前没有符合条件的用户记录</td>
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

const notifyColumnSettingsPlaceholder = () => {
  notify.info('列设置功能将在下一版本提供')
}

const formatDate = (value?: string) => (value ? value.replace('T', ' ').slice(0, 19) : '-')

onMounted(loadData)
</script>
