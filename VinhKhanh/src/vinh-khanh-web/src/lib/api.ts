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

// Auto-logout on 401 (token expired or invalid)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      const store = useAuthStore.getState()
      // Only clear if we were actually logged in (has token)
      if (store.token) {
        store.clear()
        // Force redirect to role selection
        window.location.href = '/role-select'
      }
    }
    return Promise.reject(error)
  }
)

export type Poi = {
  id: number
  name: string
  description: string
  ownerInfo?: string | null
  ownerUserId?: number | null
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
