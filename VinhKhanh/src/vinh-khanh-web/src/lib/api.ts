import axios from 'axios'
import { useAuthStore } from '@/store/authStore'

export const api = axios.create({
  baseURL: '',
  headers: { 'Content-Type': 'application/json' },
})

// Attach JWT token to every request
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Auto-retry on 503 and Auto-logout on 401
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const { config, response } = error;

    // 1. Xử lý Retry cho lỗi 503 (Service Unavailable)
    // Thử lại tối đa 3 lần với khoảng cách tăng dần (exponential backoff)
    if (response?.status === 503 && config && !config._isRetry) {
      config._retryCount = config._retryCount || 0;
      
      if (config._retryCount < 3) {
        config._retryCount++;
        const backoff = Math.pow(2, config._retryCount - 1) * 1000; // 1s, 2s, 4s
        console.warn(`API 503: Đang thử lại lần ${config._retryCount} sau ${backoff}ms...`);
        
        await new Promise(resolve => setTimeout(resolve, backoff));
        return api(config);
      }
      // Nếu đã thử 3 lần vẫn lỗi thì đánh dấu để không lặp vô tận
      config._isRetry = true;
    }

    // 2. Xử lý Logout cho lỗi 401 (Hết hạn phiên đăng nhập)
    if (response?.status === 401) {
      const store = useAuthStore.getState()
      if (store.token) {
        store.clear()
        window.location.href = '/role-select'
      }
    }

    return Promise.reject(error)
  }
)

export interface User {
  id: number
  username: string
  displayId?: string
  fullName?: string
  email?: string
  role: 'Admin' | 'Owner'
  isActive: boolean
  createdAt: string
}

export type Poi = {
  id: number
  name: string
  description: string
  ownerInfo?: string | null
  ownerUserId?: number | null
  owner?: User | null
  latitude: number
  longitude: number
  mapX: number
  mapY: number
  triggerRadiusMeters: number
  priority: number
  cooldownSeconds: number
  imageUrl?: string | null
  audioViUrl?: string | null
  qrCode?: string | null
  contentVersion?: number
  category: number
  isActive: boolean
  translations?: Array<{
    languageCode: string
    name: string
    description: string
    audioUrl?: string | null
    originalDescription?: string
  }>
}

export type Tour = {
  id: number
  name: string
  description?: string | null
  estimatedMinutes: number
  isActive: boolean
  stops?: Array<{
    stopOrder: number
    stayMinutes: number
    poiId: number
    poi?: Poi | null
  }>
}
