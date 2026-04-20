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
        <p class="brand-meta">围绕发放配置、订单履约、核销处理与权限治理的统一运营工作台。</p>
        <div class="brand-tags">
          <span class="brand-tag">混合首页</span>
          <span class="brand-tag">高频操作优先</span>
          <span class="brand-tag">只改 UI</span>
        </div>
      </div>

      <div v-for="group in visibleNavGroups" :key="group.label" class="sidebar-group">
        <div class="sidebar-group-head">
          <div class="sidebar-group-label">{{ group.label }}</div>
          <span class="sidebar-group-count">{{ group.items.length }}</span>
        </div>
        <nav class="sidebar-nav">
          <RouterLink v-for="item in group.items" :key="item.to" :to="item.to" class="sidebar-link">
            <span class="nav-icon" :class="`nav-icon-${item.icon}`">{{ item.iconText }}</span>
            <span class="sidebar-link-copy">
              <span class="sidebar-link-text">{{ item.label }}</span>
              <span v-if="item.description" class="sidebar-link-subtext">{{ item.description }}</span>
            </span>
          </RouterLink>
        </nav>
      </div>

      <div class="sidebar-footer">
        <div class="sidebar-footer-card">
          <span class="sidebar-footer-label">当前设计基线</span>
          <strong>入口优先，说明下沉</strong>
          <p>导航、首页和业务页统一转向后台工作台结构，保留原路由与权限落点。</p>
        </div>
      </div>
    </aside>

    <main class="main-panel">
      <header class="topbar">
        <div class="topbar-left">
          <div class="topbar-title">
            <span class="topbar-kicker">{{ activeGroupLabel }}</span>
            <h1>{{ pageTitle }}</h1>
            <p>{{ pageSubtitle }}</p>
          </div>
        </div>
        <div class="topbar-actions">
          <div class="user-chip user-chip-secondary">当前模块：{{ pageTitle }}</div>
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
  { label: '工作台', items: [{ to: '/', label: '仪表盘', description: '混合首页与工作入口', icon: 'dashboard', iconText: 'BI' }] },
  {
    label: '运营配置',
    items: [
      { to: '/coupon-templates', label: '券模板管理', description: '规则模板与素材配置', icon: 'templates', iconText: 'CT' },
      { to: '/coupon-packs', label: '券包管理', description: '售卖券包与组合配置', icon: 'packs', iconText: 'CP' },
      { to: '/coupon-pack-items', label: '券包明细', description: '券包构成与数量维护', icon: 'pack-items', iconText: 'CI' },
      { to: '/banners', label: '轮播图管理', description: '首页投放入口配置', icon: 'banners', iconText: 'BN' },
      { to: '/stores', label: '门店管理', description: '核销与可用范围设置', icon: 'stores', iconText: 'ST' },
      { to: '/products', label: '商品管理', description: 'ERP 映射与商品基础档案', icon: 'products', iconText: 'PD' },
    ],
  },
  {
    label: '交易履约',
    items: [
      { to: '/coupon-orders', label: '订单管理', description: '订单、支付与发券闭环', icon: 'orders', iconText: 'OR' },
      { to: '/user-coupons', label: '用户券', description: '发券结果与券码状态', icon: 'user-coupons', iconText: 'UC' },
      { to: '/writeoff', label: '核销中心', description: '门店核销与结果确认', icon: 'writeoff', iconText: 'WO' },
    ],
  },
  {
    label: '用户增长',
    items: [
      { to: '/users', label: '用户管理', description: '用户建档与识别基础', icon: 'users', iconText: 'US' },
      { to: '/share-tracking', label: '分享追踪', description: '分享与打开转化数据', icon: 'share', iconText: 'SH' },
    ],
  },
  {
    label: '系统管理',
    items: [
      { to: '/miniapp-settings', label: '小程序主题', description: '前台主题切换与外观', icon: 'miniapp', iconText: 'MP' },
      { to: '/admin-users', label: '权限管理', description: '后台账号与角色分配', icon: 'admins', iconText: 'AU' },
      { to: '/admin-roles', label: '角色管理', description: '角色授权与菜单绑定', icon: 'roles', iconText: 'AR' },
      { to: '/admin-menus', label: '菜单管理', description: '菜单树与组件映射', icon: 'menus', iconText: 'AM' },
    ],
  },
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
const activeGroupLabel = computed(() => visibleNavGroups.value.find((group) => group.items.some((item) => item.to === route.path || (route.path === '/' && item.to === '/')))?.label ?? '后台工作台')
const pageTitle = computed(() => String(route.meta.title ?? '发卷后台'))
const pageSubtitle = computed(() => String(route.meta.subtitle ?? '业务后台管理'))
const logout = async () => { authStorage.clearAll(); await router.push('/login') }
</script>
