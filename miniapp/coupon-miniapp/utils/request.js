import { useSessionStore } from '@/store/session'

const DEFAULT_TIMEOUT = 12000

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

export function request(options = {}) {
  const session = useSessionStore()
  const finalUrl = appendQuery(joinUrl(session.apiBaseUrl, options.url || ''), options.query)

  const headers = {
    'content-type': 'application/json',
    ...(options.header || {})
  }

  if (session.token) {
    headers.Authorization = `Bearer ${session.token}`
  }

  return new Promise((resolve, reject) => {
    uni.request({
      url: finalUrl,
      method: options.method || 'GET',
      data: options.data,
      timeout: options.timeout || DEFAULT_TIMEOUT,
      header: headers,
      responseType: options.responseType || 'text',
      success: (response) => {
        try {
          if (response.statusCode >= 400) {
            throw createRequestError(response.data?.message || `HTTP ${response.statusCode}`, {
              statusCode: response.statusCode,
              payload: response.data,
              url: finalUrl
            })
          }

          if (options.responseType === 'arraybuffer') {
            resolve({
              data: response.data,
              raw: response.data,
              statusCode: response.statusCode,
              url: finalUrl
            })
            return
          }

          resolve({
            data: unwrapApiEnvelope(response.data),
            raw: response.data,
            statusCode: response.statusCode,
            url: finalUrl
          })
        } catch (error) {
          reject(error)
        }
      },
      fail: (error) => {
        reject(createRequestError(error.errMsg || '网络请求失败', {
          cause: error,
          url: finalUrl
        }))
      }
    })
  })
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
