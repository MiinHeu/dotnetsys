import { useNavigate } from 'react-router'
import { useAuthStore } from '@/store/authStore'
import { Shield, Store } from 'lucide-react'

export function RoleSelect() {
  const setPortal = useAuthStore((s) => s.setPortal)
  const navigate = useNavigate()

  function pick(portal: 'admin' | 'owner') {
    setPortal(portal)
    navigate(portal === 'admin' ? '/admin-login' : '/owner-login', { replace: true })
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 p-6">
      <div className="w-full max-w-3xl">
        {/* Header */}
        <div className="text-center mb-12">
          <div className="inline-flex items-center gap-3 mb-6">
            <div className="w-14 h-14 bg-orange-600 rounded-2xl flex items-center justify-center text-3xl shadow-xl shadow-orange-600/30">
              🍲
            </div>
          </div>
          <h1 className="text-4xl font-bold text-white tracking-tight mb-3">
            Phố Ẩm Thực Vĩnh Khánh
          </h1>
          <p className="text-slate-400 text-lg font-medium">
            Hệ thống quản lý thuyết minh tự động đa ngôn ngữ
          </p>
        </div>

        {/* Role Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Admin Card */}
          <button
            onClick={() => pick('admin')}
            className="group relative bg-slate-800/60 backdrop-blur border border-slate-700/50 rounded-2xl p-8 text-left transition-all duration-300 hover:bg-slate-700/60 hover:border-orange-500/50 hover:shadow-2xl hover:shadow-orange-600/10 hover:-translate-y-1 focus:outline-none focus:ring-2 focus:ring-orange-500"
          >
            <div className="w-14 h-14 bg-orange-600/15 border border-orange-500/30 rounded-xl flex items-center justify-center mb-6 group-hover:bg-orange-600/25 transition-colors">
              <Shield size={28} className="text-orange-500" />
            </div>
            <h2 className="text-2xl font-bold text-white mb-2">Quản Trị Viên</h2>
            <p className="text-slate-400 text-sm leading-relaxed">
              Quản lý toàn bộ hệ thống: POI, Tour, Analytics, Người dùng, Cấu hình hệ thống.
            </p>
            <div className="mt-6 flex items-center gap-2 text-orange-500 font-semibold text-sm group-hover:gap-3 transition-all">
              Đăng nhập Admin
              <span className="text-lg">→</span>
            </div>
          </button>

          {/* Owner Card */}
          <button
            onClick={() => pick('owner')}
            className="group relative bg-slate-800/60 backdrop-blur border border-slate-700/50 rounded-2xl p-8 text-left transition-all duration-300 hover:bg-slate-700/60 hover:border-emerald-500/50 hover:shadow-2xl hover:shadow-emerald-600/10 hover:-translate-y-1 focus:outline-none focus:ring-2 focus:ring-emerald-500"
          >
            <div className="w-14 h-14 bg-emerald-600/15 border border-emerald-500/30 rounded-xl flex items-center justify-center mb-6 group-hover:bg-emerald-600/25 transition-colors">
              <Store size={28} className="text-emerald-500" />
            </div>
            <h2 className="text-2xl font-bold text-white mb-2">Chủ Quán</h2>
            <p className="text-slate-400 text-sm leading-relaxed">
              Quản lý thông tin quán ăn, upload audio, chỉnh sửa mô tả và bản dịch.
            </p>
            <div className="mt-6 flex items-center gap-2 text-emerald-500 font-semibold text-sm group-hover:gap-3 transition-all">
              Đăng nhập / Đăng ký
              <span className="text-lg">→</span>
            </div>
          </button>
        </div>

        {/* Footer */}
        <p className="text-center text-slate-600 text-xs mt-10">
          © 2026 Phố Ẩm Thực Vĩnh Khánh — Quận 4, TP. Hồ Chí Minh
        </p>
      </div>
    </div>
  )
}
