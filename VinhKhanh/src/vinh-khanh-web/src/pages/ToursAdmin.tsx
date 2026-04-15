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
    <div className="vk-page">
      <div className="vk-page-header flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="vk-page-title">Lộ trình trải nghiệm</h2>
          <p className="vk-page-subtitle">Thiết kế hành trình tham quan ẩm thực cho khách theo từng chủ đề.</p>
        </div>
        {role === 'Admin' && (
          <Link to="/tours/new" className="vk-btn-primary text-sm">
            + Tour mới
          </Link>
        )}
      </div>
      {q.isLoading && <p className="text-sm text-slate-500">Đang tải lộ trình…</p>}
      <ul className="vk-card divide-y divide-slate-100">
        {q.data?.map((t) => (
          <li key={t.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-4">
            <div>
              <div className="font-semibold text-slate-900">{t.name}</div>
              <div className="text-sm text-slate-500">
                {t.estimatedMinutes} phút · {t.stops?.length ?? 0} điểm
              </div>
            </div>
            <div className="flex gap-2">
              {role === 'Admin' && (
                <>
                  <Link
                    to={`/tours/${t.id}`}
                    className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-semibold text-slate-700 hover:border-slate-300"
                  >
                    Sửa
                  </Link>
                  <button
                    type="button"
                    className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-1.5 text-sm font-semibold text-rose-700"
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
