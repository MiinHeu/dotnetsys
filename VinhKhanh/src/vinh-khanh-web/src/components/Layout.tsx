import { Link, useNavigate } from 'react-router'
import { useAuthStore } from '@/store/authStore'
import { Home, MapPin, Settings, BarChart3, History, AudioLines, LogOut } from 'lucide-react'

const nav = [
  { to: '/', label: 'Tổng quan', icon: Home },
  { to: '/pois', label: 'Điểm Thú Vị', icon: MapPin },
  { to: '/map', label: 'Bản Đồ', icon: MapPin },
  { to: '/tours', label: 'Tour Du Lịch', icon: MapPin },
  { to: '/translations', label: 'Bản Dịch', icon: Settings },
  { to: '/audio', label: 'Quản Lý Audio', icon: AudioLines },
  { to: '/analytics', label: 'Thống Kê', icon: BarChart3 },
  { to: '/history', label: 'Lịch Sử Dụng', icon: History },
]

export function Layout({ children }: { children: React.ReactNode }) {
  const { role, clear } = useAuthStore()
  const navigate = useNavigate()

  return (
    <div className="vk-admin min-h-screen bg-gradient-to-br from-orange-50 to-amber-50 dark:from-gray-900 dark:to-gray-800 w-full">
      <header className="bg-white dark:bg-gray-800 shadow-lg border-b border-orange-100 dark:border-gray-700">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div className="flex items-center space-x-4">
              <div className="flex items-center">
                <div className="w-10 h-10 bg-gradient-to-r from-orange-500 to-amber-500 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">🍜</span>
                </div>
                <div>
                  <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
                    Phố Ẩm Thực Vĩnh Khánh
                  </h1>
                  <p className="text-sm text-gray-500 dark:text-gray-400">Hệ thống quản lý nội dung</p>
                </div>
              </div>
              <div className="flex items-center space-x-3">
                <span className="text-sm text-gray-700 dark:text-gray-300">
                  Xin chào, <span className="font-semibold text-orange-600 dark:text-orange-400">{role}</span>
                </span>
                <button
                  type="button"
                  className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-gradient-to-r from-orange-600 to-amber-600 hover:from-orange-700 hover:to-amber-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-orange-500 transition-all duration-200"
                  onClick={() => {
                    clear()
                    navigate('/login', { replace: true })
                  }}
                >
                  <LogOut className="h-4 w-4 mr-2" />
                  Đăng xuất
                </button>
              </div>
            </div>
          </div>
        </div>
        </header>

        <nav className="bg-white dark:bg-gray-800 shadow-sm">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex space-x-8 overflow-x-auto py-4">
              {nav.map((x) => (
                <Link
                  key={x.to}
                  to={x.to}
                  className="group inline-flex items-center px-3 py-2 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 hover:text-white hover:bg-orange-600 dark:hover:text-white dark:bg-gray-900 dark:hover:bg-orange-600 transition-all duration-200"
                >
                  <x.icon className="h-5 w-5 mr-2 text-gray-400 group-hover:text-orange-300" />
                  {x.label}
                </Link>
              ))}
            </div>
          </div>
        </nav>

        <main className="flex-1 bg-white dark:bg-gray-900">
          <div className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
            {children}
          </div>
        </main>
      </div>
  )
}