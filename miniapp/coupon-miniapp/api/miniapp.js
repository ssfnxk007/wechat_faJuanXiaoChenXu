import { request } from '@/utils/request'

export function getMiniAppHome(params) {
  return request({ url: '/api/miniapp/home', query: params }).then((response) => response.data)
}

export function getMiniAppSettings() {
  return request({ url: '/api/miniapp/settings' }).then((response) => response.data)
}

export function getMiniAppCouponPacks(params) {
  return request({ url: '/api/miniapp/coupon-packs', query: params }).then((response) => response.data)
}

export function getMiniAppCouponPackDetail(id) {
  return request({ url: `/api/miniapp/coupon-packs/${id}` }).then((response) => response.data)
}

export function getMiniAppProductDetail(id) {
  return request({ url: `/api/miniapp/products/${id}` }).then((response) => response.data)
}

export function getMiniAppUserCoupons(params) {
  return request({ url: '/api/miniapp/users/coupons', query: params }).then((response) => response.data)
}

export function getMiniAppUserCouponDetail(id) {
  return request({ url: `/api/miniapp/users/coupons/${id}` }).then((response) => response.data)
}

export function getMiniAppUserOrders(params) {
  return request({ url: '/api/miniapp/users/orders', query: params }).then((response) => response.data)
}

export function getMiniAppUserOrderDetail(id) {
  return request({ url: `/api/miniapp/users/orders/${id}` }).then((response) => response.data)
}

export function createMiniAppOrder(data) {
  return request({ url: '/api/miniapp/orders', method: 'POST', data }).then((response) => response.data)
}

export function createMiniAppOrderPayment(orderId, data) {
  return request({ url: `/api/miniapp/orders/${orderId}/pay`, method: 'POST', data }).then((response) => response.data)
}

export function completeMiniAppOrderPayment(orderId, data) {
  return request({ url: `/api/miniapp/orders/${orderId}/complete-payment`, method: 'POST', data }).then((response) => response.data)
}


export function getMiniAppCouponTemplateDetail(id, params) {
  return request({ url: `/api/miniapp/coupon-templates/${id}`, query: params }).then((response) => response.data)
}

export function claimMiniAppCouponTemplate(id, data) {
  return request({ url: `/api/miniapp/coupon-templates/${id}/claim`, method: 'POST', data }).then((response) => response.data)
}

export function reportMiniAppShareTrackingEvent(data) {
  return request({ url: '/api/miniapp/share-tracking/events', method: 'POST', data }).then((response) => response.data)
}
