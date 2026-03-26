import axios from 'axios'
import { useAuthStore } from '@/store/authStore'

export const api = axios.create({
  baseURL: '/',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

export type Poi = {
  id: number
  name: string
  description: string
  ownerInfo?: string | null
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
