import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'

export function Dashboard() {
  const pois = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get('/api/poi?lang=vi')).data as unknown[],
  })
  const tours = useQuery({
    queryKey: ['tours', 'vi'],
    queryFn: async () => (await api.get('/api/tour?lang=vi')).data as unknown[],
  })

  return (
    <div className="space-y-4">
      <h2 className="text-lg font-semibold">Tổng quan</h2>
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="rounded-lg border border-stone-200 p-4 dark:border-stone-700">
          <div className="text-3xl font-bold text-orange-600">
            {pois.isLoading ? '…' : pois.data?.length ?? 0}
          </div>
          <div className="text-sm text-stone-600">POI đang hoạt động</div>
        </div>
        <div className="rounded-lg border border-stone-200 p-4 dark:border-stone-700">
          <div className="text-3xl font-bold text-orange-600">
            {tours.isLoading ? '…' : tours.data?.length ?? 0}
          </div>
          <div className="text-sm text-stone-600">Tour đang mở</div>
        </div>
      </div>
    </div>
  )
}
