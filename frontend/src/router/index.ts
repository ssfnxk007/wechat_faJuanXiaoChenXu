import { createRouter, createWebHistory } from 'vue-router'
import { getAdminProfile } from '@/api/admin-auth'
import AdminLayout from '@/layouts/AdminLayout.vue'
import { authStorage } from '@/utils/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('@/views/login/LoginView.vue'), meta: { title: '后台登录', subtitle: '登录后进入发券运营管理系统' } },
    {
      path: '/',
      component: AdminLayout,
      children: [
        { path: '', name: 'dashboard', component: () => import('@/views/dashboard/DashboardView.vue'), meta: { title: '运营仪表盘', subtitle: '查看发券、订单、核销的整体运行情况' } },
        { path: 'users', name: 'users', component: () => import('@/views/user/UserView.vue'), meta: { title: '用户管理', subtitle: '管理微信用户建档、手机号绑定与领券用户' } },
        { path: 'stores', name: 'stores', component: () => import('@/views/store/StoreView.vue'), meta: { title: '门店管理', subtitle: '配置可用门店与核销门店范围' } },
        { path: 'products', name: 'products', component: () => import('@/views/product/ProductView.vue'), meta: { title: '商品管理', subtitle: '维护 ERP 商品映射与指定商品券基础数据' } },
        { path: 'banners', name: 'banners', component: () => import('@/views/banner/BannerView.vue'), meta: { title: '轮播图管理', subtitle: '配置首页轮播图图片、跳转链接与展示顺序' } },
        { path: 'coupon-templates', name: 'couponTemplates', component: () => import('@/views/coupon-template/CouponTemplateView.vue'), meta: { title: '券模板管理', subtitle: '配置新人券、无门槛券、商品券、满减券规则' } },
        { path: 'coupon-packs', name: 'couponPacks', component: () => import('@/views/coupon-pack/CouponPackView.vue'), meta: { title: '券包管理', subtitle: '配置可售卖券包与价格、限购规则' } },
        { path: 'coupon-pack-items', name: 'couponPackItems', component: () => import('@/views/coupon-pack/CouponPackItemView.vue'), meta: { title: '券包明细', subtitle: '维护券包内包含的券模板与数量' } },
        { path: 'coupon-orders', name: 'couponOrders', component: () => import('@/views/order/CouponOrderView.vue'), meta: { title: '订单管理', subtitle: '查看券包订单与支付发券流程' } },
        { path: 'share-tracking', name: 'shareTracking', component: () => import('@/views/share-tracking/ShareTrackingView.vue'), meta: { title: '分享追踪', subtitle: '查看小程序分享意图与打开数据' } },
        { path: 'user-coupons', name: 'userCoupons', component: () => import('@/views/user-coupon/UserCouponView.vue'), meta: { title: '用户券', subtitle: '查看发券结果、券码与有效期状态' } },
        { path: 'writeoff', name: 'writeOff', component: () => import('@/views/writeoff/WriteOffView.vue'), meta: { title: '核销中心', subtitle: '处理门店核销、券码校验与使用结果确认' } },
        { path: 'miniapp-settings', name: 'miniappSettings', component: () => import('@/views/miniapp-setting/MiniAppSettingView.vue'), meta: { title: '小程序主题', subtitle: '切换新中式绿意与极简纯白风等小程序外观配置' } },
        { path: 'admin-users', name: 'adminUsers', component: () => import('@/views/admin-user/AdminUserView.vue'), meta: { title: '权限管理', subtitle: '管理后台账号、角色分配与账号状态' } },
        { path: 'admin-roles', name: 'adminRoles', component: () => import('@/views/admin-role/AdminRoleView.vue'), meta: { title: '角色管理', subtitle: '维护角色和菜单授权关系' } },
        { path: 'admin-menus', name: 'adminMenus', component: () => import('@/views/admin-menu/AdminMenuView.vue'), meta: { title: '菜单管理', subtitle: '维护后台菜单树、路由和组件映射' } },
      ],
    },
  ],
})

router.beforeEach(async (to) => {
  const token = authStorage.getAccessToken()
  if (to.path !== '/login' && !token) {
    return '/login'
  }
  if (to.path === '/login' && token) {
    return '/'
  }
  if (!token || to.path === '/login') {
    return true
  }

  let menuPaths = authStorage.getMenuPaths()
  if (menuPaths.length === 0) {
    try {
      const profile = await getAdminProfile()
      authStorage.setUsername(profile.data.username)
      authStorage.setDisplayName(profile.data.displayName || profile.data.username)
      authStorage.setMenuPaths(profile.data.menuPaths || [])
    authStorage.setPermissionCodes(profile.data.permissionCodes || [])
      menuPaths = profile.data.menuPaths || []
    } catch {
      authStorage.clearAll()
      return '/login'
    }
  }

  if (to.path === '/') {
    return true
  }

  if (menuPaths.length > 0 && !menuPaths.includes(to.path)) {
    return menuPaths[0] || '/'
  }

  return true
})

export default router
