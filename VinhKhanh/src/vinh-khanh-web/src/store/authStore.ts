import { create } from 'zustand'

type AuthState = {
  token: string | null
  role: string | null
  setAuth: (token: string, role: string) => void
  clear: () => void
}

const tk = () => localStorage.getItem('vk_token')
const rk = () => localStorage.getItem('vk_role')

export const useAuthStore = create<AuthState>((set) => ({
  token: tk(),
  role: rk(),
  setAuth: (token, role) => {
    localStorage.setItem('vk_token', token)
    localStorage.setItem('vk_role', role)
    set({ token, role })
  },
  clear: () => {
    localStorage.removeItem('vk_token')
    localStorage.removeItem('vk_role')
    set({ token: null, role: null })
  },
}))
