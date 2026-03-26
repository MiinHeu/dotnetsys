import { Link, Navigate, Outlet, useNavigate } from 'react-router'
import { useAuthStore } from '@/store/authStore'

const nav = [
  { to: '/', label: 'Tổng quan' },
  { to: '/pois', label: 'POI' },
  { to: '/map', label: 'Bản đồ lưới' },
  { to: '/tours', label: 'Tour' },
  { to: '/translations', label: 'Bản dịch' },
  { to: '/audio', label: 'Audio' },
  { to: '/analytics', label: 'Analytics' },
  { to: '/history', label: 'Lịch sử app' },
]

export function Layout() {
  const { token, role, clear } = useAuthStore()
  const navigate = useNavigate()

  if (!token) return <Navigate to="/login" replace />

  return (
    <div className="vk-admin min-h-svh w-full max-w-6xl mx-auto px-4 py-6 text-left">
      <header className="mb-6 flex flex-wrap items-center justify-between gap-4 border-b border-stone-200 pb-4 dark:border-stone-700">
        <div>
          <h1 className="text-xl font-semibold text-stone-900 dark:text-stone-100">
            CMS · Phố ẩm thực Vĩnh Khánh
          </h1>
          <p className="text-sm text-stone-500">Vai trò: {role}</p>
        </div>
        <button
          type="button"
          className="rounded-lg bg-stone-200 px-3 py-1.5 text-sm dark:bg-stone-700"
          onClick={() => {
            clear()
            navigate('/login', { replace: true })
          }}
        >
          Đăng xuất
        </button>
      </header>
      <nav className="mb-6 flex flex-wrap gap-2">
        {nav.map((x) => (
          <Link
            key={x.to}
            to={x.to}
            className="rounded-full bg-orange-100 px-3 py-1 text-sm text-orange-900 hover:bg-orange-200 dark:bg-orange-950 dark:text-orange-100"
          >
            {x.label}
          </Link>
        ))}
      </nav>
      <main>
        <Outlet />
      </main>
    </div>
  )
}
