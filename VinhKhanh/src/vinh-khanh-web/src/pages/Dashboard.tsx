import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { MapPin, Route, Users, TrendingUp, Plus, BarChart3, UserCog } from 'lucide-react'
import { useNavigate } from 'react-router'

export function Dashboard() {
  const navigate = useNavigate()
  const pois = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get('/api/poi?lang=vi')).data as unknown[],
  })
  const tours = useQuery({
    queryKey: ['tours', 'vi'],
    queryFn: async () => (await api.get('/api/tour?lang=vi')).data as unknown[],
  })

  const cards = [
    {
      label: 'Quán Ăn',
      value: pois.isLoading ? '…' : pois.data?.length ?? 0,
      sub: 'Đang hoạt động',
      desc: 'Các quán ăn đã được đăng ký trên hệ thống',
      icon: MapPin,
      gradient: 'linear-gradient(135deg, #6B4226, #8B5E3C)',
      shadow: 'rgba(107,66,38,0.35)',
      emoji: '🍜',
    },
    {
      label: 'Lộ Trình',
      value: tours.isLoading ? '…' : tours.data?.length ?? 0,
      sub: 'Đang mở',
      desc: 'Lộ trình khám phá ẩm thực cho du khách',
      icon: Route,
      gradient: 'linear-gradient(135deg, #D4722E, #E8914D)',
      shadow: 'rgba(212,114,46,0.3)',
      emoji: '🗺️',
    },
    {
      label: 'Khách Tham Quan',
      value: '1,234',
      sub: 'Tổng lượt',
      desc: 'Lượt du khách sử dụng ứng dụng',
      icon: Users,
      gradient: 'linear-gradient(135deg, #D4A847, #E8C96A)',
      shadow: 'rgba(212,168,71,0.3)',
      emoji: '👥',
    },
    {
      label: 'Lượt Nghe',
      value: '8.5K',
      sub: 'Trong 30 ngày',
      desc: 'Lượt thuyết minh tự động đã phát',
      icon: TrendingUp,
      gradient: 'linear-gradient(135deg, #C97878, #D4918F)',
      shadow: 'rgba(201,120,120,0.3)',
      emoji: '🎧',
    },
  ]

  const actions = [
    { label: 'Thêm Quán Ăn', icon: Plus, emoji: '🍲', path: '/pois/new', gradient: 'linear-gradient(135deg, #6B4226, #8B5E3C)' },
    { label: 'Tạo Lộ Trình', icon: Route, emoji: '🗺️', path: '/tours/new', gradient: 'linear-gradient(135deg, #D4722E, #E8914D)' },
    { label: 'Quản Lý Ngôn Ngữ', icon: UserCog, emoji: '🌏', path: '/translations', gradient: 'linear-gradient(135deg, #D4A847, #C9A038)' },
    { label: 'Xem Thống Kê', icon: BarChart3, emoji: '📊', path: '/analytics', gradient: 'linear-gradient(135deg, #C97878, #B86868)' },
  ]

  return (
    <div className="space-y-6" style={{ fontFamily: "'Nunito', sans-serif" }}>
      {/* Header */}
      <div className="rounded-2xl p-6" style={{ background: '#FFFFFF', border: '1px solid #E8D5BD', boxShadow: '0 1px 3px rgba(107,66,38,0.06)' }}>
        <div className="flex items-center justify-between">
          <div>
            <h2 className="flex items-center" style={{ fontSize: '26px', fontWeight: 800, color: '#6B4226', margin: 0, letterSpacing: '-0.5px' }}>
              <span className="mr-3 text-3xl">📋</span>
              Tổng Quan Hệ Thống
            </h2>
            <p style={{ color: '#8B7B6B', fontSize: '15px', margin: '6px 0 0 0' }}>Quản lý nội dung phố ẩm thực Vĩnh Khánh</p>
          </div>
          <div className="text-right">
            <p style={{ color: '#A89888', fontSize: '13px' }}>Cập nhật lần cuối</p>
            <p style={{ color: '#6B4226', fontSize: '18px', fontWeight: 700 }}>{new Date().toLocaleDateString('vi-VN')}</p>
          </div>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(({ label, value, sub, desc, icon: Icon, gradient, shadow, emoji }) => (
          <div
            key={label}
            className="rounded-2xl p-6 text-white transition-all duration-300"
            style={{ background: gradient, boxShadow: `0 4px 16px ${shadow}` }}
            onMouseEnter={(e) => { e.currentTarget.style.transform = 'translateY(-3px)'; e.currentTarget.style.boxShadow = `0 8px 24px ${shadow}` }}
            onMouseLeave={(e) => { e.currentTarget.style.transform = 'translateY(0)'; e.currentTarget.style.boxShadow = `0 4px 16px ${shadow}` }}
          >
            <div className="flex items-center justify-between mb-3">
              <div>
                <p style={{ color: 'rgba(255,255,255,0.8)', fontSize: '13px', fontWeight: 600 }}>{label}</p>
                <p style={{ fontSize: '32px', fontWeight: 800, margin: '4px 0 0 0', lineHeight: '1' }}>{value}</p>
              </div>
              <span className="text-3xl" style={{ opacity: 0.6 }}>{emoji}</span>
            </div>
            <div style={{ borderTop: '1px solid rgba(255,255,255,0.2)', paddingTop: '10px', marginTop: '4px' }}>
              <p style={{ color: 'rgba(255,255,255,0.9)', fontSize: '13px', fontWeight: 700 }}>{sub}</p>
              <p style={{ color: 'rgba(255,255,255,0.6)', fontSize: '12px', marginTop: '2px' }}>{desc}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Quick Actions */}
      <div className="rounded-2xl p-6" style={{ background: '#FFFFFF', border: '1px solid #E8D5BD', boxShadow: '0 1px 3px rgba(107,66,38,0.06)' }}>
        <h3 style={{ fontSize: '18px', fontWeight: 800, color: '#6B4226', margin: '0 0 16px 0' }}>
          ⚡ Thao Tác Nhanh
        </h3>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {actions.map(({ label, emoji, path, gradient }) => (
            <button
              key={label}
              className="flex items-center justify-center px-4 py-3.5 rounded-xl text-white transition-all duration-200"
              style={{ background: gradient, fontWeight: 700, fontFamily: "'Nunito', sans-serif", fontSize: '14px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)' }}
              onMouseEnter={(e) => { e.currentTarget.style.transform = 'translateY(-1px)'; e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)' }}
              onMouseLeave={(e) => { e.currentTarget.style.transform = 'translateY(0)'; e.currentTarget.style.boxShadow = '0 2px 8px rgba(0,0,0,0.1)' }}
              onClick={() => navigate(path)}
            >
              <span className="mr-2">{emoji}</span>
              {label}
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
