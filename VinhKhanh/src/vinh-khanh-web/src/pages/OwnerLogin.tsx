import { useState } from 'react'
import { useNavigate, Link } from 'react-router'
import { api } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'
import { LogIn, AlertCircle, ArrowLeft, Store } from 'lucide-react'

export function OwnerLogin() {
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
      if (data.role !== 'Owner') {
        setErr('Tài khoản này không phải Chủ Quán. Vui lòng sử dụng cổng Admin.')
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
            className="flex items-center gap-2 text-sm text-slate-500 hover:text-emerald-600 font-medium mb-8 transition-colors"
          >
            <ArrowLeft size={16} /> Quay lại chọn vai trò
          </button>

          <div className="flex items-center gap-3 mb-8">
            <div className="w-12 h-12 bg-emerald-600 rounded-lg flex items-center justify-center shadow-lg">
              <Store size={24} className="text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">Chủ Quán</h1>
              <p className="text-xs text-slate-500 font-medium">Owner Dashboard</p>
            </div>
          </div>

          <h2 className="text-2xl font-bold text-slate-900 mb-2">Đăng nhập</h2>
          <p className="text-slate-500 mb-8 font-medium">
            Đăng nhập để quản lý thông tin quán ăn của bạn.
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
                placeholder="Ví dụ: owner1"
                className="w-full px-4 py-3.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-shadow"
              />
            </div>

            <div className="space-y-2">
              <label className="block text-sm font-bold text-slate-700">Mật khẩu</label>
              <input
                type="password"
                value={p}
                onChange={(e) => setP(e.target.value)}
                placeholder="Nhập mật khẩu"
                className="w-full px-4 py-3.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-shadow"
              />
            </div>

            <button
              type="submit"
              disabled={loading || !u || !p}
              className="w-full flex items-center justify-center gap-2 py-4 mt-6 rounded-lg text-white font-bold bg-emerald-600 hover:bg-emerald-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? (
                <span className="animate-pulse">Đang xử lý...</span>
              ) : (
                <>
                  <LogIn size={20} /> Đăng Nhập
                </>
              )}
            </button>
          </form>

          {/* Links */}
          <div className="mt-6 flex flex-col gap-3 text-center">
            <Link to="/owner-register" className="text-sm text-emerald-600 font-semibold hover:underline">
              Chưa có tài khoản? Đăng ký ngay
            </Link>
            <Link to="/owner-forgot-password" className="text-sm text-slate-500 hover:text-slate-700 font-medium hover:underline">
              Quên mật khẩu?
            </Link>
          </div>
        </div>
      </div>

      {/* RIGHT: Decorative */}
      <div className="hidden lg:block w-1/2 relative bg-emerald-900">
        <img
          src="https://images.unsplash.com/photo-1504674900247-0877df9cc836?q=80&w=2670&auto=format&fit=crop"
          alt="Vietnamese Food"
          className="absolute inset-0 w-full h-full object-cover opacity-70"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-emerald-900 via-emerald-900/40 to-transparent flex flex-col justify-end p-16">
          <h2 className="text-4xl font-bold text-white mb-4">Kể câu chuyện<br/>của quán bạn.</h2>
          <p className="text-lg text-emerald-100 w-3/4">Quản lý nội dung, upload giọng đọc, chỉnh sửa mô tả quán ăn của bạn trên phố ẩm thực.</p>
        </div>
      </div>
    </div>
  )
}
