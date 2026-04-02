import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router'
import { api, type Tour } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'

export function ToursAdmin() {
  const role = useAuthStore((s) => s.role)
  const qc = useQueryClient()
  const q = useQuery({
    queryKey: ['tours', 'vi'],
    queryFn: async () => (await api.get<Tour[]>('/api/tour?lang=vi')).data,
  })

  const del = useMutation({
    mutationFn: async (id: number) => api.delete(`/api/tour/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tours'] }),
  })

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="text-lg font-semibold">Tour</h2>
        {role === 'Admin' && (
          <Link to="/tours/new" className="rounded-lg bg-orange-600 px-3 py-1.5 text-sm text-white">
            + Tour mới
          </Link>
        )}
      </div>
      {q.isLoading && <p>Đang tải…</p>}
      <ul className="divide-y divide-stone-200 rounded-lg border border-stone-200 dark:divide-stone-700 dark:border-stone-700">
        {q.data?.map((t) => (
          <li key={t.id} className="flex flex-wrap items-center justify-between gap-2 px-3 py-3">
            <div>
              <div className="font-medium">{t.name}</div>
              <div className="text-sm text-stone-500">
                {t.estimatedMinutes} phút · {t.stops?.length ?? 0} điểm
              </div>
            </div>
            <div className="flex gap-2">
              {role === 'Admin' && (
                <>
                  <Link
                    to={`/tours/${t.id}`}
                    className="text-sm text-orange-700 underline dark:text-orange-400"
                  >
                    Sửa
                  </Link>
                  <button
                    type="button"
                    className="text-sm text-red-600"
                    onClick={() => del.mutate(t.id)}
                  >
                    Ẩn
                  </button>
                </>
              )}
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}
