const USER_ID_KEY = 'coupon-miniapp-user-id'

export function getCurrentUserId() {
  const rawValue = uni.getStorageSync(USER_ID_KEY)
  const userId = Number(rawValue || 0)
  return Number.isFinite(userId) && userId > 0 ? userId : 0
}

export function setCurrentUserId(userId) {
  const normalized = Number(userId || 0)
  if (normalized > 0) {
    uni.setStorageSync(USER_ID_KEY, normalized)
  }
}
