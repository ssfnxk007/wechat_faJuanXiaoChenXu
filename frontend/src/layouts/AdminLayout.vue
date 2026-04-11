<template>
  <div class="admin-layout">
    <aside class="sidebar">
      <div class="brand-block">
        <div class="brand-head">
          <div class="brand-badge">FJ</div>
          <div class="brand-copy">
            <p class="brand-subtitle">FaJuan Admin</p>
            <h1 class="brand">发卷后台</h1>
          </div>
        </div>
        <p class="brand-meta">券模板、券包、订单、核销与权限一体化管理</p>
      </div>

      <div v-for="group in visibleNavGroups" :key="group.label" class="sidebar-group">
        <div class="sidebar-group-label">{{ group.label }}</div>
        <nav class="sidebar-nav">
          <RouterLink v-for="item in group.items" :key="item.to" :to="item.to" class="sidebar-link">
            <span class="nav-icon" :class="`nav-icon-${item.icon}`">{{ item.iconText }}</span>
            <span class="sidebar-link-text">{{ item.label }}</span>
          </RouterLink>
        </nav>
      </div>

      <div class="sidebar-footer">
        <div class="sidebar-footer-card">
          <span class="sidebar-footer-label">运营工作台</span>
          <strong>统一发券管理入口</strong>
          <p>覆盖模板配置、订单跟踪、核销处理与权限协同。</p>
        </div>
      </div>
    </aside>

    <main class="main-panel">
      <header class="topbar">
        <div class="topbar-left">
          <div class="topbar-title">
            <h1>{{ pageTitle }}</h1>
            <p>{{ pageSubtitle }}</p>
          </div>
        </div>
        <div class="topbar-actions">
          <div class="user-chip">当前账号：{{ displayName || username || '未登录' }}</div>
          <div class="topbar-avatar">{{ userInitial }}</div>
          <button type="button" class="ghost-button" @click="logout">退出登录</button>
        </div>
      </header>

      <section class="page-body">
        <div class="page-shell">
          <RouterView />
        </div>
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
  { label: '总览', items: [{ to: '/', label: '仪表盘', icon: 'dashboard', iconText: 'BI' }] },
  { label: '基础资料', items: [{ to: '/users', label: '用户管理', icon: 'users', iconText: 'US' }, { to: '/stores', label: '门店管理', icon: 'stores', iconText: 'ST' }, { to: '/products', label: '商品管理', icon: 'products', iconText: 'PD' }] },
  { label: '发券运营', items: [{ to: '/coupon-templates', label: '券模板管理', icon: 'templates', iconText: 'CT' }, { to: '/coupon-packs', label: '券包管理', icon: 'packs', iconText: 'CP' }, { to: '/coupon-pack-items', label: '券包明细', icon: 'pack-items', iconText: 'CI' }, { to: '/user-coupons', label: '用户券', icon: 'user-coupons', iconText: 'UC' }] },
  { label: '交易与核销', items: [{ to: '/coupon-orders', label: '订单管理', icon: 'orders', iconText: 'OR' }, { to: '/writeoff', label: '核销中心', icon: 'writeoff', iconText: 'WO' }] },
  { label: '系统权限', items: [{ to: '/miniapp-settings', label: '小程序主题', icon: 'miniapp', iconText: 'MP' }, { to: '/admin-users', label: '权限管理', icon: 'admins', iconText: 'AU' }, { to: '/admin-roles', label: '角色管理', icon: 'roles', iconText: 'AR' }, { to: '/admin-menus', label: '菜单管理', icon: 'menus', iconText: 'AM' }] },
]

const username = computed(() => authStorage.getUsername())
const displayName = computed(() => authStorage.getDisplayName())
const userInitial = computed(() => (displayName.value || username.value || 'A').slice(0, 1).toUpperCase())
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
