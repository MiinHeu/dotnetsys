import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'

type Row = {
  id: number
  sessionId: string
  eventType: string
  poiId?: number | null
  languageCode: string
  createdAt: string
  payload?: string | null
}

export function HistoryPage() {
  const q = useQuery({
    queryKey: ['history'],
    queryFn: async () => {
      const { data } = await api.get<{
        total: number
        page: number
        size: number
        items: Row[]
      }>('/api/history?page=1&size=100')
      return data
    },
  })

  return (
    <div className="vk-page">
      <section className="vk-page-header">
        <h2 className="vk-page-title">Lịch sử sự kiện ứng dụng</h2>
        <p className="vk-page-subtitle">Theo dõi hành vi sử dụng để giám sát chất lượng vận hành.</p>
      </section>

      {q.isLoading && <p className="text-sm text-slate-500">Đang tải nhật ký…</p>}
      <p className="text-sm text-slate-600">Tổng sự kiện: <strong>{q.data?.total ?? '—'}</strong></p>
      <div className="vk-card overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-50">
            <tr>
              <th className="p-2">Thời gian</th>
              <th className="p-2">Loại</th>
              <th className="p-2">Session</th>
              <th className="p-2">POI</th>
              <th className="p-2">Ngôn ngữ</th>
            </tr>
          </thead>
          <tbody>
            {q.data?.items.map((r) => (
              <tr key={r.id} className="border-t border-slate-100">
                <td className="p-2 whitespace-nowrap">{new Date(r.createdAt).toLocaleString()}</td>
                <td className="p-2">{r.eventType}</td>
                <td className="p-2 font-mono text-xs">{r.sessionId.slice(0, 12)}…</td>
                <td className="p-2">{r.poiId ?? '—'}</td>
                <td className="p-2">{r.languageCode}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
