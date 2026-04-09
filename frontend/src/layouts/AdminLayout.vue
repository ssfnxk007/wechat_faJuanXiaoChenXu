<template>
  <div class="admin-layout">
    <aside class="sidebar">
      <div class="brand-block">
        <p class="brand-subtitle">FaJuan Admin</p>
        <h1 class="brand">发卷后台</h1>
        <p class="brand-meta">券模板、券包、订单、核销与权限一体化管理</p>
      </div>

      <div v-for="group in visibleNavGroups" :key="group.label" class="sidebar-group">
        <div class="sidebar-group-label">{{ group.label }}</div>
        <nav class="sidebar-nav">
          <RouterLink v-for="item in group.items" :key="item.to" :to="item.to" class="sidebar-link">
            <span class="nav-icon">{{ item.icon }}</span>
            <span>{{ item.label }}</span>
          </RouterLink>
        </nav>
      </div>
    </aside>

    <main class="main-panel">
      <header class="topbar">
        <div class="topbar-title">
          <h1>{{ pageTitle }}</h1>
          <p>{{ pageSubtitle }}</p>
        </div>
        <div class="topbar-actions">
          <div class="user-chip">当前账号：{{ displayName || username || '未登录' }}</div>
          <button type="button" class="ghost-button" @click="logout">退出登录</button>
        </div>
      </header>

      <section class="page-body">
        <RouterView />
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authStorage } from '@/utils.auth'

const router = useRouter()
const route = useRoute()

const navGroups = [
  { label: '总览', items: [{ to: '/', label: '仪表盘', icon: '📊' }] },
  { label: '基础资料', items: [{ to: '/users', label: '用户管理', icon: '👥' }, { to: '/stores', label: '门店管理', icon: '🏪' }, { to: '/products', label: '商品管理', icon: '📦' }] },
  { label: '发券运营', items: [{ to: '/coupon-templates', label: '券模板管理', icon: '🎟️' }, { to: '/coupon-packs', label: '券包管理', icon: '🧧' }, { to: '/coupon-pack-items', label: '券包明细', icon: '🧾' }, { to: '/user-coupons', label: '用户券', icon: '💳' }] },
  { label: '交易与核销', items: [{ to: '/coupon-orders', label: '订单管理', icon: '🧮' }, { to: '/writeoff', label: '核销中心', icon: '✅' }] },
  { label: '系统权限', items: [{ to: '/admin-users', label: '权限管理', icon: '🔐' }, { to: '/admin-roles', label: '角色管理', icon: '🛡️' }, { to: '/admin-menus', label: '菜单管理', icon: '📚' }] },
]

const username = computed(() => authStorage.getUsername())
const displayName = computed(() => authStorage.getDisplayName())
const menuPaths = computed(() => authStorage.getMenuPaths())
const visibleNavGroups = computed(() => navGroups
  .map((group) => ({
    ...group,
    items: group.items.filter((item) => item.to === '/' || menuPaths.value.length === 0 || menuPaths.value.includes(item.to)),
  }))
  .filter((group) => group.items.length > 0))
const pageTitle = computed(() => String(route.meta.title ?? '发卷后台'))
const pageSubtitle = computed(() => String(route.meta.subtitle ?? '业务后台管理'))
const logout = async () => { authStorage.clearAll(); await router.push('/login') }
</script>
