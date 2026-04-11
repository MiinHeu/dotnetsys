import { useState } from 'react'
import { useNavigate, Link } from 'react-router'
import { api } from '@/lib/api'
import { UserPlus, AlertCircle, ArrowLeft, Store, CheckCircle } from 'lucide-react'

export function OwnerRegister() {
  const [u, setU] = useState('')
  const [p, setP] = useState('')
  const [p2, setP2] = useState('')
  const [err, setErr] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)

    if (p !== p2) {
      setErr('Mật khẩu nhập lại không khớp.')
      return
    }

    setLoading(true)
    try {
      await api.post('/api/auth/register-owner', { username: u, password: p })
      setSuccess(true)
    } catch (error: any) {
      const msg = error?.response?.data?.message || 'Đăng ký thất bại. Vui lòng thử lại.'
      setErr(msg)
    } finally {
      setLoading(false)
    }
  }

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-emerald-50 p-6">
        <div className="w-full max-w-md bg-white rounded-2xl shadow-xl p-10 text-center">
          <div className="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <CheckCircle size={32} className="text-emerald-600" />
          </div>
          <h2 className="text-2xl font-bold text-slate-900 mb-3">Đăng ký thành công!</h2>
          <p className="text-slate-500 mb-8">
            Tài khoản <span className="font-bold text-emerald-600">{u}</span> đã được tạo. Bạn có thể đăng nhập ngay.
          </p>
          <button
            onClick={() => navigate('/owner-login', { replace: true })}
            className="w-full py-3.5 rounded-lg text-white font-bold bg-emerald-600 hover:bg-emerald-700 transition-colors"
          >
            Đăng nhập ngay
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-emerald-50 p-6">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-xl p-10">
        <button
          onClick={() => navigate('/owner-login', { replace: true })}
          className="flex items-center gap-2 text-sm text-slate-500 hover:text-emerald-600 font-medium mb-8 transition-colors"
        >
          <ArrowLeft size={16} /> Quay lại đăng nhập
        </button>

        <div className="flex items-center gap-3 mb-8">
          <div className="w-12 h-12 bg-emerald-600 rounded-lg flex items-center justify-center shadow-lg">
            <Store size={24} className="text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">Đăng Ký Chủ Quán</h1>
            <p className="text-xs text-slate-500 font-medium">Tạo tài khoản mới</p>
          </div>
        </div>

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
              placeholder="Ít nhất 3 ký tự"
              className="w-full px-4 py-3.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-shadow"
            />
          </div>

          <div className="space-y-2">
            <label className="block text-sm font-bold text-slate-700">Mật khẩu</label>
            <input
              type="password"
              value={p}
              onChange={(e) => setP(e.target.value)}
              placeholder="Ít nhất 6 ký tự"
              className="w-full px-4 py-3.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-shadow"
            />
          </div>

          <div className="space-y-2">
            <label className="block text-sm font-bold text-slate-700">Nhập lại mật khẩu</label>
            <input
              type="password"
              value={p2}
              onChange={(e) => setP2(e.target.value)}
              placeholder="Xác nhận mật khẩu"
              className="w-full px-4 py-3.5 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 transition-shadow"
            />
          </div>

          <button
            type="submit"
            disabled={loading || !u || !p || !p2}
            className="w-full flex items-center justify-center gap-2 py-4 mt-6 rounded-lg text-white font-bold bg-emerald-600 hover:bg-emerald-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? (
              <span className="animate-pulse">Đang xử lý...</span>
            ) : (
              <>
                <UserPlus size={20} /> Đăng Ký
              </>
            )}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500">
          Đã có tài khoản?{' '}
          <Link to="/owner-login" className="text-emerald-600 font-semibold hover:underline">
            Đăng nhập
          </Link>
        </p>
      </div>
    </div>
  )
}
