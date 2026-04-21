import axios from 'axios'
import { authStorage } from '@/utils/auth'
import { notify } from '@/utils/notify'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5265/api',
  timeout: 15000,
})

let authExpiredHandling = false

http.interceptors.request.use((config) => {
  const token = authStorage.getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error?.response?.status === 401) {
      if (!authExpiredHandling) {
        authExpiredHandling = true
        notify.error('登录已失效，请重新登录')
        window.setTimeout(() => {
          authExpiredHandling = false
        }, 1500)
      }

      authStorage.clearAll()
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  },
)

export default http
