import { useState } from 'react'
import { useNavigate } from 'react-router'
import axios from 'axios'
import { useAuthStore } from '@/store/authStore'

export function Login() {
  const [u, setU] = useState('admin')
  const [p, setP] = useState('Admin@2026')
  const [err, setErr] = useState<string | null>(null)
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    try {
      const { data } = await axios.post('/api/auth/login', {
        username: u,
        password: p,
      })
      setAuth(data.token, data.role)
      navigate('/', { replace: true })
    } catch {
      setErr('Sai tài khoản hoặc mật khẩu.')
    }
  }

  return (
    <div className="mx-auto mt-16 max-w-md rounded-xl border border-stone-200 bg-white p-8 text-left shadow-sm dark:border-stone-700 dark:bg-stone-900">
      <h1 className="mb-2 text-2xl font-bold text-stone-900 dark:text-stone-50">
        Đăng nhập CMS
      </h1>
      <p className="mb-6 text-sm text-stone-500">
        Mặc định: admin / Admin@2026 · owner1 / Owner@2026
      </p>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <label className="block text-sm font-medium">
          Tài khoản
          <input
            className="mt-1 w-full rounded border border-stone-300 px-3 py-2 dark:border-stone-600 dark:bg-stone-800"
            value={u}
            onChange={(e) => setU(e.target.value)}
            autoComplete="username"
          />
        </label>
        <label className="block text-sm font-medium">
          Mật khẩu
          <input
            type="password"
            className="mt-1 w-full rounded border border-stone-300 px-3 py-2 dark:border-stone-600 dark:bg-stone-800"
            value={p}
            onChange={(e) => setP(e.target.value)}
            autoComplete="current-password"
          />
        </label>
        {err && <p className="text-sm text-red-600">{err}</p>}
        <button
          type="submit"
          className="rounded-lg bg-orange-600 py-2 font-medium text-white hover:bg-orange-700"
        >
          Đăng nhập
        </button>
      </form>
    </div>
  )
}
