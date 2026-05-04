import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { Clock, Smartphone, Route, MapPin, Repeat } from 'lucide-react'

type SessionRow = {
  id: number
  sessionId: string
  deviceModel: string
  devicePlatform: string
  osVersion: string
  appVersion: string
  manufacturer: string
  startedAt: string
  endedAt: string | null
  durationMinutes: number
  poisVisited: number
  distanceMeters: number
  languageUsed: string
  isReturning: boolean
  isActive: boolean
}

type SessionStats = {
  totalSessions: number
  avgDurationMinutes: number
  avgPoisVisited: number
  avgDistanceMeters: number
  returningRate: number
  platformBreakdown: { platform: string; count: number }[]
  topDevices: { model: string; count: number }[]
  topManufacturers: { manufacturer: string; count: number }[]
  languageBreakdown: { language: string; count: number }[]
}

type PeakHour = {
  hour: number
  count: number
}

const COLORS = ['#ea580c', '#0ea5e9', '#84cc16', '#eab308', '#ec4899', '#8b5cf6']

export function SessionsPage() {
  const statsQ = useQuery({
    queryKey: ['sessions', 'stats'],
    queryFn: async () => (await api.get<SessionStats>('/api/admin/sessions/stats?days=30')).data,
  })

  const peakQ = useQuery({
    queryKey: ['sessions', 'peak'],
    queryFn: async () => (await api.get<PeakHour[]>('/api/admin/peak-hours?days=30')).data,
  })

  const sessionsQ = useQuery({
    queryKey: ['sessions', 'list'],
    queryFn: async () =>
      (await api.get<{ items: SessionRow[]; total: number }>('/api/admin/sessions?page=1&size=100&days=7')).data,
  })

  const stats = statsQ.data

  return (
    <div className="space-y-6">
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <h2 className="mb-1 flex items-center gap-3 text-2xl font-bold tracking-tight text-slate-900">
          <Smartphone className="text-slate-400" />
          Phiên hoạt động du khách
        </h2>
        <p className="font-medium text-slate-500">
          Theo dõi thông tin thiết bị và hành vi tham quan (30 ngày qua)
        </p>
      </div>

      {/* Thẻ thống kê */}
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <div className="flex flex-col rounded-xl bg-orange-600 p-6 text-white shadow-md">
          <div className="mb-4 flex items-center justify-between">
            <p className="text-sm font-bold uppercase tracking-wider text-white/80">Thời gian TB</p>
            <Clock className="opacity-80" size={24} />
          </div>
          <p className="text-4xl font-black">{stats?.avgDurationMinutes || 0} <span className="text-xl font-medium">phút</span></p>
        </div>
        <div className="flex flex-col rounded-xl bg-blue-600 p-6 text-white shadow-md">
          <div className="mb-4 flex items-center justify-between">
            <p className="text-sm font-bold uppercase tracking-wider text-white/80">Quãng đường TB</p>
            <Route className="opacity-80" size={24} />
          </div>
          <p className="text-4xl font-black">{stats?.avgDistanceMeters || 0} <span className="text-xl font-medium">m</span></p>
        </div>
        <div className="flex flex-col rounded-xl bg-teal-600 p-6 text-white shadow-md">
          <div className="mb-4 flex items-center justify-between">
            <p className="text-sm font-bold uppercase tracking-wider text-white/80">Điểm dừng TB</p>
            <MapPin className="opacity-80" size={24} />
          </div>
          <p className="text-4xl font-black">{stats?.avgPoisVisited || 0} <span className="text-xl font-medium">quán</span></p>
        </div>
        <div className="flex flex-col rounded-xl bg-rose-600 p-6 text-white shadow-md">
          <div className="mb-4 flex items-center justify-between">
            <p className="text-sm font-bold uppercase tracking-wider text-white/80">Tỷ lệ quay lại</p>
            <Repeat className="opacity-80" size={24} />
          </div>
          <p className="text-4xl font-black">{stats?.returningRate || 0} <span className="text-xl font-medium">%</span></p>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Biểu đồ Android/iOS */}
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h3 className="mb-4 font-bold text-slate-800">Nền tảng thiết bị</h3>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={stats?.platformBreakdown || []}
                  dataKey="count"
                  nameKey="platform"
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={80}
                  paddingAngle={5}
                >
                  {(stats?.platformBreakdown || []).map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <div className="flex justify-center gap-4 text-sm font-medium text-slate-600">
            {stats?.platformBreakdown.map((p, i) => (
              <div key={p.platform} className="flex items-center gap-2">
                <span className="h-3 w-3 rounded-full" style={{ backgroundColor: COLORS[i % COLORS.length] }}></span>
                {p.platform}: {p.count}
              </div>
            ))}
          </div>
        </div>

        {/* Biểu đồ Top Thiết bị */}
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h3 className="mb-4 font-bold text-slate-800">Top 5 Thiết bị phổ biến</h3>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={stats?.topDevices || []} layout="vertical" margin={{ left: 40 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="#f1f5f9" />
                <XAxis type="number" hide />
                <YAxis dataKey="model" type="category" axisLine={false} tickLine={false} fontSize={11} />
                <Tooltip cursor={{ fill: '#f1f5f9' }} />
                <Bar dataKey="count" fill="#0ea5e9" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Giờ cao điểm */}
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h3 className="mb-4 font-bold text-slate-800">Giờ cao điểm (Mở App)</h3>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={peakQ.data || []} margin={{ left: -20, bottom: -10 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                <XAxis dataKey="hour" axisLine={false} tickLine={false} fontSize={11} tickFormatter={(v) => `${v}h`} />
                <YAxis axisLine={false} tickLine={false} fontSize={11} />
                <Tooltip />
                <Line type="monotone" dataKey="count" stroke="#ea580c" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* Bảng danh sách phiên */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div className="border-b border-slate-100 bg-slate-50 p-4">
          <h3 className="font-bold text-slate-800">Nhật ký phiên hoạt động (7 ngày qua)</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-white">
              <tr className="border-b border-slate-100 text-slate-500">
                <th className="p-4 font-semibold">Trạng thái</th>
                <th className="p-4 font-semibold">Thiết bị</th>
                <th className="p-4 font-semibold">Bắt đầu</th>
                <th className="p-4 font-semibold">Thời lượng</th>
                <th className="p-4 font-semibold text-right">Quãng đường</th>
                <th className="p-4 font-semibold text-right">POI đã ghé</th>
              </tr>
            </thead>
            <tbody>
              {sessionsQ.data?.items.map((s) => (
                <tr key={s.id} className="border-b border-slate-50 hover:bg-slate-50">
                  <td className="p-4">
                    {s.isActive ? (
                      <span className="inline-flex items-center gap-1.5 rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-bold text-green-800">
                        <span className="h-2 w-2 animate-pulse rounded-full bg-green-500"></span> Đang mở
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-bold text-slate-600">
                        Đã đóng
                      </span>
                    )}
                    {s.isReturning && (
                      <span className="ml-2 inline-flex items-center rounded-full bg-purple-100 px-2 py-0.5 text-[10px] font-bold uppercase text-purple-700">
                        Quay lại
                      </span>
                    )}
                  </td>
                  <td className="p-4">
                    <p className="font-semibold text-slate-800">{s.deviceModel || 'Unknown'}</p>
                    <p className="text-xs text-slate-500">
                      {s.devicePlatform} {s.osVersion} • App v{s.appVersion}
                    </p>
                  </td>
                  <td className="p-4 whitespace-nowrap text-slate-600">
                    {new Date(s.startedAt).toLocaleString()}
                  </td>
                  <td className="p-4 font-mono text-slate-700">{s.durationMinutes} ph</td>
                  <td className="p-4 text-right font-mono text-slate-700">{s.distanceMeters}m</td>
                  <td className="p-4 text-right font-mono font-bold text-slate-900">{s.poisVisited}</td>
                </tr>
              ))}
              {!sessionsQ.data?.items?.length && (
                <tr>
                  <td colSpan={6} className="p-8 text-center text-slate-500">Chưa có dữ liệu phiên nào.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
