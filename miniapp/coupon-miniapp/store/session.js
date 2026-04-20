import { reactive } from 'vue'

const SESSION_STORAGE_KEY = 'coupon-miniapp:session'
const API_BASE_URL_STORAGE_KEY = 'coupon-miniapp:api-base-url'
const THEME_STORAGE_KEY = 'coupon-miniapp:theme-code'
const ONBOARDING_STORAGE_KEY = 'coupon-miniapp:onboarding'
const DEFAULT_API_BASE_URL = 'http://10.168.1.106:5265'
const DEFAULT_THEME_CODE = 'green'
const VALID_THEME_CODES = new Set(['green', 'light', 'candy', 'orange', 'red'])

const state = reactive({
  hydrated: false,
  apiBaseUrl: DEFAULT_API_BASE_URL,
  themeCode: DEFAULT_THEME_CODE,
  userId: null,
  miniOpenId: '',
  mobile: '',
  nickname: '',
  isNewUser: false,
  token: '',
  wechatConfigured: false,
  loginStatus: 'idle',
  loginMessage: '',
  skippedPhoneOnboarding: false
})

function trimText(value) {
  return typeof value === 'string' ? value.trim() : ''
}

function normalizeThemeCode(themeCode) {
  const normalized = trimText(themeCode).toLowerCase()
  return VALID_THEME_CODES.has(normalized) ? normalized : ''
}

function persistSession() {
  uni.setStorageSync(SESSION_STORAGE_KEY, {
    userId: state.userId,
    miniOpenId: state.miniOpenId,
    mobile: state.mobile,
    nickname: state.nickname,
    isNewUser: state.isNewUser,
    token: state.token
  })
}

export function hydrateSessionStore() {
  if (state.hydrated) {
    return state
  }

  const savedSession = uni.getStorageSync(SESSION_STORAGE_KEY) || {}
  const savedBaseUrl = trimText(uni.getStorageSync(API_BASE_URL_STORAGE_KEY))
  const savedThemeCode = trimText(uni.getStorageSync(THEME_STORAGE_KEY))

  state.apiBaseUrl = savedBaseUrl || DEFAULT_API_BASE_URL
  state.themeCode = normalizeThemeCode(savedThemeCode) || DEFAULT_THEME_CODE
  state.userId = Number(savedSession.userId) > 0 ? Number(savedSession.userId) : null
  state.miniOpenId = trimText(savedSession.miniOpenId)
  state.mobile = trimText(savedSession.mobile)
  state.nickname = trimText(savedSession.nickname)
  state.isNewUser = Boolean(savedSession.isNewUser)
  state.token = trimText(savedSession.token)
  state.hydrated = true

  return state
}

export function useSessionStore() {
  return hydrateSessionStore()
}

export function setApiBaseUrl(baseUrl) {
  const normalized = trimText(baseUrl).replace(/\/+$/, '')
  state.apiBaseUrl = normalized
  uni.setStorageSync(API_BASE_URL_STORAGE_KEY, normalized)
  return state.apiBaseUrl
}

export function setThemeCode(themeCode) {
  hydrateSessionStore()

  const normalized = normalizeThemeCode(themeCode)
  if (!normalized) {
    return state.themeCode
  }

  state.themeCode = normalized
  uni.setStorageSync(THEME_STORAGE_KEY, normalized)
  return state.themeCode
}

export function updateSession(payload = {}) {
  hydrateSessionStore()

  state.userId = Number(payload.userId) > 0 ? Number(payload.userId) : state.userId
  state.miniOpenId = trimText(payload.miniOpenId || state.miniOpenId)
  state.mobile = trimText(payload.mobile || state.mobile)
  state.nickname = trimText(payload.nickname || state.nickname)
  state.isNewUser = typeof payload.isNewUser === 'boolean' ? payload.isNewUser : state.isNewUser
  state.token = trimText(payload.token || state.token)
  if (state.mobile) {
    state.skippedPhoneOnboarding = false
    uni.removeStorageSync(ONBOARDING_STORAGE_KEY)
  }

  persistSession()
  return state
}

export function hydrateOnboardingState() {
  const payload = uni.getStorageSync(ONBOARDING_STORAGE_KEY) || {}
  state.skippedPhoneOnboarding = Boolean(payload.skippedPhoneOnboarding)
  return state
}

export function markPhoneOnboardingSkipped(skipped = true) {
  hydrateSessionStore()
  state.skippedPhoneOnboarding = Boolean(skipped)
  if (state.skippedPhoneOnboarding) {
    uni.setStorageSync(ONBOARDING_STORAGE_KEY, {
      skippedPhoneOnboarding: true
    })
  } else {
    uni.removeStorageSync(ONBOARDING_STORAGE_KEY)
  }
  return state
}

export function patchSessionStatus(payload = {}) {
  hydrateSessionStore()

  if (typeof payload.wechatConfigured === 'boolean') {
    state.wechatConfigured = payload.wechatConfigured
  }
  if (payload.loginStatus) {
    state.loginStatus = payload.loginStatus
  }
  if (typeof payload.loginMessage === 'string') {
    state.loginMessage = payload.loginMessage
  }

  return state
}

export function clearSession() {
  state.userId = null
  state.miniOpenId = ''
  state.mobile = ''
  state.nickname = ''
  state.isNewUser = false
  state.token = ''
  state.loginStatus = 'idle'
  state.loginMessage = ''
  state.skippedPhoneOnboarding = false
  uni.removeStorageSync(SESSION_STORAGE_KEY)
  uni.removeStorageSync(ONBOARDING_STORAGE_KEY)
}
