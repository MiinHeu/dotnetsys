import { useState, useEffect } from 'react'
import { api } from '@/lib/api'
import { KeyRound, AlertCircle, CheckCircle, User as UserIcon, ShieldCheck } from 'lucide-react'

export function ChangePassword() {
  const [profile, setProfile] = useState<any>(null)
  const [current, setCurrent] = useState('')
  const [newPw, setNewPw] = useState('')
  const [confirm, setConfirm] = useState('')
  const [err, setErr] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api.get('/api/auth/profile')
      .then(res => setProfile(res.data))
      .catch(err => console.error('Lỗi lấy profile:', err))
  }, [])

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    setSuccess(false)

    if (newPw !== confirm) {
      setErr('Mật khẩu mới nhập lại không khớp.')
      return
    }

    setLoading(true)
    try {
      await api.put('/api/auth/change-password', {
        currentPassword: current,
        newPassword: newPw,
      })
      setSuccess(true)
      setCurrent('')
      setNewPw('')
      setConfirm('')
    } catch (error: any) {
      const msg = error?.response?.data?.message || 'Đổi mật khẩu thất bại.'
      setErr(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-lg mx-auto">
      <div className="flex items-center gap-3 mb-8">
        <div className="w-10 h-10 bg-orange-100 rounded-lg flex items-center justify-center">
          <KeyRound size={20} className="text-orange-600" />
        </div>
        <h1 className="text-2xl font-bold text-slate-900">Đổi Mật Khẩu</h1>
      </div>

      {success && (
        <div className="flex items-center gap-2 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-lg p-4 mb-6">
          <CheckCircle size={20} />
          <span className="font-semibold text-sm">Đổi mật khẩu thành công! Mật khẩu mới đã được lưu.</span>
        </div>
      )}

      {err && (
        <div className="flex items-center gap-2 bg-red-50 text-red-600 border border-red-200 rounded-lg p-4 mb-6">
          <AlertCircle size={20} />
          <span className="font-semibold text-sm">{err}</span>
        </div>
      )}

      {/* Thông tin cá nhân */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-8 mb-8">
        <div className="flex items-center gap-2 mb-6 pb-4 border-b border-slate-100">
          <UserIcon size={18} className="text-slate-500" />
          <h2 className="text-lg font-bold text-slate-800">Thông tin cá nhân</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-400 uppercase tracking-wider">Mã định danh (ID)</label>
            <div className="flex items-center gap-2 text-slate-900 font-bold">
              <span className="bg-slate-100 px-2 py-1 rounded text-orange-700 font-mono text-sm border border-slate-200">
                {profile?.displayId || 'OW----'}
              </span>
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-400 uppercase tracking-wider">Tên tài khoản</label>
            <div className="text-slate-900 font-medium">{profile?.username || '---'}</div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-400 uppercase tracking-wider">Tên chủ quán</label>
            <div className="text-slate-900 font-medium">{profile?.fullName || '---'}</div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-400 uppercase tracking-wider">Email liên hệ</label>
            <div className="text-slate-900 font-medium">{profile?.email || '---'}</div>
          </div>
        </div>
      </div>

      <form onSubmit={onSubmit} className="bg-white rounded-xl border border-slate-200 shadow-sm p-8 space-y-5">
        <div className="flex items-center gap-2 mb-2">
          <ShieldCheck size={18} className="text-slate-500" />
          <h2 className="text-lg font-bold text-slate-800">Thay đổi mật khẩu</h2>
        </div>
        <div className="space-y-2">
          <label className="block text-sm font-bold text-slate-700">Mật khẩu hiện tại</label>
          <input
            type="password"
            value={current}
            onChange={(e) => setCurrent(e.target.value)}
            placeholder="Nhập mật khẩu hiện tại"
            className="w-full px-4 py-3 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500 transition-shadow"
          />
        </div>

        <div className="space-y-2">
          <label className="block text-sm font-bold text-slate-700">Mật khẩu mới</label>
          <input
            type="password"
            value={newPw}
            onChange={(e) => setNewPw(e.target.value)}
            placeholder="Ít nhất 6 ký tự"
            className="w-full px-4 py-3 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500 transition-shadow"
          />
        </div>

        <div className="space-y-2">
          <label className="block text-sm font-bold text-slate-700">Nhập lại mật khẩu mới</label>
          <input
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            placeholder="Xác nhận mật khẩu mới"
            className="w-full px-4 py-3 bg-slate-50 border border-slate-300 rounded-lg text-slate-900 font-medium placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-orange-500 transition-shadow"
          />
        </div>

        <button
          type="submit"
          disabled={loading || !current || !newPw || !confirm}
          className="w-full flex items-center justify-center gap-2 py-3.5 mt-4 rounded-lg text-white font-bold bg-orange-600 hover:bg-orange-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? (
            <span className="animate-pulse">Đang xử lý...</span>
          ) : (
            <>
              <KeyRound size={18} /> Xác Nhận Đổi Mật Khẩu
            </>
          )}
        </button>
      </form>
    </div>
  )
}
