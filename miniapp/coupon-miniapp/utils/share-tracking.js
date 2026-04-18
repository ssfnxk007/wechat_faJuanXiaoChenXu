import { reportMiniAppShareTrackingEvent } from '@/api/miniapp'

const VISITOR_KEY_STORAGE_KEY = 'coupon-miniapp:visitor-key'

function randomHex(length = 32) {
  const chars = '0123456789abcdef'
  let value = ''
  for (let i = 0; i < length; i += 1) {
    value += chars[Math.floor(Math.random() * chars.length)]
  }
  return value
}

function toPlainObject(value) {
  if (!value || typeof value !== 'object') {
    return {}
  }

  return Object.keys(value).reduce((result, key) => {
    const item = value[key]
    if (item !== undefined && item !== null) {
      result[key] = String(item)
    }
    return result
  }, {})
}

export function createShareId() {
  return randomHex(32)
}

export function getOrCreateVisitorKey() {
  const saved = String(uni.getStorageSync(VISITOR_KEY_STORAGE_KEY) || '').trim()
  if (saved) {
    return saved
  }

  const created = `v_${randomHex(16)}`
  uni.setStorageSync(VISITOR_KEY_STORAGE_KEY, created)
  return created
}

export function createEventKey(eventType, shareId, openIdentity, pagePath) {
  const normalizedPagePath = String(pagePath || '').trim() || '/'
  if (eventType === 'shareIntent') {
    return `${eventType}:${shareId}`
  }
  return `${eventType}:${shareId}:${openIdentity}:${normalizedPagePath}`
}

export function normalizeSceneValue() {
  const launchOptions = uni.getLaunchOptionsSync?.() || {}
  const scene = launchOptions.scene
  if (scene === undefined || scene === null) {
    return ''
  }
  return String(scene)
}

export async function reportShareTrackingEvent(payload = {}) {
  const eventPayload = {
    ...payload,
    query: toPlainObject(payload.query),
    clientTime: new Date().toISOString(),
  }

  try {
    await reportMiniAppShareTrackingEvent(eventPayload)
    return true
  } catch (error) {
    console.warn('[share-tracking] report failed', error)
    return false
  }
}
