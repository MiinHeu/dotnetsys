import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api, type Poi } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'

export function Pois() {
  const role = useAuthStore((s) => s.role)
  const q = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="text-lg font-semibold">Danh sách POI</h2>
        {role === 'Admin' && (
          <Link
            to="/pois/new"
            className="rounded-lg bg-orange-600 px-3 py-1.5 text-sm text-white"
          >
            + Thêm POI
          </Link>
        )}
      </div>
      {q.isLoading && <p>Đang tải…</p>}
      {q.error && <p className="text-red-600">Lỗi tải dữ liệu</p>}
      <ul className="divide-y divide-stone-200 rounded-lg border border-stone-200 dark:divide-stone-700 dark:border-stone-700">
        {q.data?.map((p) => (
          <li key={p.id} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
            <div>
              <span className="font-medium">{p.name}</span>
              <span className="ml-2 text-sm text-stone-500">
                #{p.id} · ưu tiên {p.priority}
              </span>
            </div>
            <Link
              to={`/pois/${p.id}`}
              className="text-sm text-orange-700 underline dark:text-orange-400"
            >
              Sửa
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}
