import { useSessionStore } from '@/store/session'

const DEFAULT_TIMEOUT = 30000
let pendingLoginPromise = null
let lastAuthToastAt = 0
let authModalVisible = false
const AUTH_RECOVERY_PAGE = '/pages/profile/index'

function joinUrl(baseUrl, path) {
  if (/^https?:\/\//i.test(path)) {
    return path
  }

  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  if (!baseUrl) {
    return normalizedPath
  }

  return `${baseUrl.replace(/\/+$/, '')}${normalizedPath}`
}

function appendQuery(url, query = {}) {
  const search = Object.keys(query)
    .filter((key) => query[key] !== undefined && query[key] !== null && query[key] !== '')
    .map((key) => `${encodeURIComponent(key)}=${encodeURIComponent(query[key])}`)
    .join('&')

  if (!search) {
    return url
  }

  return `${url}${url.includes('?') ? '&' : '?'}${search}`
}

function createRequestError(message, extra = {}) {
  const error = new Error(message || '请求失败')
  Object.assign(error, extra)
  return error
}

function notifyAuthIssue(message) {
  const now = Date.now()
  if (now - lastAuthToastAt < 2500) {
    return
  }

  lastAuthToastAt = now
  uni.showToast({
    title: message || '请重新登录',
    icon: 'none'
  })
}

function navigateToAuthRecoveryPage() {
  const pages = typeof getCurrentPages === 'function' ? getCurrentPages() : []
  const currentRoute = pages.length ? `/${pages[pages.length - 1].route}` : ''
  if (currentRoute === AUTH_RECOVERY_PAGE) {
    return
  }

  uni.switchTab({
    url: AUTH_RECOVERY_PAGE,
    fail: () => {
      uni.reLaunch({ url: AUTH_RECOVERY_PAGE })
    }
  })
}

function showAuthFailureModal(message) {
  if (authModalVisible) {
    return
  }

  authModalVisible = true
  uni.showModal({
    title: '请重新登录',
    content: message || '登录已过期，请重新登录。',
    showCancel: false,
    confirmText: '我知道了',
    success: (result) => {
      if (result.confirm) {
        navigateToAuthRecoveryPage()
      }
    },
    complete: () => {
      authModalVisible = false
    }
  })
}

function unwrapApiEnvelope(payload) {
  if (!payload || typeof payload !== 'object') {
    return payload
  }

  if (Object.prototype.hasOwnProperty.call(payload, 'code')) {
    if (Number(payload.code) === 200) {
      return payload.data
    }

    throw createRequestError(payload.message || '接口返回失败', {
      code: payload.code,
      payload
    })
  }

  if (Object.prototype.hasOwnProperty.call(payload, 'data') && Object.keys(payload).length <= 2) {
    return payload.data
  }

  return payload
}

function isUnauthorizedResponse(response) {
  if (!response) {
    return false
  }
  if (response.statusCode === 401) {
    return true
  }
  const code = response.data && typeof response.data === 'object' ? response.data.code : null
  return Number(code) === 401
}

async function triggerAuthRefresh() {
  if (!pendingLoginPromise) {
    pendingLoginPromise = (async () => {
      try {
        const mod = await import('@/api/auth')
        if (mod && typeof mod.ensureMiniProgramLogin === 'function') {
          const session = await mod.ensureMiniProgramLogin({ force: true })
          if (!session?.token) {
            throw createRequestError('请重新登录', { code: 401 })
          }
        }
      } catch (error) {
        console.warn('[coupon-miniapp][auth-refresh]', error)
        notifyAuthIssue(error?.message || '请重新登录')
        showAuthFailureModal(error?.message || '登录已过期，请重新登录。')
        throw error
      } finally {
        pendingLoginPromise = null
      }
    })()
  }
  return pendingLoginPromise
}

function performRequest(options, finalUrl, headers) {
  return new Promise((resolve, reject) => {
    uni.request({
      url: finalUrl,
      method: options.method || 'GET',
      data: options.data,
      timeout: options.timeout || DEFAULT_TIMEOUT,
      header: headers,
      responseType: options.responseType || 'text',
      success: (response) => resolve(response),
      fail: (error) => {
        console.warn('[coupon-miniapp][request fail]', finalUrl, error)
        reject(createRequestError(error.errMsg || '网络请求失败', {
          cause: error,
          url: finalUrl
        }))
      }
    })
  })
}

export async function request(options = {}) {
  const session = useSessionStore()
  const finalUrl = appendQuery(joinUrl(session.apiBaseUrl, options.url || ''), options.query)

  const buildHeaders = () => {
    const headers = {
      'content-type': 'application/json',
      ...(options.header || {})
    }
    if (session.token) {
      headers.Authorization = `Bearer ${session.token}`
    }
    return headers
  }

  const response = await performRequest(options, finalUrl, buildHeaders())

  if (isUnauthorizedResponse(response) && !options.skipAuthIntercept && !options._retriedAuth) {
    notifyAuthIssue('登录已过期，正在重登')
    await triggerAuthRefresh()
    return request({ ...options, _retriedAuth: true })
  }

  if (isUnauthorizedResponse(response) && !options.skipAuthIntercept && options._retriedAuth) {
    notifyAuthIssue(response.data?.message || '请重新登录')
    showAuthFailureModal(response.data?.message || '登录已过期，请重新登录。')
  }

  if (response.statusCode >= 400) {
    throw createRequestError(response.data?.message || `HTTP ${response.statusCode}`, {
      statusCode: response.statusCode,
      payload: response.data,
      url: finalUrl
    })
  }

  if (options.responseType === 'arraybuffer') {
    return {
      data: response.data,
      raw: response.data,
      statusCode: response.statusCode,
      url: finalUrl
    }
  }

  return {
    data: unwrapApiEnvelope(response.data),
    raw: response.data,
    statusCode: response.statusCode,
    url: finalUrl
  }
}

export async function requestWithFallback(options = {}, fallbackFactory) {
  try {
    const response = await request(options)
    return {
      source: 'remote',
      data: response.data,
      url: response.url
    }
  } catch (error) {
    if (!fallbackFactory) {
      throw error
    }

    console.warn('[coupon-miniapp][request fallback]', options.url, error)

    return {
      source: 'fallback',
      data: await fallbackFactory(error),
      error,
      url: options.url
    }
  }
}
