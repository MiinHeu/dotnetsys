import { useQuery } from '@tanstack/react-query'
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  ScatterChart,
  Scatter,
  ZAxis,
} from 'recharts'
import { api } from '@/lib/api'

export function AnalyticsPage() {
  const top = useQuery({
    queryKey: ['analytics', 'top'],
    queryFn: async () => (await api.get('/api/analytics/top?days=30')).data as Array<{
      poiId: number
      count: number
      avgDuration: number
    }>,
  })

  const heat = useQuery({
    queryKey: ['analytics', 'heatmap'],
    queryFn: async () =>
      (await api.get('/api/analytics/heatmap?hours=48')).data as Array<{
        latitude: number
        longitude: number
      }>,
  })

  const barData =
    top.data?.map((x) => ({
      name: `POI ${x.poiId}`,
      luot: x.count,
      tbGiay: Math.round(x.avgDuration ?? 0),
    })) ?? []

  const scatter =
    heat.data?.map((p, i) => ({
      x: p.longitude,
      y: p.latitude,
      z: 1,
      i,
    })) ?? []

  return (
    <div className="vk-page">
      <section className="vk-page-header">
        <h2 className="vk-page-title">Phân tích vận hành</h2>
        <p className="vk-page-subtitle">Theo dõi điểm đến được nghe nhiều và mật độ di chuyển gần đây.</p>
      </section>

      <section className="vk-card p-5 md:p-6">
        <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">Top POI (30 ngày)</h3>
        <div className="h-72 w-full">
          {top.isLoading ? (
            <p>Đang tải…</p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={barData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis />
                <Tooltip />
                <Bar dataKey="luot" fill="#ea580c" name="Lượt" />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </section>
      <section className="vk-card p-5 md:p-6">
        <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
          Heatmap điểm (movement log, 48h) — trục X=kinh độ, Y=vĩ độ
        </h3>
        <div className="h-80 w-full">
          {heat.isLoading ? (
            <p>Đang tải…</p>
          ) : scatter.length === 0 ? (
            <p className="text-sm text-stone-500">Chưa có dữ liệu path.</p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <ScatterChart margin={{ top: 8, right: 8, bottom: 8, left: 8 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis
                  type="number"
                  dataKey="x"
                  name="Lon"
                  domain={['dataMin - 0.002', 'dataMax + 0.002']}
                />
                <YAxis
                  type="number"
                  dataKey="y"
                  name="Lat"
                  domain={['dataMin - 0.002', 'dataMax + 0.002']}
                />
                <ZAxis type="number" dataKey="z" range={[20, 20]} />
                <Tooltip cursor={{ strokeDasharray: '3 3' }} />
                <Scatter data={scatter} fill="#f97316" />
              </ScatterChart>
            </ResponsiveContainer>
          )}
        </div>
      </section>
    </div>
  )
}
