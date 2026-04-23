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

  const poiVisitorStats = useQuery({
    queryKey: ['analytics', 'poi-heatmap-stats'],
    queryFn: async () => (await api.get('/api/analytics/poi-heatmap-stats?hours=24')).data as Array<{
      id: number
      name: string
      visitorCount: number
    }>,
  })

  const barData =
    top.data?.map((x) => ({
      name: `POI ${x.poiId}`,
      luot: x.count,
      tbGiay: Math.round(x.avgDuration ?? 0),
    })) ?? []

  const heatmapStatsData =
    poiVisitorStats.data?.map((x) => ({
      name: x.name,
      visitorCount: x.visitorCount,
    })) ?? []

  return (
    <div className="space-y-8 p-4">
      <h2 className="text-2xl font-bold text-slate-900">Analytics & Insights</h2>
      
      <div className="flex flex-col gap-8">
        {/* 1. Biểu đồ lượt nghe TTS */}
        <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h3 className="mb-6 flex items-center gap-2 text-lg font-bold text-slate-800">
             Lượt nghe TTS (30 ngày)
          </h3>
          <div className="h-96 w-full">
            {top.isLoading ? (
              <p className="flex h-full items-center justify-center text-slate-500">Đang tải...</p>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={barData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis axisLine={false} tickLine={false} />
                  <Tooltip 
                    contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                  />
                  <Bar dataKey="luot" fill="#ea580c" name="Lượt nghe" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </section>

        {/* 2. Biểu đồ lượt khách thực tế qua Heatmap */}
        <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h3 className="mb-6 flex items-center gap-2 text-lg font-bold text-slate-800">
             Khách ghé thăm thực tế (Heatmap - 24h)
          </h3>
          <div className="h-96 w-full">
            {poiVisitorStats.isLoading ? (
              <p className="flex h-full items-center justify-center text-slate-500">Đang tải...</p>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={heatmapStatsData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis axisLine={false} tickLine={false} />
                  <Tooltip 
                    contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                  />
                  <Bar dataKey="visitorCount" fill="#0ea5e9" name="Khách vãng lai" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
          <p className="mt-4 text-xs text-slate-500 italic">
            * Dữ liệu được tính bằng số lượng người dùng duy nhất xuất hiện trong bán kính kích hoạt của quán ăn.
          </p>
        </section>
      </div>
    </div>
  )
}
