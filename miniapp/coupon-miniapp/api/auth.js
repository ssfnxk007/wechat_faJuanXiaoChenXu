import {
  clearSession,
  hasUsableToken,
  hydrateOnboardingState,
  markPhoneOnboardingSkipped,
  patchSessionStatus,
  updateSession,
  useSessionStore
} from '@/store/session'
import { request } from '@/utils/request'

function loginByWeChat() {
  return new Promise((resolve, reject) => {
    uni.login({
      provider: 'weixin',
      success: resolve,
      fail: reject
    })
  })
}

export async function fetchWeChatStatus() {
  const response = await request({ url: '/api/auth/wechat-status', skipAuthIntercept: true })
  const payload = response.data || {}
  patchSessionStatus({
    wechatConfigured: Boolean(payload.isConfigured),
    loginStatus: payload.isConfigured ? 'checking' : 'offline',
    loginMessage: payload.message || ''
  })

  return response
}

export async function verifyMiniProgramSession() {
  const response = await request({
    url: '/api/auth/mini-session',
    skipAuthIntercept: true
  })

  return response.data || {}
}

export async function ensureMiniProgramLogin(options = {}) {
  const session = useSessionStore()
  hydrateOnboardingState()
  const force = Boolean(options && options.force)

  if (force) {
    clearSession()
  } else if (hasUsableToken()) {
    return session
  }

  const weChatStatus = await fetchWeChatStatus()
  if (!weChatStatus.data?.isConfigured) {
    patchSessionStatus({
      loginStatus: 'offline',
      loginMessage: weChatStatus.data?.message || '微信登录暂未配置'
    })
    return session
  }

  // #ifdef MP-WEIXIN
  try {
    const loginResult = await loginByWeChat()
    const response = await request({
      url: '/api/auth/mini-login',
      method: 'POST',
      skipAuthIntercept: true,
      data: {
        code: loginResult.code,
        nickname: session.nickname || ''
      }
    })

    updateSession(response.data || {})
    if (!useSessionStore().token || !useSessionStore().userId) {
      throw new Error(response?.raw?.message || '微信登录失败')
    }

    const verifiedSession = await verifyMiniProgramSession()
    updateSession(verifiedSession)
    if (!useSessionStore().token || !useSessionStore().userId) {
      throw new Error('微信登录成功，但会话校验失败')
    }

    patchSessionStatus({
      loginStatus: 'ready',
      loginMessage: ''
    })
  } catch (error) {
    console.warn('[coupon-miniapp][mini-login]', error)
    clearSession()
    patchSessionStatus({
      loginStatus: 'error',
      loginMessage: error.message || '微信登录失败'
    })
    throw (error instanceof Error ? error : new Error('微信登录失败'))
  }
  // #endif

  // #ifndef MP-WEIXIN
  patchSessionStatus({
    loginStatus: 'mock',
    loginMessage: '当前运行环境不支持微信登录，已跳过真实登录流程'
  })
  // #endif

  return session
}

export async function exchangePhoneNumber(code) {
  const response = await request({
    url: '/api/auth/exchange-phone-number',
    method: 'POST',
    data: { code }
  })

  updateSession(response.data || {})
  if ((response.data || {}).mobile) {
    markPhoneOnboardingSkipped(false)
  }
  return useSessionStore()
}

export function shouldRequirePhone(options = {}) {
  const session = useSessionStore()
  hydrateOnboardingState()
  if (!session.wechatConfigured) {
    return false
  }
  if (!session.userId || !session.token) {
    return false
  }
  if (session.mobile) {
    return false
  }
  return options.force === true || !session.skippedPhoneOnboarding
}

export function openPhoneOnboarding(options = {}) {
  const redirect = encodeURIComponent(options.redirect || '')
  const force = options.force ? '1' : '0'
  const url = `/pages/onboarding/phone?redirect=${redirect}&force=${force}`
  uni.navigateTo({ url })
}

export async function ensurePhoneReady(options = {}) {
  await ensureMiniProgramLogin()
  if (!shouldRequirePhone(options)) {
    return true
  }

  openPhoneOnboarding({
    redirect: options.redirect || '',
    force: options.force === true
  })
  return false
}
