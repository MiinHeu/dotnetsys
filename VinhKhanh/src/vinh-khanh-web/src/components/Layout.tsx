import { Link, useNavigate, useLocation } from 'react-router'
import { useAuthStore } from '@/store/authStore'
import { Home, MapPin, Map, Route, Languages, BarChart3, History, AudioLines, LogOut } from 'lucide-react'

const nav = [
  { to: '/', label: 'Tổng Quan', icon: Home },
  { to: '/pois', label: 'Quán Ăn', icon: MapPin },
  { to: '/map', label: 'Bản Đồ', icon: Map },
  { to: '/tours', label: 'Lộ Trình', icon: Route },
  { to: '/translations', label: 'Ngôn Ngữ', icon: Languages },
  { to: '/audio', label: 'Giọng Đọc', icon: AudioLines },
  { to: '/analytics', label: 'Thống Kê', icon: BarChart3 },
  { to: '/history', label: 'Lịch Sử', icon: History },
]

export function Layout({ children }: { children: React.ReactNode }) {
  const { role, clear } = useAuthStore()
  const navigate = useNavigate()
  const location = useLocation()

  return (
    <div className="min-h-screen w-full" style={{ fontFamily: "'Nunito', sans-serif", background: 'linear-gradient(135deg, #FFFBF5 0%, #FFF3E6 50%, #F5E6D0 100%)' }}>
      {/* Header */}
      <header style={{ background: '#FFFFFF', borderBottom: '1px solid #E8D5BD', boxShadow: '0 1px 3px rgba(107,66,38,0.08)' }}>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center space-x-4">
              <div className="w-11 h-11 rounded-xl flex items-center justify-center shadow-md text-xl" style={{ background: 'linear-gradient(135deg, #6B4226, #8B5E3C)' }}>
                🍜
              </div>
              <div>
                <h1 style={{ fontSize: '22px', margin: 0, fontWeight: 800, color: '#6B4226', letterSpacing: '-0.3px', fontFamily: "'Nunito', sans-serif" }}>
                  Phố Ẩm Thực Vĩnh Khánh
                </h1>
                <p style={{ color: '#A0704D', fontSize: '12px', fontWeight: 600, margin: '1px 0 0 0' }}>Hệ thống quản lý nội dung</p>
              </div>
            </div>
            <div className="flex items-center space-x-4">
              <span style={{ fontSize: '14px', color: '#6B5B4F' }}>
                Xin chào, <span style={{ fontWeight: 700, color: '#D4722E' }}>{role === 'Admin' ? 'Quản trị viên' : role === 'Owner' ? 'Chủ quán' : role}</span>
              </span>
              <button
                type="button"
                className="inline-flex items-center px-4 py-2.5 text-sm rounded-xl text-white transition-all duration-200"
                style={{
                  background: 'linear-gradient(135deg, #6B4226, #8B5E3C)',
                  fontWeight: 700,
                  fontFamily: "'Nunito', sans-serif",
                  boxShadow: '0 2px 8px rgba(107,66,38,0.25)',
                }}
                onMouseEnter={(e) => { e.currentTarget.style.boxShadow = '0 4px 12px rgba(107,66,38,0.4)'; e.currentTarget.style.transform = 'translateY(-1px)' }}
                onMouseLeave={(e) => { e.currentTarget.style.boxShadow = '0 2px 8px rgba(107,66,38,0.25)'; e.currentTarget.style.transform = 'translateY(0)' }}
                onClick={() => { clear(); navigate('/login', { replace: true }) }}
              >
                <LogOut className="h-4 w-4 mr-2" />
                Đăng Xuất
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Navigation */}
      <nav style={{ background: '#FFFFFF', borderBottom: '1px solid #F0E0CC', boxShadow: '0 1px 2px rgba(107,66,38,0.04)' }}>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex space-x-1 overflow-x-auto py-2.5">
            {nav.map((x) => {
              const isActive = location.pathname === x.to || (x.to !== '/' && location.pathname.startsWith(x.to))
              return (
                <Link
                  key={x.to}
                  to={x.to}
                  className="inline-flex items-center px-4 py-2.5 text-sm rounded-xl transition-all duration-200 whitespace-nowrap"
                  style={{
                    fontWeight: isActive ? 700 : 600,
                    fontFamily: "'Nunito', sans-serif",
                    background: isActive ? 'linear-gradient(135deg, #6B4226, #8B5E3C)' : 'transparent',
                    color: isActive ? '#FFFFFF' : '#6B5B4F',
                    boxShadow: isActive ? '0 2px 8px rgba(107,66,38,0.25)' : 'none',
                  }}
                  onMouseEnter={(e) => { if (!isActive) { e.currentTarget.style.background = '#F5E6D0'; e.currentTarget.style.color = '#6B4226' } }}
                  onMouseLeave={(e) => { if (!isActive) { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = '#6B5B4F' } }}
                >
                  <x.icon className="h-4 w-4 mr-2" style={{ opacity: isActive ? 0.85 : 0.6 }} />
                  {x.label}
                </Link>
              )
            })}
          </div>
        </div>
      </nav>

      {/* Main content */}
      <main className="flex-1">
        <div className="max-w-7xl mx-auto py-6 px-4 sm:px-6 lg:px-8">
          {children}
        </div>
      </main>
    </div>
  )
}