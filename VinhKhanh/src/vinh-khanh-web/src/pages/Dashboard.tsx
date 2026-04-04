import { useQuery } from '@tanstack/react-query'
import { api, type Poi } from '@/lib/api'
import { Route, Users, TrendingUp, BarChart3, UserCog, UtensilsCrossed } from 'lucide-react'
import { useNavigate } from 'react-router'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'

export function Dashboard() {
  const navigate = useNavigate()

  const pois = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  const tours = useQuery({
    queryKey: ['tours', 'vi'],
    queryFn: async () => (await api.get('/api/tour?lang=vi')).data as unknown[],
  })

  const analyticsTop = useQuery({
    queryKey: ['analytics', 'top'],
    queryFn: async () =>
      (await api.get<{ poiId: number; count: number; avgDuration: number }[]>('/api/analytics/top?days=30'))
        .data,
  })

  const chartData =
    analyticsTop.data?.map((a) => {
      const matchedPoi = pois.data?.find((p) => p.id === a.poiId)
      return {
        name: matchedPoi?.name || `Mã quán #${a.poiId}`,
        count: a.count,
        duration: Math.round(a.avgDuration || 0),
      }
    }) || []

  const cards = [
    {
      label: 'Quán ăn',
      value: pois.isLoading ? '...' : pois.data?.length ?? 0,
      sub: 'Đang hoạt động',
      desc: 'Các quán ăn đã được đăng ký trên hệ thống',
      icon: UtensilsCrossed,
      bg: 'bg-orange-600',
    },
    {
      label: 'Lộ trình',
      value: tours.isLoading ? '...' : tours.data?.length ?? 0,
      sub: 'Đang mở',
      desc: 'Lộ trình khám phá ẩm thực cho du khách',
      icon: Route,
      bg: 'bg-blue-600',
    },
    {
      label: 'Khách tham quan',
      value: '1,234',
      sub: 'Tổng lượt',
      desc: 'Lượt du khách sử dụng ứng dụng',
      icon: Users,
      bg: 'bg-teal-600',
    },
    {
      label: 'Lượt nghe TTS',
      value: analyticsTop.isLoading ? '...' : analyticsTop.data?.reduce((a, b) => a + b.count, 0) ?? 0,
      sub: 'Số lần phát',
      desc: 'Lượt thuyết minh tự động đã phát',
      icon: TrendingUp,
      bg: 'bg-rose-600',
    },
  ]

  const actions = [
    { label: 'Thêm quán ăn', path: '/pois/new' },
    { label: 'Tạo lộ trình', path: '/tours/new' },
    { label: 'Quản lý ngôn ngữ', path: '/translations' },
    { label: 'Báo cáo chi tiết', path: '/analytics' },
  ]

  return (
    <div className="space-y-6">
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm flex items-center justify-between">
        <div>
          <h2 className="mb-1 flex items-center gap-3 text-2xl font-bold text-slate-900 tracking-tight">
            <BarChart3 className="text-slate-400" />
            Tổng quan hệ thống
          </h2>
          <p className="font-medium text-slate-500">Quản lý nội dung phố ẩm thực Vĩnh Khánh</p>
        </div>
        <div className="hidden text-right sm:block">
          <p className="text-sm font-bold uppercase tracking-widest text-slate-400">Cập nhật lúc</p>
          <p className="text-lg font-black text-slate-900">{new Date().toLocaleDateString('vi-VN')}</p>
        </div>
      </div>

      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(({ label, value, sub, desc, bg, icon: Icon }) => (
          <div
            key={label}
            className={`${bg} flex flex-col rounded-xl p-6 text-white shadow-md transition-shadow duration-300 hover:shadow-lg`}
          >
            <div className="mb-4 flex items-center justify-between">
              <p className="text-sm font-bold uppercase tracking-wider text-white/80">{label}</p>
              <Icon className="opacity-80" size={24} />
            </div>
            <p className="mb-6 text-4xl font-black">{value}</p>
            <div className="mt-auto border-t border-white/20 pt-4">
              <p className="text-sm font-bold text-white">{sub}</p>
              <p className="mt-1 text-xs font-medium text-white/60">{desc}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-1">
          <h3 className="mb-6 flex items-center gap-2 text-lg font-bold text-slate-900">
            <UserCog className="text-slate-400" /> Thao tác nhanh
          </h3>
          <div className="grid gap-3">
            {actions.map(({ label, path }) => (
              <button
                key={label}
                className="flex items-center gap-3 rounded-xl border border-slate-200 bg-slate-50 px-5 py-4 font-bold text-slate-700 transition-colors hover:border-slate-900"
                onClick={() => navigate(path)}
              >
                {label}
              </button>
            ))}
          </div>
        </div>

        <div className="flex min-h-[350px] flex-col rounded-xl border border-slate-200 bg-white p-6 shadow-sm lg:col-span-2">
          <div className="mb-6 flex items-center justify-between">
            <h3 className="flex items-center gap-2 text-lg font-bold text-slate-900">
              <BarChart3 className="text-slate-400" /> Top địa điểm được lắng nghe
            </h3>
            <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-bold uppercase tracking-wider text-slate-600">
              30 ngày qua
            </span>
          </div>

          <div className="h-full w-full flex-1">
            {analyticsTop.isLoading || pois.isLoading ? (
              <div className="flex h-full w-full items-center justify-center font-bold text-slate-500">
                Đang tải dữ liệu biểu đồ...
              </div>
            ) : chartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData} margin={{ top: 20, right: 0, left: -20, bottom: 0 }}>
                  <XAxis dataKey="name" stroke="#94a3b8" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis
                    stroke="#94a3b8"
                    fontSize={12}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(value) => `${value}`}
                  />
                  <Tooltip
                    cursor={{ fill: '#f1f5f9' }}
                    contentStyle={{
                      borderRadius: '12px',
                      border: 'none',
                      boxShadow: '0 10px 15px -3px rgb(0 0 0 / 0.1)',
                    }}
                  />
                  <Bar dataKey="count" name="Số lượt nghe" fill="#0f172a" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex h-full w-full items-center justify-center font-medium text-slate-500">
                Chưa có dữ liệu lượt nghe nào trong 30 ngày qua.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
