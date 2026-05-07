import { useSessionStore } from '@/store/session'

const UPLOADS_PREFIX = '/uploads/'

function getApiOrigin() {
  const session = useSessionStore()
  const baseUrl = String(session.apiBaseUrl || '').trim().replace(/\/+$/, '')
  const match = baseUrl.match(/^(https?:\/\/[^/]+)/i)
  if (!match) {
    throw new Error(`API 地址无效，无法生成上传资源地址：${baseUrl}`)
  }

  return match[1]
}

function getPathAndQueryFromAbsoluteUrl(value) {
  const match = String(value || '').trim().match(/^https?:\/\/[^/]+([^#]*)/i)
  return match ? match[1] : ''
}

export function normalizeUploadedAssetUrl(value = '') {
  const raw = String(value || '').trim()
  if (!raw) {
    return ''
  }

  if (raw.startsWith(UPLOADS_PREFIX)) {
    return `${getApiOrigin()}${raw}`
  }

  if (/^https?:\/\//i.test(raw)) {
    const pathAndQuery = getPathAndQueryFromAbsoluteUrl(raw)
    if (pathAndQuery.startsWith(UPLOADS_PREFIX)) {
      return `${getApiOrigin()}${pathAndQuery}`
    }
  }

  return raw
}
