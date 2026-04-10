<template>
  <div class="business-page">
    <div class="page-header-row">
      <div>
        <h2>用户管理</h2>
        <p>查看小程序用户建档情况，并维护手机号绑定信息。</p>
      </div>
      <button type="button" @click="loadData">刷新列表</button>
    </div>

    <div class="card form-card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>用户建档</h3>
          <p class="section-tip">录入登录参数并完成用户建档，后续可切换到真实微信登录链路。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">OpenId 建档</span>
          <span class="badge warning">建档入口</span>
        </div>
      </div>

      <div class="grid-form">
        <input v-model="loginForm.code" type="text" placeholder="登录 code" />
        <input v-model="loginForm.nickname" type="text" placeholder="昵称" />
      </div>

      <button type="button" @click="submitLogin">登录建档</button>
      <p v-if="loginResult" class="helper-text">
        最近登录用户：<span class="cell-strong">{{ loginResult.userId }}</span>
        /
        <span class="cell-mono">{{ loginResult.miniOpenId }}</span>
      </p>
    </div>

    <div class="card form-card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>绑定手机号</h3>
          <p class="section-tip">手机号作为辅助匹配字段，便于 ERP 导入用户时做关联。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge success">辅助匹配</span>
        </div>
      </div>

      <div class="grid-form">
        <input v-model.number="bindForm.userId" type="number" placeholder="用户ID" />
        <input v-model="bindForm.mobile" type="text" placeholder="手机号" />
      </div>

      <button type="button" @click="submitBind">绑定手机号</button>
    </div>

    <div class="card form-card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>筛选工具</h3>
          <p class="section-tip">当前为前端本地筛选，不改后端接口参数。</p>
        </div>
        <div class="inline-metrics">
          <span class="badge info">总数 {{ items.length }}</span>
          <span class="badge success">命中 {{ filteredItems.length }}</span>
          <span class="badge warning">已绑手机号 {{ boundMobileCount }}</span>
        </div>
      </div>

      <div class="grid-form">
        <input v-model.trim="filters.keyword" type="text" placeholder="搜索 OpenId / 昵称 / 手机号" />
        <select v-model="filters.mobileStatus">
          <option value="all">全部手机号状态</option>
          <option value="bound">仅已绑定手机号</option>
          <option value="unbound">仅未绑定手机号</option>
        </select>
        <input v-model.trim="filters.nickname" type="text" placeholder="按昵称筛选" />
      </div>

      <div class="inline-actions">
        <button type="button" @click="resetFilters">重置筛选</button>
      </div>
    </div>

    <div class="card">
      <div class="section-head">
        <div class="section-head-main">
          <h3>用户列表</h3>
          <p class="section-tip">以小程序 OpenId 为主标识，手机号为辅助信息。当前展示 {{ filteredItems.length }} 条。</p>
        </div>
      </div>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>ID</th>
              <th>小程序 OpenId</th>
              <th>手机号</th>
              <th>昵称</th>
              <th>创建时间</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in filteredItems" :key="item.id">
              <td class="cell-strong">{{ item.id }}</td>
              <td class="cell-mono">{{ item.miniOpenId }}</td>
              <td>{{ item.mobile || '-' }}</td>
              <td>{{ item.nickname || '-' }}</td>
              <td>{{ item.createdAt }}</td>
            </tr>
            <tr v-if="filteredItems.length === 0">
              <td colspan="5" class="muted-text">暂无符合筛选条件的数据</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { bindMobile, getUserList, miniProgramLogin } from '@/api/user'
import type { AuthLoginResultDto, UserListItemDto } from '@/types/user'
import { getErrorMessage } from '@/utils/http-error'
import { notify } from '@/utils/notify'

const items = ref<UserListItemDto[]>([])
const loginResult = ref<AuthLoginResultDto | null>(null)
const loginForm = reactive({ code: '', nickname: '' })
const bindForm = reactive({ userId: 0, mobile: '' })
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

const submitLogin = async () => {
  try {
    const response = await miniProgramLogin({ ...loginForm })
    loginResult.value = response.data
    bindForm.userId = response.data.userId
    await loadData()
    notify.success('用户建档成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '用户建档失败'))
  }
}

const submitBind = async () => {
  try {
    await bindMobile({ ...bindForm })
    await loadData()
    notify.success('手机号绑定成功')
  } catch (error) {
    notify.error(getErrorMessage(error, '手机号绑定失败'))
  }
}

const resetFilters = () => {
  filters.keyword = ''
  filters.nickname = ''
  filters.mobileStatus = 'all'
  notify.info('已重置用户筛选条件')
}

onMounted(loadData)
</script>
