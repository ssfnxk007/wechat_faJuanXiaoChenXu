import { createRouter, createWebHistory } from 'vue-router'
import { getAdminProfile } from '@/api/admin-auth'
import AdminLayout from '@/layouts/AdminLayout.vue'
import AdminMenuView from '@/views/admin-menu/AdminMenuView.vue'
import AdminRoleView from '@/views/admin-role/AdminRoleView.vue'
import AdminUserView from '@/views/admin-user/AdminUserView.vue'
import CouponPackItemView from '@/views/coupon-pack/CouponPackItemView.vue'
import CouponPackView from '@/views/coupon-pack/CouponPackView.vue'
import CouponTemplateView from '@/views/coupon-template/CouponTemplateView.vue'
import DashboardView from '@/views/dashboard/DashboardView.vue'
import LoginView from '@/views/login/LoginView.vue'
import CouponOrderView from '@/views/order/CouponOrderView.vue'
import ProductView from '@/views/product/ProductView.vue'
import StoreView from '@/views/store/StoreView.vue'
import UserCouponView from '@/views/user-coupon/UserCouponView.vue'
import UserView from '@/views/user/UserView.vue'
import WriteOffView from '@/views/writeoff/WriteOffView.vue'
import { authStorage } from '@/utils.auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView, meta: { title: '后台登录', subtitle: '登录后进入发券后台管理系统' } },
    {
      path: '/',
      component: AdminLayout,
      children: [
        { path: '', name: 'dashboard', component: DashboardView, meta: { title: '运营仪表盘', subtitle: '查看发券、订单、核销的整体运行情况' } },
        { path: 'users', name: 'users', component: UserView, meta: { title: '用户管理', subtitle: '管理微信用户建档、手机号绑定与领券用户' } },
        { path: 'stores', name: 'stores', component: StoreView, meta: { title: '门店管理', subtitle: '配置可用门店与核销门店范围' } },
        { path: 'products', name: 'products', component: ProductView, meta: { title: '商品管理', subtitle: '维护 ERP 商品映射与指定商品券基础数据' } },
        { path: 'coupon-templates', name: 'couponTemplates', component: CouponTemplateView, meta: { title: '券模板管理', subtitle: '配置新人券、无门槛券、商品券、满减券规则' } },
        { path: 'coupon-packs', name: 'couponPacks', component: CouponPackView, meta: { title: '券包管理', subtitle: '配置可售卖券包与价格、限购规则' } },
        { path: 'coupon-pack-items', name: 'couponPackItems', component: CouponPackItemView, meta: { title: '券包明细', subtitle: '维护券包内包含的券模板与数量' } },
        { path: 'coupon-orders', name: 'couponOrders', component: CouponOrderView, meta: { title: '订单管理', subtitle: '查看券包订单并联调支付与发券流程' } },
        { path: 'user-coupons', name: 'userCoupons', component: UserCouponView, meta: { title: '用户券', subtitle: '查看发券结果、券码与有效期状态' } },
        { path: 'writeoff', name: 'writeOff', component: WriteOffView, meta: { title: '核销中心', subtitle: '模拟 ERP 扫码核销与门店使用场景' } },
        { path: 'admin-users', name: 'adminUsers', component: AdminUserView, meta: { title: '权限管理', subtitle: '管理后台账号、角色分配与账号状态' } },
        { path: 'admin-roles', name: 'adminRoles', component: AdminRoleView, meta: { title: '角色管理', subtitle: '维护角色和菜单授权关系' } },
        { path: 'admin-menus', name: 'adminMenus', component: AdminMenuView, meta: { title: '菜单管理', subtitle: '维护后台菜单树、路由和组件映射' } },
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
