import { clearSession, patchSessionStatus, updateSession, useSessionStore } from '@/store/session'
import { requestWithFallback, request } from '@/utils/request'

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
  const result = await requestWithFallback(
    { url: '/api/auth/wechat-status', skipAuthIntercept: true },
    () => ({ isConfigured: false, message: '后端微信配置状态暂不可用' })
  )

  const payload = result.data || {}
  patchSessionStatus({
    wechatConfigured: Boolean(payload.isConfigured),
    loginStatus: payload.isConfigured ? 'checking' : 'offline',
    loginMessage: payload.message || ''
  })

  return result
}

export async function ensureMiniProgramLogin(options = {}) {
  const session = useSessionStore()
  const force = Boolean(options && options.force)

  if (force) {
    clearSession()
  } else if (session.userId && session.token) {
    return session
  }

  const weChatStatus = await fetchWeChatStatus()
  if (!weChatStatus.data?.isConfigured) {
    patchSessionStatus({
      loginStatus: weChatStatus.source === 'fallback' ? 'fallback' : 'offline',
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
    patchSessionStatus({
      loginStatus: 'ready',
      loginMessage: ''
    })
  } catch (error) {
    console.warn('[coupon-miniapp][mini-login]', error)
    patchSessionStatus({
      loginStatus: 'error',
      loginMessage: error.message || '微信登录失败'
    })
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
