import { useState } from 'react'
import { useNavigate } from 'react-router'
import { api } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'
import { Eye, EyeOff, LogIn, AlertCircle } from 'lucide-react'

export function Login() {
  const [u, setU] = useState('admin')
  const [p, setP] = useState('Admin@2026')
  const [err, setErr] = useState<string | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    try {
      const { data } = await api.post('/api/auth/login', {
        username: u,
        password: p,
      })
      setAuth(data.token, data.role)
      navigate('/', { replace: true })
    } catch {
      setErr('Sai tài khoản hoặc mật khẩu')
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-orange-50 via-amber-50 to-orange-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        {/* Header */}
        <div className="text-center">
          <div className="flex justify-center">
            <div className="w-16 h-16 bg-gradient-to-r from-orange-500 to-amber-500 rounded-full flex items-center justify-center shadow-lg">
              <span className="text-white font-bold text-2xl">🍜</span>
            </div>
          </div>
          <h2 className="mt-6 text-3xl font-extrabold text-gray-900 dark:text-white">
            Đăng Nhập Hệ Thống
          </h2>
          <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
            Quản lý Phố Ẩm Thực Vĩnh Khánh
          </p>
        </div>

        {/* Login Form */}
        <div className="mt-8 bg-white dark:bg-gray-800 py-8 px-6 shadow-xl rounded-2xl border border-orange-100 dark:border-gray-700">
          <form className="space-y-6" onSubmit={onSubmit}>
            {err && (
              <div className="rounded-md bg-red-50 dark:bg-red-900/20 p-4 mb-4 border border-red-200 dark:border-red-800">
                <div className="flex">
                  <AlertCircle className="h-5 w-5 text-red-400 mr-2" />
                  <span className="text-sm text-red-700 dark:text-red-400">{err}</span>
                </div>
              </div>
            )}

            <div className="space-y-4">
              <div>
                <label htmlFor="username" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Tài khoản
                </label>
                <div className="mt-1 relative">
                  <input
                    id="username"
                    className="block w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm focus:ring-orange-500 focus:border-orange-500 dark:bg-gray-700 dark:text-white pr-10"
                    value={u}
                    onChange={(e) => setU(e.target.value)}
                    autoComplete="username"
                    placeholder="Nhập tài khoản"
                  />
                </div>
              </div>

              <div>
                <label htmlFor="password" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Mật khẩu
                </label>
                <div className="mt-1 relative">
                  <input
                    id="password"
                    type={showPassword ? "text" : "password"}
                    className="block w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm focus:ring-orange-500 focus:border-orange-500 dark:bg-gray-700 dark:text-white pr-10"
                    value={p}
                    onChange={(e) => setP(e.target.value)}
                    autoComplete="current-password"
                    placeholder="Nhập mật khẩu"
                  />
                  <button
                    type="button"
                    className="absolute inset-y-0 right-0 pr-3 flex items-center"
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    {showPassword ? (
                      <EyeOff className="h-5 w-5 text-gray-400 hover:text-gray-600" />
                    ) : (
                      <Eye className="h-5 w-5 text-gray-400 hover:text-gray-600" />
                    )}
                  </button>
                </div>
              </div>
            </div>

            <div className="bg-gray-50 dark:bg-gray-700 px-4 py-3 rounded-lg">
              <div className="text-sm text-gray-600 dark:text-gray-400">
                <p className="font-medium mb-2">Tài khoản mặc định:</p>
                <div className="space-y-1">
                  <p><span className="font-semibold text-orange-600">Admin:</span> admin</p>
                  <p><span className="font-semibold text-orange-600">Owner:</span> owner1</p>
                </div>
                <p className="mt-2 text-xs">
                  Mật khẩu cho cả hai: <span className="font-semibold text-orange-600">Admin@2026</span>
                </p>
              </div>
            </div>

            <button
              type="submit"
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-gradient-to-r from-orange-600 to-amber-600 hover:from-orange-700 hover:to-amber-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-orange-500 transition-all duration-200"
            >
              <LogIn className="h-5 w-5 mr-2 group-hover:translate-x-0.5 transition-transform duration-200" />
              Đăng Nhập
            </button>
          </form>
        </div>

        {/* Footer */}
        <div className="mt-6 text-center">
          <p className="text-xs text-gray-500 dark:text-gray-400">
            &copy; 2024 Phố Ẩm Thực Vĩnh Khánh. Bảo lưu thông tin đăng nhập.
          </p>
        </div>
      </div>
    </div>
  )
}
