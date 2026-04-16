import { Link, useNavigate, useLocation } from 'react-router'
import { useAuthStore } from '@/store/authStore'
import { LayoutGrid, MapPin, Globe, Route, Mic2, BarChart2, Activity, LogOut, Menu, X, Languages, KeyRound, Users } from 'lucide-react'
import { useState } from 'react'

const adminNav = [
  { to: '/', label: 'Tổng Quan', icon: LayoutGrid },
  { to: '/pois', label: 'Quán Ăn', icon: MapPin },
  { to: '/map', label: 'Bản Đồ', icon: Globe },
  { to: '/tours', label: 'Lộ Trình', icon: Route },
  { to: '/translations', label: 'Bản Dịch', icon: Languages },
  { to: '/audio', label: 'Giọng Đọc', icon: Mic2 },
  { to: '/analytics', label: 'Thống Kê', icon: BarChart2 },
  { to: '/history', label: 'Lịch Sử', icon: Activity },
  { to: '/users', label: 'Tài Khoản', icon: Users },
  { to: '/change-password', label: 'Đổi Mật Khẩu', icon: KeyRound },
]

const ownerNav = [
  { to: '/', label: 'Tổng Quan', icon: LayoutGrid },
  { to: '/pois', label: 'Quán Ăn Của Tôi', icon: MapPin },
  { to: '/map', label: 'Bản Đồ', icon: Globe },
  { to: '/tours', label: 'Lộ Trình', icon: Route },
  { to: '/translations', label: 'Bản Dịch', icon: Languages },
  { to: '/audio', label: 'Giọng Đọc', icon: Mic2 },
  { to: '/change-password', label: 'Đổi Mật Khẩu', icon: KeyRound },
]

export function Layout({ children }: { children: React.ReactNode }) {
  const { role, clear } = useAuthStore()
  const navigate = useNavigate()
  const location = useLocation()
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const isAdmin = role === 'Admin'
  const nav = isAdmin ? adminNav : ownerNav

  const toggleMenu = () => setMobileMenuOpen(!mobileMenuOpen)

  return (
    <div className="min-h-screen bg-transparent font-sans text-slate-900 flex flex-col md:flex-row">

      {/* SIDEBAR DÀNH CHO DESKTOP */}
      <aside className="hidden md:flex flex-col w-72 bg-linear-to-b from-slate-950 to-slate-900 text-white min-h-screen sticky top-0 border-r border-slate-800/80">
        <div className="p-6 flex items-center gap-3 border-b border-slate-800/90">
          <div className={`w-11 h-11 ${isAdmin ? 'bg-orange-500' : 'bg-emerald-500'} rounded-xl flex items-center justify-center text-xl shadow-xl`}>
            🍲
          </div>
          <div>
            <h1 className="text-[1.75rem] leading-8 font-black tracking-tight text-white">Vĩnh Khánh</h1>
            <p className={`text-xs ${isAdmin ? 'text-orange-300' : 'text-emerald-300'} font-semibold tracking-wide uppercase`}>
              {isAdmin ? 'Bảng điều khiển admin' : 'Bảng điều khiển chủ quán'}
            </p>
          </div>
        </div>

        <nav className="flex-1 py-6 px-4 space-y-2 overflow-y-auto">
          {nav.map((x) => {
            const isActive = location.pathname === x.to || (x.to !== '/' && location.pathname.startsWith(x.to))
            return (
              <Link
                key={x.to}
                to={x.to}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-semibold transition-all ${
                  isActive
                    ? `${isAdmin ? 'bg-linear-to-r from-orange-500 to-orange-600' : 'bg-linear-to-r from-emerald-500 to-emerald-600'} text-white shadow-lg`
                    : 'text-slate-300 hover:text-white hover:bg-slate-800/80'
                }`}
              >
                <x.icon size={20} strokeWidth={isActive ? 2.5 : 2} />
                {x.label}
              </Link>
            )
          })}
        </nav>

        <div className="p-4 border-t border-slate-800/90">
          <div className="flex items-center justify-between mb-4 px-2">
            <div>
              <p className="text-xs text-slate-500 uppercase font-bold tracking-wider mb-1">Tài khoản</p>
              <p className="text-sm font-semibold text-slate-100">
                {isAdmin ? 'Quản trị viên' : 'Chủ quán'}
              </p>
            </div>
            <button
              onClick={() => { clear(); navigate('/role-select', { replace: true }) }}
              className="p-2 bg-slate-800 text-slate-300 rounded-lg hover:text-white hover:bg-rose-600 transition-colors"
              title="Đăng xuất"
            >
              <LogOut size={20} />
            </button>
          </div>
        </div>
      </aside>

      {/* HEADER DÀNH CHO MOBILE */}
      <header className="md:hidden flex items-center justify-between bg-slate-950 text-white p-4 sticky top-0 z-50 shadow-md">
        <div className="flex items-center gap-2">
          <span className="text-2xl">🍲</span>
          <span className="font-bold text-lg">Vĩnh Khánh</span>
          <span className={`text-xs px-2 py-0.5 rounded-full ${isAdmin ? 'bg-orange-600' : 'bg-emerald-600'} font-bold`}>
            {isAdmin ? 'Admin' : 'Owner'}
          </span>
        </div>
        <button onClick={toggleMenu} className="p-2 text-slate-300 hover:text-white focus:outline-none">
          {mobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
        </button>
      </header>

      {/* MENU MOBILE DROP DOWN */}
      {mobileMenuOpen && (
        <div className="md:hidden fixed inset-0 top-16 z-40 bg-slate-900 text-white flex flex-col pt-4">
          <nav className="flex-1 px-4 space-y-2">
            {nav.map((x) => {
              const isActive = location.pathname === x.to || (x.to !== '/' && location.pathname.startsWith(x.to))
              return (
                <Link
                  key={x.to}
                  to={x.to}
                  onClick={() => setMobileMenuOpen(false)}
                  className={`flex items-center gap-3 px-4 py-4 rounded-lg text-base font-semibold ${
                    isActive
                      ? `${isAdmin ? 'bg-orange-600' : 'bg-emerald-600'} text-white`
                      : 'text-slate-300 hover:bg-slate-800'
                  }`}
                >
                  <x.icon size={22} />
                  {x.label}
                </Link>
              )
            })}
          </nav>
          <div className="p-6 border-t border-slate-800 flex justify-between items-center">
            <span className="font-bold text-slate-300">
              Vai trò: <span className={`${isAdmin ? 'text-orange-500' : 'text-emerald-500'}`}>{isAdmin ? 'Admin' : 'Chủ Quán'}</span>
            </span>
            <button
              onClick={() => { clear(); navigate('/role-select', { replace: true }) }}
              className="flex items-center gap-2 px-4 py-2 bg-red-600 rounded-lg text-white font-bold"
            >
              <LogOut size={18} /> Đăng Xuất
            </button>
          </div>
        </div>
      )}

      {/* KHÔNG GIAN NỘI DUNG CHÍNH */}
      <main className="flex-1 flex flex-col min-w-0">
        <div className="flex-1 p-4 md:p-8 overflow-y-auto w-full max-w-[1340px] mx-auto">
          {children}
        </div>
      </main>

    </div>
  )
}
