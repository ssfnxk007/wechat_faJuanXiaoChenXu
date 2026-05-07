const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5265/api'
const API_ORIGIN = API_BASE_URL.replace(/\/api\/?$/, '').replace(/\/+$/, '')

export function normalizeAssetUrl(value?: string | null) {
  const raw = String(value || '').trim()
  if (!raw) return ''
  if (/^https?:\/\//i.test(raw)) return raw
  return `${API_ORIGIN}${raw.startsWith('/') ? raw : `/${raw}`}`
}
