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
    <div className="space-y-4">
      <h2 className="text-lg font-semibold">Lịch sử sự kiện app</h2>
      {q.isLoading && <p>Đang tải…</p>}
      <p className="text-sm text-stone-500">Tổng: {q.data?.total ?? '—'}</p>
      <div className="overflow-x-auto rounded-lg border border-stone-200 dark:border-stone-700">
        <table className="w-full text-left text-sm">
          <thead className="bg-stone-100 dark:bg-stone-800">
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
              <tr key={r.id} className="border-t border-stone-200 dark:border-stone-700">
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
