import { useState } from 'react'
import { useNavigate } from 'react-router'
import { api } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'
import { LogIn, AlertCircle, ArrowLeft, Shield } from 'lucide-react'

export function AdminLogin() {
  const [u, setU] = useState('')
  const [p, setP] = useState('')
  const [err, setErr] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    setLoading(true)
    try {
      const { data } = await api.post('/api/auth/login', { username: u, password: p })
      if (data.role !== 'Admin') {
        setErr('Tài khoản này không phải Admin. Vui lòng sử dụng cổng Chủ Quán.')
        setLoading(false)
        return
      }
      setAuth(data.token, data.role)
      navigate('/', { replace: true })
    } catch (error: any) {
      console.error(error)
      if (!error.response) {
        setErr('Lỗi mạng: Không thể kết nối tới máy chủ (API offline).')
      } else if (error.response.status === 401) {
        setErr('Sai tài khoản hoặc mật khẩu.')
      } else {
        setErr(error.response?.data?.message || 'Lỗi hệ thống: ' + error.response.status)
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex w-full bg-white font-sans text-slate-900">
      {/* LEFT: Login form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-8 sm:p-12 md:p-24 bg-white z-10 shadow-2xl">
        <div className="w-full max-w-md">
          <button
            onClick={() => navigate('/role-select', { replace: true })}
            className="flex items-center gap-2 text-sm text-slate-500 hover:text-orange-600 font-medium mb-8 transition-colors"
          >
            <ArrowLeft size={16} /> Quay lại chọn vai trò
          </button>

          <div className="flex items-center gap-3 mb-8">
            <div className="w-12 h-12 bg-orange-600 rounded-lg flex items-center justify-center shadow-lg">
              <Shield size={24} className="text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">Quản Trị Viên</h1>
              <p className="text-xs text-slate-500 font-medium">Admin Dashboard</p>
            </div>
          </div>

          <h2 className="text-2xl font-bold text-slate-900 mb-2">Đăng nhập hệ thống</h2>
          <p className="text-slate-500 mb-8 font-medium">
            Đăng nhập bằng tài khoản quản trị viên để truy cập bảng điều khiển.
          </p>

          {err && (
            <div className="flex items-center gap-2 bg-red-50 text-red-600 border border-red-200 rounded-lg p-4 mb-6">
              <AlertCircle size={20} />
              <span className="font-semibold text-sm">{err}</span>
            </div>
          )}

          <form onSubmit={onSubmit} className="space-y-5">
            <div className="space-y-2">
              <label className="block text-sm font-bold text-slate-700">Tên tài khoản</label>
              <input
                type="text"
                value={u}
                onChange={(e) => setU(e.target.value)}
                placeholder="admin"
                className="w-full px-4 py-3.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500 transition-shadow"
              />
            </div>

            <div className="space-y-2">
              <label className="block text-sm font-bold text-slate-700">Mật khẩu</label>
              <input
                type="password"
                value={p}
                onChange={(e) => setP(e.target.value)}
                placeholder="Nhập mật khẩu"
                className="w-full px-4 py-3.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500 transition-shadow"
              />
            </div>

            <button
              type="submit"
              disabled={loading || !u || !p}
              className="w-full flex items-center justify-center gap-2 py-4 mt-8 rounded-lg text-white font-bold bg-slate-900 hover:bg-orange-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? (
                <span className="animate-pulse">Đang xử lý...</span>
              ) : (
                <>
                  <LogIn size={20} /> Đăng Nhập Admin
                </>
              )}
            </button>
          </form>
        </div>
      </div>

      {/* RIGHT: Decorative */}
      <div className="hidden lg:block w-1/2 relative bg-slate-900">
        <img
          src="https://images.unsplash.com/photo-1555939594-58d7cb561ad1?q=80&w=2670&auto=format&fit=crop"
          alt="Food Background"
          className="absolute inset-0 w-full h-full object-cover opacity-80"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-slate-900 via-slate-900/40 to-transparent flex flex-col justify-end p-16">
          <h2 className="text-4xl font-bold text-white mb-4">Quản lý toàn diện<br/>phố ẩm thực.</h2>
          <p className="text-lg text-slate-200 w-3/4">Bảng điều khiển quản trị hệ thống thuyết minh tự động đa ngôn ngữ.</p>
        </div>
      </div>
    </div>
  )
}
